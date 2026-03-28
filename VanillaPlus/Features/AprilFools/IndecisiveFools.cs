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
            "Bet", "Hard Pass", "Maybe?", "Yasss", "Nope", "IDK",
            "Fo Sho", "Hell Naw", "Facts", "Nah Fam", "Perchance",
            "Yeet", "Big No", "I Guess", "Yuh-huh", "No Way",
            "Toss Up", "Word", "Not Today", "Perhaps", "Slay",
            "Negative", "Who Knows", "Yessir", "Denied", "Unsure",
            "Indubidly", "Exit Left", "Possibly", "Totally", "No-go",
            "Mayhaps", "Correct", "Hard No", "50/50", "Yas", "Naur",
            "Ask Later", "For Real", "I Refuse", "Shrug", "Absolutely",
            "Never", "Could Be", "Indeed", "Noope", "Meh", "You Bet",
            "Gross, No", "It Depends",
        ];

        foreach (var x in Enumerable.Range(0, 3))
        foreach (var y in Enumerable.Range(0, 2)) {
            var buttonPhrase = phrases[Random.Shared.Next(0, phrases.Count)];
            phrases.Remove(buttonPhrase);

            var newButton = new TextButtonNode {
                Position = new Vector2(x * 125.0f, y * 30.0f) + new Vector2(24.0f, addon->AtkUnitBase.Size.Y - 110.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = buttonPhrase,
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
