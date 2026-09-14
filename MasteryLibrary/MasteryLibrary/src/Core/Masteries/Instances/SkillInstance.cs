using MasteryLibrary.src.Core.Masteries.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Core.Masteries.Instances
{
    public class SkillInstance
    {
        public ICoreAPI api { get; private set; }
        public Skill Skill { get; private set; }
        public int Level { get; private set; }
        public long LastUsedTimeUtcMs { get; private set; } = 0;

        public SkillInstance(ICoreAPI api, Skill skill, int level = 1)
        {
            this.api = api;
            Skill = skill;
            Level = level;
        }

        public SkillInstance(ICoreAPI api, Skill skill, int level, long remainingCooldownMs)
        {
            this.api = api;
            Skill = skill;
            Level = level;

            if (remainingCooldownMs > 0)
            {
                long cooldownMs = (long)(skill.Cooldown * 1000L);
                long elapsed = cooldownMs - remainingCooldownMs;
                LastUsedTimeUtcMs = NowUtcMs() - elapsed;
            }
        }

        private static long NowUtcMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public bool LevelUp()
        {
            if (Level < Skill.MaxLevel)
            {
                Level++;
                return true;
            }
            return false;
        }

        public bool IsOnCooldown()
        {
            if (Skill.Cooldown <= 0 || LastUsedTimeUtcMs == 0) return false;
            return (NowUtcMs() - LastUsedTimeUtcMs) < Skill.Cooldown * 1000;
        }

        public void UpdateCooldown()
        {
            LastUsedTimeUtcMs = NowUtcMs();
        }

        public long GetRemainingCooldownMs()
        {
            if (Skill.Cooldown <= 0 || LastUsedTimeUtcMs == 0) return 0;
            long elapsed = NowUtcMs() - LastUsedTimeUtcMs;
            long remaining = (long)(Skill.Cooldown * 1000L) - elapsed;
            return remaining > 0 ? remaining : 0;
        }

        public void SetCooldownFromRemaining(long remainingMs)
        {
            if (remainingMs <= 0)
            {
                LastUsedTimeUtcMs = 0;
                return;
            }

            long cooldownMs = (long)(Skill.Cooldown * 1000L);
            long elapsed = cooldownMs - remainingMs;
            LastUsedTimeUtcMs = NowUtcMs() - elapsed;
        }
    }
}