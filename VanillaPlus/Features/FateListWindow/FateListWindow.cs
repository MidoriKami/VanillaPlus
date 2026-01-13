using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FateListWindow;

public class FateListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FateListWindow,
        Description = Strings.ModificationDescription_FateListWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Now sorts by time remaining"),
            new ChangeLogInfo(3, "Added '/fatelist' command to open window"),
            new ChangeLogInfo(4, "Updated to use new list display system"),
        ],
    };

    public override string ImageName => "FateListWindow.png";

    private FateListAddon? addonFateList;

    public override void OnEnable() {
        addonFateList = new FateListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "FateList",
            Title = Strings.FateListWindow_Title,
            OpenCommand = "/fatelist",
            ListItems = [],
            ItemSpacing = 3.0f,
        };

        addonFateList.Initialize();

        OpenConfigAction = addonFateList.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonFateList?.Dispose();
        addonFateList = null;
    }
}
