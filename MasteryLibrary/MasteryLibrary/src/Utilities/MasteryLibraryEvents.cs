using MasteryLibrary.src.Core.Effects;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MasteryLibrary.src.Core.Events
{
    public delegate void MasteryEventHandler(IServerPlayer player, MasteryInstance mastery);
    public delegate void MasterySkillEventHandler(IServerPlayer player, MasteryInstance mastery, SkillInstance skill);
    public delegate void PlayerSkillEventHandler(IServerPlayer player, Skill skill);
    public delegate void SkillEquippedEventHandler(IServerPlayer player, string skillCode, int slot);
    public delegate void PlayerExperienceEventHandler(IServerPlayer player, double amountGained, double totalExperience);
    public delegate void PlayerLevelEventHandler(IServerPlayer player, int newLevel, int previousLevel);
    public delegate void PlayerMasteryPointsEventHandler(IServerPlayer player, int amountGained, int totalPoints);
    public delegate void PlayerEventHandler(IServerPlayer player);


    public delegate void EntityEffectEventHandler(EntityAgent entity, EffectInstance effect);

    public class MasteryLibraryEvents
    {
        // --- Mastery events ---
        public event MasteryEventHandler OnMasteryUnlocked;
        public event MasteryEventHandler OnMasteryLeveledUp;

        // --- Skill events ---
        public event MasterySkillEventHandler OnSkillUnlocked;
        public event MasterySkillEventHandler OnSkillLeveledUp;
        public event PlayerSkillEventHandler OnUniqueSkillAquired;
        public event SkillEquippedEventHandler OnSkillEquipped;

        // --- Progression events ---
        public event PlayerExperienceEventHandler OnExperienceGained;
        public event PlayerLevelEventHandler OnPlayerLeveledUp;
        public event PlayerMasteryPointsEventHandler OnMasteryPointsGained;
        public event PlayerEventHandler OnMasteriesRespecced;

        // --- Effect events ---
        public event EntityEffectEventHandler OnEffectApplied;
        public event EntityEffectEventHandler OnEffectRemoved;

        public void RaiseMasteryUnlocked(IServerPlayer player, MasteryInstance mastery) => OnMasteryUnlocked?.Invoke(player, mastery);
        public void RaiseMasteryLeveledUp(IServerPlayer player, MasteryInstance mastery) => OnMasteryLeveledUp?.Invoke(player, mastery);

        public void RaiseSkillUnlocked(IServerPlayer player, MasteryInstance mastery, SkillInstance skill) => OnSkillUnlocked?.Invoke(player, mastery, skill);
        public void RaiseSkillLeveledUp(IServerPlayer player, MasteryInstance mastery, SkillInstance skill) => OnSkillLeveledUp?.Invoke(player, mastery, skill);
        public void RaiseUniqueSkillAquired(IServerPlayer player, Skill skill) => OnUniqueSkillAquired?.Invoke(player, skill);
        public void RaiseSkillEquipped(IServerPlayer player, string skillCode, int slot) => OnSkillEquipped?.Invoke(player, skillCode, slot);

        public void RaiseExperienceGained(IServerPlayer player, double amountGained, double totalExperience) => OnExperienceGained?.Invoke(player, amountGained, totalExperience);
        public void RaisePlayerLeveledUp(IServerPlayer player, int newLevel, int previousLevel) => OnPlayerLeveledUp?.Invoke(player, newLevel, previousLevel);
        public void RaiseMasteryPointsGained(IServerPlayer player, int amountGained, int totalPoints) => OnMasteryPointsGained?.Invoke(player, amountGained, totalPoints);
        public void RaiseMasteriesRespecced(IServerPlayer player) => OnMasteriesRespecced?.Invoke(player);

        public void RaiseEffectApplied(EntityAgent entity, EffectInstance effect) => OnEffectApplied?.Invoke(entity, effect);
        public void RaiseEffectRemoved(EntityAgent entity, EffectInstance effect) => OnEffectRemoved?.Invoke(entity, effect);
    }
}