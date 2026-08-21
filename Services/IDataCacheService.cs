using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

[Flags]
public enum DataCacheType
{
    None = 0,
    Assets = 1,
    AssetTypes = 2,
    Transactions = 4,
    Categories = 8,
    IncomeSources = 16,
    Goals = 32,
    Debts = 64,
    Plans = 128,
    Analytics = 256,
    All = Assets | AssetTypes | Transactions | Categories | IncomeSources | Goals | Debts | Plans | Analytics
}

public interface IDataCacheService
{
    bool IsWarmedUp { get; }

    // Direct synchronous accessors to current in-memory cache
    List<Asset>? CachedAssets { get; }
    List<string>? CachedAssetTypes { get; }
    List<Transaction>? CachedTransactions { get; }
    List<Category>? CachedCategories { get; }
    List<IncomeSource>? CachedIncomeSources { get; }
    List<Goal>? CachedGoals { get; }
    List<Debt>? CachedDebts { get; }
    List<PlannedExpense>? CachedPlans { get; }
    AnalyticsSummary? CachedAnalytics { get; }

    // Async SWR (Stale-While-Revalidate) methods
    Task<List<Asset>> GetAssetsAsync(bool forceRefresh = false);
    Task<List<string>> GetAssetTypesAsync(bool forceRefresh = false);
    Task<List<Transaction>> GetTransactionsAsync(bool forceRefresh = false, int limit = 200);
    Task<List<Category>> GetCategoriesAsync(bool forceRefresh = false);
    Task<List<IncomeSource>> GetIncomeSourcesAsync(bool forceRefresh = false);
    Task<List<Goal>> GetGoalsAsync(bool forceRefresh = false);
    Task<List<Debt>> GetDebtsAsync(bool forceRefresh = false);
    Task<List<PlannedExpense>> GetPlansAsync(bool forceRefresh = false);
    Task<AnalyticsSummary?> GetAnalyticsSummaryAsync(bool forceRefresh = false);

    // Warm-up & Cache Control
    Task WarmUpAllDataAsync(IProgress<string>? progress = null);
    Task RevalidateAsync(DataCacheType cacheTypes = DataCacheType.All, bool force = false);
    void Invalidate(DataCacheType cacheTypes);
    void ClearAll();

    // Event fired whenever fresh data is fetched into cache
    event Action<DataCacheType>? DataUpdated;
}

public class DataCacheService : IDataCacheService
{
    private readonly IAssetService _assetService;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IIncomeSourceService _incomeSourceService;
    private readonly IGoalService _goalService;
    private readonly IDebtService _debtService;
    private readonly IPlanService _planService;
    private readonly IAnalyticsService _analyticsService;

    private readonly SemaphoreSlim _lock = new(1, 1);

    // In-memory cache storage
    public List<Asset>? CachedAssets { get; private set; }
    public List<string>? CachedAssetTypes { get; private set; }
    public List<Transaction>? CachedTransactions { get; private set; }
    public List<Category>? CachedCategories { get; private set; }
    public List<IncomeSource>? CachedIncomeSources { get; private set; }
    public List<Goal>? CachedGoals { get; private set; }
    public List<Debt>? CachedDebts { get; private set; }
    public List<PlannedExpense>? CachedPlans { get; private set; }
    public AnalyticsSummary? CachedAnalytics { get; private set; }

    public bool IsWarmedUp { get; private set; }

    // Cache timestamps for SWR
    private DateTime _lastAssetsFetch = DateTime.MinValue;
    private DateTime _lastAssetTypesFetch = DateTime.MinValue;
    private DateTime _lastTransactionsFetch = DateTime.MinValue;
    private DateTime _lastCategoriesFetch = DateTime.MinValue;
    private DateTime _lastIncomeSourcesFetch = DateTime.MinValue;
    private DateTime _lastGoalsFetch = DateTime.MinValue;
    private DateTime _lastDebtsFetch = DateTime.MinValue;
    private DateTime _lastPlansFetch = DateTime.MinValue;
    private DateTime _lastAnalyticsFetch = DateTime.MinValue;

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromSeconds(30);

    public event Action<DataCacheType>? DataUpdated;

    public DataCacheService(
        IAssetService assetService,
        ITransactionService transactionService,
        ICategoryService categoryService,
        IIncomeSourceService incomeSourceService,
        IGoalService goalService,
        IDebtService debtService,
        IPlanService planService,
        IAnalyticsService analyticsService)
    {
        _assetService = assetService;
        _transactionService = transactionService;
        _categoryService = categoryService;
        _incomeSourceService = incomeSourceService;
        _goalService = goalService;
        _debtService = debtService;
        _planService = planService;
        _analyticsService = analyticsService;
    }

    public async Task WarmUpAllDataAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Loading your financial workspace...");

        try
        {
            var assetsTask = _assetService.GetAssetsAsync();
            var assetTypesTask = _assetService.GetAssetTypesAsync();
            var txTask = _transactionService.GetTransactionsAsync(limit: 200);
            var catsTask = _categoryService.GetCategoriesAsync();
            var sourcesTask = _incomeSourceService.GetIncomeSourcesAsync();
            var goalsTask = _goalService.GetGoalsAsync();
            var debtsTask = _debtService.GetDebtsAsync();
            var plansTask = _planService.GetPlansAsync();
            var analyticsTask = _analyticsService.GetAnalyticsSummaryAsync();

            await Task.WhenAll(
                assetsTask,
                assetTypesTask,
                txTask,
                catsTask,
                sourcesTask,
                goalsTask,
                debtsTask,
                plansTask,
                analyticsTask);

            if (assetsTask.Result.Success) { CachedAssets = assetsTask.Result.Data ?? new(); _lastAssetsFetch = DateTime.UtcNow; }
            if (assetTypesTask.Result.Success) { CachedAssetTypes = assetTypesTask.Result.Data ?? new(); _lastAssetTypesFetch = DateTime.UtcNow; }
            if (txTask.Result.Success) { CachedTransactions = txTask.Result.Data ?? new(); _lastTransactionsFetch = DateTime.UtcNow; }
            if (catsTask.Result.Success) { CachedCategories = catsTask.Result.Data ?? new(); _lastCategoriesFetch = DateTime.UtcNow; }
            if (sourcesTask.Result.Success) { CachedIncomeSources = sourcesTask.Result.Data ?? new(); _lastIncomeSourcesFetch = DateTime.UtcNow; }
            if (goalsTask.Result.Success) { CachedGoals = goalsTask.Result.Data ?? new(); _lastGoalsFetch = DateTime.UtcNow; }
            if (debtsTask.Result.Success) { CachedDebts = debtsTask.Result.Data ?? new(); _lastDebtsFetch = DateTime.UtcNow; }
            if (plansTask.Result.Success) { CachedPlans = plansTask.Result.Data ?? new(); _lastPlansFetch = DateTime.UtcNow; }
            if (analyticsTask.Result.Success) { CachedAnalytics = analyticsTask.Result.Data; _lastAnalyticsFetch = DateTime.UtcNow; }

            IsWarmedUp = true;
            DataUpdated?.Invoke(DataCacheType.All);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataCacheService] Warm-up error: {ex.Message}");
            IsWarmedUp = true; // Mark as warmed up so views can proceed with fallback
        }
    }

    public async Task<List<Asset>> GetAssetsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedAssets != null && DateTime.UtcNow - _lastAssetsFetch < StaleThreshold)
        {
            return CachedAssets;
        }

        if (CachedAssets != null && !forceRefresh)
        {
            // Return stale immediately, revalidate in background
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Assets, force: true));
            return CachedAssets;
        }

        try
        {
            var res = await _assetService.GetAssetsAsync();
            if (res.Success && res.Data != null)
            {
                CachedAssets = res.Data;
                _lastAssetsFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Assets);
            }
        }
        catch { }

        return CachedAssets ?? new List<Asset>();
    }

    public async Task<List<string>> GetAssetTypesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedAssetTypes != null && DateTime.UtcNow - _lastAssetTypesFetch < StaleThreshold)
        {
            return CachedAssetTypes;
        }

        if (CachedAssetTypes != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.AssetTypes, force: true));
            return CachedAssetTypes;
        }

        try
        {
            var res = await _assetService.GetAssetTypesAsync();
            if (res.Success && res.Data != null)
            {
                CachedAssetTypes = res.Data;
                _lastAssetTypesFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.AssetTypes);
            }
        }
        catch { }

        return CachedAssetTypes ?? new List<string> { "BANK", "INVESTMENT", "CRYPTO", "REAL ESTATE", "CASH", "GOLD" };
    }

    public async Task<List<Transaction>> GetTransactionsAsync(bool forceRefresh = false, int limit = 200)
    {
        if (!forceRefresh && CachedTransactions != null && DateTime.UtcNow - _lastTransactionsFetch < StaleThreshold)
        {
            return CachedTransactions;
        }

        if (CachedTransactions != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Transactions, force: true));
            return CachedTransactions;
        }

        try
        {
            var res = await _transactionService.GetTransactionsAsync(limit: limit);
            if (res.Success && res.Data != null)
            {
                CachedTransactions = res.Data;
                _lastTransactionsFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Transactions);
            }
        }
        catch { }

        return CachedTransactions ?? new List<Transaction>();
    }

    public async Task<List<Category>> GetCategoriesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedCategories != null && DateTime.UtcNow - _lastCategoriesFetch < StaleThreshold)
        {
            return CachedCategories;
        }

        if (CachedCategories != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Categories, force: true));
            return CachedCategories;
        }

        try
        {
            var res = await _categoryService.GetCategoriesAsync();
            if (res.Success && res.Data != null)
            {
                CachedCategories = res.Data;
                _lastCategoriesFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Categories);
            }
        }
        catch { }

        return CachedCategories ?? new List<Category>();
    }

    public async Task<List<IncomeSource>> GetIncomeSourcesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedIncomeSources != null && DateTime.UtcNow - _lastIncomeSourcesFetch < StaleThreshold)
        {
            return CachedIncomeSources;
        }

        if (CachedIncomeSources != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.IncomeSources, force: true));
            return CachedIncomeSources;
        }

        try
        {
            var res = await _incomeSourceService.GetIncomeSourcesAsync();
            if (res.Success && res.Data != null)
            {
                CachedIncomeSources = res.Data;
                _lastIncomeSourcesFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.IncomeSources);
            }
        }
        catch { }

        return CachedIncomeSources ?? new List<IncomeSource>();
    }

    public async Task<List<Goal>> GetGoalsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedGoals != null && DateTime.UtcNow - _lastGoalsFetch < StaleThreshold)
        {
            return CachedGoals;
        }

        if (CachedGoals != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Goals, force: true));
            return CachedGoals;
        }

        try
        {
            var res = await _goalService.GetGoalsAsync();
            if (res.Success && res.Data != null)
            {
                CachedGoals = res.Data;
                _lastGoalsFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Goals);
            }
        }
        catch { }

        return CachedGoals ?? new List<Goal>();
    }

    public async Task<List<Debt>> GetDebtsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedDebts != null && DateTime.UtcNow - _lastDebtsFetch < StaleThreshold)
        {
            return CachedDebts;
        }

        if (CachedDebts != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Debts, force: true));
            return CachedDebts;
        }

        try
        {
            var res = await _debtService.GetDebtsAsync();
            if (res.Success && res.Data != null)
            {
                CachedDebts = res.Data;
                _lastDebtsFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Debts);
            }
        }
        catch { }

        return CachedDebts ?? new List<Debt>();
    }

    public async Task<List<PlannedExpense>> GetPlansAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedPlans != null && DateTime.UtcNow - _lastPlansFetch < StaleThreshold)
        {
            return CachedPlans;
        }

        if (CachedPlans != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Plans, force: true));
            return CachedPlans;
        }

        try
        {
            var res = await _planService.GetPlansAsync();
            if (res.Success && res.Data != null)
            {
                CachedPlans = res.Data;
                _lastPlansFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Plans);
            }
        }
        catch { }

        return CachedPlans ?? new List<PlannedExpense>();
    }

    public async Task<AnalyticsSummary?> GetAnalyticsSummaryAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && CachedAnalytics != null && DateTime.UtcNow - _lastAnalyticsFetch < StaleThreshold)
        {
            return CachedAnalytics;
        }

        if (CachedAnalytics != null && !forceRefresh)
        {
            _ = Task.Run(() => RevalidateAsync(DataCacheType.Analytics, force: true));
            return CachedAnalytics;
        }

        try
        {
            var res = await _analyticsService.GetAnalyticsSummaryAsync();
            if (res.Success && res.Data != null)
            {
                CachedAnalytics = res.Data;
                _lastAnalyticsFetch = DateTime.UtcNow;
                DataUpdated?.Invoke(DataCacheType.Analytics);
            }
        }
        catch { }

        return CachedAnalytics;
    }

    public async Task RevalidateAsync(DataCacheType cacheTypes = DataCacheType.All, bool force = false)
    {
        try
        {
            var tasks = new List<Task>();
            var updatedTypes = DataCacheType.None;

            if (cacheTypes.HasFlag(DataCacheType.Assets) && (force || DateTime.UtcNow - _lastAssetsFetch >= StaleThreshold))
            {
                tasks.Add(_assetService.GetAssetsAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedAssets = t.Result.Data;
                        _lastAssetsFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Assets;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.AssetTypes) && (force || DateTime.UtcNow - _lastAssetTypesFetch >= StaleThreshold))
            {
                tasks.Add(_assetService.GetAssetTypesAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedAssetTypes = t.Result.Data;
                        _lastAssetTypesFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.AssetTypes;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Transactions) && (force || DateTime.UtcNow - _lastTransactionsFetch >= StaleThreshold))
            {
                tasks.Add(_transactionService.GetTransactionsAsync(limit: 200).ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedTransactions = t.Result.Data;
                        _lastTransactionsFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Transactions;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Categories) && (force || DateTime.UtcNow - _lastCategoriesFetch >= StaleThreshold))
            {
                tasks.Add(_categoryService.GetCategoriesAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedCategories = t.Result.Data;
                        _lastCategoriesFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Categories;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.IncomeSources) && (force || DateTime.UtcNow - _lastIncomeSourcesFetch >= StaleThreshold))
            {
                tasks.Add(_incomeSourceService.GetIncomeSourcesAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedIncomeSources = t.Result.Data;
                        _lastIncomeSourcesFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.IncomeSources;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Goals) && (force || DateTime.UtcNow - _lastGoalsFetch >= StaleThreshold))
            {
                tasks.Add(_goalService.GetGoalsAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedGoals = t.Result.Data;
                        _lastGoalsFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Goals;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Debts) && (force || DateTime.UtcNow - _lastDebtsFetch >= StaleThreshold))
            {
                tasks.Add(_debtService.GetDebtsAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedDebts = t.Result.Data;
                        _lastDebtsFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Debts;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Plans) && (force || DateTime.UtcNow - _lastPlansFetch >= StaleThreshold))
            {
                tasks.Add(_planService.GetPlansAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedPlans = t.Result.Data;
                        _lastPlansFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Plans;
                    }
                }));
            }

            if (cacheTypes.HasFlag(DataCacheType.Analytics) && (force || DateTime.UtcNow - _lastAnalyticsFetch >= StaleThreshold))
            {
                tasks.Add(_analyticsService.GetAnalyticsSummaryAsync().ContinueWith(t =>
                {
                    if (t.Result.Success && t.Result.Data != null)
                    {
                        CachedAnalytics = t.Result.Data;
                        _lastAnalyticsFetch = DateTime.UtcNow;
                        updatedTypes |= DataCacheType.Analytics;
                    }
                }));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                if (updatedTypes != DataCacheType.None)
                {
                    DataUpdated?.Invoke(updatedTypes);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataCacheService] Revalidate error: {ex.Message}");
        }
    }

    public void Invalidate(DataCacheType cacheTypes)
    {
        if (cacheTypes.HasFlag(DataCacheType.Assets)) _lastAssetsFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.AssetTypes)) _lastAssetTypesFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Transactions)) _lastTransactionsFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Categories)) _lastCategoriesFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.IncomeSources)) _lastIncomeSourcesFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Goals)) _lastGoalsFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Debts)) _lastDebtsFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Plans)) _lastPlansFetch = DateTime.MinValue;
        if (cacheTypes.HasFlag(DataCacheType.Analytics)) _lastAnalyticsFetch = DateTime.MinValue;

        // Trigger asynchronous background revalidation for invalidated caches
        _ = Task.Run(() => RevalidateAsync(cacheTypes, force: true));
    }

    public void ClearAll()
    {
        CachedAssets = null;
        CachedAssetTypes = null;
        CachedTransactions = null;
        CachedCategories = null;
        CachedIncomeSources = null;
        CachedGoals = null;
        CachedDebts = null;
        CachedPlans = null;
        CachedAnalytics = null;
        IsWarmedUp = false;

        _lastAssetsFetch = DateTime.MinValue;
        _lastAssetTypesFetch = DateTime.MinValue;
        _lastTransactionsFetch = DateTime.MinValue;
        _lastCategoriesFetch = DateTime.MinValue;
        _lastIncomeSourcesFetch = DateTime.MinValue;
        _lastGoalsFetch = DateTime.MinValue;
        _lastDebtsFetch = DateTime.MinValue;
        _lastPlansFetch = DateTime.MinValue;
        _lastAnalyticsFetch = DateTime.MinValue;

        DataUpdated?.Invoke(DataCacheType.All);
    }
}
