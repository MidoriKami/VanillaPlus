using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.TabBar;

namespace VanillaPlus.Features.PartyFinderPresets;

public class AddonSavePreset : NativeAddon {

    private TabBarNode tabBarNode = null!;
    
    private TextInputNode textInputNode = null!;
    private TextDropDownNode textDropDownNode = null!;
    private TextNode noPresetsTextNode = null!;

    private ResNode warningContainerNode = null!;
    private IconImageNode warningIconNode = null!;
    private TextNode warningTextNode = null!;
    
    private TextButtonNode confirmButtonNode = null!;
    private TextButtonNode cancelButtonNode = null!;

    private bool anyPresets;
    private int selectedTab;
    
    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        WindowNode.SubtitleNode.X = 130.0f;

        tabBarNode = new TabBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, 24.0f),
            IsVisible = true,
        };
        
        tabBarNode.AddTab("Add New", OnShowCreateNew);
        tabBarNode.AddTab("Overwrite", OnShowOverwrite);
        
        AttachNode(tabBarNode);

        var extrasPosition = new Vector2(ContentPadding.X * 2.0f, tabBarNode.Y + tabBarNode.Height + ContentPadding.Y * 3.0f);
        var extrasSize = new Vector2(Size.X - ContentPadding.X * 4.0f, 28.0f);

        textInputNode = new TextInputNode {
            Position = extrasPosition,
            Size = extrasSize,
            IsVisible = true,
            PlaceholderString = "New preset name",
            OnInputReceived = OnTextInput,
        };
        AttachNode(textInputNode);

        warningContainerNode = new ResNode {
            Position = extrasPosition + new Vector2(0.0f, extrasSize.Y),
            Size = extrasSize,
        };
        AttachNode(warningContainerNode);

        warningIconNode = new IconImageNode {
            Position = new Vector2(25.0f, 0.0f),
            Size = new Vector2(16.0f, 16.0f),
            IconId = 60073,
            FitTexture = true,
            IsVisible = true,
        };
        System.NativeController.AttachNode(warningIconNode, warningContainerNode);

        warningTextNode = new TextNode {
            Position = new Vector2(50.0f, 0.0f),
            Size = new Vector2(200.0f, 16.0f),
            IsVisible = true,
            String = "Invalid characters in name",
            AlignmentType = AlignmentType.Left,
        };
        System.NativeController.AttachNode(warningTextNode, warningContainerNode);

        var presetNames = PresetManager.GetPresetNames();
        anyPresets = presetNames.All(preset => preset != PresetManager.DefaultString);
        
        noPresetsTextNode = new TextNode {
            Position = extrasPosition,
            Size = extrasSize,
            AlignmentType = AlignmentType.Bottom,
            String = PresetManager.DefaultString,
            IsVisible = false,
        };
        AttachNode(noPresetsTextNode);
        
        textDropDownNode = new TextDropDownNode {
            Position = extrasPosition,
            Size = extrasSize,
            MaxListOptions = 10,
            Options = presetNames,
            IsVisible = false,
            OnCollapseToggled = OnDropdownOpened,
        };
        AttachNode(textDropDownNode);

        var buttonSize = new Vector2(125.0f, 28.0f);

        confirmButtonNode = new TextButtonNode {
            Position = new Vector2(ContentPadding.X, Size.Y - buttonSize.Y - ContentPadding.Y * 2.0f),
            Size = buttonSize,
            IsVisible = true,
            String = "Confirm",
            OnClick = OnConfirm,
        };
        AttachNode(confirmButtonNode);

        cancelButtonNode = new TextButtonNode {
            Position = new Vector2(Size.X - ContentPadding.X - buttonSize.X, Size.Y - buttonSize.Y - ContentPadding.Y * 2.0f),
            Size = buttonSize,
            IsVisible = true,
            String = "Cancel",
            OnClick = OnCancel,
        };
        AttachNode(cancelButtonNode);
    }

    private void OnTextInput(SeString fileName) {
        var isEmptyFileName = fileName.ToString().IsNullOrEmpty();
        var isInvalidFileName = !PresetManager.IsValidFileName(fileName.ToString());
        
        warningContainerNode.IsVisible = !isEmptyFileName && isInvalidFileName;
    }

    private void OnCancel()
        => Close();

    private void OnConfirm() {
        var fileName = selectedTab switch {
            0 => textInputNode.String,
            1 => textDropDownNode.SelectedOption,
            _ => throw new Exception("Invalid Tab Selected"),
        };

        if (fileName is null) return;
        
        PresetManager.SavePreset(fileName);
        Close();
    }

    private void OnDropdownOpened(bool isCollapsed) {
        confirmButtonNode.CollisionNode.EventFlagsSet = isCollapsed;
        cancelButtonNode.CollisionNode.EventFlagsSet = isCollapsed;
    }

    private void OnShowCreateNew() {
        textInputNode.IsVisible = true;
        textDropDownNode.IsVisible = false;
        noPresetsTextNode.IsVisible = false;
        selectedTab = 0;
    }
    
    private void OnShowOverwrite() {
        textInputNode.IsVisible = false;
        textDropDownNode.IsVisible = anyPresets;
        noPresetsTextNode.IsVisible = !anyPresets;
        selectedTab = 1;
    }
}
