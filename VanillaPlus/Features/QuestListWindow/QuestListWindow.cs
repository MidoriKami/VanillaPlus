using System;
using System.Linq;
using System.Numerics;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.QuestListWindow;

public class QuestListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_QuestListWindow,
        Description = Strings.ModificationDescription_QuestListWindow,
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private QuestListAddon? addonQuestList;

    public override string ImageName => "QuestList.png";

    public override void OnEnable() {
        addonQuestList = new QuestListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = Strings.QuestListWindow_Title,
            DropDownOptions = Enum.GetValues<QuestFilterMode>().Select(value => value.Description).ToList(),
            OpenCommand = "/questlist",
            ListItems = [],
        };

        addonQuestList.Initialize();
        OpenConfigAction = addonQuestList.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonQuestList?.Dispose();
        addonQuestList = null;
    }
}
