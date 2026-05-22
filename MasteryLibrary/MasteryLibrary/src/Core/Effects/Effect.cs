using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace MasteryLibrary.src.Core.Effects
{
    public enum StackingBehavior
    { 
        RefreschDuration,
        ExtendDuration,
        ReplaceExisting,
        IncreaseMagnitude
    }

    public enum RemovalReason
    {
        Expired,
        Dispelled,
        Death,
        Replaced
    }

    public enum EffectType
    {
        Buff,
        Debuff,
        DamageOverTime,
        HealOverTime,
        Shield,
        Utility,
        Undefined
    }

    public abstract class Effect
    {
        public abstract string Code { get; }
        public abstract string IconPath { get; }
        public virtual bool CanStack => false;
        public virtual StackingBehavior StackingBehavior => StackingBehavior.RefreschDuration;
        public virtual EffectType Type => EffectType.Undefined;

        public virtual void OnApply(EffectInstance effectInstance, EntityAgent entityAgent) { }
        public virtual void OnTick(EffectInstance effectInstance, EntityAgent entityAgent, float deltaTime) { }
        public virtual void OnRemove(EffectInstance effectInstance, EntityAgent entityAgent, RemovalReason reason) { }
        public virtual EffectInstance OnStack(EffectInstance existing, EffectInstance incoming)
        {
            switch (StackingBehavior)
            {
                case StackingBehavior.ExtendDuration:
                    existing.Duration += incoming.Duration;
                    return existing;
                case StackingBehavior.IncreaseMagnitude:
                    existing.Magnitude += incoming.Magnitude;
                    return existing;
                case StackingBehavior.ReplaceExisting:
                    return incoming;
                default:
                    existing.Duration = Math.Max(existing.Duration, incoming.Duration);
                    return existing;
            }
        }
        public virtual string GetName(EffectInstance effectInstance)
        {
            return Lang.Get($"masterylib:effect-{Code}-name");
        }
        public virtual string GetDescription(EffectInstance effectInstance)
        {
            return Lang.Get($"masterylib:effect-{Code}-desc");
        }
    }
}
