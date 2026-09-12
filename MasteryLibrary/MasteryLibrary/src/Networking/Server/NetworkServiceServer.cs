using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Abilities;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Networking.Packets;
using MasteryLibrary.src.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Server;

namespace MasteryLibrary.src.Networking.Server
{
    public class NetworkServiceServer : INetworkService
    {
        private readonly ICoreServerAPI api;
        private IServerNetworkChannel channel;

        public NetworkServiceServer(ICoreServerAPI api)
        {
            this.api = api;
            channel = api.Network.RegisterChannel("masterylibrary")
                .RegisterMessageType<MasteryStateRequestPacket>()
                .RegisterMessageType<MasteryUpgradeRequestPacket>()
                .RegisterMessageType<SkillUpgradeRequestPacket>()
                .RegisterMessageType<SkillActivationRequestPacket>()
                .RegisterMessageType<SkillEquipRequestPacket>()
                .RegisterMessageType<MasteryStateResponsePacket>()
                .RegisterMessageType<ActionFailedResponsePacket>()
                .RegisterMessageType<CooldownUpdatePacket>()
                .SetMessageHandler<MasteryStateRequestPacket>(HandleMasteryStateRequest)
                .SetMessageHandler<MasteryUpgradeRequestPacket>(HandleMasteryUpgradeRequest)
                .SetMessageHandler<SkillUpgradeRequestPacket>(HandleSkillUpgradeRequest)
                .SetMessageHandler<SkillActivationRequestPacket>(HandleSkillActivationRequest)
                .SetMessageHandler<SkillEquipRequestPacket>(HandleSkillEquipRequest);
        }

        private EntityBehaviorPlayerMasteries? GetPlayerMasteryBehavior(IServerPlayer player)
        {
            return player?.Entity.GetBehavior<EntityBehaviorPlayerMasteries>();
        }

        public void SendMasterySyncPacket(IServerPlayer player)
        {
            PlayerMasteryState? state = GetPlayerMasteryBehavior(player)?.PlayerMasteryData.ToState();
            if (state != null)
            {
                channel.SendPacket(new MasteryStateResponsePacket { MasteryState = state }, player);
            }
        }

        private void HandleMasteryStateRequest(IServerPlayer player, MasteryStateRequestPacket packet)
        {
            SendMasterySyncPacket(player);
        }

        private void HandleMasteryUpgradeRequest(IServerPlayer player, MasteryUpgradeRequestPacket packet)
        {
            var masteryBehavior = GetPlayerMasteryBehavior(player);
            if (masteryBehavior == null || String.IsNullOrEmpty(packet.MasteryCode)) return;
            if (!masteryBehavior.PlayerMasteryData.LearnOrUpgradeMastery(packet.MasteryCode))
            {
                channel.SendPacket(new ActionFailedResponsePacket { Reason = "upgrademasteryfailed" }, player);
                return;
            }
            PassiveStatUpdater.UpdatePassivePlayerStats(masteryBehavior);
            SendMasterySyncPacket(player);
        }

        private void HandleSkillUpgradeRequest(IServerPlayer player, SkillUpgradeRequestPacket packet)
        {
            var masteryBehavior = GetPlayerMasteryBehavior(player);
            if (masteryBehavior == null || String.IsNullOrEmpty(packet.MasteryCode) || String.IsNullOrEmpty(packet.SkillCode)) return;
            if (!masteryBehavior.PlayerMasteryData.LearnOrUpgradeSkill(packet.MasteryCode, packet.SkillCode))
            {
                channel.SendPacket(new ActionFailedResponsePacket { Reason = "upgradeskillfailed" }, player);
                return;
            }
            PassiveStatUpdater.UpdatePassivePlayerStats(masteryBehavior);
            SendMasterySyncPacket(player);
        }

        private void HandleSkillActivationRequest(IServerPlayer player, SkillActivationRequestPacket packet)
        {
            EntityBehaviorPlayerMasteries? behavior = GetPlayerMasteryBehavior(player);
            if (behavior == null) return;

            if (packet.SlotIndex < 0 || packet.SlotIndex >= behavior.PlayerMasteryData.EquippedActiveSkills.Length) return;
            string skillcode = behavior.PlayerMasteryData.EquippedActiveSkills[packet.SlotIndex];
            if (string.IsNullOrEmpty(skillcode)) return;

            SkillInstance? skill = behavior.PlayerMasteryData.GetSkillInstance(skillcode);
            if (skill == null || skill?.Skill.SkillType != EnumSkillType.Active || skill.IsOnCooldown()) return;

            bool AbilityUsed = false;
            if (!string.IsNullOrEmpty(skill.Skill.Ability))
            {
                MasteryLibraryAPI MasteryAPI = api.ModLoader.GetModSystem<MasteryLibraryAPI>();
                if (MasteryAPI.AbilityRegistry != null)
                {
                    AbilityContext context = new AbilityContext(
                        player,
                        skill.Level,
                        api,
                        $"skill:{skill.Skill.Code}",
                        skill.Skill.Attributes
                        );

                    AbilityResult result = MasteryAPI.AbilityRegistry.ActivateAbility(skill.Skill.Ability, context);
                    AbilityUsed = result.Success;

                    if (!AbilityUsed && !string.IsNullOrEmpty(result.Message))
                    {
                        api.Logger.Event($"[MasteryLibrary] Ability {skill.Skill.Ability} failed for player {player.PlayerName}: {result.Message}");
                    }
                }
            }

            if(AbilityUsed)
            {
                skill.UpdateCooldown();
                channel.SendPacket(new CooldownUpdatePacket
                {
                    SkillCode = skillcode,
                    RemainingCooldownMs = (long)(skill.Skill.Cooldown * 1000L)
                }, player);
            }
        }

        private void HandleSkillEquipRequest(IServerPlayer player, SkillEquipRequestPacket packet)
        {
            EntityBehaviorPlayerMasteries? behavior = GetPlayerMasteryBehavior(player);
            if (behavior == null) return;

            if (packet.SlotIndex < 0 || packet.SlotIndex >= behavior.PlayerMasteryData.EquippedActiveSkills.Length) return;
            string skillcode = packet.SkillCode ?? string.Empty;

            if (string.IsNullOrEmpty(skillcode) || behavior.PlayerMasteryData.GetSkillInstance(skillcode) != null)
            {
                behavior.PlayerMasteryData.EquipActiveSkill(skillcode, packet.SlotIndex);
                SendMasterySyncPacket(player);
            }
            else
            {
                channel.SendPacket(new ActionFailedResponsePacket() { Reason = "equipSkillFailed" }, player);
            }
        }
    }
}
