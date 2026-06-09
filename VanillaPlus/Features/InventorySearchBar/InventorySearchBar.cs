using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Config;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using Lumina.Text.ReadOnly;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InventorySearchBar;

public class InventorySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventorySearchBar,
        Description = Strings.ModificationDescription_InventorySearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "InventorySearchBar.png";

    private AddonController? inventoryController;
    private KeybindListener? keybindListener;
    private TextInputWithHintNode? searchInputNode;

    public override async Task OnEnableAsync() {
        if (Services.ClientState.IsLoggedIn) {
            await SetupInventoryController();
        }

        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;
        Services.GameGui.AgentUpdate += OnAgentUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.GameGui.AgentUpdate -= OnAgentUpdate;
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.Logout -= OnLogout;

        await Services.Framework.Run(() => inventoryController?.Dispose());
        keybindListener = null;
        inventoryController = null;
    }

    private void OnLogin()
        => Task.Run(SetupInventoryController);

    private void OnLogout(int type, int code) {
        inventoryController?.Dispose();
        inventoryController = null;

        keybindListener = null;
    }

    private async Task SetupInventoryController() {
        if (!Services.GameConfig.TryGet(UiConfigOption.ItemInventryWindowSizeType, out uint inventoryType)) {
            throw new Exception("Unable to read GameConfig.");
        }

        unsafe {
            inventoryController = new AddonController {
                AddonName = inventoryType switch {
                    0 => "Inventory",
                    1 => "InventoryLarge",
                    2 => "InventoryExpansion",
                    _ => throw new ArgumentOutOfRangeException(),
                },
                OnSetup = OnInventorySetup,
                OnFinalize = OnInventoryFinalize,
                OnPreUpdate = OnInventoryUpdate,
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

        await Services.Framework.Run(inventoryController.Enable);
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

    private unsafe void OnInventoryUpdate(AtkUnitBase* addon) {
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
