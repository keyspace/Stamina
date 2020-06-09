﻿using Sandbox.Game;
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
            // Character falls soon after jumping; skip first recalc after that so
            // the stamina change doesn't look too inconsistent from jump to jump.
            if (player.Character.PreviousMovementState != MyCharacterMovementEnum.Jump)
            {
                float staminaDelta = MovementCosts.Map[player.Character.CurrentMovementState];
                
                // MAGICNUM 1.0f: for simplicity, stamina recovery doesn't get affected by gravity.
                float gravityInfluence = 1.0f;
                if (staminaDelta < 0.0f)
                {
                    // MAGICNUM 0.1f: arbitrary non-negative to limit bonus in low-gravity (TODO: configurable!).
                    // MAGICNUM 19.62f: G constant of 9.81f times two, don't know why it's scaled that way.
                    gravityInfluence = Math.Max(0.1f, player.Character.Physics.Gravity.Length() / gravityConstant);
                }
                
                Stamina += staminaDelta * gravityInfluence;
            }

            // Apply negative stamina as damage, with some scaling.
            if (Stamina < 0.0f)
            {
                // MAGICNUM -10.0f: chosen arbitrarily (TODO: configurable!).
                player.Character.DoDamage(Stamina * -10.0f, fatigueDamage, true);
            }

            // Clamp stamina between -100% (unattainable enough) and current health.
            Stamina = Math.Max(-1.0f, Math.Min(Stamina, player.Character.Integrity / 100.0f));
        }
    }

    /// <summary>
    /// Helper class to work around dictionaries being non-serialisable to XML.
    /// </summary>
    public class PlayerStatsStore
    {
        public ulong[] PlayerIdArray { get; set; }
        public PlayerStats[] PlayerStatsArray { get; set; }

        public PlayerStatsStore()
        {
            PlayerIdArray = new ulong[0];
            PlayerStatsArray = new PlayerStats[0];
        }

        internal PlayerStatsStore(Dictionary<ulong, PlayerStats> playerStatsDict)
        {
            PlayerIdArray = new ulong[playerStatsDict.Count];
            PlayerStatsArray = new PlayerStats[playerStatsDict.Count];

            int i = 0;
            foreach (ulong steamId in playerStatsDict.Keys)
            {
                PlayerIdArray[i] = steamId;
                PlayerStatsArray[i] = playerStatsDict[steamId];
                i++;
            }
        }

        internal Dictionary<ulong, PlayerStats> ToDict()
        {
            Dictionary<ulong, PlayerStats> playerStatsDict = new Dictionary<ulong, PlayerStats>();

            for (int i = 0; i < PlayerIdArray.Length; i++)
            {
                playerStatsDict.Add(PlayerIdArray[i], PlayerStatsArray[i]);
            }

            return playerStatsDict;
        }
    }

    static class MovementCosts
    {
        // TODO: configurable!
        private const float GAIN_HIGH =  0.075f;
        private const float GAIN_MED  =  0.025f;
        private const float GAIN_LOW  =  0.005f;
        private const float COST_NONE =  0.000f;
        private const float COST_LOW  = -0.005f;
        private const float COST_MED  = -0.025f;
        private const float COST_HIGH = -0.075f;

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
