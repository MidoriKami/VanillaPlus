using System.Linq;
using System.Numerics;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;
using VanillaPlus.Features.DutyLootPreview.Data;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview.Nodes;

/// <summary>
/// Button that opens the Duty Loot Preview window, with checkmark when all misc items unlocked.
/// </summary>
public class DutyLootOpenWindowButtonNode : SimpleComponentNode {
    private readonly TextureButtonNode buttonNode;
    private readonly SimpleImageNode checkmarkNode;

    private readonly DutyLootDataLoader dataLoader;

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
        checkmarkNode.AttachNode(buttonNode);

        this.dataLoader = dataLoader;
        this.dataLoader.OnDutyLootDataChanged += OnDataLoaderStateChanged;
        OnDataLoaderStateChanged();
    }

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        if (disposing) {
            dataLoader.OnDutyLootDataChanged -= OnDataLoaderStateChanged;
        }

        base.Dispose(disposing, isNativeDestructor);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        buttonNode.Size = Size;

        // Scale checkmark proportionally (28x24 checkmark designed for 44px icons)
        var scale = Size.X / 44f;
        checkmarkNode.Size = new Vector2(28, 24) * scale;

        var checkmarkPosition = buttonNode.Size - checkmarkNode.Size;
        var checkmarkBouncePosition = checkmarkPosition + new Vector2(0.0f, 1.0f);

        checkmarkNode.AddTimeline(new TimelineBuilder()
            .AddFrameSetWithFrame(1, 9, 1, checkmarkPosition, 255, multiplyColor: new Vector3(100.0f))
            .AddFrameSetWithFrame(10, 19, 10, checkmarkPosition, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
            .AddFrameSetWithFrame(20, 29, 20, checkmarkBouncePosition, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
            .AddFrameSetWithFrame(30, 39, 30, checkmarkPosition, 178, multiplyColor: new Vector3(50.0f))
            .AddFrameSetWithFrame(40, 49, 40, checkmarkPosition, 255, multiplyColor: new Vector3(100.0f), addColor: new Vector3(16.0f))
            .AddFrameSetWithFrame(50, 59, 50, checkmarkPosition, 255, multiplyColor: new Vector3(100.0f))
            .Build());
    }

    private void OnDataLoaderStateChanged(DutyLootData data) { OnDataLoaderStateChanged(); }

    private void OnDataLoaderStateChanged() {
        var lootData = dataLoader.CurrentDutyLootData;
        if (lootData.IsLoading || lootData.ContentId is null) {
            CheckmarkVisible = false;
            return;
        }

        var unlockableItems = lootData.Items.Where(item => item.IsUnlockable);
        var allUnlockableItemsUnlocked = unlockableItems.All(item => item.IsUnlocked);
        CheckmarkVisible = allUnlockableItemsUnlocked;
    }
}
