using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

namespace VanillaPlus.Features.GearSetReorderButtons.Nodes;

public unsafe class GearSetListReorderButtonNode : SimpleComponentNode {
    private int GearSetId { get; set; }

    private readonly CircleButtonNode upButtonNode;
    private readonly CircleButtonNode downButtonNode;

    public GearSetListReorderButtonNode() {
        upButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.UpArrow,
            Size = new Vector2(32.0f, 32.0f),
            OnClick = () => AgentGearSet.Instance()->MoveSetUp(GearSetId),
            TextTooltip = "Move gear set up.",
            IsEnabled = false,
        };

        downButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.ArrowDown,
            Size = new Vector2(32.0f, 32.0f),
            Position = new Vector2(28.0f, 0.0f),
            OnClick = () => AgentGearSet.Instance()->MoveSetDown(GearSetId),
            TextTooltip = "Move gear set down.",
            IsEnabled = false,
        };

        upButtonNode.AttachNode(this);
        downButtonNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        upButtonNode.Size = new Vector2(Height, Height);

        downButtonNode.Size = new Vector2(Height, Height);
        downButtonNode.Position = new Vector2(Width - Height, 0.0f);
    }

    public void Update(GearSetListListItem listItemData) {
        GearSetId = listItemData.GearSetId;

        upButtonNode.IsEnabled = listItemData.ItemIndex > 0;
        downButtonNode.IsEnabled = listItemData.ItemIndex < RaptureGearsetModule.Instance()->NumGearsets - 1;

        IsVisible = listItemData.IsChecked;
    }
}
