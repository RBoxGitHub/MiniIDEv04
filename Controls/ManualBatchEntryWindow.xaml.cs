using CryptoCostBasesEFCore.Data;
using CryptoCostBasesEFCore.Models;
using CryptoCostBasesEFCore.Services;
using CryptoCostBasesEFCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CryptoCostBasesEFCore.Views
    {
    public partial class ManualBatchEntryWindow : Window
        {
        private readonly ViewModelManualInput _vm;
        private readonly AppDbContext         _context;  // sourced from DbService.Context

        private string _lastExchange = string.Empty;
        private string _lastType     = string.Empty;

        private static readonly List<string> TypeList = new()
            { string.Empty, "buy", "sell", "trade", "transfer",
              "deposit", "withdrawal", "reward", "fee" };

        public ManualBatchEntryWindow(ViewModelManualInput vm, DbService db)
            {
            InitializeComponent();
            _vm      = vm;
            _context = db.Context;
            DataContext = _vm;

            QuickExchangeCombo.SelectionChanged += (s, e) =>
                {
                var val = QuickExchangeCombo.SelectedItem as string ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(val))
                    _lastExchange = val;
                infoExchange.Text = $"Selected Exchange: {_lastExchange}";
                };

            QuickTypeCombo.SelectionChanged += (s, e) =>
                {
                var val = QuickTypeCombo.SelectedItem as string ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(val))
                    _lastType = val;
                };

            Loaded  += OnWindowLoaded;
            Closing += OnWindowClosing;
            }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
            {
            var exchanges    = await _vm.GetExchangeNamesAsync();
            var allWallets   = await _vm.GetAllWalletsAsync();
            var currencies   = await _vm.GetAllCurrenciesAsync();
            var fundsSources = await _vm.GetAllFundsSourcesAsync();

            foreach (var ex in exchanges)
                QuickExchangeCombo.Items.Add(ex);

            foreach (var t in TypeList)
                QuickTypeCombo.Items.Add(t);

            // Seed unified wallet collection from DB
            WalletService.LoadWallets(allWallets);

            ExchangeColumn.ItemsSource      = exchanges;
            TypeColumn.ItemsSource          = TypeList;
            FromWalletColumn.ItemsSource    = WalletService.AllWallets;
            ToWalletColumn.ItemsSource      = WalletService.AllWallets;
            FromCurrencyColumn.ItemsSource  = currencies;
            ToCurrencyColumn.ItemsSource    = currencies;
            FeeCurrencyColumn.ItemsSource   = currencies;
            FundsSourceColumn.ItemsSource   = fundsSources;
            }

        // ── Flush pending wallets to DB on window close ───────────────────
        private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
            {
            await FlushPendingWalletsAsync();
            }

        // ── Write any in-memory new wallets to the database ───────────────
        private async System.Threading.Tasks.Task FlushPendingWalletsAsync()
            {
            if (!WalletService.PendingWallets.Any()) return;

            try
                {
                foreach (var wallet in WalletService.PendingWallets)
                    {
                    // Only write if not already in DB (guard against duplicates)
                    bool exists = _context.Transactions
                        .Any(t => t.FromWalletId == wallet.WalletId
                               || t.ToWalletId   == wallet.WalletId);

                    if (!exists)
                        {
                        // Wallets live implicitly in transactions — if you have a
                        // dedicated Wallets table, insert here instead.
                        // For now we store as a marker transaction or just log.
                        // TODO: replace with your wallet table insert if one exists.
                        }
                    }

                await _context.SaveChangesAsync();
                WalletService.ClearPending();
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Warning: could not save new wallets to database.\n{ex.Message}",
                                "Wallet Save Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

        // ── Add row ───────────────────────────────────────────────────────
        private void AddRow_Click(object sender, RoutedEventArgs e)
            {
            _vm.AddBlankRow(exchange: _lastExchange, type: _lastType);
            RefreshQuickFillDisplay();
            }

        // ── New FROM currency button clicked in grid row ──────────────────
        private void NewFromCurrency_Click(object sender, RoutedEventArgs e)
            {
            if (sender is not Button btn || btn.Tag is not BatchEntryRow row) return;

            var dlg = new SimpleInputDialog("Add From Currency", "Enter crypto symbol (e.g. BTC):")
                { Owner = this };

            if (dlg.ShowDialog() != true) return;

            var symbol = dlg.ResponseText.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(symbol)) return;

            row.FromCurrency = symbol;
            AddCurrencyToColumnIfMissing(symbol);
            }

        // ── New TO currency button clicked in grid row ────────────────────
        private void NewToCurrency_Click(object sender, RoutedEventArgs e)
            {
            if (sender is not Button btn || btn.Tag is not BatchEntryRow row) return;

            var dlg = new SimpleInputDialog("Add To Currency", "Enter crypto symbol (e.g. ETH):")
                { Owner = this };

            if (dlg.ShowDialog() != true) return;

            var symbol = dlg.ResponseText.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(symbol)) return;

            row.ToCurrency = symbol;
            AddCurrencyToColumnIfMissing(symbol);
            }

        // ── New FROM wallet button clicked in grid row ────────────────────
        private void NewFromWallet_Click(object sender, RoutedEventArgs e)
            {
            if (sender is not Button btn || btn.Tag is not BatchEntryRow row) return;

            var dlg = new dlgNewWallet(dlgNewWallet.WalletSide.From,
                                       prefilledExchange: row.TransExchange.Length > 0
                                           ? row.TransExchange : _lastExchange)
                { Owner = this };

            if (dlg.ShowDialog() == true && dlg.CreatedWallet is not null)
                {
                row.FromWallet   = dlg.CreatedWallet.WalletName;
                row.FromWalletId = dlg.CreatedWallet.WalletId;
                }
            }

        // ── New TO wallet button clicked in grid row ──────────────────────
        private void NewToWallet_Click(object sender, RoutedEventArgs e)
            {
            if (sender is not Button btn || btn.Tag is not BatchEntryRow row) return;

            var dlg = new dlgNewWallet(dlgNewWallet.WalletSide.To,
                                       prefilledExchange: row.TransExchange.Length > 0
                                           ? row.TransExchange : _lastExchange)
                { Owner = this };

            if (dlg.ShowDialog() == true && dlg.CreatedWallet is not null)
                {
                row.ToWallet   = dlg.CreatedWallet.WalletName;
                row.ToWalletId = dlg.CreatedWallet.WalletId;
                }
            }

        // ── Refresh quick fill display ────────────────────────────────────
        private void RefreshQuickFillDisplay()
            {
            if (!string.IsNullOrEmpty(_lastExchange))
                {
                QuickExchangeCombo.SelectedItem = _lastExchange;
                QuickExchangeCombo.Text         = _lastExchange;
                }

            if (!string.IsNullOrEmpty(_lastType))
                {
                QuickTypeCombo.SelectedItem = _lastType;
                QuickTypeCombo.Text         = _lastType;
                }

            QuickExchangeCombo.SelectedItem = QuickExchangeCombo.Items
                .OfType<string>()
                .FirstOrDefault(x => x == _lastExchange);
            QuickExchangeCombo.Text = _lastExchange;
            }

        // ── Track the active editing element for currency free-form override ─
        private ComboBox? _editingCurrencyCombo = null;
        private Telerik.Windows.Controls.GridViewColumn? _editingCurrencyColumn = null;

        // ── Capture the ComboBox element when a currency cell enters edit mode ─
        private void BatchGrid_PreparingCellForEdit(object sender,
            Telerik.Windows.Controls.GridViewPreparingCellForEditEventArgs e)
            {
            _editingCurrencyCombo   = null;
            _editingCurrencyColumn  = null;

            bool isCurrencyCol = e.Column == FromCurrencyColumn
                              || e.Column == ToCurrencyColumn
                              || e.Column == FeeCurrencyColumn;
            if (!isCurrencyCol) return;

            // Walk the visual tree to find the ComboBox inside the editing element
            var combo = FindVisualChild<ComboBox>(e.EditingElement);
            if (combo is null) return;

            _editingCurrencyCombo  = combo;
            _editingCurrencyColumn = e.Column;

            // Allow any typed text — this is the key: IsTextSearchEnabled only affects
            // keyboard navigation, but we need to ensure the text box accepts free input.
            combo.IsEditable        = true;
            combo.IsTextSearchEnabled = false;
            combo.StaysOpenOnEdit   = true;
            }

        // ── Populate WalletId when user picks from wallet combo ───────────
        // ── Also push free-typed currency text into the row when list is empty ─
        private void BatchGrid_CellEditEnded(object sender,
            Telerik.Windows.Controls.GridViewCellEditEndedEventArgs e)
            {
            if (e.Cell?.ParentRow?.Item is not BatchEntryRow row) return;

            // ── Currency override: when ItemsSource had no items (empty DB) the
            //    Telerik combo discards the typed value.  We manually push it back.
            if (e.Cell.Column == FromCurrencyColumn
             || e.Cell.Column == ToCurrencyColumn
             || e.Cell.Column == FeeCurrencyColumn)
                {
                // Read the typed text from the ComboBox captured in PreparingCellForEdit.
                // We cannot use e.NewValue — it does not exist on this Telerik build.
                string typed = (_editingCurrencyCombo?.Text ?? string.Empty).Trim();
                string value = typed;

                if (!string.IsNullOrEmpty(value))
                    {
                    if (e.Cell.Column == FromCurrencyColumn)
                        row.FromCurrency = value;
                    else if (e.Cell.Column == ToCurrencyColumn)
                        row.ToCurrency = value;
                    else if (e.Cell.Column == FeeCurrencyColumn)
                        row.FeeCurrency = value;

                    // If the value is new (not in list yet), add it so the next row's
                    // combo already has it available without reopening the window.
                    AddCurrencyToColumnIfMissing(value);
                    }

                _editingCurrencyCombo  = null;
                _editingCurrencyColumn = null;
                return;
                }

            // From Wallet selected — look up the WalletId
            if (e.Cell.Column == FromWalletColumn)
                {
                var match = WalletService.AllWallets
                    .FirstOrDefault(w => w.WalletName == row.FromWallet);
                if (match is not null)
                    row.FromWalletId = match.WalletId;
                }

            // To Wallet selected — look up the WalletId
            if (e.Cell.Column == ToWalletColumn)
                {
                var match = WalletService.AllWallets
                    .FirstOrDefault(w => w.WalletName == row.ToWallet);
                if (match is not null)
                    row.ToWalletId = match.WalletId;
                }
            }

        // ── Add a newly-typed currency to all three currency column lists ──────
        private void AddCurrencyToColumnIfMissing(string currency)
            {
            if (string.IsNullOrWhiteSpace(currency)) return;

            foreach (var col in new[] { FromCurrencyColumn, ToCurrencyColumn, FeeCurrencyColumn })
                {
                if (col.ItemsSource is System.Collections.IList list
                    && !list.Contains(currency))
                    {
                    list.Add(currency);
                    }
                else if (col.ItemsSource is null)
                    {
                    // ItemsSource was never set (no DB rows) — initialise it now
                    col.ItemsSource = new System.Collections.Generic.List<string> { currency };
                    }
                }
            }

        // ── Visual-tree helper ────────────────────────────────────────────────
        private static T? FindVisualChild<T>(System.Windows.DependencyObject? parent)
            where T : System.Windows.DependencyObject
            {
            if (parent is null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
                {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var found = FindVisualChild<T>(child);
                if (found is not null) return found;
                }
            return null;
            }

        // ── Delete row ────────────────────────────────────────────────────
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
            {
            if (sender is Button btn && btn.Tag is BatchEntryRow row)
                _vm.Rows.Remove(row);
            }

        // ── Close ─────────────────────────────────────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e)
            {
            Close(); // triggers OnWindowClosing → FlushPendingWalletsAsync
            }

        private void ucCryptoPurchase_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
            MessageBox.Show("Double-clicked on CryptoPurchase control. This is a placeholder for future functionality.");
            }

        private void ucCryptoSwap_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
            MessageBox.Show("Double-clicked on CryptoSwap control. This is a placeholder for future functionality.");
            }
        }
    }
