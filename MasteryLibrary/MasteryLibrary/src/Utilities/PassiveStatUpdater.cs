using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Masteries.Instances;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using static HarmonyLib.Code;

namespace MasteryLibrary.src.Utilities
{
    public static class PassiveStatUpdater
    {
        public static void UpdatePassivePlayerStats(EntityBehaviorPlayerMasteries entityBehavior) 
        {
            EntityPlayer? entityPlayer = entityBehavior.entity as EntityPlayer;
            if (entityPlayer == null || !entityPlayer.Alive) return;

            List<string> ExistingStatKeys = new List<string>();
            foreach(var StatEntry in entityPlayer.Stats)
            {
                ExistingStatKeys.Add(StatEntry.Key);
            }

            foreach (var StatKey in ExistingStatKeys)
            {
                entityPlayer.Stats.Remove(StatKey, "MasteryPassives");
            }

            PlayerMasteryData MasteryData = entityBehavior.PlayerMasteryData;
            Dictionary<string, float> AggregatedStats = new Dictionary<string, float>();

            foreach (var MInst in MasteryData.LearntMasteries.Values) 
            { 
                foreach(var SInst in MInst.UnlockedSkills.Values)
                {
                    Dictionary<string, StatConfiguration>? SkillStats = SInst.Skill.GetValueSafe<Dictionary<string, StatConfiguration>>("PassiveStats");
                    if (SkillStats == null) continue;
                    foreach (var stat in SkillStats) 
                    {
                        float LeveledStat = StatScalingUtil.GetScaledValue(stat.Value, SInst.Level);

                        AggregatedStats[stat.Key] = AggregatedStats.GetValueOrDefault(stat.Key, 0f) + LeveledStat;
                    }
                }

                Dictionary<string, StatConfiguration>? MasteryStats = MInst.Mastery.GetValueSafe<Dictionary<string, StatConfiguration>>("PassiveStats");
                if (MasteryStats == null) continue;
                foreach (var stat in MasteryStats)
                {
                    float LeveledStat = StatScalingUtil.GetScaledValue(stat.Value, MInst.Level);

                    AggregatedStats[stat.Key] = AggregatedStats.GetValueOrDefault(stat.Key, 0f) + LeveledStat;
                }
            }

            foreach(var AggregatedStat in AggregatedStats)
            {
                entityPlayer.Stats.Set(AggregatedStat.Key, "MasteryPassives", AggregatedStat.Value, persistent: false);
            }

            entityPlayer.GetBehavior<EntityBehaviorHealth>()?.MarkDirty();
            entityPlayer.WatchedAttributes.MarkAllDirty();
        }
    }
}
