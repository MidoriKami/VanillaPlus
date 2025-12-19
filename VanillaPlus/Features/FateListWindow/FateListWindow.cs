using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.FateListWindow;

public class FateListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_FateListWindow"),
        Description = Strings("ModificationDescription_FateListWindow"),
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Now sorts by time remaining"),
            new ChangeLogInfo(3, "Added '/fatelist' command to open window"),
        ],
    };

    private NodeListAddon? addonFateList;
    
    public override string ImageName => "FateListWindow.png";

    public override void OnEnable() {
        addonFateList = new NodeListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "FateList",
            Title = Strings("FateListWindow_Title"),
            OpenCommand = "/fatelist",
            UpdateListFunction = UpdateList,
        };

        addonFateList.Initialize();

        OpenConfigAction = addonFateList.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonFateList?.Dispose();
        addonFateList = null;
    }

    private static bool UpdateList(VerticalListNode listNode, bool isOpening) {
        var validFates = Services.FateTable.Where(fate => fate is { State: FateState.Running or FateState.Preparation }).ToList();
        var listChanged = listNode.SyncWithListData(validFates, node => node.Fate, data => new FateEntryNode {
            Size = new Vector2(listNode.Width, 53.0f),
            Fate = data,
        });

        if (listChanged) {
            listNode.ReorderNodes(Comparison);
        }

        foreach (var fateNode in listNode.GetNodes<FateEntryNode>()) {
            fateNode.Update();
        }

        return listChanged;
    }

    private static int Comparison(NodeBase x, NodeBase y) {
        if (x is not FateEntryNode left || y is not FateEntryNode right) return 0;
        
        return left.Fate.TimeRemaining.CompareTo(right.Fate.TimeRemaining);
    }
}
