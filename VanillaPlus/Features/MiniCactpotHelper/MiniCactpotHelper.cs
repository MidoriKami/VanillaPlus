using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using Exception = System.Exception;
using OperationCanceledException = System.OperationCanceledException;

namespace VanillaPlus.Features.MiniCactpotHelper;

public unsafe class MiniCactpotHelper : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Mini Cactpot Helper",
        Description = "Indicates which Mini Cactpot spots you should reveal next.",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatabilityModule = new PluginCompatabilityModule("MiniCactpotSolver"),
    };

    private AddonController<AddonLotteryDaily>? lotteryDailyController;

    private MiniCactpotHelperConfig? config;
    private MiniCactpotHelperConfigWindow? configWindow;
    private PerfectCactpot? perfectCactpot;

    private int[]? boardState;
    private GameGrid? gameGrid;
    private Task? gameTask;
    private ButtonBase? configButton;

    public override string ImageName => "MiniCactpotHelper.png";

    public override void OnEnable() {
        boardState = [];
        
        perfectCactpot = new PerfectCactpot();
        
        config = MiniCactpotHelperConfig.Load();
        configWindow = new MiniCactpotHelperConfigWindow(config, ApplyConfigStyle);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        lotteryDailyController = new AddonController<AddonLotteryDaily>("LotteryDaily");
        lotteryDailyController.OnAttach += AttachNodes;
        lotteryDailyController.OnDetach += DetachNodes;
        lotteryDailyController.OnUpdate += UpdateNodes;
        lotteryDailyController.Enable();
    }

    public override void OnDisable() {
        gameTask?.Dispose();
        gameTask = null;
        
        configWindow?.RemoveFromWindowSystem();
        configWindow = null;
        
        lotteryDailyController?.Dispose();
        lotteryDailyController = null;
        
        config = null;
    }

    private void ApplyConfigStyle() {
        if (config is null) return;
        gameGrid?.UpdateButtonStyle(config);
    }

    private void AttachNodes(AddonLotteryDaily* addon) {
        if (config is null) return;
        if (configWindow is null) return;
		if (addon is null) return;

		var buttonContainerNode = addon->GetNodeById(8);
		if (buttonContainerNode is null) return;

		gameGrid = new GameGrid(config) {
			Size = new Vector2(542.0f, 320.0f),
			IsVisible = true,
		};
		
        System.NativeController.AttachNode(gameGrid, buttonContainerNode);

		configButton = new CircleButtonNode {
			Position = new Vector2(8.0f, 8.0f),
			Size = new Vector2(32.0f, 32.0f),
			Icon = ButtonIcon.GearCog,
			Tooltip = "Configure EzMiniCactpot Plugin",
			OnClick = () => configWindow.Toggle(),
			IsVisible = true,
		};
		
		System.NativeController.AttachNode(configButton, buttonContainerNode);
	}
	
	private void UpdateNodes(AddonLotteryDaily* addon) {
        if (perfectCactpot is null) return;

        var newState = Enumerable.Range(0, 9).Select(i => addon->GameNumbers[i]).ToArray();
		if (!boardState?.SequenceEqual(newState) ?? true) {
			try {
				if (gameTask is null or { Status: TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled }) {
					gameTask = Task.Run(() => {
			    
						if (!newState.Contains(0)) {
							gameGrid?.SetActiveButtons(null);
							gameGrid?.SetActiveLanes(null);
						}
						else {
							var solution = perfectCactpot.Solve(newState);
							var activeIndexes = solution
								.Select((value, index) => new { value, index })
								.Where(item => item.value)
								.Select(item => item.index)
								.ToArray();
					
							if (solution.Length is 8) {
								gameGrid?.SetActiveButtons(null);
								gameGrid?.SetActiveLanes(activeIndexes);
							}
							else {
								gameGrid?.SetActiveButtons(activeIndexes);
								gameGrid?.SetActiveLanes(null);
							}
						}
					});
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception ex) {
				Services.PluginLog.Error(ex, "Updater has crashed");
			}
		}
		
		boardState = newState;
	}
	
	private void DetachNodes(AddonLotteryDaily* addon) {
        System.NativeController.DisposeNode(ref gameGrid);
		System.NativeController.DisposeNode(ref configButton);
	}
}
