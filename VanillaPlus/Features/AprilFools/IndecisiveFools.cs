using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.AprilFools;

public unsafe class IndecisiveFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private AddonController<AddonSelectYesno>? addonController;

    private List<TextButtonNode>? textButtons;

    public void Enable() {
        textButtons = [];
        
        addonController = new AddonController<AddonSelectYesno>("SelectYesno");
        addonController.OnAttach += OnAttach;
        addonController.OnDetach += OnDetach;
        addonController.Enable();
    }

    public void Disable() {
        foreach (var textButton in textButtons ?? []) {
            textButton.Dispose();
        }
        textButtons?.Clear();
        textButtons = null;

        addonController?.Dispose();
        addonController = null;
    }
    
    private void OnAttach(AddonSelectYesno* addon) {
        if (!Config.Indecisive) return;
        if (textButtons is null) return;
        
        addon->AtkUnitBase.Size += new Vector2(0.0f, 65.0f);

        List<string> phrases = [
            "Yas Queen",
            "Noooo",
            "はいはい",
            "Fo Sho",
            "Hell Naw",
            "Maybe?",
        ];

        foreach (var x in Enumerable.Range(0, 3))
        foreach (var y in Enumerable.Range(0, 2)) {
            var newButton = new TextButtonNode {
                Position = new Vector2(x * 125.0f, y * 30.0f) + new Vector2(24.0f, addon->AtkUnitBase.Size.Y - 110.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = phrases[x + y * 3],
            };

            newButton.OnClick = () => {
                newButton.IsEnabled = false;
                UIGlobals.PlayChatSoundEffect((uint) Random.Shared.Next(1, 17));
            };
            
            newButton.AttachNode(&addon->AtkUnitBase);
            textButtons.Add(newButton);
        }
    }

    private void OnDetach(AddonSelectYesno* addon) {
        addon->AtkUnitBase.Size -= new Vector2(0.0f, 65.0f);
        
        foreach (var textButton in textButtons ?? []) {
            textButton.Dispose();
        }
        textButtons?.Clear();
    }
}
