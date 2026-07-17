using System.Threading.Tasks;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.InventoryCooldowns;

public class InventoryCooldowns : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventoryCooldowns,
        Description = Strings.ModificationDescription_InventoryCooldowns,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["Haselnussbomber"],
    };

    public override string ImageName => "InventoryCooldowns.png";

    private ExpandedInventoryController? expandedInventoryController;
    private LargeInventoryController? largeInventoryController;
    private NormalInventoryController? normalInventoryController;

    public override async Task OnEnableAsync() {
        if (Service<IClientState>.Get().IsLoggedIn) {
            await Service<IFramework>.Get().RunSafely(ReinitializeController);
        }

        Service<IGameConfig>.Get().UiConfigChanged += OnUiConfigChanged;
    }

    public override async Task OnDisableAsync() {
        Service<IGameConfig>.Get().UiConfigChanged -= OnUiConfigChanged;

        await Service<IFramework>.Get().RunSafely(() => {
            expandedInventoryController?.Dispose();
            largeInventoryController?.Dispose();
            normalInventoryController?.Dispose();
        });
        expandedInventoryController = null;
        largeInventoryController = null;
        normalInventoryController = null;
    }

    private void OnUiConfigChanged(object? sender, ConfigChangeEvent e) {
        if (e.Option is not UiConfigOption.ItemInventryWindowSizeType) return;

        ReinitializeController();
    }

    private void ReinitializeController() {
        if (!Service<IGameConfig>.Get().UiConfig.TryGet("ItemInventryWindowSizeType", out uint inventoryType)) return;

        expandedInventoryController?.Dispose();
        expandedInventoryController = null;

        largeInventoryController?.Dispose();
        largeInventoryController = null;

        normalInventoryController?.Dispose();
        normalInventoryController = null;

        switch (inventoryType) {
            case 0: // "Inventory"
                normalInventoryController = new NormalInventoryController();
                normalInventoryController.Enable();
                break;

            case 1: // "InventoryLarge"
                largeInventoryController = new LargeInventoryController();
                largeInventoryController.Enable();
                break;

            case 2: // "InventoryExpansion"
                expandedInventoryController = new ExpandedInventoryController();
                expandedInventoryController.Enable();
                break;
        }
    }
}
