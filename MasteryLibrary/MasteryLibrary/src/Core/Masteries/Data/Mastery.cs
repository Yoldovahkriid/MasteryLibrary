using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;

namespace MasteryLibrary.src.Core.Masteries.Data
{
    public abstract class Mastery
    {
        public abstract String Code { get; }
        public virtual String IconPath { get; } = String.Empty;
        public virtual int MaxLevel { get; } = 10;
        public virtual Dictionary<String, Object> Attributes { get; } = new Dictionary<String, Object>();
        public virtual Dictionary<String, Skill> Skills { get; } = new Dictionary<String, Skill>();

        public virtual bool HasSkill(String skillCode)
        {
            return Skills.ContainsKey(skillCode);
        }
        public virtual String GetDisplayName(int level)
        {
            return Lang.Get($"masterylibrary:mastery-{Code}-name");
        }

        public virtual String GetDescription(int level)
        {
            return Lang.Get($"masterylibrary:mastery-{Code}-desc");
        }
        public T? GetValueSafe<T>(string key)
        {
            if (Attributes.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }
    }
}
