using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views;

/// <summary>
/// The compact transport row that stands at the bottom right-hand corner of every screen but the player's while
/// something plays: the reference master's footer control (master_bare_media.xaml:181-200), worn in this front
/// end's own button design.
/// </summary>
/// <remarks>
/// It is a second pair of hands on one machine rather than a second machine: the row works on the application's
/// single playback model, the same one the player screen drives, because this front end has one player. The
/// reference gave the same row to each of its two player contexts and let <c>IsCurrentPlayer</c> decide which
/// one was showing; here there is one model, so the row is either worth showing or it is not, and the window
/// that hosts it decides that (see ShellWindow.UpdatePlaybackBar).
///
/// The row therefore holds no state of its own - no paused flag, no volume of its own to disagree about - and
/// every mark on it is the model's own answer read the same way the player screen reads it.
/// </remarks>
public partial class PlaybackBar : UserControl
{
  private readonly NowPlayingViewModel? _model;

  public PlaybackBar()
      : this((Application.Current as App)?.Playback)
  {
  }

  public PlaybackBar(NowPlayingViewModel? model)
  {
    _model = model;
    DataContext = model;
    InitializeComponent();
  }

  /// <summary>
  /// The film in front of the viewer: the player screen comes up over the screen the viewer is standing on,
  /// which is what the reference's first footer button did with its push onto the player screen.
  /// </summary>
  private void OnPlayer(object? sender, RoutedEventArgs e)
  {
    (Application.Current as App)?.ShowPlaybackScreen();
  }

  private void OnStop(object? sender, RoutedEventArgs e)
  {
    _model?.Stop();
  }

  private void OnPlayPause(object? sender, RoutedEventArgs e)
  {
    _model?.TogglePause();
  }

  private void OnBack(object? sender, RoutedEventArgs e)
  {
    NowPlayingViewModel? model = _model;
    if (model != null)
      model.Skip(model.InstantSkipMs(-1));
  }

  private void OnForward(object? sender, RoutedEventArgs e)
  {
    NowPlayingViewModel? model = _model;
    if (model != null)
      model.Skip(model.InstantSkipMs(1));
  }

  private void OnChapterBack(object? sender, RoutedEventArgs e)
  {
    _model?.PreviousChapter();
  }

  private void OnChapterForward(object? sender, RoutedEventArgs e)
  {
    _model?.NextChapter();
  }
}
