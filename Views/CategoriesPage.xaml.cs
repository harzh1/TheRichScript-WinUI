using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TheRichScript_WinUI.Dialogs;
using TheRichScript_WinUI.Models;
using TheRichScript_WinUI.Services;
using TheRichScript_WinUI.ViewModels;

namespace TheRichScript_WinUI.Views;

public sealed partial class CategoriesPage : Page
{
    public CategoriesViewModel ViewModel { get; }

    public CategoriesPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<CategoriesViewModel>();
        DataContext = ViewModel;

        Loaded += CategoriesPage_Loaded;
    }

    private async void CategoriesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCategoriesAsync();
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var dataCache = App.GetService<IDataCacheService>();
        var categoryService = App.GetService<ICategoryService>();
        var dialogService = App.GetService<IDialogService>();
        var busy = App.GetService<IBusyIndicatorService>();

        var dialog = new AddCategoryDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result != null)
        {
            using (busy?.BeginBusy("Creating category limit..."))
            {
                var createResp = await categoryService.CreateCategoryAsync(dialog.Result);
                if (createResp.Success)
                {
                    dataCache?.Invalidate(DataCacheType.Categories);
                    await ViewModel.LoadCategoriesAsync();
                }
                else
                {
                    await dialogService.ShowErrorAsync(createResp.Error ?? "Failed to create category.");
                }
            }
        }
    }

    private async void EditCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Category category)
        {
            var dataCache = App.GetService<IDataCacheService>();
            var categoryService = App.GetService<ICategoryService>();
            var dialogService = App.GetService<IDialogService>();
            var busy = App.GetService<IBusyIndicatorService>();

            var dialog = new AddCategoryDialog(category)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                using (busy?.BeginBusy($"Updating category '{category.Name}'..."))
                {
                    var updateReq = new UpdateCategoryRequest
                    {
                        Name = dialog.Result.Name,
                        MonthlyLimit = dialog.Result.MonthlyLimit,
                        YearlyLimit = dialog.Result.YearlyLimit
                    };

                    var updateResp = await categoryService.UpdateCategoryAsync(category.Id, updateReq);
                    if (updateResp.Success)
                    {
                        dataCache?.Invalidate(DataCacheType.Categories);
                        await ViewModel.LoadCategoriesAsync();
                    }
                    else
                    {
                        await dialogService.ShowErrorAsync(updateResp.Error ?? "Failed to update category.");
                    }
                }
            }
        }
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Category category)
        {
            await ViewModel.DeleteCategoryCommand.ExecuteAsync(category);
        }
    }
}
