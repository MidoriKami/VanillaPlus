using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Config;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using Lumina.Text.ReadOnly;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ArmourySearchBar;

public class ArmourySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ArmourySearchBar,
        Description = Strings.ModificationDescription_ArmourySearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "ArmourySearchBar.png";

    private AddonController? inventoryController;
    private KeybindListener? keybindListener;
    private TextInputWithHintNode? searchInputNode;

    private bool? configFadeUnusable;
    private bool searchStarted;
    private int lastTab;

    public override async Task OnEnableAsync() {
        unsafe {
            inventoryController = new AddonController {
                AddonName = "ArmouryBoard",
                OnSetup = OnArmourySetup,
                OnFinalize = OnArmouryFinalize,
                OnPreUpdate = OnArmouryUpdate,
            };
        }

        keybindListener = new KeybindListener {
            Keybind = new Keybind {
                Modifiers = [VirtualKey.CONTROL],
                Key = VirtualKey.F,
            },
            IsEnabled = true,
            Callback = OnKeybindPressed,
        };

        await IFramework.Get().RunSafely(inventoryController.Enable);

        IGameGui.Get().AgentUpdate += OnAgentUpdate;
        IFramework.Get().Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        IFramework.Get().Update -= OnFrameworkUpdate;
        IGameGui.Get().AgentUpdate -= OnAgentUpdate;

        await IFramework.Get().RunSafely(() => inventoryController?.Dispose());
        inventoryController = null;
    }

    private unsafe void OnArmourySetup(AtkUnitBase* addon) {
        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);

        searchInputNode = new TextInputWithHintNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f),
            Size = size,
            OnInputReceived = searchString => OnSearchInputChanged(addon, searchString),
        };
        searchInputNode.AttachNode(addon);

        keybindListener?.IsEnabled = true;
    }

    private unsafe void OnArmouryFinalize(AtkUnitBase* addon) {
        keybindListener?.IsEnabled = false;

        searchInputNode?.Dispose();
        searchInputNode = null;
    }

    private unsafe void OnArmouryUpdate(AtkUnitBase* addon) {
        var currentTab = Inventory.GetTabForInventory(addon);
        if (lastTab != currentTab) {
            lastTab = currentTab;

            if (searchInputNode is not null) {
                Inventory.FadeInventoryNodes(addon, searchInputNode.SearchString.ToString());
            }
        }
    }

    private void OnFrameworkUpdate(IFramework framework) {
        keybindListener?.Update();
    }

    private unsafe void OnAgentUpdate(AgentUpdateFlag updateFlags) {
        if (inventoryController is null) return;
        if (searchInputNode is null) return;

        if (!updateFlags.HasFlag(AgentUpdateFlag.InventoryUpdate)) return;

        var inventoryAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(inventoryController.AddonName);
        if (inventoryAddon is null) return;

        Inventory.FadeInventoryNodes(inventoryAddon, searchInputNode.SearchString.ToString());
    }

    private unsafe void OnSearchInputChanged(AtkUnitBase* addon, ReadOnlySeString searchString) {
        if (configFadeUnusable is null) {
            IGameConfig.Get().TryGet(UiConfigOption.ItemNoArmoryMaskOff, out bool value);
            configFadeUnusable = value;
        }

        var search = searchString.ToString();

        if (!search.IsNullOrEmpty() && !searchStarted) {
            IGameConfig.Get().Set(UiConfigOption.ItemNoArmoryMaskOff, true);
            searchStarted = true;
        }

        if (searchStarted && search.IsNullOrEmpty()) {
            IGameConfig.Get().Set(UiConfigOption.ItemNoArmoryMaskOff, configFadeUnusable.Value);
            searchStarted = false;
        }

        Inventory.FadeInventoryNodes(addon, searchString.ToString());
    }

    private unsafe void OnKeybindPressed(ref bool isHandled) {
        if (inventoryController is null) return;
        if (searchInputNode is null) return;

        var focusedAddonCount = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count;
        if (focusedAddonCount < 1) return;

        var focusedAddon = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[focusedAddonCount - 1];
        if (focusedAddon.Value is null) return;
        if (focusedAddon.Value->Id is 0) return;

        var addonPointer = RaptureAtkUnitManager.Instance()->GetAddonByName(inventoryController.AddonName);
        if (addonPointer is null) return;

        if (focusedAddon.Value->Id == addonPointer->Id || focusedAddon.Value->ParentId == addonPointer->Id) {
            AtkStage.Instance()->AtkInputManager->SetFocus(searchInputNode.FocusNode, addonPointer, 0);
            isHandled = true;
        }
    }
}
