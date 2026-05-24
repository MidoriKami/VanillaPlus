using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.QuestListWindow;

public class QuestListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_QuestListWindow,
        Description = Strings.ModificationDescription_QuestListWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };

    private QuestListAddon? addonQuestList;

    public override string ImageName => "QuestList.png";

    public override async Task OnEnableAsync() {
        addonQuestList = new QuestListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = Strings.QuestListWindow_Title,
            DropDownOptions = Enum.GetValues<QuestFilterMode>().Cast<Enum>().ToList(),
            OpenCommand = "/questlist",
            ListItems = [],
        };

        await addonQuestList.Initialize();
        OpenConfigAction = addonQuestList.OpenAddonConfig;
    }

    public override Task OnDisableAsync() {
        addonQuestList?.Dispose();
        addonQuestList = null;

        return Task.CompletedTask;
    }
}
