using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.NativeElements.Addons.SearchAddons;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetRedirectConfigAddon : NativeAddon {

    private ModifyListNode<GearsetInfo>? gearsetListNode;
    private VerticalLineNode? lineNode;
    private SimpleComponentNode? optionsContainerNode;
    private IconImageNode? jobImageNode;
    private TextNode? gearsetLabelNode;

    private ModifyListNode<RedirectInfo>? redirectListNode;

    public required GearsetRedirectConfig Config { get; init; }

    private readonly SearchAddon<GearsetInfo> gearsetSearchAddon = GearsetSearchAddon.GetAddon();

    private readonly NewRedirectionAddon newRedirectionAddon = new() {
        Size = new Vector2(500.0f, 275.0f),
        InternalName = "AddRedirectionWindow",
        Title = "Add New Gearset Redirection",
    };

    public override void Dispose() {
        base.Dispose();

        gearsetSearchAddon.Dispose();
        newRedirectionAddon.Dispose();
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        gearsetListNode = new ModifyListNode<GearsetInfo> {
            Size = new Vector2(225.0f, ContentSize.Y),
            Position = ContentStartPosition,
            SelectionOptions = GetConfigInfos(),
            SortOptions = [ "Alphabetical", "Id" ],
            AddNewEntry = OnAddEntry,
            RemoveEntry = OnRemoveEntry,
            OnOptionChanged = OnOptionChanged,
        };
        gearsetListNode.AttachNode(this);

        lineNode = new VerticalLineNode {
            Size = new Vector2(4.0f, ContentSize.Y),
            Position = new Vector2(gearsetListNode.X + gearsetListNode.Width + 8.0f, ContentStartPosition.Y),
        };
        lineNode.AttachNode(this);

        optionsContainerNode = new SimpleComponentNode {
            Position = new Vector2(lineNode.X + lineNode.Width, ContentStartPosition.Y),
            Size = new Vector2(ContentSize.X - lineNode.X - lineNode.Width, ContentSize.Y),
        };
        gearsetListNode.AttachNode(this);

        var backgroundImageSize = optionsContainerNode.Size * 3.0f / 4.0f;
        var minSize = MathF.Min(backgroundImageSize.X, backgroundImageSize.Y);
        var adjustedSize = new Vector2(minSize, minSize);
        
        jobImageNode = new IconImageNode {
            Size = adjustedSize,
            Position = optionsContainerNode.Size / 2.0f - adjustedSize / 2.0f,
            IconId = gearsetListNode.SelectedOption?.GetIconId() ?? 0,
            IsVisible = gearsetListNode.SelectedOption is not null,
            FitTexture = true,
            Alpha = 0.1f,
        };
        jobImageNode.AttachNode(optionsContainerNode);

        gearsetLabelNode = new TextNode {
            Size = new Vector2(optionsContainerNode.Width, 28.0f),
            Position = new Vector2(0.0f, 45.0f),
            IsVisible = gearsetListNode.SelectedOption is not null,
            String = gearsetListNode.SelectedOption?.GetLabel() ?? string.Empty,
            FontSize = 28,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            AlignmentType = AlignmentType.Center,
        };
        gearsetLabelNode.AttachNode(optionsContainerNode);

        redirectListNode = new ModifyListNode<RedirectInfo> {
            Position = new Vector2(0.0f, gearsetLabelNode.Y + gearsetLabelNode.Height + 10.0f),
            Size = optionsContainerNode.Size - new Vector2(0.0f, gearsetLabelNode.Y + gearsetLabelNode.Height + 10.0f),
            IsVisible = gearsetListNode.SelectedOption is not null,
            AddNewEntry = thisListNode => {
                newRedirectionAddon.OnSelectionsConfirmed = () => {
                    if (newRedirectionAddon.SelectedGearset is null) return;
                    if (newRedirectionAddon.SelectedTerritory is null) return;
                    if (gearsetListNode?.SelectedOption is null) return;

                    var redirectInfo = new RedirectInfo {
                        AlternateGearsetId = newRedirectionAddon.SelectedGearset.GearsetId,
                        TerritoryType = newRedirectionAddon.SelectedTerritory.Value.RowId,
                    };
                    
                    thisListNode.AddOption(redirectInfo);
                    Config.Save();
                };
                
                newRedirectionAddon.Open();
            },
            RemoveEntry = _ => {
                Config.Save();
            },
        };
        redirectListNode.AttachNode(optionsContainerNode);
    }

    private void OnOptionChanged(GearsetInfo? obj) {
        if (jobImageNode is null) return;
        if (gearsetLabelNode is null) return;
        if (redirectListNode is null) return; 

        jobImageNode.IconId = obj?.GetIconId() ?? 0;
        jobImageNode.IsVisible = obj is not null;
        
        gearsetLabelNode.IsVisible = obj is not null;
        gearsetLabelNode.String = obj?.GetLabel() ?? string.Empty;

        if (obj is not null) {
            redirectListNode.SelectionOptions = Config.Redirections[obj.GearsetId];
            redirectListNode.IsVisible = true;
        }
        else {
            redirectListNode.IsVisible = false;
        }
    }

    private void OnRemoveEntry(GearsetInfo obj) {
        OnOptionChanged(null);
        
        Config.Redirections.Remove(obj.GearsetId);
        Config.Save();
    }

    private void OnAddEntry(ModifyListNode<GearsetInfo> obj) {
        gearsetSearchAddon.UpdateGearsets(Config.Redirections.Keys.ToList());
        gearsetSearchAddon.SelectionResult = result => {
            if (Config.Redirections.TryAdd(result.GearsetId, [])) {
                if (gearsetListNode is not null) {
                    gearsetListNode.SelectionOptions = GetConfigInfos();
                }
            
                Config.Save();
            }
        };
        
        gearsetSearchAddon.Open();
    }

    private List<GearsetInfo> GetConfigInfos()
        => Config.Redirections.Keys.Select(key => new GearsetInfo {
            GearsetId = key,
        }).ToList();
}
