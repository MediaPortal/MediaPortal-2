using System;
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
  /// <summary>
  /// How wide the film's line stands, as a share of the screen's width, however many plates are under it. The
  /// line is the one thing on this row that speaks about the film rather than about the row's own use, and a
  /// film does not change length because the row beside it lost a chapter button.
  /// </summary>
  internal const double LINE_SHARE = 0.30;

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
  /// Holds the film's line to <see cref="LINE_SHARE"/> of the screen. The width arrives from the window because
  /// a control knows no percentage of a screen and this row measures itself to the plates it carries, which are
  /// not a measure of anything outside the row: the plates come and go with what plays (a stream has no chapters
  /// to jump between), so a line left to the row's width would stretch and shrink while the film ran on.
  /// </summary>
  /// <param name="widthInPoints">
  /// The screen's width in the points the skin lays out in - the window's dots divided by its skin scale, which
  /// is what the row's own measures are in (see ShellWindow.SkinScale).
  /// </param>
  public void HoldLineToScreen(double widthInPoints)
  {
    if (progressLine == null || widthInPoints < 1.0)
      return; // no screen to measure against yet; the first layout pass brings one

    double across = Math.Round(widthInPoints * LINE_SHARE);
    if (Math.Abs(progressLine.Width - across) < 0.5)
      return; // the same answer twice would be a layout pass thrown away on every chrome tick

    progressLine.Width = across;
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

  /// <summary>
  /// Ask for the level of the sound. The dialog is not this row's own: the window raises the very dialog the
  /// player's loudspeaker mark raises (Views/VolumePicker.axaml) over the screen the viewer is standing on, so
  /// the number a viewer reads is the number the film plays at, wherever they were standing when they changed it.
  /// </summary>
  private void OnSound(object? sender, RoutedEventArgs e)
  {
    (TopLevel.GetTopLevel(this) as ShellWindow)?.OpenSoundDialog();
  }
}
