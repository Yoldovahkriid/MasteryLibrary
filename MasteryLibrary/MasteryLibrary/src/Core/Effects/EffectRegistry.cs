using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Core.Effects
{
    public class EffectRegistry
    {
        private readonly Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

        public void RegisterEffect(Effect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (effects.ContainsKey(effect.Code)) throw new InvalidOperationException($"Effect with code '{effect.Code}' is already registered.");
            effects[effect.Code] = effect;
        }

        public Effect? GetEffect(string code)
        {
            effects.TryGetValue(code, out var effect);
            return effect;
        }

        public EffectInstance? CreateEffectInstance(string code, string source, long duration, int magnitude)
        {
            var effect = GetEffect(code);
            if (effect == null) return null;
            return new EffectInstance(effect, source, duration, magnitude);
        }
    }
}