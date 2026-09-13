using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Helpers;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddGoalDialog : ContentDialog
{
    private readonly List<string> _existingImageUrls = new();
    private readonly List<string> _newImageFilePaths = new();
    private readonly Goal? _existingGoal;

    public CreateGoalRequest? Result { get; private set; }
    public UpdateGoalRequest? UpdateResult { get; private set; }
    public List<string> ImageFilePaths => _newImageFilePaths.ToList();
    public List<string> ExistingImageUrls => _existingImageUrls.ToList();
    public bool IsEditMode => _existingGoal != null;

    public AddGoalDialog(List<Asset> assets, Goal? existingGoal = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _existingGoal = existingGoal;

        DepositAccountComboBox.ItemsSource = assets;
        SourceAccountComboBox.ItemsSource = assets;
        CurrencyComboBox.ItemsSource = CountryCurrencyData.AllCurrencies;

        if (assets.Count > 0)
        {
            DepositAccountComboBox.SelectedIndex = 0;
            SourceAccountComboBox.SelectedIndex = assets.Count > 1 ? 1 : 0;
        }

        DatePicker.Date = DateTimeOffset.Now.AddMonths(6);

        if (existingGoal != null)
        {
            Title = "Edit Financial Goal";
            PrimaryButtonText = "Save Changes";
            InitialDepositPanel.Visibility = Visibility.Collapsed;

            NameBox.Text = existingGoal.Name;
            TargetAmountNumberBox.Value = existingGoal.TargetAmount;
            DatePicker.Date = existingGoal.ExpectedDate;

            if (!string.IsNullOrWhiteSpace(existingGoal.Currency))
            {
                CurrencyComboBox.SelectedValue = existingGoal.Currency.ToUpperInvariant();
            }
            else
            {
                SetDefaultCurrency();
            }

            if (existingGoal.RecurringAmount.HasValue)
            {
                RecurringAmountNumberBox.Value = existingGoal.RecurringAmount.Value;
            }

            // Select Deposit Account (Target Asset)
            var matchDeposit = assets.FirstOrDefault(a => a.Id == existingGoal.TargetAssetId);
            if (matchDeposit != null)
            {
                DepositAccountComboBox.SelectedItem = matchDeposit;
            }

            // Select Source Account
            var matchSource = assets.FirstOrDefault(a => a.Id == existingGoal.SourceAssetId);
            if (matchSource != null)
            {
                SourceAccountComboBox.SelectedItem = matchSource;
            }

            if (!string.IsNullOrEmpty(existingGoal.RecurringFrequency))
            {
                foreach (ComboBoxItem item in FrequencyComboBox.Items)
                {
                    if (string.Equals(item.Tag?.ToString(), existingGoal.RecurringFrequency, StringComparison.OrdinalIgnoreCase))
                    {
                        FrequencyComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Load existing images so they can be viewed and deleted
            if (existingGoal.ImageUrls != null && existingGoal.ImageUrls.Count > 0)
            {
                _existingImageUrls.AddRange(existingGoal.ImageUrls);
            }
        }
        else
        {
            SetDefaultCurrency();
        }

        CurrencyComboBox.SelectionChanged += (_, _) => UpdateCurrencyDisplay();
        SourceAccountComboBox.SelectionChanged += (_, _) => UpdateCurrencyDisplay();
        DepositAccountComboBox.SelectionChanged += (_, _) => UpdateCurrencyDisplay();
        InitialDepositNumberBox.ValueChanged += (_, _) => UpdateCurrencyDisplay();
        UpdateCurrencyDisplay();

        NumericInputHelper.Attach(TargetAmountNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(InitialDepositNumberBox, allowDecimals: true, allowNegative: false);
        NumericInputHelper.Attach(RecurringAmountNumberBox, allowDecimals: true, allowNegative: false);

        RefreshThumbnails();
        PrimaryButtonClick += AddGoalDialog_PrimaryButtonClick;
    }

    private void ShowError(string message)
    {
        DialogErrorBar.Message = message;
        DialogErrorBar.IsOpen = true;
    }

    private void HideError()
    {
        DialogErrorBar.IsOpen = false;
        DialogErrorBar.Message = string.Empty;
    }

    private async void UpdateCurrencyDisplay()
    {
        var selectedCurr = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurr) && CurrencyComboBox.SelectedItem is CurrencyInfo ci)
        {
            selectedCurr = ci.Code;
        }

        var goalCurr = !string.IsNullOrWhiteSpace(selectedCurr) ? selectedCurr.ToUpperInvariant() : "INR";
        var goalSymbol = CountryCurrencyData.GetCurrencySymbol(goalCurr);

        if (TargetAmountLabel != null)
        {
            TargetAmountLabel.Text = $"Target Amount ({goalSymbol})";
        }
        if (TargetAmountNumberBox != null)
        {
            TargetAmountNumberBox.PlaceholderText = $"0.00 ({goalSymbol})";
        }

        var sourceAsset = SourceAccountComboBox.SelectedItem as Asset;
        var sourceCurr = !string.IsNullOrWhiteSpace(sourceAsset?.Currency) ? sourceAsset.Currency.ToUpperInvariant() : goalCurr;
        var sourceSymbol = CountryCurrencyData.GetCurrencySymbol(sourceCurr);

        if (InitialDepositLabel != null)
        {
            InitialDepositLabel.Text = $"Initial Deposit ({sourceSymbol} {sourceCurr}) (Optional)";
        }
        if (InitialDepositNumberBox != null)
        {
            InitialDepositNumberBox.PlaceholderText = $"0.00 ({sourceSymbol})";
        }

        if (RecurringAmountLabel != null)
        {
            RecurringAmountLabel.Text = $"Auto-Fund Amount ({sourceSymbol} {sourceCurr})";
        }
        if (RecurringAmountNumberBox != null)
        {
            RecurringAmountNumberBox.PlaceholderText = $"0.00 ({sourceSymbol})";
        }

        if (InitialDepositFxPanel != null)
        {
            var isCrossCurrency = !string.Equals(sourceCurr, goalCurr, StringComparison.OrdinalIgnoreCase);
            if (isCrossCurrency && sourceAsset != null)
            {
                InitialDepositFxPanel.Visibility = Visibility.Visible;
                InitialDepositFxText.Text = $"Source account currency ({sourceCurr}) differs from goal currency ({goalCurr}). Initial deposit will be deducted in {sourceCurr} and converted at live European Central Bank rate.";

                var depositVal = InitialDepositNumberBox?.Value ?? 0;
                if (!double.IsNaN(depositVal) && depositVal > 0)
                {
                    try
                    {
                        var fxService = App.GetService<IFxRateService>();
                        if (fxService != null)
                        {
                            var rate = await fxService.GetExchangeRateAsync(sourceCurr, goalCurr);
                            if (rate.HasValue && rate.Value > 0)
                            {
                                var converted = Math.Round(depositVal * rate.Value, 2);
                                InitialDepositConvertedText.Text = $"~ Will credit approx. {goalSymbol}{converted:N2} to your goal.";
                                InitialDepositConvertedText.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    InitialDepositConvertedText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                InitialDepositFxPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void SetDefaultCurrency()
    {
        var defaultCurr = "INR";
        try
        {
            var auth = App.GetService<TheRichScript_WinUI.Services.IAuthService>();
            if (!string.IsNullOrWhiteSpace(auth?.CurrentUser?.Currency))
            {
                defaultCurr = auth.CurrentUser.Currency.ToUpperInvariant();
            }
        }
        catch { }
        CurrencyComboBox.SelectedValue = defaultCurr;
    }

    private async void BrowseImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (!_newImageFilePaths.Contains(file.Path))
                    {
                        _newImageFilePaths.Add(file.Path);
                    }
                }
                RefreshThumbnails();
            }
        }
        catch { }
    }

    private void RefreshThumbnails()
    {
        ImageThumbnailsPanel.Children.Clear();

        if (_existingImageUrls.Count == 0 && _newImageFilePaths.Count == 0)
        {
            ImageThumbnailsPanel.Children.Add(NoImagesText);
            return;
        }

        // Render existing images from server
        foreach (var url in _existingImageUrls)
        {
            ImageThumbnailsPanel.Children.Add(CreateThumbnailItem(url, isExisting: true));
        }

        // Render newly added local images
        foreach (var path in _newImageFilePaths)
        {
            ImageThumbnailsPanel.Children.Add(CreateThumbnailItem(path, isExisting: false));
        }
    }

    private FrameworkElement CreateThumbnailItem(string source, bool isExisting)
    {
        var container = new Grid
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 8, 0)
        };

        try
        {
            var img = new Image
            {
                Source = new BitmapImage(new Uri(source)),
                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
            };
            container.Children.Add(img);
        }
        catch { }

        var deleteBtn = new Button
        {
            Content = new FontIcon { Glyph = "\uE711", FontSize = 9 },
            Padding = new Thickness(0),
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(3),
            CornerRadius = new CornerRadius(10),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 0, 0, 0)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Tag = (source, isExisting)
        };

        deleteBtn.Click += (s, _) =>
        {
            if (s is Button b && b.Tag is ValueTuple<string, bool> info)
            {
                if (info.Item2)
                {
                    _existingImageUrls.Remove(info.Item1);
                }
                else
                {
                    _newImageFilePaths.Remove(info.Item1);
                }
                RefreshThumbnails();
            }
        };
        container.Children.Add(deleteBtn);

        return container;
    }

    private void AddGoalDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            ShowError("Goal Name is required.");
            NameBox.Focus(FocusState.Programmatic);
            return;
        }

        var target = TargetAmountNumberBox.Value;
        if ((double.IsNaN(target) || target <= 0) && double.TryParse(TargetAmountNumberBox.Text, out var parsedTarget))
        {
            target = parsedTarget;
        }

        if (double.IsNaN(target) || target <= 0)
        {
            args.Cancel = true;
            ShowError("Please enter a target amount greater than 0.");
            TargetAmountNumberBox.Focus(FocusState.Programmatic);
            return;
        }

        var deposit = InitialDepositNumberBox.Value;
        if (double.IsNaN(deposit))
        {
            if (double.TryParse(InitialDepositNumberBox.Text, out var parsedDeposit))
            {
                deposit = parsedDeposit;
            }
            else
            {
                deposit = 0;
            }
        }
        else if (deposit == 0 && double.TryParse(InitialDepositNumberBox.Text, out var parsedDepositFromText) && parsedDepositFromText > 0)
        {
            deposit = parsedDepositFromText;
        }

        var recurring = RecurringAmountNumberBox.Value;
        if (double.IsNaN(recurring) && double.TryParse(RecurringAmountNumberBox.Text, out var parsedRecurring))
        {
            recurring = parsedRecurring;
        }
        var hasRecurring = !double.IsNaN(recurring) && recurring > 0;
        var freq = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";

        var selectedDepositAsset = DepositAccountComboBox.SelectedItem as Asset;
        var selectedSourceAsset = SourceAccountComboBox.SelectedItem as Asset;

        string? depositAssetId = selectedDepositAsset?.Id;
        string? sourceAssetId = selectedSourceAsset?.Id;

        // Validation for initial deposit: requires source asset and adequate balance
        if (deposit > 0)
        {
            if (selectedSourceAsset == null)
            {
                args.Cancel = true;
                ShowError("Please select a Source Account to fund the initial deposit.");
                SourceAccountComboBox.Focus(FocusState.Programmatic);
                return;
            }

            if (selectedSourceAsset.Value < deposit)
            {
                args.Cancel = true;
                ShowError($"Insufficient funds in {selectedSourceAsset.Name} for initial deposit. Available: {selectedSourceAsset.Currency} {selectedSourceAsset.Value:N2}");
                InitialDepositNumberBox.Focus(FocusState.Programmatic);
                return;
            }
        }

        // Validation for recurring: requires source asset
        if (hasRecurring && string.IsNullOrEmpty(sourceAssetId))
        {
            args.Cancel = true;
            ShowError("Please select a Source Account for the recurring auto-fund amount.");
            SourceAccountComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var selectedCurrency = CurrencyComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCurrency) && CurrencyComboBox.SelectedItem is CurrencyInfo currInfo)
        {
            selectedCurrency = currInfo.Code;
        }

        Result = new CreateGoalRequest
        {
            Name = name,
            TargetAmount = target,
            InitialDeposit = deposit,
            Currency = selectedCurrency ?? "INR",
            AssetId = deposit > 0 ? sourceAssetId : null,
            TargetAssetId = depositAssetId,
            SourceAssetId = (hasRecurring || deposit > 0) ? sourceAssetId : null,
            RecurringAmount = hasRecurring ? recurring : null,
            RecurringFrequency = hasRecurring ? freq : null,
            ExpectedDate = DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(6),
            ImageFilePaths = _newImageFilePaths.ToList()
        };

        if (IsEditMode)
        {
            UpdateResult = new UpdateGoalRequest
            {
                Name = name,
                TargetAmount = target,
                Currency = selectedCurrency ?? "INR",
                TargetAssetId = depositAssetId,
                SourceAssetId = (hasRecurring || sourceAssetId != null) ? sourceAssetId : null,
                RecurringAmount = hasRecurring ? recurring : null,
                RecurringFrequency = hasRecurring ? freq : null,
                ExpectedDate = DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(6),
                IsPinned = _existingGoal?.IsPinned,
                ImageUrls = _existingImageUrls.ToList()
            };
        }
    }
}
