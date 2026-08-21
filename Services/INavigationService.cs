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
}

public class NavigationService : INavigationService
{
    private Frame? _shellFrame;
    private Frame? _rootFrame;

    public Type? CurrentPageType => _shellFrame?.CurrentSourcePageType;
    public bool CanGoBack => _shellFrame?.CanGoBack ?? false;

    public event Action<Type?>? Navigated;

    public void Initialize(Frame shellFrame, Frame rootFrame)
    {
        _shellFrame = shellFrame;
        _rootFrame = rootFrame;

        if (_shellFrame != null)
        {
            _shellFrame.Navigated += (s, e) => Navigated?.Invoke(e.SourcePageType);
        }
    }

    public bool NavigateTo(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null)
    {
        if (_shellFrame == null) return false;
        transitionInfo ??= new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
        return _shellFrame.Navigate(pageType, parameter, transitionInfo);
    }

    public bool NavigateRoot(Type pageType, object? parameter = null)
    {
        if (_rootFrame == null) return false;
        return _rootFrame.Navigate(pageType, parameter, new EntranceNavigationTransitionInfo());
    }

    public bool GoBack()
    {
        if (_shellFrame != null && _shellFrame.CanGoBack)
        {
            _shellFrame.GoBack();
            return true;
        }
        return false;
    }
}
