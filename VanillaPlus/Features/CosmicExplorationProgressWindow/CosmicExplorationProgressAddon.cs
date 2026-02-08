using System.Collections.Generic;
using System.Numerics;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public class CosmicExplorationProgressAddon : NativeAddon {
    private readonly List<DatasetNode> datasetNodes = [];
    public ResNode? ContentNode;

    public CosmicExplorationProgressAddon() {
        CreateWindowNode = () => new CosmicExplorationWindowNode();
    }

    public void Initialize() {
        // Padding is handled elsewhere, so don't add any here.
        ContentPadding = Vector2.Zero;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        ContentNode = new ResNode {
            Position = ContentStartPosition,
            Size = ContentSize - CosmicExplorationWindowNode.ContentPadding,
        };
        ContentNode.AddNodeFlags(NodeFlags.AnchorLeft, NodeFlags.AnchorTop, NodeFlags.AnchorRight,
            NodeFlags.AnchorBottom);
        ContentNode.AttachNode(this);
    }

    private unsafe void LayoutBars() {
        var lp = new ListPanel();
        lp.Ctor();
        lp.Width = (ushort)ContentSize.X;
        foreach (var node in datasetNodes) lp.AddNode(node.Node, 0, 0, (ushort)node.Height);
        lp.UpdateLayout();

        SetWindowSize(CosmicExplorationWindowNode.WindowSizeForContentSize(ContentSize.WithY(lp.Height)));

        lp.Dtor(0);
    }

    public void UpdateProgress(List<CosmicExplorationProgressWindow.Progress> progress) {
        foreach (var node in datasetNodes) {
            node.DetachNode();
            node.Dispose();
        }

        datasetNodes.Clear();

        foreach (var p in progress) {
            var dn = new DatasetNode(ContentSize.X);
            dn.AddNodeFlags(NodeFlags.AnchorTop);
            dn.UpdateData(p);
            dn.AttachNode(ContentNode);
            datasetNodes.Add(dn);
        }

        LayoutBars();
    }
}
