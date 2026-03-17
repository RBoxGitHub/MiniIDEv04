using CommunityToolkit.Mvvm.ComponentModel;
using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MiniIDEv04.Services
{
    public class PanelManagerService
    {
        private readonly ISysPanelRepository _repo = new SqliteSysPanelRepository();

        public ObservableCollection<SysPanelViewModel> Panels { get; } = new();

        // ── Boot ──────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            var rows = await _repo.GetAllAsync();
            Panels.Clear();
            foreach (var row in rows)
            {
                var vm = new SysPanelViewModel(row, _repo);
                vm.Init();
                Panels.Add(vm);
            }
        }

        // ── Panel operations ──────────────────────────────────────────────

        public SysPanelViewModel? Get(string panelKey)
            => Panels.FirstOrDefault(p => p.PanelKey == panelKey);

        public async Task ShowAsync(string panelKey)
        {
            var vm = Get(panelKey);
            if (vm is null) return;
            vm.IsVisible = true;
            await _repo.SetVisibilityAsync(panelKey, true);
        }

        public async Task HideAsync(string panelKey)
        {
            var vm = Get(panelKey);
            if (vm is null) return;
            vm.IsVisible = false;
            await _repo.SetVisibilityAsync(panelKey, false);
        }

        public async Task ToggleAsync(string panelKey)
        {
            var vm = Get(panelKey);
            if (vm is null) return;
            if (vm.IsVisible) await HideAsync(panelKey);
            else              await ShowAsync(panelKey);
        }

        public Task SetVisibility(string panelKey, bool visible)
            => visible ? ShowAsync(panelKey) : HideAsync(panelKey);

        /// <summary>
        /// Reads LaunchTarget from sys_Panels for the given key and returns
        /// the window name to open. UI layer instantiates the actual window.
        /// Returns null if no LaunchTarget is set.
        /// </summary>
        public string? GetLaunchTarget(string panelKey)
            => Get(panelKey)?._model.LaunchTarget;

        public async Task<SysPanelViewModel> CloneAsync(string sourcePanelKey, string newName)
        {
            var cloned = await _repo.CloneAsync(sourcePanelKey, newName);
            var vm     = new SysPanelViewModel(cloned, _repo);
            vm.Init();
            Panels.Add(vm);
            return vm;
        }

        public async Task SavePositionAsync(
            string panelKey, double left, double top, double width, double height)
        {
            await _repo.SavePositionAsync(panelKey, left, top, width, height);
            Get(panelKey)?.SilentUpdate(left, top, width, height);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // SysPanelViewModel
    // ══════════════════════════════════════════════════════════════════════

    public partial class SysPanelViewModel : ObservableObject
    {
        internal readonly ISysPanelRepository _repo;
        internal readonly SysPanel            _model;

        public SysPanelViewModel(SysPanel model, ISysPanelRepository repo)
        {
            _model = model;
            _repo  = repo;
        }

        public string PanelKey => _model.PanelKey;
        public bool   IsPinned => _model.IsPinned;
        public bool   IsCloned => _model.IsCloned;

        [ObservableProperty] private string _panelName     = string.Empty;
        [ObservableProperty] private bool   _isVisible;
        [ObservableProperty] private double _posLeft;
        [ObservableProperty] private double _posTop;
        [ObservableProperty] private double _panelWidth;
        [ObservableProperty] private double _panelHeight;
        [ObservableProperty] private string _titleBarColor = "#FF37474F";

        public void Init()
        {
            PanelName     = _model.PanelName;
            IsVisible     = _model.IsVisible;
            PosLeft       = _model.PosLeft;
            PosTop        = _model.PosTop;
            PanelWidth    = _model.PanelWidth;
            PanelHeight   = _model.PanelHeight;
            TitleBarColor = _model.TitleBarColor;
        }

        public void SilentUpdate(double left, double top, double width, double height)
        {
            PosLeft     = left;
            PosTop      = top;
            PanelWidth  = width;
            PanelHeight = height;
        }

        public SolidColorBrush TitleBarBrush
            => new((Color)ColorConverter.ConvertFromString(
                string.IsNullOrWhiteSpace(TitleBarColor) ? "#FF37474F" : TitleBarColor));
    }
}
