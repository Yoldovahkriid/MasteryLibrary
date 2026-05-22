using MasteryLibrary.src.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Core.Effects.DefaultEffects
{
    public class StatBoostEffect : Effect
    {
        public override string Code => "StatBoost";

        public override string IconPath => String.Empty;
        public override StackingBehavior StackingBehavior => StackingBehavior.RefreschDuration;

        public override void OnApply(EffectInstance effectInstance, EntityAgent entityAgent)
        {
            ApplyStats(effectInstance, entityAgent);
        }

        public override void OnRemove(EffectInstance effectInstance, EntityAgent entityAgent, RemovalReason reason)
        {
            RemoveStats(effectInstance, entityAgent);
        }

        private void ApplyStats(EffectInstance instance, EntityAgent entityAgent) 
        {
            Dictionary<string, StatConfiguration>? stats = instance.GetValueSafe<Dictionary<string, StatConfiguration>>("ActiveStats");
            foreach (var stat in stats)
            {
                entityAgent.Stats.Set(stat.Key, $"masterylib-actives-{Code}", StatScalingUtil.GetScaledValue(stat.Value, instance.Magnitude));
            }
        }

        private void RemoveStats(EffectInstance instance, EntityAgent entityAgent)
        {
            List<string> existingStatKeys = new List<string>();
            foreach (var statEntry in entityAgent.Stats)
            {
                existingStatKeys.Add(statEntry.Key);
            }

            foreach (var key in existingStatKeys)
            {
                entityAgent.Stats.Remove(key, $"masterylib-actives-{Code}");
            }
        }
    }
}
