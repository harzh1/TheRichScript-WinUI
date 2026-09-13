using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace TheRichScript_WinUI.Services;

public interface INavigationService
{
    void Initialize(Frame shellFrame, Frame rootFrame);
    bool NavigateTo(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null);
    bool NavigateRoot(Type pageType, object? parameter = null);
    bool GoBack();
    bool CanGoBack { get; }
    Type? CurrentPageType { get; }
    event Action<Type?>? Navigated;
    event Action<bool>? CanGoBackChanged;
}

public class NavigationService : INavigationService
{
    private Frame? _shellFrame;
    private Frame? _rootFrame;

    public Type? CurrentPageType => _shellFrame?.CurrentSourcePageType ?? _rootFrame?.CurrentSourcePageType;
    public bool CanGoBack => (_shellFrame != null && _shellFrame.CanGoBack) || (_rootFrame != null && _rootFrame.CanGoBack);

    public event Action<Type?>? Navigated;
    public event Action<bool>? CanGoBackChanged;

    public void Initialize(Frame shellFrame, Frame rootFrame)
    {
        if (_shellFrame != null)
        {
            _shellFrame.Navigated -= OnFrameNavigated;
        }
        if (_rootFrame != null && _rootFrame != _shellFrame)
        {
            _rootFrame.Navigated -= OnFrameNavigated;
        }

        _shellFrame = shellFrame;
        _rootFrame = rootFrame;

        if (_shellFrame != null)
        {
            _shellFrame.Navigated += OnFrameNavigated;
        }
        if (_rootFrame != null && _rootFrame != _shellFrame)
        {
            _rootFrame.Navigated += OnFrameNavigated;
        }

        CanGoBackChanged?.Invoke(CanGoBack);
    }

    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Navigated?.Invoke(e.SourcePageType);
        CanGoBackChanged?.Invoke(CanGoBack);
    }

    public bool NavigateTo(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null)
    {
        if (_shellFrame == null) return false;
        if (_shellFrame.CurrentSourcePageType == pageType && parameter == null) return false;
        transitionInfo ??= new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
        var result = _shellFrame.Navigate(pageType, parameter, transitionInfo);
        CanGoBackChanged?.Invoke(CanGoBack);
        return result;
    }

    public bool NavigateRoot(Type pageType, object? parameter = null)
    {
        if (_rootFrame == null) return false;
        var result = _rootFrame.Navigate(pageType, parameter, new EntranceNavigationTransitionInfo());
        _rootFrame.BackStack.Clear();
        CanGoBackChanged?.Invoke(CanGoBack);
        return result;
    }

    public bool GoBack()
    {
        if (_shellFrame != null && _shellFrame.CanGoBack)
        {
            _shellFrame.GoBack();
            CanGoBackChanged?.Invoke(CanGoBack);
            return true;
        }
        if (_rootFrame != null && _rootFrame.CanGoBack)
        {
            _rootFrame.GoBack();
            CanGoBackChanged?.Invoke(CanGoBack);
            return true;
        }
        return false;
    }
}
