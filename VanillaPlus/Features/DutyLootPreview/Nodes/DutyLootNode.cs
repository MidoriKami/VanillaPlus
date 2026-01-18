using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.ContextMenu;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.NativeElements.Nodes;
using ContextMenu = KamiToolKit.ContextMenu.ContextMenu;

namespace VanillaPlus.Features.DutyLootPreview.Nodes;

public unsafe class DutyLootNode : ListItemNode<DutyLootItemView> {
    public override float ItemHeight => 36.0f;

    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    private readonly SimpleImageNode favoriteStarNode;
    private readonly SimpleImageNode infoIconNode;
    private readonly SimpleImageNode checkmarkIconNode;
    private readonly ContextMenu contextMenu;

    public DutyLootNode() {
        contextMenu = new ContextMenu();

        iconNode = new IconWithCountNode();
        iconNode.AttachNode(this);

        favoriteStarNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(96, 0),
            TextureSize = new Vector2(20, 20),
            TexturePath = "ui/uld/MinionNoteBook.tex",
            Size = new Vector2(20, 20),
            IsVisible = false,
        };
        favoriteStarNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
        };
        itemNameTextNode.AttachNode(this);

        infoIconNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(112, 84),
            TextureSize = new Vector2(28, 28),
            TexturePath = "ui/uld/CircleButtons.tex",
            WrapMode = WrapMode.Stretch,
        };
        infoIconNode.AttachNode(this);

        checkmarkIconNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(60, 28),
            TextureSize = new Vector2(28, 24),
            TexturePath = "ui/uld/RecipeNoteBook.tex",
            IsVisible = false,
        };
        checkmarkIconNode.AttachNode(this);

        CollisionNode.AddEvent(AtkEventType.MouseClick, (_, _, _, _, atkEventData) => {
            if (ItemData is null) return;

            if (atkEventData->IsLeftClick) {
                OnLeftClick();
            }
            else if (atkEventData->IsRightClick) {
                OnRightClick();
            }
        });
    }

    private void OnLeftClick() {
        if (ItemData is not { Item: var item }) return;

        if (item.CanTryOn) {
            AgentTryon.TryOn(0, item.ItemId);
        }
    }

    private void OnRightClick() {
        if (ItemData is not { Item: var item, Config: var config }) return;

        contextMenu.Clear();

        if (item.CanTryOn) {
            contextMenu.AddItem(
                Services.DataManager.GetAddonText(2426), // Try On
                () => AgentTryon.TryOn(0, item.ItemId));
        }

        var isFavorite = config.FavoriteItems.Contains(item.ItemId);
        contextMenu.AddItem(new ContextMenuItem {
            Name = isFavorite
                ? Services.DataManager.GetAddonText(8324) // Remove from Favorites
                : Services.DataManager.GetAddonText(8323), // Add to Favorites
            OnClick = () => {
                if (isFavorite) {
                    config.FavoriteItems.Remove(item.ItemId);
                }
                else {
                    config.FavoriteItems.Add(item.ItemId);
                }
                config.Save();
                favoriteStarNode.IsVisible = !isFavorite;
            },
        });

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(4379), // Search for Item
            () => ItemFinderModule.Instance()->SearchForItem(item.ItemId));

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(4697), // Link
            () => AgentChatLog.Instance()->LinkItem(item.ItemId));

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(13439), // Search Recipes Using This Material
            () => AgentRecipeProductList.Instance()->SearchForRecipesUsingItem(item.ItemId));

        contextMenu.Open();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        // Scale star proportionally (original: 20x20 star on 44x44 icon)
        var starSize = iconNode.Height * (20f / 44f);
        favoriteStarNode.Size = new Vector2(starSize, starSize);

        // Position in top-right corner, slightly above icon edge
        favoriteStarNode.Position = new Vector2(iconNode.Width - favoriteStarNode.Width, -2);

        var infoSize = Size.Y * 0.6f;
        infoIconNode.Size = new Vector2(infoSize, infoSize);
        infoIconNode.Position = new Vector2(Width - infoSize, Height / 2 - infoSize / 2);

        checkmarkIconNode.Size = new Vector2(28, 24);
        checkmarkIconNode.Position = iconNode.Size - checkmarkIconNode.Size * 0.8f;

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - infoSize - 12.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }

    protected override void SetNodeData(DutyLootItemView view) {
        var item = view.Item;

        iconNode.IconId = item.IconId;
        itemNameTextNode.String = item.Name;
        iconNode.Count = 1;
        infoIconNode.TextTooltip = string.Join("\n", item.Sources);
        checkmarkIconNode.IsVisible = item.IsUnlocked;
        CollisionNode.ItemTooltip = item.ItemId;
        favoriteStarNode.IsVisible = view.IsFavorite;
    }
}
