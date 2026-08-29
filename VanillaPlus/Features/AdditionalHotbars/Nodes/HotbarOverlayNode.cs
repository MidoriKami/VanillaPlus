using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using VanillaPlus.Features.AdditionalHotbars.Config;

namespace VanillaPlus.Features.AdditionalHotbars.Nodes;

public sealed class HotbarOverlayNode : OverlayNode {
    public readonly HotbarConfig Config;

    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    protected override unsafe void OnUpdate() {
        IsVisible = Config.IsEnabled;

        if (Config.NeedsRebuildLayout) {
            RebuildLayout();
            Config.NeedsRebuildLayout = false;
        }

        if (Config.NeedsRecalcLayout) {
            RecalcLayout();
            Config.NeedsRecalcLayout = false;
        }

        EnableMoving = Config.MovingEnabled;

        IGameConfig.Get().UiConfig.TryGetBool("HotbarLock", out var isHotbarLocked);
        IGameConfig.Get().UiControl.TryGetBool("HotbarEmptyVisible", out var isHotbarEmptyVisible);

        var configAddonExists = IGameGui.Get().GetAddonByName("AdditionalHotbarsConfig");

        ref var dragDropManager = ref AtkStage.Instance()->DragDropManager;
        var isDragging = dragDropManager is { IsDragging: true, MouseMoved: true };

        foreach (var node in hotbarNodes) {
            node.IsDraggable = !isHotbarLocked;
            node.IsBackgroundShown = isHotbarEmptyVisible || isDragging || configAddonExists.IsVisible;

            node.Update();
        }
    }

    private void RebuildLayout() {
        foreach (var node in hotbarNodes) {
            node.Dispose();
        }
        hotbarNodes.Clear();

        foreach (var colum in Enumerable.Range(0, Config.Width)) {
            foreach (var row in Enumerable.Range(0, Config.Height)) {
                var newHotbarNode = new HotbarNode {
                    Position = new Vector2(8.0f, 8.0f) +
                               new Vector2(44.0f * colum, 44.0f * row) +
                               new Vector2(Config.HorizontalSpacing * colum, Config.VerticalSpacing * row),
                    Size = new Vector2(44.0f, 44.0f),
                    IsClickable = true,
                };

                hotbarNodes.Add(newHotbarNode);
                var nodeIndex = hotbarNodes.IndexOf(newHotbarNode);
                newHotbarNode.OnPayloadAccepted += (_, payload) => OnPayloadAccepted(nodeIndex, payload);
                newHotbarNode.OnDiscard += _ => OnPayloadDiscard(nodeIndex);

                var configForSlot = Config.Slots[nodeIndex];

                newHotbarNode.SetSlot(configForSlot.DragDropType, configForSlot.Id);

                newHotbarNode.AttachNode(this);
            }
        }

        Size = new Vector2(
            Config.Width * (44.0f + Config.HorizontalSpacing) + 16.0f,
            Config.Height * (44.0f + Config.VerticalSpacing) + 16.0f
        );
    }

    private void RecalcLayout() {
        foreach (var colum in Enumerable.Range(0, Config.Width)) {
            foreach (var row in Enumerable.Range(0, Config.Height)) {
                hotbarNodes[colum + row * Config.Width].Position =
                    new Vector2(8.0f, 8.0f) +
                    new Vector2(44.0f * colum, 44.0f * row) +
                    new Vector2(Config.HorizontalSpacing * colum, Config.VerticalSpacing * row);
            }
        }

        Size = new Vector2(
            Config.Width * (44.0f + Config.HorizontalSpacing) + 16.0f,
            Config.Height * (44.0f + Config.VerticalSpacing) + 16.0f
        );
    }

    private void OnPayloadAccepted(int index, DragDropPayload payload) {
        Config.Slots[index] = new SlotData {
            DragDropType = payload.Type,
            Id = (uint) payload.Int2,
        };
        mainConfig.Save();
    }

    private void OnPayloadDiscard(int nodeIndex) {
        Config.Slots[nodeIndex].DragDropType = DragDropType.Nothing;
        Config.Slots[nodeIndex].Id = 0;

        mainConfig.Save();
    }

    public HotbarOverlayNode(AdditionalHotbarsConfig mainConfig, HotbarConfig config) {
        this.mainConfig = mainConfig;
        Config = config;

        OnMoveComplete = _ => {
            Config.Position = Position;
        };
    }

    private readonly List<HotbarNode> hotbarNodes = [];
    private readonly AdditionalHotbarsConfig mainConfig;
}
