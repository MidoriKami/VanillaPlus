using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.Features.GearsetRedirect.Nodes;
using VanillaPlus.NativeElements.SearchAddons;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetRedirectConfigAddon : NativeAddon {
    private ModifyListNode<GearsetInfo, GearsetInfoListItemNode>? gearsetListNode;
    private VerticalLineNode? lineNode;
    private SimpleComponentNode? optionsContainerNode;
    private IconImageNode? jobImageNode;
    private TextNode? gearsetLabelNode;

    private ModifyListNode<RedirectInfo, RedirectInfoListItemNode>? redirectListNode;

    public required GearsetRedirectConfig Config { get; init; }

    private readonly GearsetSearchAddon gearsetSearchAddon = new() {
        Size = new Vector2(275.0f, 555.0f),
        InternalName = "GearsetSearch",
        Title = Strings.SearchAddon_GearsetTitle,
    };

    private readonly NewRedirectionAddon newRedirectionAddon = new() {
        Size = new Vector2(550.0f, 275.0f),
        InternalName = "AddRedirectionWindow",
        Title = Strings.GearsetRedirect_AddRedirectionTitle,
    };

    protected override void OnSetup(AtkUnitBase* addon) {
        gearsetListNode = new ModifyListNode<GearsetInfo, GearsetInfoListItemNode> {
            Size = new Vector2(225.0f, ContentSize.Y),
            Position = ContentStartPosition,
            Options = GetConfigInfos(),
            ItemSpacing = 3.0f,
            SortOptions = [ Strings.GearsetRedirect_SortAlphabetical, Strings.GearsetRedirect_SortId ],
            AddNewEntry = OnAddEntry,
            RemoveEntry = OnRemoveEntry,
            SelectionChanged = OnOptionChanged,
            ItemComparer = GearsetInfo.Comparer,
            IsSearchMatch = GearsetInfo.IsMatch,
        };
        gearsetListNode.AttachNode(this);

        lineNode = new VerticalLineNode {
            Size = new Vector2(4.0f, ContentSize.Y),
            Position = new Vector2(gearsetListNode.X + gearsetListNode.Width + 8.0f, ContentStartPosition.Y),
        };
        lineNode.AttachNode(this);

        optionsContainerNode = new SimpleComponentNode {
            Size = new Vector2(ContentSize.X - lineNode.Bounds.Left, ContentSize.Y),
            Position = new Vector2(lineNode.Bounds.Right + 4.0f, ContentStartPosition.Y),
        };
        optionsContainerNode.AttachNode(this);

        var backgroundImageSize = optionsContainerNode.Size * 3.0f / 4.0f;
        var minSize = MathF.Min(backgroundImageSize.X, backgroundImageSize.Y);
        var adjustedSize = new Vector2(minSize, minSize);
        
        jobImageNode = new IconImageNode {
            Size = adjustedSize,
            Position = optionsContainerNode.Size / 2.0f - adjustedSize / 2.0f,
            IsVisible = gearsetListNode.SelectedOption is not null,
            FitTexture = true,
            Alpha = 0.1f,
        };
        jobImageNode.AttachNode(optionsContainerNode);

        gearsetLabelNode = new TextNode {
            Size = new Vector2(optionsContainerNode.Width, 28.0f),
            Position = new Vector2(0.0f, 45.0f),
            IsVisible = gearsetListNode.SelectedOption is not null,
            FontSize = 28,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            AlignmentType = AlignmentType.Center,
        };
        gearsetLabelNode.AttachNode(optionsContainerNode);

        redirectListNode = new ModifyListNode<RedirectInfo, RedirectInfoListItemNode> {
            Position = new Vector2(0.0f, gearsetLabelNode.Y + gearsetLabelNode.Height + 10.0f),
            Size = optionsContainerNode.Size - new Vector2(0.0f, gearsetLabelNode.Y + gearsetLabelNode.Height + 10.0f),
            IsVisible = gearsetListNode.SelectedOption is not null,
            ItemSpacing = 6.0f,
            AddNewEntry = () => {
                newRedirectionAddon.OnSelectionsConfirmed = () => {
                    if (newRedirectionAddon.SelectedGearset is null) return;
                    if (newRedirectionAddon.SelectedTerritory is null) return;
                    if (gearsetListNode?.SelectedOption is null) return;
                
                    var redirectInfo = new RedirectInfo {
                        AlternateGearsetId = newRedirectionAddon.SelectedGearset.GearsetId,
                        TerritoryType = newRedirectionAddon.SelectedTerritory.Value.RowId,
                    };

                    if (!Config.Redirections.TryAdd(gearsetListNode.SelectedOption.GearsetId, [redirectInfo])) {
                        Config.Redirections[gearsetListNode.SelectedOption.GearsetId].Add(redirectInfo);
                    }
                    
                    Config.Save();
                    redirectListNode?.RefreshList();
                };
                
                newRedirectionAddon.Open();
            },
            RemoveEntry = entry => {
                if (gearsetListNode?.SelectedOption is null) return;
                
                Config.Redirections[gearsetListNode.SelectedOption.GearsetId].Remove(entry);
                Config.Save();
            },
            IsSearchMatch = RedirectInfo.IsMatch,
        };
        redirectListNode.AttachNode(optionsContainerNode);
    }
    
    public override void Dispose() {
        base.Dispose();

        gearsetSearchAddon.Dispose();
        newRedirectionAddon.Dispose();
    }

    private void OnOptionChanged(GearsetInfo? gearsetInfo) {
        if (jobImageNode is null) return;
        if (gearsetLabelNode is null) return;
        if (redirectListNode is null) return;

        if (gearsetInfo is not null) {
            var gearsetData = RaptureGearsetModule.Instance()->GetGearset(gearsetInfo.GearsetId);
        
            jobImageNode.IconId = gearsetData->ClassJob + 62000u;
            jobImageNode.IsVisible = true;
        
            gearsetLabelNode.IsVisible = true;
            gearsetLabelNode.String = gearsetData->NameString;
            
            redirectListNode.Options = Config.Redirections[gearsetInfo.GearsetId];
            redirectListNode.IsVisible = true;
        }
        else {
            redirectListNode.IsVisible = false;
        }
    }

    private void OnRemoveEntry(GearsetInfo obj) {
        Config.Redirections.Remove(obj.GearsetId);
        Config.Save();
    }

    private void OnAddEntry() {
        gearsetSearchAddon.SelectionResult = result => {
            if (Config.Redirections.TryAdd(result.Id, [])) {
                gearsetListNode?.Options = GetConfigInfos();
                Config.Save();
            }
        };
        
        gearsetSearchAddon.Open();
    }

    private List<GearsetInfo> GetConfigInfos() {
        List<GearsetInfo> gearsets = [];
        List<int> invalidGearsets = [];
        
        foreach (var redirection in Config.Redirections.Keys) {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(redirection);
            if (gearset is null) continue;
            if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) {
                invalidGearsets.Add(redirection);
                continue;
            }
            
            gearsets.Add(new GearsetInfo {
                GearsetId = redirection,
            });
        }

        foreach (var gearset in invalidGearsets) {
            Config.Redirections.Remove(gearset);
        }

        if (invalidGearsets.Count is not 0) {
            Config.Save();
        }
        
        return gearsets;
    }
}
