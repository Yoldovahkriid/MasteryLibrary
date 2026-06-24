using HarmonyLib;
using MasteryLibrary.src.Config;
using MasteryLibrary.src.Core.Masteries.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Core.Masteries.Instances
{
    public class PlayerMasteryData
    {
        public ICoreAPI Api { get; private set; }
        public int CharacterLevel { get; private set; } = 1;
        public int MasteryPoints { get; private set; } = 0;
        public int BonusMasteryPoints { get; private set; } = 0;
        public double Experience { get; private set; } = 0f;
        private Dictionary<string, MasteryInstance> learntMasteries = new Dictionary<string, MasteryInstance>();
        public IReadOnlyDictionary<string, MasteryInstance> LearntMasteries => learntMasteries;
        public Action? OnDataChanged;
        public string[] EquippedActiveSkills { get; } = new string[MasteryLibConfigCommon.Loaded.MaxEquippedActiveSkills];
        private MasteryLibraryAPI MasteryLibAPI;

        public PlayerMasteryData(ICoreAPI api)
        {
            Api = api;
            MasteryLibAPI = api.ModLoader.GetModSystem<MasteryLibraryAPI>();

            for (int i = 0; i < EquippedActiveSkills.Length; i++)
            {
                EquippedActiveSkills[i] = string.Empty;
            }
        }

        public void GainExperience(double amount)
        {
            if (amount <= 0) return;
            Experience += amount * (MasteryLibConfigCommon.Loaded.GlobalExperienceMultiplier > 0 ? MasteryLibConfigCommon.Loaded.GlobalExperienceMultiplier : 1f);
            while (Experience >= GetExperienceForNextLevel() && CharacterLevel < MasteryLibConfigCommon.Loaded.MaxPlayerLevel)
            {
                Experience -= GetExperienceForNextLevel();
                CharacterLevel++;
                MasteryPoints += MasteryLibConfigCommon.Loaded.MasteryPointsPerLevel;
                OnDataChanged?.Invoke();
            }
        }

        public double GetExperienceForNextLevel()
        {
            double[] coefficients = MasteryLibConfigCommon.Loaded.ExperienceFunctionParameters ?? new double[] { 100f };
            if (coefficients.Length == 0) coefficients = new double[] { 100f };

            // Evaluate: coefficients[0]*L^(n-1) + coefficients[1]*L^(n-2) + ... + coefficients[n-1]
            int degree = coefficients.Length - 1;
            double result = 0f;
            for (int i = 0; i < coefficients.Length; i++)
            {
                result += coefficients[i] * Math.Pow(CharacterLevel, degree - i);
            }
            return result;
        }

        public void AddBonusMasteryPoints(int amount)
        {
            if (amount <= 0) return;
            BonusMasteryPoints += amount;
            MasteryPoints += amount;
            OnDataChanged?.Invoke();
        }

        public void RespecMasteries()
        {
            MasteryPoints = BonusMasteryPoints + (CharacterLevel - 1) * MasteryLibConfigCommon.Loaded.MasteryPointsPerLevel;
            learntMasteries.Clear();

            for (int i = 0; i < EquippedActiveSkills.Length; i++)
            {
                EquippedActiveSkills[i] = string.Empty;
            }

            OnDataChanged?.Invoke();
        }

        public void EquipActiveSkill(string skillcode, int slot)
        {
            if (slot < 0 || slot >= EquippedActiveSkills.Length) return;
            EquippedActiveSkills[slot] = skillcode;
            OnDataChanged?.Invoke();
        }

        private bool TrySpendPoints(int amount)
        {
            if (MasteryPoints >= amount)
            {
                MasteryPoints -= amount;
                OnDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool LearnOrUpgradeMastery(string masterycode)
        {
            if (!MasteryLibAPI.MasteryDefinitions.Masteries.TryGetValue(masterycode, out Mastery mastery)) return false;

            if (learntMasteries.TryGetValue(masterycode, out MasteryInstance instance))
            {
                int cost = 1 + (instance.Level / MasteryLibConfigCommon.Loaded.MasteryPointsCostIncreaseIntervall) * MasteryLibConfigCommon.Loaded.MasteryPointsCostIncreasePerTier;

                if (instance.Level < mastery.MaxLevel && TrySpendPoints(cost))
                {
                    instance.LevelUp();
                    return true;
                }
            }
            else if (learntMasteries.Count < MasteryLibConfigCommon.Loaded.MaxLearnableMasteries && TrySpendPoints(1))
            {
                learntMasteries[masterycode] = new MasteryInstance(Api, mastery);
                return true;
            }
            return false;
        }

        public bool LearnOrUpgradeSkill(string masterycode, string skillcode)
        {
            if (MasteryPoints <= 0 || !learntMasteries.TryGetValue(masterycode, out MasteryInstance masteryInstance))
                return false;

            if (!masteryInstance.Mastery.Skills.TryGetValue(skillcode, out Skill skill))
                return false;

            if (!masteryInstance.UnlockedSkills.TryGetValue(skillcode, out SkillInstance skillInstance))
            {
                if (masteryInstance.Level >= skill.RequiredMasteryLevel 
                    && masteryInstance.ArePrerequisitesMet(skill.LevelRequirements.GetValueOrDefault(1) ?? new List<SkillPrerequisite>())
                    && !ConflictsWithLearntSkills(masteryInstance, skill))
                {
                    if (masteryInstance.UnlockSkill(skill))
                    {
                        if (!skill.IsUnique) MasteryPoints--;
                        OnDataChanged?.Invoke();
                        return true;
                    }
                }
            }
            else if (!skill.IsUnique && skillInstance.Level < skill.MaxLevel)
            {
                int nextLevel = skillInstance.Level + 1;

                if (skill.LevelRequirements.TryGetValue(nextLevel, out var specificReqs))
                {
                    if (!masteryInstance.ArePrerequisitesMet(specificReqs)) return false;
                }

                if (skillInstance.LevelUp())
                {
                    MasteryPoints--;
                    OnDataChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        private bool ConflictsWithLearntSkills(MasteryInstance mastery, Skill skill)
        {
            foreach (var learntskill in mastery.UnlockedSkills.Values)
            {
                if (learntskill.Skill.ExclusiveGroup == skill.ExclusiveGroup)
                {
                    return true;
                }
            }
            return false;
        }

        public SkillInstance? GetSkillInstance(string skillCode)
        {
            if (string.IsNullOrEmpty(skillCode)) return null;
            foreach (var masteryInstance in learntMasteries.Values)
            {
                if (masteryInstance.UnlockedSkills.TryGetValue(skillCode, out var skillInstance))
                {
                    return skillInstance;
                }
            }
            return null;
        }

        public bool HasSkill(string skillCode) => GetSkillInstance(skillCode) != null;

        public PlayerMasteryState ToState()
        {
            return new PlayerMasteryState
            {
                CharacterLevel = CharacterLevel,
                MasteryPoints = MasteryPoints,
                BonusMasteryPoints = BonusMasteryPoints,
                Experience = Experience,
                EquippedActiveSkills = EquippedActiveSkills,
                LearntMasteries = learntMasteries.Select(kv => new MasteryState
                {
                    Code = kv.Key,
                    Level = kv.Value.Level,
                    UnlockedSkills = kv.Value.UnlockedSkills.Select(sk => new SkillState
                    {
                        Code = sk.Key,
                        Level = sk.Value.Level,
                        RemainingCooldownMs = sk.Value.GetRemainingCooldownMs() // see below
                    }).ToList()
                }).ToList()
            };
        }

        public void FromState(PlayerMasteryState state)
        {
            CharacterLevel = state.CharacterLevel;
            MasteryPoints = state.MasteryPoints;
            BonusMasteryPoints = state.BonusMasteryPoints;
            Experience = state.Experience;

            for (int i = 0; i < EquippedActiveSkills.Length; i++)
            {
                EquippedActiveSkills[i] = (state.EquippedActiveSkills != null && i < state.EquippedActiveSkills.Length)
                    ? (state.EquippedActiveSkills[i] ?? string.Empty)
                    : string.Empty;
            }

            learntMasteries.Clear();
            foreach (var ms in state.LearntMasteries ?? new())
            {
                if (!MasteryLibAPI.MasteryDefinitions.Masteries.TryGetValue(ms.Code, out var mastery)) continue;
                var instance = new MasteryInstance(Api, mastery, ms.Level);
                foreach (var ss in ms.UnlockedSkills ?? new())
                {
                    if (!mastery.Skills.TryGetValue(ss.Code, out var skill)) continue;
                    var skillInst = new SkillInstance(Api, skill, ss.Level,
                        remainingCooldownMs: ss.RemainingCooldownMs,
                        currentTime: Api.World.ElapsedMilliseconds);
                    instance.UnlockedSkills[ss.Code] = skillInst;
                }
                learntMasteries[ms.Code] = instance;
            }
        }
    }
}
