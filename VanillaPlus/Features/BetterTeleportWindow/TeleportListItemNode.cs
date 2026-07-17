using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.ContextMenu;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using VanillaPlus.Native.Addons;
using ContextMenu = KamiToolKit.ContextMenu.ContextMenu;

namespace VanillaPlus.Features.BetterTeleportWindow;

public unsafe class TeleportListItemNode : ListItemNode<IAetheryteEntry>, IListItemNode {
    public static float ItemHeight => 32.0f;

    private readonly TextNode aetheryteName;
    private readonly TextNode regionName;
    private readonly TextNode gilCost;

    private readonly ContextMenu contextMenu;

    private RenameAddon? renameAddon;

    public TeleportListItemNode() {
        contextMenu = new ContextMenu();

        aetheryteName = new TextNode {
            TextFlags = TextFlags.Ellipsis,
        };
        aetheryteName.AttachNode(this);

        gilCost = new TextNode {
            AlignmentType = AlignmentType.Right,
        };
        gilCost.AttachNode(this);

        regionName = new TextNode {
            TextColor = ColorHelper.GetColor(3),
        };
        regionName.AttachNode(this);

        AddEvent(AtkEventType.MouseOver, OnMouseOver);
        AddEvent(AtkEventType.MouseClick, OnClicked);
    }

    protected override void Dispose(bool isNativeDestructor) {
        if (IsDisposed) return;

        renameAddon?.Dispose();
        renameAddon = null;

        base.Dispose(isNativeDestructor);
    }

    private void OnMouseOver()
        => BetterTeleportWindow.CustomTeleportAddon?.SetPreviewImage(ItemData);

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        aetheryteName.Size = new Vector2(Width * 8.5f / 10.0f, Height / 2.0f);
        aetheryteName.Position = new Vector2(0.0f, 0.0f);

        gilCost.Size = new Vector2(Width * 1.5f / 10.0f, Height / 2.0f);
        gilCost.Position = new Vector2(Width - Width * 1.0f / 5.0f, 0.0f);

        regionName.Size = new Vector2(Width, Height / 2.0f);
        regionName.Position = new Vector2(0.0f, Height / 2.0f);
    }

    protected override void SetNodeData(IAetheryteEntry itemData) {
        aetheryteName.String = GetDisplayName(itemData);
        gilCost.String = itemData.GilCostString;
        regionName.String = itemData.PlaceName;
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
                Name = IDataManager.Get().GetAddonText(8441), // Open Map
                OnClick = () => {
                    AgentTeleport.Instance()->AgentInterface.SendCommand(2, [1, ItemData.EntryIndex, 0]);
                    AgentTeleport.Instance()->AgentInterface.SendCommand(4, [0, 0, 0, 0, 0]);
                },
            });

            contextMenu.AddItem(new ContextMenuItem {
                Name = isFavorite
                           ? IDataManager.Get().GetAddonText(8324)  // Remove from Favorites
                           : IDataManager.Get().GetAddonText(8323), // Add to Favorites
                OnClick = () => {
                    if (isFavorite) {
                        config.FavoriteAetherytes.Remove(ItemData.AetheryteId);
                    }
                    else {
                        config.FavoriteAetherytes.Add(ItemData.AetheryteId);
                    }
                    Task.Run(config.Save);
                },
            });

            contextMenu.AddItem(new ContextMenuItem {
                Name = IDataManager.Get().GetAddonText(14511), // Rename
                OnClick = () => {
                    renameAddon?.Dispose();
                    renameAddon = new RenameAddon {
                        Title = Strings.BetterTeleportWindow_RenameTitle,
                        InternalName = "RenameTeleport",
                        AutoSelectAll = true,
                        DefaultString = GetCustomName(ItemData),
                        OnRenameComplete = newName => {
                            if (newName.ToString() is "") {
                                config.CustomNames.Remove(ItemData.AetheryteId);
                            }
                            else {
                                if (!config.CustomNames.TryAdd(ItemData.AetheryteId, newName.ToString())) {
                                    config.CustomNames[ItemData.AetheryteId] = newName.ToString();
                                }
                            }

                            Task.Run(config.Save);

                            renameAddon?.Dispose();
                            renameAddon = null;
                        },
                    };

                    renameAddon.Open();
                },
            });

            contextMenu.Open();
        }
    }

    private static string GetCustomName(IAetheryteEntry entry) {
        if (BetterTeleportWindow.Config is not {} config) return string.Empty;
        if (config.CustomNames.TryGetValue(entry.AetheryteId, out var customName)) return customName;

        return entry.AetheryteName.ToString();
    }

    private static string GetDisplayName(IAetheryteEntry entry) {
        if (BetterTeleportWindow.Config is not {} config) return string.Empty;
        if (config.CustomNames.TryGetValue(entry.AetheryteId, out var customName)) return $"{customName} ({entry.AetheryteName})";

        return entry.AetheryteName.ToString();
    }
}
