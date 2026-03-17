using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using MiniIDEv04.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace MiniIDEv04.Views
{
    public partial class SysManagerWindow : Window
    {
        private readonly SqliteSysPanelRepository   _panelRepo   = new();
        private readonly SqliteSysControlRepository _controlRepo = new();

        private SysPanel?  _selectedPanel;
        private SysControl? _selectedControl;

        public SysManagerWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                await LoadPanelsAsync();
                await LoadControlsAsync();
                await ApplySaveButtonVisibilityAsync();
                LoadControlTypePicker();
            };
        }

        // ── Tab 1: sys_Panels ─────────────────────────────────────────────

        private async Task LoadPanelsAsync()
        {
            var panels = await _panelRepo.GetAllAsync();
            PanelsGrid.ItemsSource = new ObservableCollection<SysPanel>(panels);
            PanelCountText.Text    = $"{panels.Count} panels registered";
            ActionStatusText.Text  = string.Empty;
        }

        private async Task ApplySaveButtonVisibilityAsync()
        {
            var panel = await _panelRepo.GetByKeyAsync("SysManagerPanel");
            SaveChangesButton.Visibility = (panel?.HasSaveButton == true)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PanelsGrid_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
            => _selectedPanel = PanelsGrid.SelectedItem as SysPanel;

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var panels = PanelsGrid.ItemsSource as IEnumerable<SysPanel>;
            if (panels is null) return;

            int count = 0;
            foreach (var panel in panels)
            {
                panel.UpdatedAt = DateTime.UtcNow;
                await _panelRepo.UpdateAsync(panel);
                count++;
            }
            ActionStatusText.Text = $"✓  {count} rows saved  ·  {DateTime.Now:HH:mm:ss}";
            StatusText.Text       = "All panel changes written to miniIDE.db";
        }

        private async void NewPanel_Click(object sender, RoutedEventArgs e)
        {
            var source = _selectedPanel
                ?? (await _panelRepo.GetAllAsync()).FirstOrDefault(p => !p.IsPinned);
            if (source is null) { ActionStatusText.Text = "Select a panel to clone from."; return; }

            var clone = await _panelRepo.CloneAsync(source.PanelKey, $"{source.PanelName} — Copy");
            await LoadPanelsAsync();
            ActionStatusText.Text = $"Created: {clone.PanelKey}";
        }

        private async void ClonePanel_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPanel is null) { ActionStatusText.Text = "Select a panel row first."; return; }
            var clone = await _panelRepo.CloneAsync(_selectedPanel.PanelKey, $"{_selectedPanel.PanelName} — Copy");
            await LoadPanelsAsync();
            ActionStatusText.Text = $"Cloned → {clone.PanelKey}";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadPanelsAsync();
            ActionStatusText.Text = "Refreshed.";
        }

        // ── Tab 2: sys_Controls ───────────────────────────────────────────

        private async Task LoadControlsAsync()
        {
            var controls = await _controlRepo.GetAllAsync();
            ControlsGrid.ItemsSource = new ObservableCollection<SysControl>(controls);
            ControlCountText.Text    = $"{controls.Count} controls registered";
        }

        private void LoadControlTypePicker()
        {
            ControlTypePicker.ItemsSource   = ControlTypeRegistry.AllTypes;
            ControlTypePicker.SelectedIndex = 0;
        }

        private async void ControlsGrid_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {
            _selectedControl = ControlsGrid.SelectedItem as SysControl;
            if (_selectedControl is null) return;

            PropPanelTitle.Text = $"Properties — {_selectedControl.ControlName}";

            // Select matching type in picker
            var match = ControlTypeRegistry.Get(_selectedControl.ControlType);
            if (match is not null)
                ControlTypePicker.SelectedItem = match;

            // Load existing properties
            var props = await _controlRepo.GetPropertiesAsync(_selectedControl.ControlKey);
            PropertiesGrid.ItemsSource = new ObservableCollection<SysControlProperty>(props);
        }

        private void ControlTypePicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ControlTypePicker.SelectedItem is not KnownControlType type) return;
            PropertyPicker.ItemsSource   = type.Properties;
            PropertyPicker.SelectedIndex = 0;
        }

        private void PropertyPicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PropertyPicker.SelectedItem is not KnownControlProperty prop) return;
            PropertyValueBox.Text  = prop.DefaultValue;
            PropertyHintText.Text  = prop.Description ?? string.Empty;
        }

        private async void AddProperty_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedControl is null)
            { StatusText.Text = "Select a control first."; return; }
            if (PropertyPicker.SelectedItem is not KnownControlProperty prop)
            { StatusText.Text = "Select a property first."; return; }

            var newProp = new SysControlProperty
            {
                ControlKey    = _selectedControl.ControlKey,
                PropertyName  = prop.PropertyName,
                PropertyValue = PropertyValueBox.Text.Trim(),
                PropertyType  = prop.PropertyType,
                Category      = prop.Category
            };

            await _controlRepo.SavePropertyAsync(newProp);

            // Reload properties grid
            var props = await _controlRepo.GetPropertiesAsync(_selectedControl.ControlKey);
            PropertiesGrid.ItemsSource = new ObservableCollection<SysControlProperty>(props);
            StatusText.Text = $"Property '{prop.PropertyName}' saved.";
        }

        private async void NewControl_Click(object sender, RoutedEventArgs e)
        {
            var type = ControlTypePicker.SelectedItem as KnownControlType;
            var key  = $"ctrl_{Guid.NewGuid():N[..8]}";

            var control = new SysControl
            {
                ControlKey     = key,
                ControlName    = type?.ShortName ?? "NewControl",
                ControlType    = type?.FullTypeName ?? "System.Windows.Controls.Button",
                ParentKey      = "MainWindow",
                ParentType     = "View",
                AssemblySource = type?.AssemblySource ?? "WPF"
            };

            await _controlRepo.InsertAsync(control);
            await LoadControlsAsync();
            StatusText.Text = $"Created: {key}";
        }

        private async void CloneControl_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedControl is null) { StatusText.Text = "Select a control first."; return; }
            var newKey = $"{_selectedControl.ControlKey}_copy_{DateTime.UtcNow:HHmmss}";
            await _controlRepo.CloneAsync(_selectedControl.ControlKey, newKey, $"{_selectedControl.ControlName} — Copy");
            await LoadControlsAsync();
            StatusText.Text = $"Cloned → {newKey}";
        }

        private async void SaveControl_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedControl is null) { StatusText.Text = "Select a control first."; return; }

            // Update control type from picker
            if (ControlTypePicker.SelectedItem is KnownControlType type)
            {
                _selectedControl.ControlType    = type.FullTypeName;
                _selectedControl.AssemblySource = type.AssemblySource;
            }

            await _controlRepo.UpdateAsync(_selectedControl);

            // Save any edited property rows too
            if (PropertiesGrid.ItemsSource is IEnumerable<SysControlProperty> props)
                foreach (var p in props)
                    await _controlRepo.SavePropertyAsync(p);

            StatusText.Text = $"✓  {_selectedControl.ControlName} saved  ·  {DateTime.Now:HH:mm:ss}";
        }

        private async void DeleteControl_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedControl is null) { StatusText.Text = "Select a control first."; return; }
            await _controlRepo.DeletePropertiesAsync(_selectedControl.ControlKey);
            await _controlRepo.DeleteAsync(_selectedControl.Id);
            await LoadControlsAsync();
            PropertiesGrid.ItemsSource = null;
            PropPanelTitle.Text        = "Select a control to edit properties";
            StatusText.Text            = "Control deleted.";
        }

        private async void RefreshControls_Click(object sender, RoutedEventArgs e)
        {
            await LoadControlsAsync();
            StatusText.Text = "Controls refreshed.";
        }

        // ── Footer ────────────────────────────────────────────────────────

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
