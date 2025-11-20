using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.MissingJobStoneLockout;

public unsafe class MissingJobStoneLockout : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Missing Job Stone Lockout",
        Description = "Prevents queuing for a duty while you are missing a jobstone.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami", "KazWolfe" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        Tags = [ "Duty Finder" ],
    };

    private AddonController<AddonContentsFinder>? contentsFinderController;
    private ResNode? animationContainer;
    private TextNode? warningTextNode;
    private bool suppressed;
    private int clickCount;

    public override string ImageName => "MissingJobStone.png";

    public override void OnEnable() {
        contentsFinderController = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinderController.OnAttach += AttachNodes;
        contentsFinderController.OnUpdate += OnUpdate;
        contentsFinderController.OnRefresh += OnUpdate;
        contentsFinderController.OnDetach += DetachNodes;
        contentsFinderController.Enable();
    }

    public override void OnDisable() {
        contentsFinderController?.Dispose();
        contentsFinderController = null;
    }

    private void AttachNodes(AddonContentsFinder* addon) {
        suppressed = false;
        clickCount = 0;
        
        var joinButtonNode = addon->JoinButton->OwnerNode;
        var buttonCoordinate = new Vector2(joinButtonNode->X,  joinButtonNode->Y);

        var newNodeSize = new Vector2(joinButtonNode->Width + 50.0f, 30.0f);

        animationContainer = new ResNode {
            Position = buttonCoordinate - new Vector2(25.0f, 30.0f),
            Size = newNodeSize,
        };
        animationContainer.AttachNode((AtkUnitBase*)addon);
        
        warningTextNode = new TextNode {
            Size = newNodeSize,
            Origin = newNodeSize / 2.0f,
            AlignmentType = AlignmentType.Center,
            FontSize = 14,
            String = "Missing Job Stone!",
            TooltipString = "[VanillaPlus]: Click to disable lock",
            DrawFlags = DrawFlags.ClickableCursor,
        };
        warningTextNode.AttachNode(animationContainer);
        
        animationContainer.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(30, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .EndFrameSet()
            .Build());
        
        warningTextNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddFrame(1, alpha: 155)
            .AddFrame(15, alpha: 255) 
            .AddFrame(30, alpha: 155)
            .EndFrameSet()
            .Build());
        
        animationContainer.Timeline?.PlayAnimation(1);
        
        warningTextNode.AddEvent(AtkEventType.MouseClick, () => {
            if (clickCount++ >= 5) {
                suppressed = true;
                animationContainer.IsVisible = false;
            }

            warningTextNode.TooltipString = $"[VanillaPlus]: Click to disable lock\n{6 - clickCount} Clicks remaining";
            warningTextNode.ShowTooltip();
        });
    }

    private void OnUpdate(AddonContentsFinder* addon) {
        if (animationContainer is null) return;
        var showWarning = !HasJobStoneEquipped() && CouldHaveJobStoneEquipped();
        
        animationContainer.IsVisible = showWarning && !suppressed;
        addon->JoinButton->SetEnabledState(!showWarning || suppressed);
    }

    private void DetachNodes(AddonContentsFinder* addon) {
        warningTextNode?.Dispose();
        warningTextNode = null;
        
        animationContainer?.Dispose();
        animationContainer = null;
    }

    private static bool HasJobStoneEquipped()
        => InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(13)->ItemId is not 0;

    private static bool CouldHaveJobStoneEquipped() {
        var currentJob = PlayerState.Instance()->CurrentClassJobId;
        var currentLevel = PlayerState.Instance()->CurrentLevel;
        if (currentJob is 0) return false;
        
        var job = Services.DataManager.GetExcelSheet<ClassJob>()
            .Where(classJob => classJob.ClassJobParent.RowId != classJob.RowId)
            .Where(classJob => classJob.ClassJobParent.RowId == currentJob)
            .FirstOrDefault();

        if (job.RowId is 0) return false;
        if (!job.UnlockQuest.IsValid) return false;
        if (job.UnlockQuest.Value.ClassJobLevel.First() > currentLevel) return false;
        
        return QuestManager.IsQuestComplete(job.UnlockQuest.RowId);
    }
}
