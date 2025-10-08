using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay;

public unsafe class CurrencyNode : SimpleComponentNode {
    private IconImageNode iconImageNode;
    private CounterNode countNode;

    public CurrencyNode() {
        iconImageNode = new IconImageNode {
            IsVisible = true,
            FitTexture = true,
        };
        System.NativeController.AttachNode(iconImageNode, this);

        countNode = new CounterNode {
            IsVisible = true,
            NumberWidth = 10,
            CommaWidth = 8,
            SpaceWidth = 6,
            TextAlignment = AlignmentType.Right,
            CounterWidth = 104.0f,
            Font = CounterFont.MoneyFont,
        };
        System.NativeController.AttachNode(countNode, this);

        BuildTimelines();
    }

    private void BuildTimelines() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 120)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(61, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());
        
        countNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .AddFrame(30, scale: new Vector2(1.075f, 1.075f), addColor: new Vector3(100.0f, 0.0f, 0.0f))
            .AddFrame(60, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .EndFrameSet()
            .BeginFrameSet(61, 120)
            .AddFrame(61, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .EndFrameSet()
            .Build());
    }

    public required CurrencySettings Currency {
        get;
        set {
            field = value;

            iconImageNode.IconId = Services.DataManager.GetExcelSheet<Item>().GetRow(value.ItemId).Icon;

            if (value.IconReversed) {
                iconImageNode.Position = new Vector2(0.0f, 0.0f);
                iconImageNode.Size = new Vector2(36.0f, 36.0f);

                countNode.TextAlignment = AlignmentType.Left;
                countNode.Position = new Vector2(iconImageNode.X + iconImageNode.Width, 8.0f);
                countNode.Size = new Vector2(128.0f, 22.0f);
            }
            else {
                countNode.TextAlignment = AlignmentType.Right;
                countNode.Position = new Vector2(0.0f, 8.0f);
                countNode.Size = new Vector2(128.0f, 22.0f);
                
                iconImageNode.Position = new Vector2(countNode.X + countNode.Width, 0.0f);
                iconImageNode.Size = new Vector2(36.0f, 36.0f);
            }

            countNode.Origin = countNode.Size / 2.0f;
        }
    }

    public void UpdateValues() {
        if (!Services.ClientState.IsLoggedIn) return;

        var inventoryCount = InventoryManager.Instance()->GetInventoryItemCount(Currency.ItemId);

        countNode.Number = inventoryCount;

        var isLowWarning = Currency.EnableLowLimit && inventoryCount < Currency.LowLimit;
        var isHighWarning = Currency.EnableHighLimit && inventoryCount > Currency.HighLimit;

        if (isLowWarning || isHighWarning) {
            Timeline?.PlayAnimation(1);
        }
        else {
            Timeline?.PlayAnimation(2);
        }
    }
}
