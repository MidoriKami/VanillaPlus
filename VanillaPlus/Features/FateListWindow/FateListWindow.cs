using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Modals;

namespace VanillaPlus.Features.FateListWindow;

public class FateListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Fate List Window",
        Description = "Displays a list of all fates that are currently active in the current zone",
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Now sorts by time remaining"),
        ],
    };

    private AddonFateList? addonFateList;
    private AddonConfig? config;
    private KeybindModal? keybindModal;
    private KeybindListener? keybindListener;
    
    public override string ImageName => "FateListWindow.png";

    public override void OnEnable() {
        config = AddonConfig.Load("FateList.addon.json", [VirtualKey.CONTROL, VirtualKey.F]);

        addonFateList = new AddonFateList(config) {
            NativeController = System.NativeController,
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "FateList",
            Title = "Fate List",
        };
        
        if (config.WindowPosition is { } windowPosition) {
            addonFateList.Position = windowPosition;
        }

        keybindListener = new KeybindListener {
            KeybindCallback = addonFateList.Toggle,
            KeyCombo = config.OpenKeyCombo,
        };
        
        keybindModal = new KeybindModal {
            KeybindSetCallback = keyBind => {
                config.OpenKeyCombo = keyBind;
                config.Save();

                keybindListener.KeyCombo = keyBind;
            },
        };
        OpenConfigAction = keybindModal.Open;
    }

    public override void OnDisable() {
        addonFateList?.Dispose();
        addonFateList = null;
        
        keybindModal?.Dispose();
        keybindModal = null;
        
        keybindListener?.Dispose();
        keybindListener = null;

        config = null;
    }
}
