using System;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.DutyLootPreview.Enums;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// Loads duty loot data for the "active" duty.
/// Listens to game events and manages async loading.
/// </summary>
public class DutyLootDataLoader : IAsyncDisposable {
    public event Action? OnChanged;

    public bool IsLoading => dutyLootDataCache.State == CacheState.Loading;

    public uint? ActiveDutyContentFinderConditionId { get; private set; }

    public DutyLootData? ActiveDutyLootData => ActiveDutyContentFinderConditionId.HasValue ? dutyLootDataCache.ReadDutyLootData(ActiveDutyContentFinderConditionId.Value) : null;

    private readonly DutyLootDataCache dutyLootDataCache = new();
    private AddonController<AddonContentsFinder>? contentsFinder;
    private AddonController<AddonRaidFinder>? raidFinder;

    public async Task EnableAsync() {
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        Services.GameGui.AgentUpdate += OnAgentUpdate;

        unsafe {
            contentsFinder = new AddonController<AddonContentsFinder> {
                AddonName = "ContentsFinder",
                OnSetup = OnContentsFinderChanged,
                OnRefresh = OnContentsFinderChanged,
                OnFinalize = OnContentsFinderChanged,
            };

            raidFinder = new AddonController<AddonRaidFinder> {
                AddonName = "RaidFinder",
                OnSetup = OnRaidFinderChanged,
                OnRefresh = OnRaidFinderChanged,
                OnFinalize = OnRaidFinderChanged,
            };
        }

        await Services.Framework.Run(() => {
            contentsFinder.Enable();
            raidFinder.Enable();
        });

        dutyLootDataCache.OnChanged += OnCacheChanged;

        RefreshActiveDuty();
    }

    public async ValueTask DisposeAsync() {
        await Services.Framework.Run(() => {
            contentsFinder?.Dispose();
            raidFinder?.Dispose();
        });

        contentsFinder = null;
        raidFinder = null;

        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.GameGui.AgentUpdate -= OnAgentUpdate;

        dutyLootDataCache.OnChanged -= OnCacheChanged;
        dutyLootDataCache.Dispose();
    }

    private static unsafe uint? GetActiveContentId() {
        // Priority 1: Currently in a duty
        var currentDutyId = GameMain.Instance()->CurrentContentFinderConditionId;
        if (currentDutyId != 0 && IsSupportedContent(new ContentsId { ContentType = ContentsType.Regular, Id = currentDutyId })) {
            return currentDutyId;
        }

        // Priority 2: Viewing a specific duty in ContentsFinder or RaidFinder
        var agentContentsFinder = AgentContentsFinder.Instance();
        if (agentContentsFinder->IsAddonShown() && IsSupportedContent(agentContentsFinder->SelectedDuty)) {
            return agentContentsFinder->SelectedDuty.Id;
        }

        var agentRaidFinder = AgentRaidFinder.Instance();
        if (agentRaidFinder->IsAddonShown()) {
            var selectedTab = (int)agentRaidFinder->SelectedTab;
            var selectedEntry = (int)agentRaidFinder->SelectedEntry;
            var raidId = agentRaidFinder->Tabs[selectedTab].Entries[selectedEntry].ContentFinderConditionId;

            if (IsSupportedContent(new ContentsId { ContentType = ContentsType.Regular, Id = raidId })) {
                return raidId;
            }
        }

        return null;
    }

    private static bool IsSupportedContent(ContentsId content) {
        // Not for Content Roulette
        if (content.ContentType != ContentsType.Regular)
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
        }
        else {
            dutyLootDataCache.ClearCache();
        }
        OnChanged?.Invoke();
    }

    private void OnCacheChanged() => OnChanged?.Invoke();

    private unsafe void OnContentsFinderChanged(AddonContentsFinder* addon) => RefreshActiveDuty();
    private unsafe void OnRaidFinderChanged(AddonRaidFinder* addon) => RefreshActiveDuty();

    private void OnTerritoryChanged(uint u) => RefreshActiveDuty();

    private void OnAgentUpdate(AgentUpdateFlag flag) {
        if (flag.HasFlag(AgentUpdateFlag.UnlocksUpdate)) {
            dutyLootDataCache.LoadCacheAsync(forceReload: true);
        }
    }
}
