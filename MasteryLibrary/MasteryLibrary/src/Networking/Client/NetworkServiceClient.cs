using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Config;
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
        // Doesnt need to be in the event class its solely for internal use
        public event Action<XpPopUpPacket>? XpGained;

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
                .RegisterMessageType<ConfigSyncPacket>()
                .RegisterMessageType<XpPopUpPacket>()
                .SetMessageHandler<MasteryStateResponsePacket>(HandleMasteryStateResponse)
                .SetMessageHandler<ActionFailedResponsePacket>(HandleActionFailedResponse)
                .SetMessageHandler<CooldownUpdatePacket>(HandleCooldownUpdate)
                .SetMessageHandler<ConfigSyncPacket>(HandleConfigSyncResponse)
                .SetMessageHandler<XpPopUpPacket>(HandleXpPopUp);
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
            var skill = MasteryBehavior.PlayerMasteryData.GetSkillInstance(packet.SkillCode);
            if (skill == null) return;
            skill.SetCooldownFromRemaining(packet.RemainingCooldownMs);
        }

        private void HandleActionFailedResponse(ActionFailedResponsePacket packet)
        {
            string errorCode = packet.Reason ?? "unknownerror";
            api.ShowChatMessage(Lang.Get($"masterylibrary:actionfailed-{errorCode}"));
        }

        private void HandleConfigSyncResponse(ConfigSyncPacket packet)
        {
            if (packet.Config == null) return;
            MasteryLibConfigCommon.Loaded = packet.Config;
        }

        private void HandleXpPopUp(XpPopUpPacket packet)
        {
            XpGained?.Invoke(packet);
        }
    }
}
