using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Core.Effects
{
    public class EffectInstance
    {
        public string Source { get; set; }
        public Effect Effect { get; set; }
        public long AppliedTime { get; set; }
        public long Duration { get; set; }
        public int Magnitude { get; set; }
        public Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();
        public EffectInstance(Effect effect, string source, long duration, int magnitude)
        {
            Effect = effect;
            Source = source;
            Duration = duration;
            Magnitude = magnitude;
        }
        public T? GetValueSafe<T>(string key)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }
    }

    public record EffectState
    {
        public string Source { get; set; }
        public string Effect { get; set; }
        public long AppliedTime { get; set; }
        public long Duration { get; set; }
        public int Magnitude{ get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }
}
