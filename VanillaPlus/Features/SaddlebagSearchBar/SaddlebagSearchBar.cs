using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
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

namespace VanillaPlus.Features.SaddlebagSearchBar;

public class SaddlebagSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SaddlebagSearchBar,
        Description = Strings.ModificationDescription_SaddlebagSearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "SaddlebagSearchBar.png";

    private AddonController? inventoryController;
    private KeybindListener? keybindListener;
    private TextInputWithHintNode? searchInputNode;

    public override async Task OnEnableAsync() {
        unsafe {
            inventoryController = new AddonController {
                AddonName = "InventoryBuddy",
                OnSetup = OnBuddySetup,
                OnFinalize = OnBuddyFinalize,
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

        await Service<IFramework>.Get().RunSafely(inventoryController.Enable);

        Service<IGameGui>.Get().AgentUpdate += OnAgentUpdate;
        Service<IFramework>.Get().Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Service<IFramework>.Get().Update -= OnFrameworkUpdate;
        Service<IGameGui>.Get().AgentUpdate -= OnAgentUpdate;

        await Service<IFramework>.Get().RunSafely(() => inventoryController?.Dispose());
        inventoryController = null;
    }

    private unsafe void OnBuddySetup(AtkUnitBase* addon) {
        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);

        searchInputNode = new TextInputWithHintNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 10.0f),
            Size = size,
            OnInputReceived = searchString => OnSearchInputChanged(addon, searchString),
        };
        searchInputNode.AttachNode(addon);
    }

    private unsafe void OnBuddyFinalize(AtkUnitBase* addon) {
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
