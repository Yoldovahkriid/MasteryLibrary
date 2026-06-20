using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;

namespace MasteryLibrary.src.Core.Masteries.Data
{
    public enum EnumSkillType
    {
        Active,
        Passive
    }

    public class SkillPrerequisite
    {
        public required string SkillCode { get; set; }
        public int MinimumLevel { get; set; } = 1;
    }

    public abstract class Skill
    {
        public abstract string Code { get; }
        public virtual string IconPath { get; } = string.Empty;
        public virtual int MaxLevel { get; } = 1;
        public virtual int RequiredMasteryLevel { get; } = 1;
        public virtual Dictionary<int, List<SkillPrerequisite>> LevelRequirements { get; set; } = new Dictionary<int, List<SkillPrerequisite>>();
        public virtual string? ExclusiveGroup { get; } = null;
        public abstract int Column { get; }
        public abstract EnumSkillType SkillType { get; }
        public virtual string Ability { get; } = string.Empty;
        public virtual float Cooldown { get; } = 0f;
        public virtual float Duration { get; } = 0f;
        public virtual bool IsUnique { get; } = false;
        public virtual Dictionary<string, Object> Attributes { get; } = new Dictionary<string, Object>();

        public virtual string GetDisplayName(int level)
        {
            return Lang.Get($"masterylibrary:skill-{Code}-name");
        }

        public virtual string GetDescription(int level)
        {
            return Lang.Get($"masterylibrary:skill-{Code}-desc");
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
