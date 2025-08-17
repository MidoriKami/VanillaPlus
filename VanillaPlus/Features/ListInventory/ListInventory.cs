using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Classes;
using VanillaPlus.Modals;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "List Inventory Window",
        Description = "Adds a window that displays your inventory as a list, with toggleable filters.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };
    
    private AddonListInventory? listInventory;
    private AddonConfig? config;
    private KeybindModal? keybindModal;
    
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    
    public override void OnEnable() {
        config = AddonConfig.Load("ListInventory.addon.config", [SeVirtualKey.CONTROL, SeVirtualKey.N]);
        OpenConfigAction = () => {
            keybindModal ??= new KeybindModal {
                KeybindSetCallback = keyBind => {
                    config.OpenKeyCombo = keyBind;
                    config.Save();
                    keybindModal = null;
                },
            };
        };
        
        listInventory = new AddonListInventory {
            NativeController = System.NativeController,
            InternalName = "ListInventory",
            Title = "Inventory List",
            Size = new Vector2(450.0f, 700.0f),
            Config = config,
        };
        
        if (config.WindowPosition is { } windowPosition) {
            listInventory.Position = windowPosition;
        }

        if (config.WindowSize is { } windowSize) {
            listInventory.Size = windowSize;
        }
        
        Services.Framework.Update += OnFrameworkUpdate;
        
#if DEBUG
        listInventory.Open();
#endif
    }

    public override void OnDisable() {
        listInventory?.Dispose();
        listInventory = null;
    }
    
    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if (config is null || listInventory is null) return;
        
        if (UIInputData.Instance()->IsComboPressed(config.OpenKeyCombo.ToArray()) && stopwatch.ElapsedMilliseconds >= 250) {
            if (listInventory.IsOpen) {
                listInventory.Close();
            }
            else {
                listInventory.Open();
            }
            
            stopwatch.Restart();
        }
    }
}
