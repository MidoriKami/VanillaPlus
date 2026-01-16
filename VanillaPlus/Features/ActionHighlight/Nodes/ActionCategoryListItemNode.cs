using KamiToolKit.Premade.GenericListItemNodes;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public class ActionCategoryListItemNode : GenericListItemNode<ActionCategory> {
    protected override uint GetIconId(ActionCategory data) 
        => data.IconId;
    
    protected override string GetLabelText(ActionCategory data) 
        => data.Name;
    
    protected override string GetSubLabelText(ActionCategory data) 
        => data.SubLabel;
    
    protected override uint? GetId(ActionCategory data) 
        => null;
}
