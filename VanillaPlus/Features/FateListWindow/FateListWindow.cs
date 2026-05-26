using System.Numerics;
using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FateListWindow;

public class FateListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FateListWindow,
        Description = Strings.ModificationDescription_FateListWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "FateListWindow.png";

    private FateListAddon? addonFateList;

    public override async Task OnEnableAsync() {
        addonFateList = new FateListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "FateList",
            Title = Strings.FateListWindow_Title,
            OpenCommand = "/fatelist",
            ItemSpacing = 3.0f,
        };

        await addonFateList.Initialize();

        OpenConfigAction = addonFateList.OpenAddonConfig;
    }

    public override async Task OnDisableAsync() {
        if (addonFateList is not null) {
            await addonFateList.DisposeAsync();
            addonFateList = null;
        }
    }
}
