using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public unsafe class GearSetListListItem : ListItemData {
    public uint NodeId => ItemRenderer->OwnerNode->NodeId;

    public bool IsChecked => ItemRenderer->IsChecked;

    public int GearSetId => int.TryParse(gearSetIdLabel, out var id) ? id - 1 : 0;
    public int GearSetIndex => ItemRenderer->ListItemIndex;

    public AtkCollisionNode* ButtonAnchorNode => ItemRenderer->GetCollisionNodeById(16);

    private string gearSetIdLabel => ItemRenderer->GetTextNodeById(3)->GetText()
        .ExtractText()
        .Trim();
}
