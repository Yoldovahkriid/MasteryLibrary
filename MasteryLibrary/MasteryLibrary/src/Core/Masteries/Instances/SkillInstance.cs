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
        public long LastUsedTime { get; private set; } = 0;

        public SkillInstance(ICoreAPI api, Skill skill, int level = 1)
        {
            this.api = api;
            Skill = skill;
            Level = level;
        }

        public SkillInstance(ICoreAPI api, Skill skill, int level, long remainingCooldownMs, long currentTime)
        {
            this.api = api;
            Skill = skill;
            Level = level;
            long cooldownMs = (long)(skill.Cooldown * 1000L);
            long elapsed = cooldownMs - remainingCooldownMs;
            LastUsedTime = remainingCooldownMs > 0 ? currentTime - elapsed : 0;
        }

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
            if (Skill.Cooldown <= 0) return false;
            long currentTime = api.World.ElapsedMilliseconds;
            return (currentTime - LastUsedTime) < Skill.Cooldown * 1000;
        }

        public void UpdateCooldown()
        {
            LastUsedTime = api.World.ElapsedMilliseconds;
        }

        public long GetRemainingCooldownMs()
        {
            if (Skill.Cooldown <= 0) return 0;
            long elapsed = api.World.ElapsedMilliseconds - LastUsedTime;
            long remaining = (long)(Skill.Cooldown * 1000L) - elapsed;
            return remaining > 0 ? remaining : 0;
        }
        public void SetCooldownFromRemaining(long remainingMs, long currentTime)
        {
            long cooldownMs = (long)(Skill.Cooldown * 1000L);
            long elapsed = cooldownMs - remainingMs;
            LastUsedTime = currentTime - elapsed;
        }
    }
}
