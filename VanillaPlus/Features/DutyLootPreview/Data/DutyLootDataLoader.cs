using System;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// Loads duty loot data for the "active" duty.
/// Listens to game events and manages async loading.
/// </summary>
public class DutyLootDataLoader : IDisposable {
    public event Action? OnChanged;

    public bool IsLoading => dutyLootDataCache.State == DutyLootDataCache.CacheState.Loading;

    public uint? ActiveDutyContentFinderConditionId { get; private set; }

    public DutyLootData? ActiveDutyLootData => ActiveDutyContentFinderConditionId.HasValue ? dutyLootDataCache.ReadDutyLootData(ActiveDutyContentFinderConditionId.Value) : null;

    private readonly DutyLootDataCache dutyLootDataCache = new();
    private AddonController<AddonContentsFinder>? contentsFinder;

    public unsafe void Enable() {
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        Services.GameGui.AgentUpdate += OnAgentUpdate;

        contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinder.OnAttach += OnContentsFinderChanged;
        contentsFinder.OnRefresh += OnContentsFinderChanged;
        contentsFinder.OnDetach += OnContentsFinderChanged;
        contentsFinder.Enable();

        dutyLootDataCache.OnChanged += OnCacheChanged;

        RefreshActiveDuty();
    }

    public void Dispose() {
        contentsFinder?.Dispose();
        contentsFinder = null;

        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.GameGui.AgentUpdate -= OnAgentUpdate;

        dutyLootDataCache.OnChanged -= OnCacheChanged;
        dutyLootDataCache.Dispose();
    }

    private static unsafe uint? GetActiveContentId() {
        // Priority 1: Currently in a duty
        var currentDutyId = GameMain.Instance()->CurrentContentFinderConditionId;
        if (currentDutyId != 0 && IsSupportedContent(new ContentsId { ContentType = ContentsId.ContentsType.Regular, Id = currentDutyId })) {
            return currentDutyId;
        }

        // Priority 2: Viewing a specific duty in ContentsFinder
        var agent = AgentContentsFinder.Instance();
        if (agent->IsAddonShown() && IsSupportedContent(agent->SelectedDuty)) {
            return agent->SelectedDuty.Id;
        }

        return null;
    }

    private static bool IsSupportedContent(ContentsId content) {
        // Not for Content Roulette
        if (content.ContentType != ContentsId.ContentsType.Regular)
            return false;

        if (!Services.DataManager.GetExcelSheet<ContentFinderCondition>().TryGetRow(content.Id, out var cfc))
            return false;

        // Not for Guildhests (3), PvP (6), Gold Saucer (19)
        return cfc.ContentType.RowId is not (3 or 6 or 19);
    }

    private unsafe void RefreshActiveDuty() {
        var newContentId = GetActiveContentId();
        if (newContentId == ActiveDutyContentFinderConditionId) return;

        ActiveDutyContentFinderConditionId = newContentId;
        if (newContentId.HasValue) {
            // Load only the current duty when in-duty, all duties when browsing duty finder
            var inDuty = GameMain.Instance()->CurrentContentFinderConditionId != 0;
            dutyLootDataCache.LoadCacheAsync(onlyContentId: inDuty ? newContentId : null);
        } else {
            dutyLootDataCache.ClearCache();
        }
        OnChanged?.Invoke();
    }

    private void OnCacheChanged() => OnChanged?.Invoke();

    private unsafe void OnContentsFinderChanged(AddonContentsFinder* addon) => RefreshActiveDuty();

    private void OnTerritoryChanged(ushort territory) => RefreshActiveDuty();

    private void OnAgentUpdate(AgentUpdateFlag flag) {
        if (flag.HasFlag(AgentUpdateFlag.UnlocksUpdate)) {
            dutyLootDataCache.LoadCacheAsync(forceReload: true);
        }
    }
}
