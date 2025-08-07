using System.Linq;
using Dalamud.Game.Config;
using Dalamud.Game.Gui.ContextMenu;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.PetSizeContextMenu;

public class PetSizeContextMenu : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Pet Size Context Menu",
        Description = "When right clicking on a pet, or a player with a pet, show a context menu entry for changing the pet size",
        Authors = [ "MidoriKami" ],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private readonly UiConfigOption[] configEntries = [
        UiConfigOption.BahamutSize, UiConfigOption.PhoenixSize, UiConfigOption.GarudaSize,
        UiConfigOption.TitanSize, UiConfigOption.IfritSize, UiConfigOption.SolBahamutSize,
    ];

    public override string ImageName => "PetSizeContextMenu.png";

    public override void OnEnable()
        => Services.ContextMenu.OnMenuOpened += OnMenuOpened;

    public override void OnDisable()
        => Services.ContextMenu.OnMenuOpened -= OnMenuOpened;

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (args is not { MenuType: ContextMenuType.Default }) return;

        var isPetTarget = Services.TargetManager.Target?.IsPet() ?? false;
        var isPetOwnerTarget = Services.ObjectTable.FirstOrDefault(obj => obj.IsPet() && obj.OwnerId == Services.TargetManager.Target?.EntityId) is not null;
        if (!isPetTarget && !isPetOwnerTarget) return;

        var currentPetSize = GetPetSize();
        
        args.AddMenuItem(new MenuItem {
            IsSubmenu = true,
            UseDefaultPrefix = true,
            Name = "Pet Size",
            OnClicked = clickedArgs => {
                clickedArgs.OpenSubmenu([
                    new MenuItem {
                        IsEnabled = currentPetSize is not 0,
                        UseDefaultPrefix = true, 
                        Name = "Small", 
                        OnClicked = _ => SetPetSize(0),
                    },
                    new MenuItem {
                        IsEnabled = currentPetSize is not 1,
                        UseDefaultPrefix = true, 
                        Name = "Medium", 
                        OnClicked = _ => SetPetSize(1),
                    },
                    new MenuItem {
                        IsEnabled = currentPetSize is not 2,
                        UseDefaultPrefix = true, 
                        Name = "Large", 
                        OnClicked = _ => SetPetSize(2),
                    },
                ]);
            },
        });
    }

    private void SetPetSize(uint size) {
        foreach(var configEntry in configEntries) {
            Services.GameConfig.Set(configEntry, size);
        }
    }

    private uint? GetPetSize()
        => configEntries
           .Select(configKey => Services.GameConfig.TryGet(configKey, out uint value) ? value : 0)
           .GroupBy(configValue => configValue)
           .MaxBy(group => group.Count())?
           .Key;
}
