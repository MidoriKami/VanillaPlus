using System;
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

namespace VanillaPlus.Features.RetainerSearchBar;

public class RetainerSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RetainerSearchBar,
        Description = Strings.ModificationDescription_RetainerSearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "RetainerSearchBar.png";

    private AddonController? inventoryController;
    private KeybindListener? keybindListener;
    private TextInputWithHintNode? searchInputNode;

    public override async Task OnEnableAsync() {
        if (IClientState.Get().IsLoggedIn) {
            await IFramework.Get().Run(ReinitializeController);
        }

        keybindListener = new KeybindListener {
            Keybind = new Keybind {
                Modifiers = [VirtualKey.CONTROL],
                Key = VirtualKey.F,
            },
            IsEnabled = true,
            Callback = OnKeybindPressed,
        };

        IGameGui.Get().AgentUpdate += OnAgentUpdate;
        IFramework.Get().Update += OnFrameworkUpdate;
        IGameConfig.Get().UiConfigChanged += OnUiConfigChanged;
    }

    public override async Task OnDisableAsync() {
        IGameConfig.Get().UiConfigChanged -= OnUiConfigChanged;
        IFramework.Get().Update -= OnFrameworkUpdate;
        IGameGui.Get().AgentUpdate -= OnAgentUpdate;

        await IFramework.Get().RunSafely(() => inventoryController?.Dispose());
        keybindListener = null;
        inventoryController = null;
    }

    private void OnUiConfigChanged(object? sender, ConfigChangeEvent e) {
        if (e.Option is not UiConfigOption.ItemInventryRetainerWindowSizeType) return;

        ReinitializeController();
    }

    private unsafe void ReinitializeController() {
        if (!IGameConfig.Get().UiConfig.TryGet("ItemInventryRetainerWindowSizeType", out uint inventoryType)) return;

        inventoryController?.Dispose();

        inventoryController = new AddonController {
            AddonName = inventoryType switch {
                0 => "InventoryRetainer",
                1 => "InventoryRetainerLarge",
                _ => throw new ArgumentOutOfRangeException(),
            },
            OnSetup = OnInventorySetup,
            OnFinalize = OnInventoryFinalize,
        };

        inventoryController.Enable();
    }

    private unsafe void OnInventorySetup(AtkUnitBase* addon) {
        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);

        searchInputNode = new TextInputWithHintNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f),
            Size = size,
            OnInputReceived = searchString => OnSearchInputChanged(addon, searchString),
        };
        searchInputNode.AttachNode(addon);
    }

    private unsafe void OnInventoryFinalize(AtkUnitBase* addon) {
        searchInputNode?.Dispose();
        searchInputNode = null;
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

    private static unsafe void OnSearchInputChanged(AtkUnitBase* addon, ReadOnlySeString searchString) {
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
