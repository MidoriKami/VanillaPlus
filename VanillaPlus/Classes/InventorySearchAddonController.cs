using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Classes;

public unsafe class InventorySearchAddonController : IDisposable {

    private MultiAddonController? inventoryController;
    private Dictionary<string, TextInputWithHintNode>? inputTextNodes;
    private Dictionary<string, int>? selectedTabs;

    public InventorySearchAddonController(params string[] addons) {
        inputTextNodes = [];
        selectedTabs = [];

        inventoryController = new MultiAddonController(addons);
        inventoryController.OnAttach += OnInventoryAttach;
        inventoryController.OnUpdate += OnInventoryUpdate;
        inventoryController.OnDetach += OnInventoryDetach;
        inventoryController.Enable();
    }

    public void Dispose() {
        Services.PluginLog.Info("InventorySearchAddonController.Dispose");

        inventoryController?.Dispose();
        inventoryController = null;

        foreach (var (_, node) in inputTextNodes ?? []) {
            node.Dispose();
        }

        inputTextNodes?.Clear();
        inputTextNodes = null;
                
        selectedTabs?.Clear();
        selectedTabs = null;
    }
    
    private void OnInventoryDetach(AtkUnitBase* addon) {
        Services.PluginLog.Info($"OnInventoryAttach: {addon->NameString}");
        if (inputTextNodes?.TryGetValue(addon->NameString, out var node) ?? false) {
            node.Dispose();
            inputTextNodes.Remove(addon->NameString);
        }
    }

    private void OnInventoryUpdate(AtkUnitBase* addon) {
        if (selectedTabs is null) return;
        if (inputTextNodes is null) return;
        if (!addon->IsReady) return;

        var currentTab = InventorySearchController.GetTabForInventory(addon);

        selectedTabs.TryAdd(addon->NameString, currentTab);
        if (selectedTabs[addon->NameString] != currentTab && inputTextNodes.TryGetValue(addon->NameString, out var inputTextNode)) {
            PerformSearch(addon, inputTextNode.SearchString.ToString());
        }

        selectedTabs[addon->NameString] = currentTab;
    }

    private void OnInventoryAttach(AtkUnitBase* addon) {
        Services.PluginLog.Info($"OnInventoryAttach: {addon->NameString}");
        if (inputTextNodes is null) return;
        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);

        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var newInputNode = new TextInputWithHintNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f), Size = size, OnInputReceived = searchString => PerformSearch(addon, searchString.ToString()),
        };

        newInputNode.AttachNode(addon);
        inputTextNodes.TryAdd(addon->NameString, newInputNode);
    }

    public Action<string>? PreSearch { get; set; }
    public Action<string>? PostSearch { get; set; }

    private void PerformSearch(AtkUnitBase* addon, string searchString) {
        PreSearch?.Invoke(searchString);
        InventorySearchController.FadeInventoryNodes(addon, searchString);
        PostSearch?.Invoke(searchString);
    }
}
