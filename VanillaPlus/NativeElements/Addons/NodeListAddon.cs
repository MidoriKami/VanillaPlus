using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Addons;

public unsafe class NodeListAddon<T, TU> : NativeAddon where TU : ListItemNode<T>, new() {
    protected ListNode<T, TU>? ListNode;

    private AddonConfig? config;
    private KeybindListener? keybindListener;
    private AddonConfigAddon? addonConfigWindow;

    public void Initialize() {
        config = AddonConfig.Load($"{InternalName}.addon.json");

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
            InternalName = $"{InternalName}Config",
            Title = Strings.NodeList_ConfigWindowTitle.Format(InternalName),
            AddonConfig = config,
        };
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        ListNode = new ListNode<T, TU> {
            Position = ContentStartPosition,
            Size = ContentSize,
            OptionsList = ListItems,
            ItemSpacing = ItemSpacing,
        };
        ListNode.AttachNode(this);
    }

    protected override void OnUpdate(AtkUnitBase* addon)
        => ListNode?.Update();

    protected override void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);
        
        OnClose?.Invoke();
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
                    HelpMessage = Strings.NodeList_OpenCommandHelp.Format(Title.ToString()),
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

    public List<T> ListItems {
        get;
        set {
            field = value;
            ListNode?.OptionsList = value;
        }
    } = [];

    public float ItemSpacing {
        get;
        set {
            field = value;
            ListNode?.ItemSpacing = value;
        }
    }
    
    public Action? OnClose { get; set; }
}
