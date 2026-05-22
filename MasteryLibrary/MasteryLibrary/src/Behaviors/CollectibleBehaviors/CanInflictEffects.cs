using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Effects;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace MasteryLibrary.src.Behaviors.CollectibleBehaviors
{
    public class CanInflictEffects : CollectibleBehavior
    {
        private List<EffectConfiguration> AppliedEffects = new List<EffectConfiguration>();
        public CanInflictEffects(CollectibleObject collObj) : base(collObj) { }

        public override float GetDamageToEntity(float baseDamage, Entity entity, ItemStack itemStack, ref bool isCriticalHit, ref EnumHandling handling)
        {
            EntityAgent? agent = entity as EntityAgent;
            if (agent != null)
            {
                EntityBehaviorEffects? entityBehavior = agent.GetBehavior<EntityBehaviorEffects>();
                if (entityBehavior != null)
                {
                    foreach(EffectConfiguration effect in AppliedEffects)
                    {
                        ApplyEffect(entity.Api, entityBehavior.EffectManager, effect);
                    }
                    AppliedEffects.Clear();
                }
            }
            handling = EnumHandling.PassThrough;
            return base.GetDamageToEntity(baseDamage, entity, itemStack, ref isCriticalHit, ref handling);
        }

        public void AddEffect(EffectConfiguration effectConfig) => AppliedEffects.Add(effectConfig);

        private void ApplyEffect(ICoreAPI api, EffectManager effectManager, EffectConfiguration configuration)
        {
            EffectInstance? instance = api.ModLoader.GetModSystem<MasteryLibraryAPI>()?.EffectRegistry.CreateEffectInstance(configuration.EffectCode, $"item_{collObj.Code}", configuration.DurationMs, configuration.Magnitude);
            if (instance == null) return;
            foreach(var data in configuration.CustomData)
            {
                instance.CustomData.Add(data.Key, data.Value);
            }
            effectManager.AddEffect(instance);
        }
    }

    public class EffectConfiguration
    {
        public string EffectCode { get; set; }
        public long DurationMs { get; set; }
        public int Magnitude { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
    }
}
