using System;
using System.Collections.Generic;
using ProtoBuf;

namespace MasteryLibrary.src.Core.Masteries.Instances
{
    [ProtoContract]
    public record PlayerMasteryState
    {
        [ProtoMember(1)] public int CharacterLevel { get; init; }
        [ProtoMember(2)] public int MasteryPoints { get; init; }
        [ProtoMember(3)] public int BonusMasteryPoints { get; init; }
        [ProtoMember(4)] public double Experience { get; init; }
        [ProtoMember(5)] public List<MasteryState> LearntMasteries { get; init; } = new();
        [ProtoMember(6)] public string[] EquippedActiveSkills { get; init; }
    }

    [ProtoContract]
    public record MasteryState
    {
        [ProtoMember(1)] public string Code { get; init; }
        [ProtoMember(2)] public int Level { get; init; }
        [ProtoMember(3)] public List<SkillState> UnlockedSkills { get; init; } = new();
    }

    [ProtoContract]
    public record SkillState
    {
        [ProtoMember(1)] public string Code { get; init; }
        [ProtoMember(2)] public int Level { get; init; }
        [ProtoMember(3)] public long LastUsedTime { get; init; }
    }
}