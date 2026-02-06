using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public class CosmicExplorationProgressWindow : GameModification {
    private readonly List<Progress> researchProgress = [];
    private readonly Dictionary<byte, List<Progress>> researchProgressByJob = [];

    private CosmicExplorationProgressAddon? addon;
    private CircleButtonNode? hudShowNode;

    private UpdateFlags needUpdate;

    private Hook<WKSResearchModule.Delegates.SetIntData>? setIntDataHook;

    private AddonController? wksHudController;

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CosmicExplorationProgressWindow,
        Description = Strings.ModificationDescription_CosmicExplorationProgressWindow,
        Type = ModificationType.NewWindow,
        Authors = ["salanth357"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "CosmicExplorationProgressWindow.png";

    public override unsafe void OnEnable() {
        InitResearchProgress();

        // Create the window
        addon = new CosmicExplorationProgressAddon {
            Size = new Vector2(320.0f, 290.0f),
            InternalName = "CosmicExplorationProgress",
            Title = "", // No title actually needed for this addon
            DisableClose = true,
            DisableCloseTransition = true,
        };
        addon.Initialize();

        // Update WKSHud to include a button to open the addon
        wksHudController = new AddonController("WKSHud");
        wksHudController.OnAttach += wksHud => {
            hudShowNode = new CircleButtonNode {
                Icon = ButtonIcon.Eye,
                AddColor = new Vector3(0.0f, -0.125f, 128f / 255f),
                Size = new Vector2(28.0f),
                Position = new Vector2(26.0f, 26.0f),
            };
            // override the texture to use the base theme, since that's what the gear button in WKSHud does
            hudShowNode.ImageNode.LoadTexture("ui/uld/CircleButtons.tex", false);
            hudShowNode.AttachNode(wksHud);
            hudShowNode.OnClick += () => {
                needUpdate = UpdateFlags.Force;
                addon?.Toggle();
            };
        };
        wksHudController.OnDetach += _ => {
            hudShowNode?.Dispose();
            hudShowNode = null;
        };
        wksHudController.Enable();

        // Check if we need to update the UI
        Services.Framework.Update += OnTick;
        Services.ClientState.ClassJobChanged += OnClassJobChange;
        setIntDataHook?.Enable();

        if (!TryHookResearchModule()) {
            // Watch for ResearchModule to be loaded when we enter a WKS zone
            Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        }
    }

    private void OnTerritoryChanged(ushort obj) {
        if (setIntDataHook != null)
            Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        TryHookResearchModule();
    }

    private unsafe bool TryHookResearchModule() {
        var mgr = WKSManager.Instance();
        if (mgr == null) return false;
        if (mgr->ResearchModule == null) return false;

        setIntDataHook = Services.Hooker.HookFromAddress<WKSResearchModule.Delegates.SetIntData>(
            WKSManager.Instance()->ResearchModule->WKSModuleBase.VirtualTable->SetIntData,
            ResearchModuleSetIntDataDetour);
        setIntDataHook.Enable();

        return true;
    }

    public override void OnDisable() {
        setIntDataHook?.Disable();
        setIntDataHook?.Dispose();
        setIntDataHook = null;
        Services.ClientState.ClassJobChanged -= OnClassJobChange;
        Services.Framework.Update -= OnTick;
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        wksHudController?.Disable();
        wksHudController = null;
        hudShowNode = null;
        addon?.Dispose();
        addon = null;

        needUpdate = UpdateFlags.None;
        researchProgress.Clear();
        researchProgressByJob.Clear();
    }

    private void OnTick(IFramework framework) {
        if (needUpdate != UpdateFlags.None) Update();
    }

    private void OnClassJobChange(uint classJobId) {
        needUpdate = UpdateFlags.Force;
        Update();
    }

    private unsafe void Update() {
        var mgr = WKSManager.Instance();
        if (mgr == null) return; // Should never be null, but let's be safe
        var research = mgr->ResearchModule;
        if (research == null) return; // We aren't in CE

        var changed = false;
        foreach (var p in researchProgress) {
            var p2 = new Progress(0, 0) {
                Show = research->IsTypeAvailable(p.JobId, p.ResearchType),
                Current = research->GetCurrentAnalysis(p.JobId, p.ResearchType),
                Needed = research->GetNeededAnalysis(p.JobId, p.ResearchType),
                Max = research->GetMaxAnalysis(p.JobId, p.ResearchType),
            };
            if (p2.Show != p.Show || p2.Current != p.Current || p2.Needed != p.Needed || p2.Max != p.Max)
                changed = true;
            p.Show = p2.Show;
            p.Current = p2.Current;
            p.Needed = p2.Needed;
            p.Max = p2.Max;
        }

        if (changed || needUpdate.HasFlag(UpdateFlags.Force)) {
            var jobId = Services.PlayerState.ClassJob.Value.RowId;
            if (jobId is < 8 or > 18) return;

            addon?.UpdateProgress(researchProgressByJob[(byte)(jobId - 7)]);
        }

        needUpdate = UpdateFlags.None;
    }

    private unsafe bool ResearchModuleSetIntDataDetour(
        WKSResearchModule* thisPtr, int a2, int a3, int a4, int a5, int a6, int a7) {
        // This gets called a bunch rapidly in a single tick, so we'll debounce on the next Framework tick
        needUpdate |= UpdateFlags.Need;
        return setIntDataHook!.Original.Invoke(thisPtr, a2, a3, a4, a5, a6, a7);
    }

    private void InitResearchProgress() {
        researchProgress.Clear();
        researchProgressByJob.Clear();

        var toolSheet = Services.DataManager.Excel.GetSheet<WKSCosmoToolClass>();
        foreach (var row in toolSheet.Where(row => row.RowId != 0)) {
            var jobId = (byte)row.RowId;
            var l = new List<Progress>();
            for (var typ = 0; typ < row.Types.Count; typ++) {
                var researchType = (byte)(typ + 1);
                var p = new Progress(jobId, researchType) {
                    IconId = row.Types[typ].Icon,
                };
                researchProgress.Add(p);
                l.Add(p);
            }

            researchProgressByJob.Add(jobId, l);
        }
    }


    [Flags]
    private enum UpdateFlags {
        None = 0,
        Need = 1,
        Force = 2,
    }

    public class Progress(byte jobId, byte researchType) {
        public readonly byte JobId = jobId;
        public readonly byte ResearchType = researchType;

        public ushort Current;
        public uint IconId;
        public ushort Max;
        public ushort Needed;
        public bool Show;

        public float Percentage => float.Clamp(Current / (float)Needed, 0, 1);

        public string Overlay => $"{Current} / {Needed} [{Max}]";

        public bool Complete => Current >= Needed;
        public bool Capped => Current >= Max;
    }
}
