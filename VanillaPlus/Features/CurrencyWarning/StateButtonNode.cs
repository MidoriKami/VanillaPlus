using System;
using System.Collections.Generic;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CurrencyWarning;

public class StateButtonNode : TextButtonNode {
    private List<string> _states = new();
    private int _selectedIndex = 0;

    /// <summary>
    /// Triggered when the user clicks the button and cycles to the next state.
    /// Passes the new SelectedIndex.
    /// </summary>
    public Action<int>? OnStateChanged { get; set; }

    public StateButtonNode() {
        OnClick = CycleState;
    }

    public List<string> States {
        get => _states;
        set {
            _states = value;
            UpdateDisplay();
        }
    }

    public int SelectedIndex {
        get => _selectedIndex;
        set {
            if (value < 0 || (value >= _states.Count && _states.Count > 0)) return;
            _selectedIndex = value;
            UpdateDisplay();
        }
    }

    private void CycleState() {
        if (_states.Count == 0) return;

        SelectedIndex = (SelectedIndex + 1) % _states.Count;
        OnStateChanged?.Invoke(_selectedIndex);
    }

    private void UpdateDisplay() {
        if (_selectedIndex >= 0 && _selectedIndex < _states.Count) {
            String = _states[_selectedIndex];
        }
    }
}
