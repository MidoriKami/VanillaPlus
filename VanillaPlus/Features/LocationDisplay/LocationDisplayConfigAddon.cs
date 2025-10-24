using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.LocationDisplay;

public class LocationDisplayConfigAddon : NativeAddon {

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
            TextFlags = TextFlags.MultiLine,
            String = "Use the text box below to define how you want the text to be formatted.\n" +
                     "Use symbols {0} {1} {2} {3} {4} where you want the following values to be in the string\n\n" +
                     "{0} - Region (Ex. The Northern Empty)\n" +
                     "{1} - Territory (Ex. Old Sharlayan)\n" +
                     "{2} - Area (Ex. Archons Design)\n" +
                     "{3} - Sub-Area (Ex. Old Sharlayan Aetheryte Plaza)\n" +
                     "{4} - Housing Ward (Ex. Ward 14)",
        };
        AttachNode(instructionTextNode);

        infoBarEntryLayoutNode = new HorizontalListNode {
            Position = new Vector2(ContentStartPosition.X, instructionTextNode.Y + instructionTextNode.Height),
            Size = new Vector2(ContentSize.X, 30.0f),
            IsVisible = true,
        };
        AttachNode(infoBarEntryLayoutNode);

        entryLabelNode = new TextNode {
            Size = new Vector2(125.0f, 30.0f),
            String = "Info Bar Entry",
            AlignmentType = AlignmentType.Left,
            IsVisible = true,
        };
        infoBarEntryLayoutNode.AddNode(entryLabelNode);

        entryInputNode = new TextInputNode {
            Size = new Vector2(ContentSize.X - 125.0f - 125.0f, 30.0f),
            String = Config.FormatString,
            IsVisible = true,
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
            Size = new Vector2(125.0f, 30.0f),
            String = "Reset to Default",
            IsVisible = true,
            OnClick = () => {
                entryInputNode.IsError = false;
                entryInputNode.String = "{0}, {1}, {2}, {3}";
                Config.FormatString = "{0}, {1}, {2}, {3}";
                Config.Save();
            },
        };
        infoBarEntryLayoutNode.AddNode(resetEntryButtonNode);

        infoBarTooltipLayoutNode = new HorizontalListNode {
            Position = new Vector2(ContentStartPosition.X, infoBarEntryLayoutNode.Y + infoBarEntryLayoutNode.Height),
            Size = new Vector2(ContentSize.X, 30.0f),
            IsVisible = true,
        };
        AttachNode(infoBarTooltipLayoutNode);

        tooltipLabelNode = new TextNode {
            Size = new Vector2(125.0f, 30.0f),
            String = "Info Bar Tooltip",
            AlignmentType = AlignmentType.Left,
            IsVisible = true,
        };
        infoBarTooltipLayoutNode.AddNode(tooltipLabelNode);

        tooltipInputNode = new TextInputNode {
            Size = new Vector2(ContentSize.X - 125.0f - 125.0f, 30.0f),
            String = Config.TooltipFormatString,
            IsVisible = true,
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
            Size = new Vector2(125.0f, 30.0f),
            String = "Reset to Default",
            IsVisible = true,
            OnClick = () => {
                tooltipInputNode.IsError = false;
                tooltipInputNode.String = "{0}, {1}, {2}, {3}";
                Config.TooltipFormatString = "{0}, {1}, {2}, {3}";
                Config.Save();
            },
        };
        infoBarTooltipLayoutNode.AddNode(tooltipResetButtonNode);

        showInstanceNumberNode = new CheckboxNode {
            Size = new Vector2(ContentSize.X, 24.0f),
            Position = new Vector2(ContentStartPosition.X, infoBarTooltipLayoutNode.Y + infoBarTooltipLayoutNode.Height + 15.0f),
            String = "Show Instance Number",
            IsVisible = true,
            IsChecked = Config.ShowInstanceNumber,
            OnClick = newValue => {
                Config.ShowInstanceNumber = newValue;
                Config.Save();
            },
        };
        AttachNode(showInstanceNumberNode);

        showPreciseHousingLocationNode = new CheckboxNode {
            Size = new Vector2(ContentSize.X, 24.0f),
            Position = new Vector2(ContentStartPosition.X, showInstanceNumberNode.Y + showInstanceNumberNode.Height),
            String = "Show Precise Housing Location",
            IsVisible = true,
            IsChecked = Config.UsePreciseHousingLocation,
            OnClick = newValue => {
                Config.UsePreciseHousingLocation = newValue;
                Config.Save();
            },
        };
        AttachNode(showPreciseHousingLocationNode);
    }

    private static bool BracesMismatched(string formatString)
        => formatString.Count(c => c is '{') != formatString.Count(c => c is '}');
}
