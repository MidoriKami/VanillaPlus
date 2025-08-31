using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.QuestListWindow;

public record QuestInfo(uint IconId, ReadOnlySeString Name, ushort Level, MapMarkerData MarkerData) {
    public bool IsRegexMatch(string searchString) {
        const RegexOptions regexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    
        if (Regex.IsMatch(Name.ToString(), searchString, regexOptions)) return true;
        if (Regex.IsMatch(Level.ToString(), searchString, regexOptions)) return true;

        return false;
    }
}
