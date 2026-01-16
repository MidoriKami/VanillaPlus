using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.Enums;
using VanillaPlus.Features.ActionHighlight.Nodes;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightAddon : NativeAddon {
    private ModifyListNode<ActionCategory, ActionCategoryListItemNode>? selectionListNode;
    private ActionHighlightConfigNode? configNode;
    private TextNode? nothingSelectedTextNode;

    private readonly List<ActionCategory> allCategories = [];

    public ActionHighlightConfig? Config { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        var combatJobs = Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>()
            .Where(classJob => classJob.JobIndex > 0 && classJob.Role != 0)
            .Select(classJob => new ActionCategory(ActionCategoryType.Job, classJob))
            .ToList();

        allCategories.Add(new ActionCategory(ActionCategoryType.General));
        allCategories.AddRange(combatJobs);
        allCategories.Add(new ActionCategory(ActionCategoryType.Role));

        allCategories.Sort(ActionCategory.Compare);

        selectionListNode = new ModifyListNode<ActionCategory, ActionCategoryListItemNode> {
            Position = ContentStartPosition,
            Size = ContentSize with { X = 250.0f },
            Options = allCategories,
            SortOptions = [ "Role Priority", "Alphabetical" ],
            SelectionChanged = OnSelectionChanged,
            ItemComparer = (left, right, mode) => mode switch {
                "Alphabetical" => string.CompareOrdinal(left.Name, right.Name),
                _ => ActionCategory.Compare(left, right),
            },
            IsSearchMatch = (data, search) => data.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase),
        };
        selectionListNode.AttachNode(this);

        new VerticalLineNode {
            Position = ContentStartPosition + new Vector2(250.0f + 8.0f, 0.0f),
            Size = ContentSize with { X = 4.0f },
        }.AttachNode(this);

        nothingSelectedTextNode = new TextNode {
            Position = ContentStartPosition + new Vector2(250.0f + 16.0f, 0.0f),
            Size = ContentSize - new Vector2(250.0f + 16.0f, 0.0f),
            AlignmentType = AlignmentType.Center,
            String = Strings.SelectionPrompt,
        };
        nothingSelectedTextNode.AttachNode(this);

        configNode = new ActionHighlightConfigNode {
            Position = ContentStartPosition + new Vector2(250.0f + 16.0f, 0.0f),
            Size = ContentSize - new Vector2(250.0f + 16.0f, 0.0f),
            IsVisible = false,
        };

        if (Config is not null) {
            configNode.SetConfig(Config);
        }

        configNode.AttachNode(this);
    }

    private void OnSelectionChanged(ActionCategory? category) {
        if (configNode is null) return;

        configNode.ConfigurationOption = category;
        configNode.IsVisible = category is not null;
        nothingSelectedTextNode?.IsVisible = category is null;
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        allCategories.Clear();
        base.OnFinalize(addon);
    }
}
