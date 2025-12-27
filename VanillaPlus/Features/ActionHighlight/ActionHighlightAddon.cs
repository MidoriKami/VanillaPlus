using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightAddon : NativeAddon {

    private ModifyListNode<ClassJobWrapper>? selectionListNode;
    private VerticalLineNode? separatorLine;
    private ActionHighlightConfigNode? configNode;
    private TextNode? nothingSelectedTextNode;

    public ActionHighlightConfig? Config { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        var classJobs = Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>()
            .Where(classJob => classJob.JobIndex > 0)
            .Select(classJob => new ClassJobWrapper(classJob))
            .ToList();

        classJobs.Add(ClassJobWrapper.RoleActions);
        classJobs.Add(ClassJobWrapper.GeneralSettings);

        classJobs.Sort((left, right) => left.Compare(right, ""));

        selectionListNode = new ModifyListNode<ClassJobWrapper> {
            Position = ContentStartPosition,
            Size = new Vector2(250.0f, ContentSize.Y),
            SelectionOptions = classJobs,
            OnOptionChanged = OnOptionChanged,
        };
        selectionListNode.AttachNode(this);

        separatorLine = new VerticalLineNode {
            Position = ContentStartPosition + new Vector2(250.0f + 8.0f, 0.0f),
            Size = new Vector2(4.0f, ContentSize.Y),
        };
        separatorLine.AttachNode(this);

        nothingSelectedTextNode = new TextNode {
            Position = ContentStartPosition + new Vector2(250.0f + 16.0f, 0.0f),
            Size = ContentSize - new Vector2(250.0f + 16.0f, 0.0f),
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            String = Strings.SelectionPrompt,
            TextColor = ColorHelper.GetColor(1),
        };
        nothingSelectedTextNode.AttachNode(this);

        configNode = new ActionHighlightConfigNode {
            Position = ContentStartPosition + new Vector2(250.0f + 16.0f, 0.0f),
            Size = ContentSize - new Vector2(250.0f + 16.0f, 0.0f),
            IsVisible = false,
        };
        if (Config != null) {
            configNode.SetConfig(Config);
        }
        configNode.AttachNode(this);
    }

    private void OnOptionChanged(ClassJobWrapper? newOption) {
        if (configNode is null) return;

        configNode.IsVisible = newOption is not null;
        nothingSelectedTextNode?.IsVisible = newOption is null;

        configNode.ConfigurationOption = newOption;
    }
}
