using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TheRichScript_WinUI.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class AddGoalDialog : ContentDialog
{
    private readonly List<string> _selectedImagePaths = new();

    public CreateGoalRequest? Result { get; private set; }

    public AddGoalDialog(List<Asset> assets, Goal? existingGoal = null)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        AssetComboBox.ItemsSource = assets;
        if (assets.Count > 0)
        {
            AssetComboBox.SelectedIndex = 0;
        }

        DatePicker.Date = DateTimeOffset.Now.AddMonths(6);

        if (existingGoal != null)
        {
            Title = "Edit Financial Goal";
            NameBox.Text = existingGoal.Name;
            TargetAmountNumberBox.Value = existingGoal.TargetAmount;
            DatePicker.Date = existingGoal.ExpectedDate;

            if (existingGoal.RecurringAmount.HasValue)
            {
                RecurringAmountNumberBox.Value = existingGoal.RecurringAmount.Value;
            }

            var matchAsset = assets.FirstOrDefault(a => a.Id == existingGoal.SourceAssetId || a.Id == existingGoal.TargetAssetId);
            if (matchAsset != null)
            {
                AssetComboBox.SelectedItem = matchAsset;
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
        }

        PrimaryButtonClick += AddGoalDialog_PrimaryButtonClick;
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
                    if (!_selectedImagePaths.Contains(file.Path))
                    {
                        _selectedImagePaths.Add(file.Path);
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

        if (_selectedImagePaths.Count == 0)
        {
            ImageThumbnailsPanel.Children.Add(NoImagesText);
            return;
        }

        foreach (var path in _selectedImagePaths)
        {
            var container = new Grid
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 4, 0)
            };

            try
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(path)),
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                };
                container.Children.Add(img);
            }
            catch { }

            var deleteBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE711", FontSize = 8 },
                Padding = new Thickness(2),
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(2),
                Tag = path
            };
            deleteBtn.Click += (s, _) =>
            {
                if (s is Button b && b.Tag is string p)
                {
                    _selectedImagePaths.Remove(p);
                    RefreshThumbnails();
                }
            };
            container.Children.Add(deleteBtn);

            ImageThumbnailsPanel.Children.Add(container);
        }
    }

    private void AddGoalDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameBox.Text.Trim();
        var target = TargetAmountNumberBox.Value;

        if (string.IsNullOrWhiteSpace(name) || double.IsNaN(target) || target <= 0)
        {
            args.Cancel = true;
            return;
        }

        var deposit = InitialDepositNumberBox.Value;
        if (double.IsNaN(deposit)) deposit = 0;

        var recurring = RecurringAmountNumberBox.Value;
        var hasRecurring = !double.IsNaN(recurring) && recurring > 0;
        var freq = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MONTHLY";

        string? assetId = null;
        if (AssetComboBox.SelectedItem is Asset selectedAsset)
        {
            assetId = selectedAsset.Id;
        }

        if (deposit > 0 && string.IsNullOrEmpty(assetId))
        {
            args.Cancel = true;
            return;
        }

        Result = new CreateGoalRequest
        {
            Name = name,
            TargetAmount = target,
            InitialDeposit = deposit,
            AssetId = assetId,
            SourceAssetId = hasRecurring ? assetId : null,
            RecurringAmount = hasRecurring ? recurring : null,
            RecurringFrequency = hasRecurring ? freq : null,
            ExpectedDate = DatePicker.Date?.DateTime ?? DateTime.Now.AddMonths(6),
            ImageFilePaths = _selectedImagePaths.ToList()
        };
    }
}
