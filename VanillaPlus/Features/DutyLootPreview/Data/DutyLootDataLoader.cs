using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// Loads duty loot data for the "active" duty.
/// Listens to game events and manages async loading.
/// </summary>
public class DutyLootDataLoader : IDisposable {
    public event Action<DutyLootData>? OnDutyLootDataChanged;

    private DutyLootData _currentDutyLootData = DutyLootData.Empty;
    public DutyLootData CurrentDutyLootData {
        get => _currentDutyLootData;
        private set {
            _currentDutyLootData = value;
            OnDutyLootDataChanged?.Invoke(value);
        }
    }

    private int _loadGeneration;
    private AddonController<AddonContentsFinder>? _contentsFinder;

    public unsafe void Enable() {
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        Services.GameGui.AgentUpdate += OnAgentUpdate;

        _contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        _contentsFinder.OnAttach += OnContentsFinderChanged;
        _contentsFinder.OnRefresh += OnContentsFinderChanged;
        _contentsFinder.OnDetach += OnContentsFinderChanged;
        _contentsFinder.Enable();

        RefreshActiveDuty();
    }

    public void Dispose() {
        Interlocked.Increment(ref _loadGeneration); // Invalidate any in-flight loads

        _contentsFinder?.Dispose();
        _contentsFinder = null;

        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.GameGui.AgentUpdate -= OnAgentUpdate;
    }

    private void Clear() {
        Interlocked.Increment(ref _loadGeneration); // Invalidate any in-flight loads
        CurrentDutyLootData = DutyLootData.Empty;
    }

    private void Reload() {
        if (CurrentDutyLootData.ContentId is { } contentId) {
            RequestLoad(contentId, forceReload: true);
        }
    }

    private void RequestLoad(uint contentId, bool forceReload = false) {
        if (!forceReload && contentId == CurrentDutyLootData.ContentId && !CurrentDutyLootData.IsLoading) {
            return;
        }

        var generation = Interlocked.Increment(ref _loadGeneration);
        _ = LoadDutyAsync(contentId, generation);
    }

    private async Task LoadDutyAsync(uint contentId, int generation) {
        try {
            var loadTask = Task.Run(() => DutyLootItem.ForContent(contentId).ToList());

            // Only show loading state if it takes longer than 50ms (avoid flicker)
            if (await Task.WhenAny(loadTask, Task.Delay(50)) != loadTask) {
                if (generation != _loadGeneration) return;
                CurrentDutyLootData = new DutyLootData {
                    IsLoading = true,
                    ContentId = contentId,
                    Items = [],
                };
            }

            var items = await loadTask;
            if (generation != _loadGeneration) return;

            CurrentDutyLootData = new DutyLootData {
                IsLoading = false,
                ContentId = contentId,
                Items = items,
            };
        }
        catch (Exception ex) {
            Services.PluginLog.Error(ex, "Failed to load duty loot");
            if (generation != _loadGeneration) return;
            CurrentDutyLootData = new DutyLootData {
                IsLoading = false,
                ContentId = contentId,
                Items = [],
            };
        }
    }

    private unsafe uint? GetActiveContentId() {
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
        if (GetActiveContentId() is { } contentId) {
            RequestLoad(contentId);
        } else {
            Clear();
        }
    }

    private unsafe void OnContentsFinderChanged(AddonContentsFinder* addon) => RefreshActiveDuty();

    private unsafe void OnTerritoryChanged(ushort territory) => RefreshActiveDuty();

    private void OnAgentUpdate(AgentUpdateFlag flag) {
        if (flag.HasFlag(AgentUpdateFlag.UnlocksUpdate)) {
            Reload();
        }
    }
}
