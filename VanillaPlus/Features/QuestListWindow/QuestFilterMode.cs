using System.ComponentModel;

namespace VanillaPlus.Features.QuestListWindow;

public enum QuestFilterMode {
    [Description(nameof(Strings.QuestListWindow_FilterType))]
    Type,
    
    [Description(nameof(Strings.QuestListWindow_FilterAlphabetically))]
    Alphabetically,
    
    [Description(nameof(Strings.QuestListWindow_FilterLevel))]
    ClassJobLevel,
    
    [Description(nameof(Strings.QuestListWindow_FilterDistance))]
    Distance,
    
    [Description(nameof(Strings.QuestListWindow_FilterIssuerName))]
    IssuerName,
}
