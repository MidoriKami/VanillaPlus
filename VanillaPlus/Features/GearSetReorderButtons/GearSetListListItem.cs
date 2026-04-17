using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public unsafe class GearSetListListItem : ListItemData {
    public bool IsChecked => ItemRenderer->IsChecked;
    public int GearSetId => int.TryParse(GearSetIndexNode->NodeText, out var id) ? id - 1 : 0;
    public AtkResNode* ResNode => GetNode<AtkResNode>(0);
    public AtkTextNode* GearSetIndexNode => GetNode<AtkTextNode>(1);
    public AtkImageNode* ImageNode => GetNode<AtkImageNode>(2);
    public AtkTextNode* GearSetNameNode => GetNode<AtkTextNode>(3);
    public AtkResNode* ResNode2 => GetNode<AtkResNode>(4);
    public AtkResNode* ResNode3 => GetNode<AtkResNode>(5);
    public AtkComponentNode* GlamorPlateComponentNode => GetNode<AtkComponentNode>(6);
    public AtkTextNode* ItemLevelNode => GetNode<AtkTextNode>(7);
    public AtkCollisionNode* CollisionNode => ItemRenderer->GetCollisionNodeById(16);
}
