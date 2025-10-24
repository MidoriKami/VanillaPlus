using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Addons;

public unsafe class NodeListAddon : NativeAddon {
    protected ScrollingAreaNode<VerticalListNode>? ScrollingAreaNode;
    private VerticalListNode ListNode => ScrollingAreaNode?.ContentNode ?? throw new Exception("Invalid List Node");

    private AddonConfig? config;
    private KeybindListener? keybindListener;
    private AddonConfigAddon? addonConfigWindow;

    public void Initialize(Keybind? defaultOpenCombo = null) {
        config = AddonConfig.Load($"{InternalName}.addon.json", defaultOpenCombo ?? new Keybind());

        keybindListener = new KeybindListener {
            AddonConfig = config,
            KeybindCallback = () => {
                if (config.WindowSize != Vector2.Zero) {
                    Size = config.WindowSize;
                }

                Toggle();
            },
        };

        addonConfigWindow = new AddonConfigAddon {
            NativeController = System.NativeController,
            InternalName = $"{InternalName}Config",
            Title = $"{InternalName} Configuration Window",
            AddonConfig = config,
        };
    }

    public override void Dispose() {
        config = null;
        
        addonConfigWindow?.Dispose();
        addonConfigWindow = null;
        
        keybindListener?.Dispose();
        keybindListener = null;
        
        if (OpenCommand is not null) {
            Services.CommandManager.RemoveHandler(OpenCommand);
        }
        
        base.Dispose();
    }

    public string? OpenCommand {
        private get;
        init {
            if (field is null && value is not null) {
                Services.CommandManager.AddHandler(value, new CommandInfo(OnOpenCommand) {
                    DisplayOrder = 3,
                    HelpMessage = $"Opens the {Title} Window",
                });
                
                field = value;
            }
        }
    }

    public Action? OpenAddonConfig {
        get {
            if (addonConfigWindow is not null) {
                return addonConfigWindow.Toggle;
            }

            return null;
        }
    }

    private void OnOpenCommand(string command, string arguments)
        => Toggle();

    protected override void OnSetup(AtkUnitBase* addon) {
        ScrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
            ContentHeight = 100,
        };
        ScrollingAreaNode.ContentNode.FitContents = true;
        AttachNode(ScrollingAreaNode);
        
        DoListUpdate(true);
    }
    
    /// <summary>
    ///     Return true to indicate contents were changed.
    /// </summary>
    public delegate bool UpdateList(VerticalListNode listNode, bool isOpening);
    
    public required UpdateList UpdateListFunction { get; init; }

    protected override void OnUpdate(AtkUnitBase* addon)
        => DoListUpdate();

    public void DoListUpdate(bool isOpening = false) {
        if (ScrollingAreaNode is null) return;
        
        if (UpdateListFunction(ListNode, isOpening)) {
            ScrollingAreaNode.ContentHeight = ListNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
        }
    }
}
