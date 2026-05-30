using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.NativeElements.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class InventorySearchAddonController : IDisposable {

    private readonly MultiAddonController inventoryController;
    private readonly Dictionary<string, TextInputWithHintNode> inputTextNodes = [];
    private readonly Dictionary<string, int> selectedTabs = [];

    private static KeybindListener? keybindListener;

    public unsafe InventorySearchAddonController(params string[] addons) {
        keybindListener ??= new KeybindListener {
            AddonConfig = new AddonConfig {
                DisableInCombat = true,
                Keybind = new Keybind {
                    Modifiers = [VirtualKey.CONTROL],
                    Key = VirtualKey.F,
                },
                KeybindEnabled = true,
                WindowSize = Vector2.Zero,
            },
        };

        keybindListener.KeybindCallback += OnKeybindPressed;

        inventoryController = new MultiAddonController {
            AddonNames = addons.ToList(),
            OnSetup = SetupInventory,
            OnUpdate = UpdateInventory,
            OnFinalize = FinalizeInventory,
        };
    }

    public void Dispose() {
        Services.PluginLog.Info("Disposing", "InventorySearchAddonController");

        foreach (var node in inputTextNodes.Values) {
            node.Dispose();
        }

        inventoryController.Dispose();

        inputTextNodes.Clear();
        selectedTabs.Clear();

        keybindListener?.KeybindCallback -= OnKeybindPressed;
    }

    public void Enable()
        => inventoryController.Enable();

    private unsafe void OnKeybindPressed(ref bool isHandled) {
        var focusedAddonCount = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count;
        if (focusedAddonCount < 1) return;

        var focusedAddon = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[focusedAddonCount - 1];
        if (focusedAddon.Value is null) return;
        if (focusedAddon.Value->Id is 0) return;

        foreach (var (addonName, searchBarNode) in inputTextNodes) {
            var addonPointer = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
            if (addonPointer is null) continue;

            if (focusedAddon.Value->Id == addonPointer->Id || focusedAddon.Value->ParentId == addonPointer->Id) {
                AtkStage.Instance()->AtkInputManager->SetFocus(searchBarNode.FocusNode, addonPointer, 0);
                isHandled = true;
                return;
            }
        }
    }

    private unsafe void SetupInventory(AtkUnitBase* addon) {
        Services.PluginLog.Info($"OnInventoryAttach: {addon->NameString}", "InventorySearchAddonController");
        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);

        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var newInputNode = new TextInputWithHintNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f),
            Size = size,
            OnInputReceived = searchString => PerformSearch(addon, searchString.ToString()),
        };

        newInputNode.AttachNode(addon);
        inputTextNodes.TryAdd(addon->NameString, newInputNode);
    }

    private unsafe void UpdateInventory(AtkUnitBase* addon) {
        if (!addon->IsReady) return;

        var currentTab = Inventory.GetTabForInventory(addon);

        selectedTabs.TryAdd(addon->NameString, currentTab);
        if (selectedTabs[addon->NameString] != currentTab && inputTextNodes.TryGetValue(addon->NameString, out var inputTextNode)) {
            PerformSearch(addon, inputTextNode.SearchString.ToString());
        }

        selectedTabs[addon->NameString] = currentTab;
    }

    private unsafe void FinalizeInventory(AtkUnitBase* addon) {
        Services.PluginLog.Info($"OnInventoryDetach: {addon->NameString}", "InventorySearchAddonController");
        if (inputTextNodes.TryGetValue(addon->NameString, out _)) {
            // Intentionally leak node for now, the memory should still be automatically cleaned up by the game.
            // Node will still get manually disposed on plugin unload correctly.
            // node.Dispose();

            inputTextNodes.Remove(addon->NameString);
        }
    }

    public Action<string>? PreSearch { get; set; }
    public Action<string>? PostSearch { get; set; }

    private unsafe void PerformSearch(AtkUnitBase* addon, string searchString) {
        PreSearch?.Invoke(searchString);
        InventorySearchController.FadeInventoryNodes(addon, searchString);
        PostSearch?.Invoke(searchString);
    }
}
