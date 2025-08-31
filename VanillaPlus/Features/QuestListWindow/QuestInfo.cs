using System;
using System.Numerics;
using System.Text.RegularExpressions;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.QuestListWindow;

public class QuestInfo : IEquatable<QuestInfo> {
    public uint ObjectiveId { get; init; }
    public uint IconId { get; init; }
    public ReadOnlySeString Name { get; init; }
    public ushort Level { get; init; }
    public Vector3 Position { get; init; }
    public ReadOnlySeString IssuerName { get; init; }
    
    public float Distance => Vector3.Distance(Services.ClientState.LocalPlayer?.Position ?? Vector3.Zero, Position);
    
    public bool IsRegexMatch(string searchString) {
        const RegexOptions regexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    
        if (Regex.IsMatch(Name.ToString(), searchString, regexOptions)) return true;
        if (Regex.IsMatch(Level.ToString(), searchString, regexOptions)) return true;
        if (Regex.IsMatch(IssuerName.ToString(), searchString, regexOptions)) return true;

        return false;
    }

    public bool Equals(QuestInfo? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ObjectiveId == other.ObjectiveId;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((QuestInfo) obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(ObjectiveId);
}
