using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using InteropGenerator.Runtime;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.AdditionalHotbarSlots;

public class AdditionalHotbarSlots : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Additional Hotbar Slots",
        Description = "Allows you to add as many additional individual hotbar slots as you want.",
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    // public override string ImageName => "SampleGameModification.png";

    public override bool IsExperimental => true;

    private OverlayController? overlayController;

    private List<HotbarOverlayNode>? nodes;

    public unsafe delegate bool PrepareSlotForRender(
        RaptureHotbarModule* thisPtr,
        RaptureHotbarModule.HotbarSlot* slot,
        HotbarUiIntermediate* outIntermediate);

    [Signature("E8 ?? ?? ?? ?? 88 83 ?? ?? ?? ?? 48 83 C4 40")]
    public static PrepareSlotForRender? UpdateSlotData = null;

    public override async Task OnEnableAsync() {
        IGameInteropProvider.Get().InitializeFromAttributes(this);

        nodes = [];

        await IFramework.Get().Run(() => {
            overlayController = new OverlayController();

            foreach (var x in Enumerable.Range(0, 20)) {
                foreach (var y in Enumerable.Range(0, 20)) {
                    var newNode = new HotbarOverlayNode {
                        Position = new Vector2(2048.0f - 300.0f, 1080.0f - 500.0f) + new Vector2(x * 52.0f, y * 52.0f),
                    };

                    overlayController.AddNode(newNode);

                    nodes.Add(newNode);
                }
            }
        });
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().Run( () => overlayController?.Dispose());
        overlayController = null;
        nodes = null;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x43)]
public unsafe struct HotbarUiIntermediate {
    [FieldOffset(0x00)] public Utf8String* PopUpHelpText;   // to StringArray idx slotBase + 14
    [FieldOffset(0x08)] public CStringPointer CostTextPtr;      // to StringArray idx slotBase + 1
    [FieldOffset(0x10)] public uint IntermediateActionType; // to NumberArray idx slotBase + 0
    [FieldOffset(0x14)] public uint ActionId;               // to NumberArray idx slotBase + 3
    [FieldOffset(0x18)] public uint IconId;                 // to NumberArray idx slotBase + 4
    [FieldOffset(0x1C)] public uint CooldownMode;           // to NumberArray idx slotBase + 7
    [FieldOffset(0x20)] public uint CooldownSeconds;
    [FieldOffset(0x24)] public uint CooldownPercent; // to NumberArray idx slotBase + 8
    [FieldOffset(0x28)] public uint LastCooldownPercent;
    [FieldOffset(0x2C)] public uint ChargePercent; // to NumberArray idx slotBase + 9
    [FieldOffset(0x30)] public uint LastChargePercent;
    [FieldOffset(0x34)] public uint CurrentCharges;        // to NumberArray idx slotBase + 13
    [FieldOffset(0x38)] public uint CostValue;             // to NumberArray idx slotBase + 10
    [FieldOffset(0x3C)] public byte CostType;              // to NumberArray idx slotBase + 1
    [FieldOffset(0x3D)] public byte CostDisplayMode;       // to NumberArray idx slotBase + 2
    [FieldOffset(0x3E)] public bool ActionAvailable1;      // to NumberArray idx slotBase + 5
    [FieldOffset(0x3F)] public bool ActionAvailable2;      // to NumberArray idx slotBase + 6
    [FieldOffset(0x40)] public bool ActionTargetSatisfied; // to NumberArray idx slotBase + 15
    [FieldOffset(0x41)] public bool DrawAnts;              // to NumberArray idx slotBase + 14
    [FieldOffset(0x42)] public byte Unk0x42;
}
