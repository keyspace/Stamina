﻿using Sandbox.Game;
//using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Keyspace.Stamina
{
    public class PlayerStats
    {
        private static MyStringHash fatigueDamage = MyStringHash.GetOrCompute("Fatigue");
        private static float gravityConstant = 9.81f * MyPerGameSettings.CharacterGravityMultiplier;

        private MyCharacterMovementEnum prevMovementState = MyCharacterMovementEnum.Standing;

        public float Stamina { get; set; }

        public PlayerStats(float stamina)
        {
            Stamina = stamina;
        }

        public PlayerStats()
        {
            Stamina = 1.0f;
        }

        public void Recalculate(IMyPlayer player)
        {
            MyCharacterMovementEnum currMovementState = player.Character.CurrentMovementState;

            float staminaDelta;
            if (prevMovementState != MyCharacterMovementEnum.Jump)
            {
                staminaDelta = MovementCosts.Map[currMovementState];
            }
            else
            {
                // Character falls soon after jumping; dupe cost on first recalc after that
                // so the stamina change doesn't look too inconsistent from jump to jump.
                staminaDelta = MovementCosts.Map[MyCharacterMovementEnum.Jump];
            }

            // DEBUG
            //var msg = $"{currMovementState} {prevMovementState} {player.Character.Integrity}";
            //MyLog.Default.WriteLineAndConsole(msg);
            //MyAPIGateway.Utilities.ShowNotification(msg, 1000);

            float gravityInfluence;
            if (staminaDelta < 0.0f)
            {
                // MAGICNUM 0.1f: arbitrary non-negative to limit bonus in low-gravity (TODO: configurable!).
                gravityInfluence = Math.Max(0.1f, player.Character.Physics.Gravity.Length() / gravityConstant);
            }
            else
            {
                // MAGICNUM 1.0f: for simplicity, stamina recovery doesn't get affected by gravity.
                gravityInfluence = 1.0f;
            }

            Stamina += staminaDelta * gravityInfluence;

            // Apply negative stamina as damage, with some scaling.
            if (Stamina < 0.0f)
            {
                // MAGICNUM -10.0f: chosen arbitrarily (TODO: configurable!).
                player.Character.DoDamage(Stamina * -10.0f, fatigueDamage, true);
            }

            // Clamp stamina between -100% (unattainable enough) and current health.
            Stamina = Math.Max(-1.0f, Math.Min(Stamina, player.Character.Integrity / 100.0f));

            // Update for next time.
            prevMovementState = currMovementState;
        }
    }

    static class MovementCosts
    {
        // TODO: configurable!
        private const float GAIN_HIGH =  0.0050f;
        private const float GAIN_MED  =  0.0025f;
        private const float GAIN_LOW  =  0.0005f;
        private const float COST_NONE =  0.0000f;
        private const float COST_LOW  = -0.0005f;
        private const float COST_MED  = -0.0025f;
        private const float COST_HIGH = -0.0050f;

        // helpers
        private const float WALK      = GAIN_LOW;
        private const float CROUCH_WK = COST_LOW;
        private const float RUN       = COST_LOW;

        internal static readonly Dictionary<MyCharacterMovementEnum, float> Map
            = new Dictionary<MyCharacterMovementEnum, float>
            {
                // Enum values generated by MSVS2019 drom:
                // Assembly VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
                // C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64\VRage.Game.dll
                { MyCharacterMovementEnum.Standing,                GAIN_MED  },
                { MyCharacterMovementEnum.Sitting,                 GAIN_HIGH },
                { MyCharacterMovementEnum.Crouching,               GAIN_HIGH },
                { MyCharacterMovementEnum.Flying,                  COST_NONE },
                { MyCharacterMovementEnum.Falling,                 GAIN_MED  },
                { MyCharacterMovementEnum.Jump,                    COST_HIGH },
                { MyCharacterMovementEnum.Died,                    COST_NONE },
                { MyCharacterMovementEnum.Ladder,                  GAIN_LOW  },
                { MyCharacterMovementEnum.Walking,                 WALK      },
                { MyCharacterMovementEnum.CrouchWalking,           CROUCH_WK },
                { MyCharacterMovementEnum.BackWalking,             WALK      },
                { MyCharacterMovementEnum.CrouchBackWalking,       CROUCH_WK },
                { MyCharacterMovementEnum.WalkStrafingLeft,        WALK      },
                { MyCharacterMovementEnum.CrouchStrafingLeft,      CROUCH_WK },
                { MyCharacterMovementEnum.WalkingLeftFront,        WALK      },
                { MyCharacterMovementEnum.CrouchWalkingLeftFront,  CROUCH_WK },
                { MyCharacterMovementEnum.WalkingLeftBack,         WALK      },
                { MyCharacterMovementEnum.CrouchWalkingLeftBack,   CROUCH_WK },
                { MyCharacterMovementEnum.WalkStrafingRight,       WALK      },
                { MyCharacterMovementEnum.CrouchStrafingRight,     CROUCH_WK },
                { MyCharacterMovementEnum.WalkingRightFront,       WALK      },
                { MyCharacterMovementEnum.CrouchWalkingRightFront, CROUCH_WK },
                { MyCharacterMovementEnum.WalkingRightBack,        WALK      },
                { MyCharacterMovementEnum.CrouchWalkingRightBack,  CROUCH_WK },
                { MyCharacterMovementEnum.LadderUp,                COST_LOW  },
                { MyCharacterMovementEnum.LadderDown,              COST_LOW  },
                { MyCharacterMovementEnum.Running,                 RUN       },
                { MyCharacterMovementEnum.Backrunning,             RUN       },
                { MyCharacterMovementEnum.RunStrafingLeft,         RUN       },
                { MyCharacterMovementEnum.RunningLeftFront,        RUN       },
                { MyCharacterMovementEnum.RunningLeftBack,         RUN       },
                { MyCharacterMovementEnum.RunStrafingRight,        RUN       },
                { MyCharacterMovementEnum.RunningRightFront,       RUN       },
                { MyCharacterMovementEnum.RunningRightBack,        RUN       },
                { MyCharacterMovementEnum.Sprinting,               COST_MED  },
                { MyCharacterMovementEnum.RotatingLeft,            COST_NONE },
                { MyCharacterMovementEnum.CrouchRotatingLeft,      COST_LOW  },
                { MyCharacterMovementEnum.RotatingRight,           COST_NONE },
                { MyCharacterMovementEnum.CrouchRotatingRight,     COST_LOW  },
                { MyCharacterMovementEnum.LadderOut,               COST_NONE }
            };
    }
}
