using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Premade.Node.Simple;

namespace VanillaPlus.Features.ConfigSearchBar;

public unsafe class TabEntry : IDisposable {
    private readonly ConfigSearchBarConfig config;
    private readonly SimpleImageNode? highlightNode;
    private readonly List<TextEntry> textEntries = [];
    
    public TabEntry(AtkUnitBase* addon, uint tabNodeId, uint settingsContainerNodeId, ConfigSearchBarConfig config) {
        this.config = config;

        if (addon is null) return;
        
        var dropDownComponentNode = addon->GetNodeById<AtkComponentNode>(tabNodeId);
        if (dropDownComponentNode is null) return;

        highlightNode = new SimpleImageNode {
            TexturePath = "ui/uld/IconA_Frame.tex",
            TextureCoordinates = new Vector2(240.0f, 0.0f),
            TextureSize = new Vector2(72.0f, 72.0f),
            Position = new Vector2(-14.0f, -12.0f) + dropDownComponentNode->AtkResNode.Position,
            Size = new Vector2(72.0f, 72.0f),
            IsVisible = false,
        };
        
        highlightNode.AttachNode(dropDownComponentNode, NodePosition.AfterTarget);

        var settingsContainerNode = addon->GetNodeById(settingsContainerNodeId);
        if (settingsContainerNode is null) return;

        textEntries = GetTextEntries(settingsContainerNode->ChildNode);
    }

    public void Dispose() {
        foreach (var textEntry in textEntries) {
            textEntry.ClearHighlight();
        }
        textEntries.Clear();
        
        highlightNode?.Dispose();
    }

    public void TryMatchString(string searchString) {
        if (string.IsNullOrEmpty(searchString)) {
            highlightNode?.IsVisible = false;

            foreach (var textEntry in textEntries) {
                textEntry.ClearHighlight();
            }
            
            return;
        }

        var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        var anyEntries = false;
        foreach (var entry in textEntries) {
            if (entry.IsMatch(regex)) {
                anyEntries = true;
                entry.ApplyHighlight();
            }
            else {
                entry.ClearHighlight();
            }
        }
        
        highlightNode?.IsVisible = anyEntries;
        highlightNode?.MultiplyColor = config.TabColor.AsVector3();
    }

    private List<TextEntry> GetTextEntries(AtkResNode* node, List<TextEntry>? strings = null) {
        strings ??= [];

        if (node is null) return strings;
        
        var currentNode = node;
        while (currentNode is not null) {

            switch (currentNode->GetNodeType()) {
                case NodeType.Text:
                    var textNode = currentNode->GetAsAtkTextNode();
                    if (textNode is not null && textNode->IsVisible()) {
                        strings.Add(new TextEntry {
                            Text = textNode->GetText().ToString(),
                            TextNode = textNode,
                            Config = config,
                        });
                    }
                    break;

                case NodeType.Component:
                    var component = currentNode->GetComponent();
                    if (component is not null) {
                        switch (component->GetComponentType()) {
                            case ComponentType.CheckBox:
                                strings.AddRange(GetTextEntries(component->UldManager.RootNode));
                                break;
                        }
                    }
                    break;

                case NodeType.Res:
                    strings.AddRange(GetTextEntries(currentNode->ChildNode));
                    break;
            }

            currentNode = currentNode->PrevSiblingNode;
        }
        
        return strings;
    }
}
