using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.Timelines;
using Lumina.Text.Payloads;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.WondrousTailsProbabilities;

public class WondrousTailsProbabilities : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_WondrousTailsProbabilities,
        Description = Strings.ModificationDescription_WondrousTailsProbabilities,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        CompatibilityModule = new PluginCompatibilityModule("WondrousTailsSolver"),
    };

    private AddonController<AddonWeeklyBingo>? weeklyBingoController;
    private TextNode? probabilityTextNode;
    private PerfectTails? perfectTails;
    private ResNode? animationContainer;
    private NineGridNode? currentDutyNode;

    public override string ImageName => "WondrousTailsProbabilities.png";

    public override async Task OnEnableAsync() {
        perfectTails = new PerfectTails();

        await perfectTails.Initialize();

        unsafe {
            weeklyBingoController = new AddonController<AddonWeeklyBingo> {
                AddonName = "WeeklyBingo",
                OnSetup = SetupWeeklyBingo,
                OnFinalize = FinalizeWeeklyBingo,
                OnRefresh = RefreshWeeklyBingo,
            };
        }

        await weeklyBingoController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await weeklyBingoController.DisposeAsyncSafe();
        weeklyBingoController = null;

        perfectTails = null;
    }

    private unsafe void SetupWeeklyBingo(AddonWeeklyBingo* addon) {
        if (perfectTails is null) return;

        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is null) return;

        // Shrink existing node, the game doesn't need that space anyway.
        existingTextNode->SetHeight((ushort)(existingTextNode->GetHeight() * 2.0f / 3.0f));

        // Add new custom text node to ui
        probabilityTextNode = new TextNode {
            Size = new Vector2(290.0f, 36.0f),
            Position = new Vector2(68.0f, 469.0f),
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            String = perfectTails.SolveAndGetProbabilitySeString(),
        };
        probabilityTextNode.AttachNode(existingTextNode, NodePosition.AfterTarget);

        animationContainer = new ResNode {
            Size = new Vector2(72.0f, 48.0f),
        };

        animationContainer.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .EndFrameSet()
            .Build());

        animationContainer.AttachNode(addon->DutySlotList.DutyContainer);

        currentDutyNode = new SimpleNineGridNode {
            Size = new Vector2(72.0f, 48.0f),
            Origin = new Vector2(72.0f, 48.0f) / 2.0f,
            TexturePath = "ui/uld/WeeklyBingo.tex",
            TextureCoordinates = new Vector2(0.0f, 182.0f),
            TextureSize = new Vector2(52.0f, 52.0f),
            LeftOffset = 10,
            RightOffset = 10,
            Color = Vector4.Zero with { W = 0.66f },
            AddColor = KnownColor.OrangeRed.Vector().AsVector3(),
        };

        currentDutyNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, alpha: 155, scale: new Vector2(1.0f, 1.0f))
            .AddFrame(30, alpha: 255, scale: new Vector2(1.05f, 1.05f))
            .AddFrame(60, alpha: 155, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .Build());

        currentDutyNode.AttachNode(animationContainer);

        animationContainer.Timeline?.PlayAnimation(1);
    }

    private unsafe void RefreshWeeklyBingo(AddonWeeklyBingo* addon) {
        if (perfectTails is null) return;

        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            var nodeText = existingTextNode->NodeText.AsReadOnlySeString();

            // Finds the double newline, and strips everything after it.
            // We wanna clear "※Applicable duties may be confirmed via the subcommand." to ensure there's room for our text.
            using var stringBuilder = new RentedSeStringBuilder();

            var wasNewLine = false;
            foreach (var payload in nodeText) {
	            var isNewLine = payload.IsMacro(MacroCode.NewLine);

	            if (wasNewLine && isNewLine) {
		            break;
	            }

	            stringBuilder.Append(payload);
                wasNewLine = isNewLine;
            }

            existingTextNode->SetText(stringBuilder.GetViewAsSpan());
        }

        foreach (var index in Enumerable.Range(0, 16)) {
            perfectTails.GameState[index] = PlayerState.Instance()->IsWeeklyBingoStickerPlaced(index);
        }

        probabilityTextNode?.String = perfectTails.SolveAndGetProbabilitySeString();

        AdjustCurrentDutyIndicator(addon);
    }

    private unsafe void FinalizeWeeklyBingo(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            existingTextNode->SetHeight((ushort)(existingTextNode->GetHeight() * 3.0f / 2.0f));
        }

        probabilityTextNode?.Dispose();
        probabilityTextNode = null;

        currentDutyNode?.Dispose();
        currentDutyNode = null;
    }

    private unsafe void AdjustCurrentDutyIndicator(AddonWeeklyBingo* addon) {
        if (animationContainer is null || currentDutyNode is null) return;
        if (GetTaskForCurrentTerritory(IClientState.Get().TerritoryType.RowId) is { } dutySlot) {
            var nativeDutySlot = addon->DutySlotList[dutySlot].DutyButton->OwnerNode;
            var nativeDutySlotPosition = new Vector2(nativeDutySlot->X, nativeDutySlot->Y - 2.0f);

            animationContainer.Position = nativeDutySlotPosition;
            animationContainer.IsVisible = true;

        }
        else {
            animationContainer.IsVisible = false;
        }
    }

    private static unsafe int? GetTaskForCurrentTerritory(uint territory) {
        foreach (var index in Enumerable.Range(0, 16)) {
            var territoriesForSlot = IDataManager.Get().GetTerritoriesForOrderData(PlayerState.Instance()->WeeklyBingoOrderData[index]);

            if (territoriesForSlot.Any(terr => terr.RowId == territory)) {
                return index;
            }
        }

        return null;
    }
}
