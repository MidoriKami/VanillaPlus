using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;

namespace VanillaPlus.Features.AdditionalHotbarSlots;

public sealed class HotbarOverlayNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    protected override unsafe void OnUpdate() {
        var hotbarModule = RaptureHotbarModule.Instance();
        if (hotbarModule is null) return;

        fixed (RaptureHotbarModule.HotbarSlot* data = &hotbarData)
        fixed (HotbarUiIntermediate* state = &hotbarState)
        {
            RaptureHotbarModule.HotbarSlotType outType;
            uint outActionId;
            ushort unkC4;

            RaptureHotbarModule.GetSlotAppearance(&outType, &outActionId, &unkC4, hotbarModule, data);
            hotbarData.ApparentActionId = outActionId;
            hotbarData.ApparentSlotType = outType;

            AdditionalHotbarSlots.UpdateSlotData?.Invoke(RaptureHotbarModule.Instance(), data, state);

            hotbarSlot.IconId = hotbarState.IconId;

            hotbarSlot.ShowResourceCost = hotbarState.CostType is 2;
            hotbarSlot.ResourceCost = hotbarState.CostValue;

            hotbarSlot.ShowChargeCount = hotbarState.CostType is 0;
            hotbarSlot.ChargeCount = hotbarState.CurrentCharges;
            hotbarSlot.ChargePercent = hotbarState.ChargePercent / 100.0f;

            hotbarSlot.ShowCooldownSeconds = hotbarState.CooldownSeconds is not 0;
            hotbarSlot.CooldownSeconds = hotbarState.CooldownSeconds;

            hotbarSlot.ShowCooldownPercent = hotbarState.CooldownPercent is not 0;
            hotbarSlot.CooldownPercent = hotbarState.CooldownPercent / 100.0f;
        }
    }

    public HotbarOverlayNode() {
        hotbarSlot = new HotbarNode {
            AcceptedType = DragDropType.Action,
            IsClickable = true,
            OnRollOver = OnRollOver,
            OnRollOut = OnRollOut,
            OnPayloadAccepted = OnPayloadAccepted,
            OnClicked = OnClicked,
            OnDiscard = OnDiscard,
        };
        hotbarSlot.AttachNode(this);

        Size = new Vector2(44.0f, 44.0f);

        hotbarSlot.Payload.Type = DragDropType.Action;
        hotbarSlot.Payload.Int2 = 125;

        hotbarData = new RaptureHotbarModule.HotbarSlot();
        hotbarData.Set(RaptureHotbarModule.HotbarSlotType.Action, 125);

        VanillaPlus.PluginInterface.UiBuilder.Draw += UiBuilderOnDraw;
    }

    protected override void Dispose(bool isNativeDestructor) {
        if (IsDisposed) return;

        base.Dispose(isNativeDestructor);

        VanillaPlus.PluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;
    }

    private void UiBuilderOnDraw() {
        ImGui.Text($"Available1: {hotbarState.ActionAvailable1}");
        ImGui.Text($"Available2: {hotbarState.ActionAvailable2}");
        ImGui.Text($"ActionId: {hotbarState.ActionId}");
        ImGui.Text($"TargetSatisfied: {hotbarState.ActionTargetSatisfied}");
        ImGui.Text($"ChargePercent: {hotbarState.ChargePercent}");
        ImGui.Text($"CooldownMode: {hotbarState.CooldownMode}");
        ImGui.Text($"CooldownPercent: {hotbarState.CooldownPercent}");
        ImGui.Text($"CooldownSeconds: {hotbarState.CooldownSeconds}");
        ImGui.Text($"CostDisplayMode: {hotbarState.CostDisplayMode}");
        ImGui.Text($"CostType: {hotbarState.CostType}");
        ImGui.Text($"CostValue: {hotbarState.CostValue}");
        ImGui.Text($"CurrentCharges: {hotbarState.CurrentCharges}");
        ImGui.Text($"DrawAnts: {hotbarState.DrawAnts}");
        ImGui.Text($"IconId: {hotbarState.IconId}");
        ImGui.Text($"IntermediateActionType: {hotbarState.IntermediateActionType}");
        ImGui.Text($"LastChargePercent: {hotbarState.LastChargePercent}");
        ImGui.Text($"LastCooldownPercent: {hotbarState.LastCooldownPercent}");
        ImGui.Text($"Unk0x42: {hotbarState.Unk0x42}");
    }

    private void OnDiscard(DragDropNode node) {

    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        hotbarSlot.Size = Size;
        hotbarSlot.Position = new Vector2(0.0f, 0.0f);
    }

    private void OnRollOver(DragDropNode node) {
        switch (node.Payload.Type) {
            case DragDropType.Action:
                ActionTooltip = (uint) node.Payload.Int2;
                ShowTooltip();
                break;
        }
    }

    private void OnRollOut(DragDropNode node)
        => HideTooltip();

    private void OnPayloadAccepted(DragDropNode node, DragDropPayload payload) {
        IPluginLog.Get().Debug($"Type: {payload.Type}, Int2: {payload.Int2}");

        node.Payload.Type = payload.Type;
        node.Payload.Int2 = payload.Int2;

        var slotType = payload.Type switch {
            DragDropType.Action => RaptureHotbarModule.HotbarSlotType.Action,
            _ => throw new ArgumentOutOfRangeException(nameof(node),  nameof(payload.Type)),
        };

        hotbarData.Set(slotType, (uint) payload.Int2);
    }

    private unsafe void OnClicked(DragDropNode node) {
        var hotbarModule = RaptureHotbarModule.Instance();

        switch (node.Payload.Type) {
            case DragDropType.Action:
                hotbarModule->ScratchSlot.Set(RaptureHotbarModule.HotbarSlotType.Action, (uint) node.Payload.Int2);
                hotbarModule->ExecuteSlot(&hotbarModule->ScratchSlot);
                break;
        }
    }

    private readonly HotbarNode hotbarSlot;
    private RaptureHotbarModule.HotbarSlot hotbarData;
    private HotbarUiIntermediate hotbarState;
}
