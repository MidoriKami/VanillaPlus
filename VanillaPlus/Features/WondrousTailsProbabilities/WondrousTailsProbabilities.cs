using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.WondrousTailsProbabilities;

public unsafe class WondrousTailsProbabilities : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Wondrous Tails Probabilities",
        Description = "Displays current line probabilities and average reroll probabilities in the Wondrous Tails Book.",
        Authors = [ "MidoriKami" ],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Add Current Duty Indicator"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("WondrousTailsSolver"),
    };

    private AddonController<AddonWeeklyBingo>? weeklyBingoController;
    private TextNode? probabilityTextNode;
    private PerfectTails? perfectTails;
    private ResNode? animationContainer;
    private NineGridNode? currentDutyNode;

    public override string ImageName => "WondrousTailsProbabilities.png";

    public override void OnEnable() {
        perfectTails = new PerfectTails();

        weeklyBingoController = new AddonController<AddonWeeklyBingo>("WeeklyBingo");
        weeklyBingoController.OnAttach += AttachNodes;
        weeklyBingoController.OnDetach += DetachNodes;
        weeklyBingoController.OnRefresh += RefreshNodes;
        weeklyBingoController.Enable();
    }

    public override void OnDisable() {
        weeklyBingoController?.Dispose();
        weeklyBingoController = null;
        
        perfectTails = null;
    }

    private void AttachNodes(AddonWeeklyBingo* addon) {
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
            SeString = perfectTails.SolveAndGetProbabilitySeString(),
            IsVisible = true,
        };
        System.NativeController.AttachNode(probabilityTextNode, (AtkResNode*)existingTextNode, NodePosition.AfterTarget);

        animationContainer = new ResNode {
            Size = new Vector2(72.0f, 48.0f),
            IsVisible = true,
        };
        
        animationContainer.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .EndFrameSet()
            .Build());
        
        System.NativeController.AttachNode(animationContainer, addon->DutySlotList.DutyContainer);
        
        currentDutyNode = new SimpleNineGridNode {
            Size = new Vector2(72.0f, 48.0f),
            Origin = new Vector2(72.0f, 48.0f) / 2.0f,
            TexturePath = "ui/uld/WeeklyBingo.tex",
            TextureCoordinates = new Vector2(0.0f, 182.0f),
            TextureSize = new Vector2(52.0f, 52.0f),
            LeftOffset = 10,
            RightOffset = 10,
            IsVisible = true,
            Color = Vector4.Zero with { W = 0.66f },
            AddColor = KnownColor.OrangeRed.Vector().AsVector3(),
        }; 
        
        currentDutyNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, alpha: 155, scale: new Vector2(1.0f, 1.0f) )
            .AddFrame(30, alpha: 255, scale: new Vector2(1.05f, 1.05f) )
            .AddFrame(60, alpha: 155, scale: new Vector2(1.0f, 1.0f) )
            .EndFrameSet()
            .Build());
        
        System.NativeController.AttachNode(currentDutyNode, animationContainer);
        
        animationContainer.Timeline?.PlayAnimation(1);
    }
    
    private void RefreshNodes(AddonWeeklyBingo* addon) {
        if (perfectTails is null) return;
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            var nodeText = SeString.Parse(existingTextNode->NodeText);

            // Get the first index of the double-newline payloads
            var lineBreakIndex = -1;
            for (var index = 0; index < nodeText.Payloads.Count; index++) {
                if (index > 0) {
                    var previousPayload = nodeText.Payloads[index - 1];
                    var payload = nodeText.Payloads[index];

                    if (previousPayload.Type is PayloadType.NewLine && payload.Type is PayloadType.NewLine) {
                        lineBreakIndex = index - 1;
                        break;
                    }
                }
            }

            // Copy all payloads up until the double-newline payload
            if (lineBreakIndex is not -1) {
                var newString = new SeStringBuilder();

                for(var index = 0; index < lineBreakIndex; index++) {
                    newString.Add(nodeText.Payloads[index]);
                }

                existingTextNode->SetText(newString.Encode());
            }
        }
        
        foreach (var index in Enumerable.Range(0, 16)) {
            perfectTails.GameState[index] = PlayerState.Instance()->IsWeeklyBingoStickerPlaced(index);
        }

        if (probabilityTextNode is not null) {
            probabilityTextNode.SeString = perfectTails.SolveAndGetProbabilitySeString();
        }

        AdjustCurrentDutyIndicator(addon);
    }

    private void DetachNodes(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            existingTextNode->SetHeight((ushort)(existingTextNode->GetHeight() * 3.0f / 2.0f));
        }

        System.NativeController.DisposeNode(ref probabilityTextNode);
        System.NativeController.DisposeNode(ref currentDutyNode);
    }
    
    private void AdjustCurrentDutyIndicator(AddonWeeklyBingo* addon) {
        if (animationContainer is null || currentDutyNode is null) return;
        if (GetTaskForCurrentTerritory(Services.ClientState.TerritoryType) is { } dutySlot) {
            var nativeDutySlot = addon->DutySlotList[dutySlot].DutyButton->OwnerNode;
            var nativeDutySlotPosition = new Vector2(nativeDutySlot->X, nativeDutySlot->Y - 2.0f);

            animationContainer.Position = nativeDutySlotPosition;
            animationContainer.IsVisible = true;

        }
        else {
            animationContainer.IsVisible = false;
        }
    }

    private static int? GetTaskForCurrentTerritory(uint territory) {
        foreach (var index in Enumerable.Range(0, 16)) {
            var territoriesForSlot = Services.DataManager.GetTerritoriesForOrderData(PlayerState.Instance()->WeeklyBingoOrderData[index]);

            if (territoriesForSlot.Any(terr => terr.RowId == territory)) {
                return index;
            }
        }

        return null;
    }
}
