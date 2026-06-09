using MasteryLibrary.src.Core.Masteries.Instances;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Networking.Packets
{
    [ProtoContract]
    public class MasteryStateRequestPacket { }
    [ProtoContract]
    public class MasteryUpgradeRequestPacket 
    {
        [ProtoMember(1)]
        public required string MasteryCode;
    }
    [ProtoContract]
    public class SkillUpgradeRequestPacket
    {
        [ProtoMember(1)]
        public required string MasteryCode;
        [ProtoMember(2)]
        public required string SkillCode;
    }
    [ProtoContract]
    public class SkillActivationRequestPacket
    {
        [ProtoMember(1)]
        public required int SlotIndex;
    }
    [ProtoContract]
    public class SkillEquipRequestPacket
    {
        [ProtoMember(1)]
        public required int SlotIndex;
        [ProtoMember(2)]
        public required string SkillCode;
    }


    [ProtoContract]
    public class MasteryStateResponsePacket
    {
        [ProtoMember(1)]
        public required PlayerMasteryState MasteryState;
    }

    [ProtoContract]
    public class ActionFailedResponsePacket
    {
        [ProtoMember(1)]
        public required string Reason;
    }

    //Packet sent to update the clients UI (Note we cannot send the servers time as it is different from client time causing discrepancies)
    [ProtoContract]
    public class CooldownUpdatePacket
    {
        [ProtoMember(1)] 
        public required string SkillCode;
        [ProtoMember(2)] 
        public long RemainingCooldownMs;
    }
}
