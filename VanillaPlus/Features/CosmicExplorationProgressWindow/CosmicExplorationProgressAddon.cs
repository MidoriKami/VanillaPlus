using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public class CosmicExplorationProgressAddon : NativeAddon {
    private readonly List<DatasetNode> datasetNodes = [];
    public ResNode? ContentNode;
    private bool watchHud;

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

        addon->Flags1C8 = 0x100001; // Properly allow ESC-closing.
        watchHud = true;
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        watchHud = false;
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        // This behavior is a bit spicy- in general we don't want long-lived custom addons. This particular
        // addon is limited to only exist in WKS zones so this is reasonably safe.
        // Seriously, please don't do this- there are real risks to having too many long-lived custom addons. Those are
        // generally best-suited as overlays instead.

        // If we don't set this to false in OnHide, for some reason the addon never gets cleaned up out of AtkUnitManager
        // I tried figuring out why, but I couldn't find any errors that would indicate why it happened.
        if (!watchHud) return; 
        
        var hud = RaptureAtkUnitManager.Instance()->GetAddonByName("WKSHud");
        if (hud == null) return;
        addon->IsVisible = hud->IsVisible;
    }

    public void UpdateProgress(List<CosmicExplorationProgressWindow.Progress> progress) {
        // This churns the nodes, but this UI only redraws when you get new dataset progress or change jobs
        // so the cost is minimal. As a bonus, if we suddenly get more progress types than expected (say in 7.51),
        // the UI still works- it'll just overflow a little until we fix the UI height.
        foreach (var node in datasetNodes) {
            node.DetachNode();
            node.Dispose();
        }

        datasetNodes.Clear();
        var y = 0.0f;
        foreach (var p in progress) {
            var dn = new DatasetNode(ContentSize.X);
            dn.Y = y;
            y += dn.Height;
            dn.AddNodeFlags(NodeFlags.AnchorTop);
            dn.UpdateData(p);
            dn.AttachNode(ContentNode);
            datasetNodes.Add(dn);
        }
    }
}
