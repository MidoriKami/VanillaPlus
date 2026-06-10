using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using Task = System.Threading.Tasks.Task;

namespace VanillaPlus.Native.Addons;

public class NodeListAddon<T, TU> : NativeAddon where TU : ListItemNode<T>, IListItemNode, new() {
    protected ListNode<T, TU>? ListNode;

    private AddonConfig? config;
    private AddonKeybindListener? keybindListener;
    private AddonConfigAddon? addonConfigWindow;

    public async Task InitializeAsync() {
        config = await AddonConfig.Load($"{InternalName}.addon.json");

        keybindListener = new AddonKeybindListener {
            AddonConfig = config,
            Callback = (ref isHandled) => {
                if (config.WindowSize != Vector2.Zero) {
                    Size = config.WindowSize;
                }

                Toggle();

                isHandled = true;
            },
        };

        addonConfigWindow = new AddonConfigAddon {
            InternalName = $"{InternalName}Config",
            Title = Strings.NodeList_ConfigWindowTitle.Format(InternalName),
            AddonConfig = config,
        };
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        ListNode = new ListNode<T, TU> {
            Position = ContentStartPosition,
            Size = ContentSize,
            OptionsList = ListItems,
            ItemSpacing = ItemSpacing,
        };
        ListNode.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon)
        => ListNode?.Update();

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        OnClose?.Invoke();
        ListNode = null;
    }

    public override async ValueTask DisposeAsync() {
        config = null;

        await Task.WhenAll(addonConfigWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        addonConfigWindow = null;

        keybindListener?.Dispose();
        keybindListener = null;

        await Services.Framework.Run(() => {
            if (OpenCommand is not null) {
                Services.CommandManager.RemoveHandler(OpenCommand);
            }
        });

        await base.DisposeAsync();
    }

    public string? OpenCommand {
        private get;
        init {
            if (field is null && value is not null) {
                Services.Framework.Run(() => {
                    Services.CommandManager.AddHandler(value, new CommandInfo(OnOpenCommand) {
                        DisplayOrder = 3,
                        HelpMessage = Strings.NodeList_OpenCommandHelp.Format(Title.ToString()),
                    });
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

    public void RefreshList()
        => ListNode?.FullRebuild();

    public Action? OnClose { get; set; }
}
