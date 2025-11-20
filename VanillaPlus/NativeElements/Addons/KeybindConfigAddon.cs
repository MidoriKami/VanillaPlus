using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using Lumina.Extensions;
using Keybind = VanillaPlus.Classes.Keybind;
using VirtualKeyExtensions = VanillaPlus.Extensions.VirtualKeyExtensions;

namespace VanillaPlus.NativeElements.Addons;

public unsafe class KeybindConfigAddon : NativeAddon {
    private CategoryTextNode? inputComboLabelNode;
    private HorizontalLineNode? topLineNode;
    private TextNode? currentComboTextNode;

    private CategoryTextNode? conflictsLabelNode;
    private HorizontalLineNode? conflictsLineNode;
    private ScrollingAreaNode<VerticalListNode>? conflictsScrollableAreaNode;

    private HorizontalLineNode? buttonsLineNode;
    private TextButtonNode? confirmButtonNode;
    private TextButtonNode? cancelButtonNode;
    
    private readonly HashSet<VirtualKey> combo = [VirtualKey.NO_KEY];
    private readonly List<InputId> conflicts = [];
    
    public required Keybind InitialKeybind { get; init; }
    
    protected override void OnSetup(AtkUnitBase* addon) {
        SetWindowSize(500.0f, 333.0f);

        inputComboLabelNode = new CategoryTextNode {
            AlignmentType = AlignmentType.Left,
            Position = ContentStartPosition + new Vector2(0.0f, 10.0f),
            String = "Input Desired Key Combo",
        };
        inputComboLabelNode.AttachNode(this);

        topLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, inputComboLabelNode.Y + inputComboLabelNode.Height),
            Size = new Vector2(225.0f, 2.0f),
        };
        topLineNode.AttachNode(this);

        currentComboTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, topLineNode.Y + topLineNode.Height),
            Size = new Vector2(ContentSize.X, 75.0f),
            FontSize = 24,
            AlignmentType = AlignmentType.Center,
            String = "Press a Key Combo",
        };
        currentComboTextNode.AttachNode(this);

        conflictsLabelNode = new CategoryTextNode {
            AlignmentType = AlignmentType.Left,
            Position = new Vector2(ContentStartPosition.X, currentComboTextNode.Position.Y + currentComboTextNode.Height),
            String = "Keybind Conflict(s)",
        };
        conflictsLabelNode.AttachNode(this);

        conflictsLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, conflictsLabelNode.Y + conflictsLabelNode.Height),
            Size = new Vector2(175.0f, 2.0f),
        };
        conflictsLineNode.AttachNode(this);

        conflictsScrollableAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = new Vector2(ContentStartPosition.X, conflictsLabelNode.Y + conflictsLabelNode.Height + 10.0f),
            Size = new Vector2(ContentSize.X, 90.0f),
            ContentHeight = 75.0f,
            AutoHideScrollBar = true,
        };
        conflictsScrollableAreaNode.AttachNode(this);
        
        conflictsScrollableAreaNode.ContentNode.AddNode(new CategoryTextNode {
            String = "No Conflicts Detected",
        });
        conflictsScrollableAreaNode.ContentHeight = conflictsScrollableAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);

        buttonsLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, conflictsScrollableAreaNode.Y + conflictsScrollableAreaNode.Height),
            Size = new Vector2(ContentSize.X, 2.0f),
        };
        buttonsLineNode.AttachNode(this);

        confirmButtonNode = new TextButtonNode {
            Position = ContentStartPosition + new Vector2(0.0f, ContentSize.Y - 26.0f),
            Size = new Vector2(100.0f, 24.0f),
            String = "Confirm",
            OnClick = () => {
                var newKeybind = new Keybind {
                    Key = combo.FirstOrNull(VirtualKeyExtensions.IsKey) ?? VirtualKey.NO_KEY,
                    Modifiers = combo.Where(VirtualKeyExtensions.IsModifier).ToHashSet(),
                };
                OnKeybindChanged(newKeybind); 
                Close();
            },
        };
        confirmButtonNode.AttachNode(this);

        cancelButtonNode = new TextButtonNode {
            Position = ContentStartPosition + new Vector2(ContentSize.X - 100.0f, ContentSize.Y - 26.0f),
            Size = new Vector2(100.0f, 24.0f),
            String = "Cancel",
            OnClick = Close,
        };
        cancelButtonNode.AttachNode(this);

        System.KeyListener.OnKeyPressed += KeyPressed;
    }

    private void KeyPressed(VirtualKey pressedKey, bool isPressed) {
        if (conflictsScrollableAreaNode is null) return;
        if (currentComboTextNode is null) return;
        if (!IsOpen) return;
        if (!isPressed) return;

        combo.Clear();
        foreach (var key in Services.KeyState.GetValidVirtualKeys()) {
            if (Services.KeyState[(int)key]) {
                combo.Add(key);
            }
        }

        currentComboTextNode.String = string.Join(" + ", combo);

        conflicts.Clear();
        var keybindSpan = UIInputData.Instance()->GetKeybindSpan();
        foreach (var index in Enumerable.Range(0, keybindSpan.Length)) {
            ref var keybind = ref keybindSpan[index];
            if (keybind.IsKeybindMatch(combo)) {
                conflicts.Add((InputId)index);
            }
        }
        
        conflictsScrollableAreaNode.ContentNode.Clear();

        if (conflicts.Count == 0) {
            conflictsScrollableAreaNode.ContentNode.AddNode(new CategoryTextNode {
                String = "No Conflicts Detected",
            });
        }
        else {
            foreach (var conflict in conflicts) {
                conflictsScrollableAreaNode.ContentNode.AddNode(new CategoryTextNode {
                    String = conflict.ToString(),
                });
            }
        }
        
        conflictsScrollableAreaNode.ContentHeight = conflictsScrollableAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);

        Services.KeyState.ResetKeyCombo(combo);
    }

    protected override void OnFinalize(AtkUnitBase* addon)
        => System.KeyListener.OnKeyPressed -= KeyPressed;

    public required Action<Keybind> OnKeybindChanged { get; init; }
}
