using System.Numerics;
using Dalamud.Game.ClientState.Aetherytes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.ContextMenu;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.BetterTeleportWindow;

public unsafe class TeleportListItemNode : ListItemNode<IAetheryteEntry>, IListItemNode {
    public static float ItemHeight => 32.0f;

    private readonly TextNode nameText;
    private readonly TextNode aetheryteText;
    private readonly TextNode gilCost;

    private readonly ContextMenu contextMenu;

    public TeleportListItemNode() {
        contextMenu = new ContextMenu();

        nameText = new TextNode();
        nameText.AttachNode(this);

        gilCost = new TextNode { AlignmentType = AlignmentType.Right };
        gilCost.AttachNode(this);

        aetheryteText = new TextNode { TextColor = ColorHelper.GetColor(3) };
        aetheryteText.AttachNode(this);

        CollisionNode.AddEvent(AtkEventType.MouseOver, OnMouseOver);
        CollisionNode.AddEvent(AtkEventType.MouseClick, OnClicked);
    }

    private void OnMouseOver() {
        Services.PluginLog.Debug(ItemData?.MapTexturePath ?? string.Empty);
        BetterTeleportWindow.CustomTeleportAddon?.SetPreviewImage(ItemData);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        nameText.Size = new Vector2(Width * 4.0f / 5.0f, Height / 2.0f);
        nameText.Position = new Vector2(0.0f, 0.0f);

        gilCost.Size = new Vector2(Width * 1.0f / 5.0f, Height / 2.0f);
        gilCost.Position = new Vector2(Width - Width * 1.0f / 5.0f, 0.0f);

        aetheryteText.Size = new Vector2(Width, Height / 2.0f);
        aetheryteText.Position = new Vector2(0.0f, Height / 2.0f);
    }

    protected override void SetNodeData(IAetheryteEntry itemData) {
        nameText.String = itemData.PlaceName;
        gilCost.String = itemData.GilCostString;
        aetheryteText.String = itemData.AetheryteName;
    }

    private void OnClicked(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (BetterTeleportWindow.Config is not {} config) return;
        if (ItemData is null) return;

        IsSelected = false;

        if (atkEventData->IsLeftClick) {
            ItemData?.Teleport();
        }
        else {
            contextMenu.Clear();

            var isFavorite = config.FavoriteAetherytes.Contains(ItemData.AetheryteId);

            contextMenu.AddItem(new ContextMenuItem {
                Name = isFavorite
                           ? Services.DataManager.GetAddonText(8324)  // Remove from Favorites
                           : Services.DataManager.GetAddonText(8323), // Add to Favorites
                OnClick = () => {
                    if (isFavorite) {
                        config.FavoriteAetherytes.Remove(ItemData.AetheryteId);
                    }
                    else {
                        config.FavoriteAetherytes.Add(ItemData.AetheryteId);
                    }
                    config.Save();
                },
            });

            contextMenu.Open();
        }
    }
}
