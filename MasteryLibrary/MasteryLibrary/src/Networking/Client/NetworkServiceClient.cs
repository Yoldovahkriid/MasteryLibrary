using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace MasteryLibrary.src.Networking.Client
{
    public class NetworkServiceClient : INetworkService
    {
        private readonly ICoreClientAPI api;
        public IClientNetworkChannel channel;

        public NetworkServiceClient(ICoreClientAPI api)
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
                .SetMessageHandler<MasteryStateResponsePacket>(HandleMasteryStateResponse)
                .SetMessageHandler<ActionFailedResponsePacket>(HandleActionFailedResponse)
                .SetMessageHandler<CooldownUpdatePacket>(HandleCooldownUpdate);
        }

        public void RequestMasteryState() => channel.SendPacket(new MasteryStateRequestPacket());
        public void RequestMasteryUpgrade(string masteryCode) => channel.SendPacket(new MasteryUpgradeRequestPacket() { MasteryCode = masteryCode });
        public void RequestSkillUpgrade(string masteryCode, string skillCode) => channel.SendPacket(new SkillUpgradeRequestPacket() { MasteryCode = masteryCode, SkillCode = skillCode });
        public void RequestSkillActivation(int slotIndex) => channel.SendPacket(new SkillActivationRequestPacket() { SlotIndex = slotIndex });
        public void RequestSkillEquip(int slotIndex, string skillCode) => channel.SendPacket(new SkillEquipRequestPacket() { SlotIndex = slotIndex, SkillCode = skillCode });
        private void HandleMasteryStateResponse(MasteryStateResponsePacket packet)
        {
            if (packet.MasteryState == null) return;
            EntityBehaviorPlayerMasteries? MasteryBehavior = api.World.Player.Entity.GetBehavior<EntityBehaviorPlayerMasteries>();
            if (MasteryBehavior == null) return;
            MasteryBehavior.PlayerMasteryData.FromState(packet.MasteryState);
        }
        private void HandleCooldownUpdate(CooldownUpdatePacket packet)
        {
            EntityBehaviorPlayerMasteries? MasteryBehavior = api.World.Player.Entity.GetBehavior<EntityBehaviorPlayerMasteries>();
            if (MasteryBehavior == null) return;
            MasteryBehavior.PlayerMasteryData.GetSkillInstance(packet.SkillCode)?.UpdateCooldown();
        }

        private void HandleActionFailedResponse(ActionFailedResponsePacket packet)
        {
            string errorCode = packet.Reason ?? "unknownerror";
            api.ShowChatMessage(Lang.Get($"masterylibrary:actionfailed-{errorCode}"));
        }
    }
}
