using System;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.Timelines;

namespace VanillaPlus.Features.CommandPanelExpansion;

/// <summary>
/// A fully-resolved snapshot of how a filled slot should look, computed from game state by the feature
/// and pushed to <see cref="CommandPanelSlotPresenter"/>. It carries no FFXIVClientStructs hotbar types,
/// so the presentation layer stays purely about driving the node's visuals and knows nothing about
/// RaptureHotbarModule, ActionManager or the inventory.
/// </summary>
internal readonly struct ActionSlotVisualState
{
    /// <summary>The slot holds a macro (shows the macro corner marker).</summary>
    public bool IsMacro { get; init; }

    /// <summary>The command is currently not usable (icon drawn dimmed).</summary>
    public bool IsUnusable { get; init; }

    /// <summary>The action has a target that is out of range (red frame tint + "×").</summary>
    public bool OutOfRange { get; init; }

    /// <summary>A recast/cooldown sweep is active.</summary>
    public bool HasCooldown { get; init; }

    /// <summary>Cooldown recovery, 0 (just used) .. 1 (ready), mapped to the recast sweep parts.</summary>
    public float CooldownProgress { get; init; }

    /// <summary>The action is highlighted (animated marching-ants border).</summary>
    public bool Highlighted { get; init; }

    /// <summary>Bottom-right overlay text, or null/empty for no overlay.</summary>
    public string? QuantityText { get; init; }

    /// <summary>True when <see cref="QuantityText"/> is an item/gear-set count (white + edge) rather than a cost value (cream).</summary>
    public bool QuantityIsCount { get; init; }
}

/// <summary>
/// Drives a KamiToolKit <see cref="DragDropNode"/> to render like a native FFXIV hotbar action slot.
/// This is the presentation layer: it only writes to nodes and never reads game state - the feature
/// computes an <see cref="ActionSlotVisualState"/> and hands it here. Kept inside VanillaPlus (rather than
/// KamiToolKit) because the Command Panel is currently its only consumer.
/// </summary>
internal static class CommandPanelSlotPresenter
{
    // The native empty-slot frame texture (loaded non-themed so it matches the panel, which does
    // not use the darker themed "img06" variant that KamiToolKit would resolve by default).
    private const string SlotFrameTexturePath = "ui/uld/DragTargetA.tex";

    // The native slot-frame add colors: resting -50, hovered -34.
    private static readonly Vector3 RestingAddColor = new(-50.0f);
    private static readonly Vector3 HoverAddColor = new(-34.0f);

    // The action-icon frame add colors: white at rest, the native red tint when the action's target is
    // out of range. KamiToolKit's AddColor takes a normalised 0-1 vector (multiplied by 255 onto the
    // native short channels), so the native raw (+48 / -48 / -48) is expressed as 48/255 here.
    private static readonly Vector3 FrameNormalAddColor = Vector3.Zero;
    private static readonly Vector3 FrameOutOfRangeAddColor = new(48.0f / 255.0f, -48.0f / 255.0f, -48.0f / 255.0f);

    // The out-of-range indicator shown bottom-left, matching the native hotbar: the multiplication
    // sign × in red (text DE4040, outline 640000).
    private const string OutOfRangeIndicatorText = "×";
    private static readonly Vector4 OutOfRangeTextColor = new(0xDE / 255.0f, 0x40 / 255.0f, 0x40 / 255.0f, 1.0f);
    private static readonly Vector4 OutOfRangeOutlineColor = new(0x64 / 255.0f, 0x00, 0x00, 1.0f);

    // The bottom-right count text (#9): native renders item counts white with a hard edge outline, while
    // KamiToolKit's QuantityTextNode defaults to a cream colour with no edge (used for action cost values).
    private static readonly Vector4 ItemCountTextColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Vector4 DefaultQuantityTextColor = new(238.0f / 255.0f, 225.0f / 255.0f, 197.0f / 255.0f, 1.0f);

    // The marching-ants highlight (ui/uld/IconA_Frame.tex) animates by cycling parts 6..13.
    private const uint FirstAntsPartId = 6;
    private const uint LastAntsPartId = 13;

    // Native ant-border timing: advance one frame every 0.03s.
    private const long AntsFrameTimeMs = 30;

    // The recast sweep (ui/uld/IconA_Recast.tex) walks parts 1 (fully covered) -> 79 (clear) as the
    // action's cooldown elapses.
    private const uint FirstRecastPartId = 1;
    private const uint LastRecastPartId = 79;

    // Prototype toggle: drive the recast sweep via the game's own AtkComponentIcon.SetCooldownProgress
    // instead of hand-stepping IconA_Recast part ids. Disassembly of InitializeFromComponentData confirms
    // our KTK IconNode wires the component's internal Frame pointer (from Data.Nodes[1] = CooldownNode #16),
    // and the setter applies the CooldownImage child timeline synchronously on each call - so it works
    // without the game ticking our timeline. progress is the 0..1 recovery fraction (0 = just used / fully
    // covered, 1 = ready / cleared), identical to ActionSlotVisualState.CooldownProgress, so it maps straight
    // through with no inversion. Set false to fall back to manual part-stepping if the in-game sweep is wrong.
    private static readonly bool UseNativeCooldownSetter = true;

    /// <summary>
    /// Builds the native slot frame (background texture + hover/press timeline) on a freshly created node.
    /// </summary>
    public static void ApplyNativeSlotFrame(DragDropNode slot)
    {
        var background = slot.DragDropBackgroundNode;

        // KamiToolKit loads the slot frame with theme resolution, which swaps in the darker
        // "ui/uld/img##/DragTargetA.tex" variant when a UI color theme is active. The Command Panel
        // uses the plain, non-themed texture, so reload it without theme resolution.
        if (background is SimpleImageNode simpleBackground)
        {
            simpleBackground.LoadTexture(SlotFrameTexturePath, resolveTheme: false);
        }

        // Resting baseline in case the component timeline has not ticked yet.
        background.AddColor = RestingAddColor;

        // Replicate the native slot-frame timeline. The DragDrop component already drives the matching
        // hover/press labels (frame ranges 1-10 rest, 11-19 hover-in, 20-29 hover, 30-39 press,
        // 40-49, 50-59 hover-out), so the engine animates Add -50 <-> -34 just like the real slots.
        background.AddTimeline(new TimelineBuilder()
            .AddFrameSetWithFrame(1, 10, 1, Vector2.Zero, 255, RestingAddColor, new Vector3(100.0f))
            .BeginFrameSet(11, 19)
            .AddFrame(11, Vector2.Zero, 255, RestingAddColor, new Vector3(100.0f))
            .AddFrame(13, Vector2.Zero, 255, HoverAddColor, new Vector3(100.0f))
            .EndFrameSet()
            .AddFrameSetWithFrame(20, 29, 20, Vector2.Zero, 255, HoverAddColor, new Vector3(100.0f))
            .AddFrameSetWithFrame(30, 39, 30, Vector2.Zero, 178, RestingAddColor, new Vector3(50.0f))
            .AddFrameSetWithFrame(40, 49, 40, Vector2.Zero, 255, HoverAddColor, new Vector3(100.0f))
            .BeginFrameSet(50, 59)
            .AddFrame(50, Vector2.Zero, 255, HoverAddColor, new Vector3(100.0f))
            .AddFrame(52, Vector2.Zero, 255, RestingAddColor, new Vector3(100.0f))
            .EndFrameSet()
            .Build());
    }

    /// <summary>
    /// Brightens (or restores) an EMPTY slot's frame on hover. Empty DragDrop slots receive the hover
    /// event but their component does not play the frame-highlight timeline (it only animates while holding
    /// content), so we drive the add-color ourselves to match the native empty-slot hover.
    /// </summary>
    public static void SetEmptySlotHovered(DragDropNode node, bool hovered)
        => node.DragDropBackgroundNode.AddColor = hovered ? HoverAddColor : RestingAddColor;

    /// <summary>
    /// Drives a filled slot's visuals from the given <paramref name="state"/>. The quantity cache
    /// (<paramref name="appliedQuantityText"/> / <paramref name="appliedQuantityIsCount"/>) is owned by the
    /// caller's slot and lets the per-frame text re-write be skipped when nothing changed.
    /// </summary>
    public static void Apply(DragDropNode node, in ActionSlotVisualState state, ref string? appliedQuantityText, ref bool appliedQuantityIsCount)
    {
        var icon = node.IconNode;
        var extras = icon.IconExtras;

        icon.IsMacro = state.IsMacro;
        node.IsIconDisabled = state.IsUnusable;

        // These overlays are normally driven by the component's timelines, which the game does NOT run for
        // our manually-managed slots - so they would stay frozen in an "on" frame (a faint white wash, a
        // permanent hover-glow border, a stuck click-flash). Native keeps them hidden at rest.
        extras.TimelineImageNode.IsVisible = false;
        extras.HoveredBorderImageNode.IsVisible = false;
        extras.ClickFlashImageNode.IsVisible = false;

        // The icon frame is the CooldownNode's GlossyImageFrame (#18); a filled slot always shows it,
        // matching the native action-icon's resting white border, tinted red when out of range.
        var cooldownNode = extras.CooldownNode;
        cooldownNode.IsVisible = true;
        cooldownNode.AddColor = state.OutOfRange ? FrameOutOfRangeAddColor : FrameNormalAddColor;

        // Recast sweep. Either hand the 0..1 progress to the game's native cooldown setter (it drives the
        // CooldownImage child timeline exactly like a real hotbar slot) or fall back to manual part-stepping.
        ApplyCooldownSweep(node, cooldownNode, state.HasCooldown, state.CooldownProgress);

        // The out-of-range "×" reuses the icon's native ResourceCostTextNode (#8), already positioned like
        // the native hotbar; we only colour it red and toggle it.
        var indicator = extras.ResourceCostTextNode;
        if (state.OutOfRange)
        {
            indicator.String = OutOfRangeIndicatorText;
            indicator.FontSize = 12;
            indicator.AlignmentType = AlignmentType.Left;
            indicator.TextColor = OutOfRangeTextColor;
            indicator.TextOutlineColor = OutOfRangeOutlineColor;

            // The native cost/× text is rendered bold with a hard edge outline (KamiToolKit's TextNode
            // defaults to Emboss only, which is why our "×" looked thinner). Axis has no separate bold
            // font - boldness is the TextFlags.Bold bit - so set it explicitly to match the native weight.
            indicator.TextFlags = TextFlags.Bold | TextFlags.Edge;
        }
        indicator.IsVisible = state.OutOfRange;

        // Marching-ants highlight: the component does not auto-play the part cycle for our slots, so step
        // through parts 6..13 ourselves on the native 0.03s-per-frame cadence.
        var antsNode = extras.AntsNode;
        if (state.Highlighted)
        {
            var span = LastAntsPartId - FirstAntsPartId + 1;
            var frame = (uint)(Environment.TickCount64 / AntsFrameTimeMs % span);
            antsNode.IsVisible = true;
            antsNode.AntsImageNode.PartId = FirstAntsPartId + frame;
        }
        else
        {
            antsNode.IsVisible = false;
        }

        ApplyQuantity(node, state.QuantityText, state.QuantityIsCount, ref appliedQuantityText, ref appliedQuantityIsCount);
    }

    /// <summary>
    /// Resets an empty/invalid slot to its resting (blank) state.
    /// </summary>
    public static void Clear(DragDropNode node, ref string? appliedQuantityText)
    {
        var icon = node.IconNode;
        var extras = icon.IconExtras;

        icon.IsMacro = false;
        node.IsIconDisabled = false;

        var cooldownNode = extras.CooldownNode;
        cooldownNode.IsVisible = false;
        cooldownNode.CooldownImage.IsVisible = false;
        cooldownNode.AddColor = FrameNormalAddColor;

        extras.TimelineImageNode.IsVisible = false;
        extras.HoveredBorderImageNode.IsVisible = false;
        extras.ClickFlashImageNode.IsVisible = false;

        extras.ResourceCostTextNode.IsVisible = false;
        extras.AntsNode.IsVisible = false;

        ClearQuantity(node, ref appliedQuantityText);
    }

    private static void ApplyCooldownSweep(DragDropNode node, CooldownNode cooldownNode, bool hasCooldown, float progress)
    {
        if (UseNativeCooldownSetter)
        {
            // Drive the component's internal Frame -> CooldownImage timeline natively. progress is the
            // recovery fraction (0 = full sweep, 1 = cleared); 1.0f clears the sweep when ready.
            unsafe
            {
                node.IconNode.Component->SetCooldownProgress(hasCooldown ? Math.Clamp(progress, 0.0f, 1.0f) : 1.0f);
            }

            cooldownNode.CooldownImage.IsVisible = hasCooldown;
            return;
        }

        // Manual fallback: hand-step the IconA_Recast part id from the progress fraction.
        if (hasCooldown)
        {
            var partId = FirstRecastPartId + (uint)MathF.Round(progress * (LastRecastPartId - FirstRecastPartId));
            cooldownNode.CooldownImage.IsVisible = true;
            cooldownNode.CooldownImage.PartId = Math.Clamp(partId, FirstRecastPartId, LastRecastPartId);
        }
        else
        {
            cooldownNode.CooldownImage.IsVisible = false;
        }
    }

    private static void ApplyQuantity(DragDropNode node, string? text, bool isCount, ref string? appliedText, ref bool appliedIsCount)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ClearQuantity(node, ref appliedText);
            return;
        }

        // Skip the (allocating, unsafe) re-write when the overlay text and style are unchanged.
        if (text == appliedText && isCount == appliedIsCount) return;

        appliedText = text;
        appliedIsCount = isCount;

        node.QuantityString = text;

        unsafe
        {
            var iconComponent = node.IconNode.Component;
            if (iconComponent->QuantityText is not null)
            {
                using var builder = new RentedSeStringBuilder();
                iconComponent->QuantityText->SetText(builder.Builder.Append(text).GetViewAsSpan());
                iconComponent->QuantityText->ToggleVisibility(true);
            }
        }

        var quantityNode = node.IconNode.IconExtras.QuantityTextNode;
        quantityNode.String = text;

        // Match the native count text: white with a hard edge outline for item/gear-set counts, otherwise
        // the cream/no-edge default used for action cost values.
        quantityNode.TextColor = isCount ? ItemCountTextColor : DefaultQuantityTextColor;
        quantityNode.TextFlags = isCount ? TextFlags.Edge : TextFlags.None;

        quantityNode.IsVisible = true;
        node.IconNode.IconExtras.IsVisible = true;
    }

    private static void ClearQuantity(DragDropNode node, ref string? appliedText)
    {
        if (appliedText is null) return;
        appliedText = null;

        node.QuantityString = default;

        unsafe
        {
            var iconComponent = node.IconNode.Component;
            if (iconComponent->QuantityText is not null)
            {
                iconComponent->QuantityText->SetText(""u8);
                iconComponent->QuantityText->ToggleVisibility(false);
            }
        }

        var quantityNode = node.IconNode.IconExtras.QuantityTextNode;
        quantityNode.String = default;
        quantityNode.IsVisible = false;
    }
}
