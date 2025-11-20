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

        inventoryController.OnAttach += addon => {
            var size = new Vector2(addon->GetSize().X / 2.0f, 28.0f);

            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var newInputNode = new TextInputWithHintNode {
                Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f),
                Size = size,
                OnInputReceived = searchString => PerformSearch(addon, searchString.ToString()),
            };

            newInputNode.AttachNode(addon);
            inputTextNodes.TryAdd(addon->NameString, newInputNode);
        };

        inventoryController.OnUpdate += addon => {
            if (!addon->IsReady) return;

            var currentTab = InventorySearchController.GetTabForInventory(addon);
            
            selectedTabs.TryAdd(addon->NameString, currentTab);
            if (selectedTabs[addon->NameString] != currentTab && inputTextNodes.TryGetValue(addon->NameString, out var inputTextNode)) {
                PerformSearch(addon, inputTextNode.SearchString.ToString());
            }

            selectedTabs[addon->NameString] = currentTab;
        };

        inventoryController.OnDetach += addon => {
            if (inputTextNodes.TryGetValue(addon->NameString, out var node)) {
                node.Dispose();
                inputTextNodes.Remove(addon->NameString);
            }
        };

        inventoryController.Enable();
    }
    
    public void Dispose() {
        inventoryController?.Dispose();
        inventoryController = null;
        
        inputTextNodes?.Clear();
        inputTextNodes = null;
                
        selectedTabs?.Clear();
        selectedTabs = null;
    }

    public Action<string>? PreSearch { get; set; }
    public Action<string>? PostSearch { get; set; }

    private void PerformSearch(AtkUnitBase* addon, string searchString) {
        PreSearch?.Invoke(searchString);
        InventorySearchController.FadeInventoryNodes(addon, searchString);
        PostSearch?.Invoke(searchString);
    }
}
