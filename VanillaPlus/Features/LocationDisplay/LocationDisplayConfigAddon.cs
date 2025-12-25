using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.LocationDisplay;

public class LocationDisplayConfigAddon : NativeAddon {

    private const float ResetButtonWidth = 190.0f;

    private TextNode? instructionTextNode;

    private HorizontalListNode? infoBarEntryLayoutNode;
    private TextNode? entryLabelNode;
    private TextInputNode? entryInputNode;
    private TextButtonNode? resetEntryButtonNode;

    private HorizontalListNode? infoBarTooltipLayoutNode;
    private TextNode? tooltipLabelNode;
    private TextInputNode? tooltipInputNode;
    private TextButtonNode? tooltipResetButtonNode;

    private CheckboxNode? showInstanceNumberNode;
    private CheckboxNode? showPreciseHousingLocationNode;
    
    public required LocationDisplayConfig Config { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SetWindowSize(560.0f, 335.0f);
        
        instructionTextNode = new TextNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, 150.0f),
            LineSpacing = 16,
            TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
            String = Strings("Label_LocationDisplayInstructions"),
        };
        instructionTextNode.AttachNode(this);

        infoBarEntryLayoutNode = new HorizontalListNode {
            Position = new Vector2(ContentStartPosition.X, instructionTextNode.Y + instructionTextNode.Height),
            Size = new Vector2(ContentSize.X, 30.0f),
        };
        infoBarEntryLayoutNode.AttachNode(this);

        entryLabelNode = new TextNode {
            Size = new Vector2(125.0f, 30.0f),
            String = Strings("LocationDisplay_InfoBarEntryLabel"),
            AlignmentType = AlignmentType.Left,
        };
        infoBarEntryLayoutNode.AddNode(entryLabelNode);

        entryInputNode = new TextInputNode {
            Size = new Vector2(ContentSize.X - 125.0f - ResetButtonWidth, 30.0f),
            String = Config.FormatString,
            OnInputReceived = newString => {
                if (!BracesMismatched(newString.ToString())) {
                    Config.FormatString = newString.ToString();
                    Config.Save();
                    entryInputNode!.IsError = false;
                }
                else {
                    entryInputNode!.IsError = true;
                }
            },
        };
        infoBarEntryLayoutNode.AddNode(entryInputNode);

        resetEntryButtonNode = new TextButtonNode {
            Size = new Vector2(ResetButtonWidth, 30.0f),
            String = Strings("LocationDisplay_ResetButton"),
            OnClick = () => {
                entryInputNode.IsError = false;
                entryInputNode.String = Strings("LocationDisplay_DefaultEntryFormat");
                Config.FormatString = Strings("LocationDisplay_DefaultEntryFormat");
                Config.Save();
            },
        };
        infoBarEntryLayoutNode.AddNode(resetEntryButtonNode);

        infoBarTooltipLayoutNode = new HorizontalListNode {
            Position = new Vector2(ContentStartPosition.X, infoBarEntryLayoutNode.Y + infoBarEntryLayoutNode.Height),
            Size = new Vector2(ContentSize.X, 30.0f),
        };
        infoBarTooltipLayoutNode.AttachNode(this);

        tooltipLabelNode = new TextNode {
            Size = new Vector2(125.0f, 30.0f),
            String = Strings("LocationDisplay_InfoBarTooltipLabel"),
            AlignmentType = AlignmentType.Left,
        };
        infoBarTooltipLayoutNode.AddNode(tooltipLabelNode);

        tooltipInputNode = new TextInputNode {
            Size = new Vector2(ContentSize.X - 125.0f - ResetButtonWidth, 30.0f),
            String = Config.TooltipFormatString,
            OnInputReceived = newString => {
                if (!BracesMismatched(newString.ToString())) {
                    Config.TooltipFormatString = newString.ToString();
                    Config.Save();
                    tooltipInputNode!.IsError = false;
                }
                else {
                    tooltipInputNode!.IsError = true;
                }
            },
        };
        infoBarTooltipLayoutNode.AddNode(tooltipInputNode);

        tooltipResetButtonNode = new TextButtonNode {
            Size = new Vector2(ResetButtonWidth, 30.0f),
            String = Strings("LocationDisplay_ResetButton"),
            OnClick = () => {
                tooltipInputNode.IsError = false;
                tooltipInputNode.String = Strings("LocationDisplay_DefaultTooltipFormat");
                Config.TooltipFormatString = Strings("LocationDisplay_DefaultTooltipFormat");
                Config.Save();
            },
        };
        infoBarTooltipLayoutNode.AddNode(tooltipResetButtonNode);

        showInstanceNumberNode = new CheckboxNode {
            Size = new Vector2(ContentSize.X, 24.0f),
            Position = new Vector2(ContentStartPosition.X, infoBarTooltipLayoutNode.Y + infoBarTooltipLayoutNode.Height + 15.0f),
            String = Strings("LocationDisplay_ShowInstanceNumber"),
            IsChecked = Config.ShowInstanceNumber,
            OnClick = newValue => {
                Config.ShowInstanceNumber = newValue;
                Config.Save();
            },
        };
        showInstanceNumberNode.AttachNode(this);

        showPreciseHousingLocationNode = new CheckboxNode {
            Size = new Vector2(ContentSize.X, 24.0f),
            Position = new Vector2(ContentStartPosition.X, showInstanceNumberNode.Y + showInstanceNumberNode.Height),
            String = Strings("LocationDisplay_ShowPreciseHousing"),
            IsChecked = Config.UsePreciseHousingLocation,
            OnClick = newValue => {
                Config.UsePreciseHousingLocation = newValue;
                Config.Save();
            },
        };
        showPreciseHousingLocationNode.AttachNode(this);
    }

    private static bool BracesMismatched(string formatString)
        => formatString.Count(c => c is '{') != formatString.Count(c => c is '}');
}
