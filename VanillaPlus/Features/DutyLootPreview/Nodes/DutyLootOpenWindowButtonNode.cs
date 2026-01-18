using System.Linq;
using System.Numerics;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Features.DutyLootPreview.Data;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview.Nodes;

/// <summary>
/// Button that opens the Duty Loot Preview window, with checkmark when all misc items unlocked.
/// </summary>
public class DutyLootOpenWindowButtonNode : SimpleComponentNode {
    private readonly TextureButtonNode buttonNode;
    private readonly SimpleImageNode checkmarkNode;

    private readonly DutyLootDataLoader DataLoader;

    public Action? OnClick {
        get => buttonNode.OnClick;
        set => buttonNode.OnClick = value;
    }

    public bool CheckmarkVisible {
        get => checkmarkNode.IsVisible;
        set => checkmarkNode.IsVisible = value;
    }

    public DutyLootOpenWindowButtonNode(DutyLootDataLoader dataLoader) {
        buttonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),
        };
        buttonNode.AttachNode(this);

        checkmarkNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(64, 32),
            TextureSize = new Vector2(20, 16),
            TexturePath = "ui/uld/RecipeNoteBook.tex",
            WrapMode = WrapMode.Stretch,
            IsVisible = false
        };
        checkmarkNode.AttachNode(this);

        DataLoader = dataLoader;
        DataLoader.OnDutyLootDataChanged += OnDataLoaderStateChanged;
        OnDataLoaderStateChanged();
    }

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        if (disposing) {
            DataLoader.OnDutyLootDataChanged -= OnDataLoaderStateChanged;
        }

        base.Dispose(disposing, isNativeDestructor);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        buttonNode.Size = Size;

        // Scale checkmark proportionally (28x24 checkmark designed for 44px icons)
        var scale = Size.X / 44f;
        checkmarkNode.Size = new Vector2(28, 24) * scale;
        checkmarkNode.Position = Size - checkmarkNode.Size;
    }

    private void OnDataLoaderStateChanged(DutyLootData data) { OnDataLoaderStateChanged(); }

    private void OnDataLoaderStateChanged() {
        var lootData = DataLoader.CurrentDutyLootData;
        if (lootData.IsLoading || lootData.ContentId is null) {
            CheckmarkVisible = false;
            return;
        }

        var unlockableItems = lootData.Items.Where(item => item.IsUnlockable).ToList();
        var allUnlockableItemsUnlocked = unlockableItems.Count == 0 || unlockableItems.All(item => item.IsUnlocked);
        CheckmarkVisible = allUnlockableItemsUnlocked;
    }
}
