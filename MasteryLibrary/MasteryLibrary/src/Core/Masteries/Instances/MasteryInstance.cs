using MasteryLibrary.src.Core.Masteries.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Core.Masteries.Instances
{
    public class MasteryInstance
    {
        public ICoreAPI Api { get; private set; }
        public Mastery Mastery;
        public int Level;
        public Dictionary<String, SkillInstance> UnlockedSkills = new Dictionary<String, SkillInstance>();

        public MasteryInstance(ICoreAPI api, Mastery mastery, int level = 1)
        {
            Api = api;
            Mastery = mastery;
            Level = level;
        }

        public void LevelUp()
        {
            if (Level < Mastery.MaxLevel) Level++;
        }

        public bool ArePrerequisitesMet(List<SkillPrerequisite> requirements)
        {
            if (requirements == null || requirements.Count == 0) return true;

            foreach (var req in requirements)
            {
                if (!UnlockedSkills.TryGetValue(req.SkillCode, out SkillInstance instance) ||
                    instance.Level < req.MinimumLevel)
                {
                    return false;
                }
            }
            return true;
        }

        public bool UnlockSkill(Skill skill)
        {
            bool levelMet = Level >= skill.RequiredMasteryLevel;

            if (levelMet && !UnlockedSkills.ContainsKey(skill.Code))
            {
                var instance = new SkillInstance(Api, skill, 1);
                UnlockedSkills[skill.Code] = instance;
                return true;
            }
            return false;
        }

        public bool LevelUpSkill(String skillCode)
        {
            if (UnlockedSkills.ContainsKey(skillCode))
            {
                UnlockedSkills[skillCode].LevelUp();
                return true;
            }
            return false;
        }
    }
}
