using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Core.Masteries.Data
{
    public class MasteryDefinitions
    {
        public ICoreAPI CoreAPI { get; private set; }
        public Dictionary<String, Mastery> Masteries { get; set; } = new Dictionary<String, Mastery>();
        
        public MasteryDefinitions(ICoreAPI coreAPI)
        {
            CoreAPI = coreAPI;
        }
        public void AddMastery(Mastery mastery)
        {
            if (!Masteries.ContainsKey(mastery.Code))
            {
                Masteries.Add(mastery.Code, mastery);
            }
            else
            {
                CoreAPI.Logger.Error($"Mastery with code {mastery.Code} already exists. Skipping addition.");
            }
        }
        public Mastery? GetMastery(string code)
        {
            return Masteries.TryGetValue(code, out var mastery) ? mastery : null;
        }

        public Skill? GetSkill(string code)
        {
            foreach (var mastery in Masteries.Values)
            {
                if (mastery.Skills.TryGetValue(code, out var skill))
                {
                    return skill;
                }
            }
            return null;
        }
    }
}
