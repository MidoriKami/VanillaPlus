using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BetterSelectString;

public class BetterSelectString : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterSelectString,
        Description = Strings.ModificationDescription_BetterSelectString,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "BetterSelectString.png";

    private AddonController<AddonSelectString>? selectStringController;
    private NativeListController<AddonSelectString>? selectStringListController;

    public override async Task OnEnableAsync() {
        unsafe {
            selectStringController = new AddonController<AddonSelectString> {
                AddonName = "SelectString",
                OnSetup = addon
                    => addon->AtkUnitBase.Size += new Vector2(32.0f, 0.0f),
                OnFinalize = addon
                    => addon->AtkUnitBase.Size -= new Vector2(32.0f, 0.0f),

                OnUpdate = UpdateSelectString,
            };

            selectStringListController = new NativeListController<AddonSelectString> {
                AddonName = "SelectString",
                GetPopulatorNode = addon => addon->GetComponentListById(3)->GetComponentItemRendererById(5),
                UpdateElement = (_, item) => {
                    var textNode = item.GetNode<AtkTextNode>(0);
                    if (textNode is null) return;
                    if (item.ItemIndex > 9) return;

                    using var stringBuilder = new RentedSeStringBuilder();
                    var builtString = stringBuilder
                        .Builder.Append($"{(item.ItemIndex + 1) % 10}. ")
                        .Append(textNode->GetText().AsReadOnlySeStringSpan())
                        .GetViewAsSpan();

                    textNode->SetText(builtString);
                },
            };
        }

        await Services.GetService<IFramework>().RunSafely(() => {
            selectStringController.Enable();
            selectStringListController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Services.GetService<IFramework>().RunSafely(() => {
            selectStringController?.Dispose();
            selectStringListController?.Dispose();
        });

        selectStringController = null;
        selectStringListController = null;
    }

    private static unsafe void UpdateSelectString(AddonSelectString* addon) {
        if (RaptureAtkModule.Instance()->IsTextInputActive()) return;

        var listComponent = addon->GetComponentListById(3);
        if (listComponent is null) return;

        // Doesn't need to have any actual data, just needs to exist so the game can set it as "handled"
        var atkEvent = stackalloc AtkEvent[1];
        var atkEventData = stackalloc AtkEventData[1];

        foreach (var index in Enumerable.Range(0, listComponent->ListLength)) {
            var keyIndex = (index + 1) % 10;

            var topRowKey = VirtualKey.KEY_0 + (ushort)keyIndex;
            var isTopRowKeyPressed = Services.GetService<IKeyState>().IsVirtualKeyValid(topRowKey) && Services.GetService<IKeyState>()[(int)topRowKey];

            if (isTopRowKeyPressed) {
                Services.GetService<IKeyState>()[(int)topRowKey] = false;

                atkEventData->ListItemData.SelectedIndex = index;
                addon->PopupMenu.ReceiveEvent(AtkEventType.ListItemClick, 0, atkEvent, atkEventData);
                return;
            }

            var numpadKey = VirtualKey.NUMPAD0 + (ushort)keyIndex;
            var isNumpadKeyPressed = Services.GetService<IKeyState>().IsVirtualKeyValid(numpadKey) && Services.GetService<IKeyState>()[(int)numpadKey];

            if (isNumpadKeyPressed) {
                Services.GetService<IKeyState>()[(int)numpadKey] = false;

                atkEventData->ListItemData.SelectedIndex = index;
                addon->PopupMenu.ReceiveEvent(AtkEventType.ListItemClick, 0, atkEvent, atkEventData);
                return;
            }
        }
    }
}
