using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views;

/// <summary>
/// The dialog of the sound: the level of the film as an upright line that a hand presses, drags or steps, with
/// the number of it over its head and the mark that silences the film under its foot.
/// </summary>
/// <remarks>
/// It is a control of its own rather than a piece of the player's panel because two places ask for the same
/// level: the player's loudspeaker mark in its row of controls, and the loudspeaker plate of the compact
/// transport row at the foot of every other screen. One control worn twice means the number a viewer reads is
/// the number the film plays at, wherever they were standing when they changed it.
///
/// Like the player's panel it is a dialog and not a screen: the level is a word about the film that is playing,
/// and Back goes back to the film rather than out of a place.
/// </remarks>
public partial class VolumePicker : UserControl
{
  /// <summary>How much one key or one mark on the dialog moves the sound, which is also what the machine's own
  /// volume keys move it by, so that the two answers agree. </summary>
  private const int VOLUME_STEP = 5;

  /// <summary>The room the dialog keeps to the left and right edges of whatever screen it stands over.</summary>
  private const double SIDE_MARGIN = 48;

  /// <summary>The room between the head of the row below it and the foot of the dialog.</summary>
  private const double ABOVE_ROW = 24;

  private NowPlayingViewModel? _model;
  private bool _dragging;
  private double _keptRowHeight;

  public VolumePicker()
  {
    InitializeComponent();

    // The knob and the filled part stand at a height measured along the line, so a line that is laid out again
    // has to be asked where they go.
    volumeRow.SizeChanged += delegate { Repaint(); };

    // The line of the sound answers the hand as well as the key, and it asks for the press with the handled
    // flag honoured: the control it lives in is a button, and a button takes the press for its own click first
    // and marks it done with. Asked the ordinary way, the line would sit there stone deaf to a pointer and
    // only a remote could move the volume.
    volumeControl.AddHandler(PointerPressedEvent, OnVolumePointer, RoutingStrategies.Bubble, true);
    volumeControl.AddHandler(PointerMovedEvent, OnVolumeMoved, RoutingStrategies.Bubble, true);
    volumeControl.AddHandler(PointerCaptureLostEvent, OnVolumeReleased, RoutingStrategies.Bubble, true);
  }

  /// <summary>Raised when the dialog goes back down, so that the host can ask its screen for the keyboard.</summary>
  public event Action? Closed;

  /// <summary>Whether the dialog of the sound is standing over the picture.</summary>
  public bool IsOpen
  {
    get { return volumeDialog != null && volumeDialog.IsVisible; }
  }

  /// <summary>
  /// Raise the dialog over the picture and hand it the model whose level it shows and sets. The model is asked
  /// for each time rather than kept from the last opening because the two hosts are built at different moments
  /// of the program's life, and one of them may be standing before the application has a playback model at all.
  /// </summary>
  public void Open(NowPlayingViewModel model)
  {
    HoldModel(model);
    Repaint();
    volumeDialog.IsVisible = true;
    volumeControl.Focus();
  }

  /// <summary>Send the dialog back down, and give the plate that called it the standing back.</summary>
  public void Close()
  {
    if (!IsOpen)
      return;

    volumeDialog.IsVisible = false;
    Closed?.Invoke();
  }

  /// <summary>
  /// Stand the dialog clear of the row beneath it rather than over it: how much room that row takes is a height
  /// only the layout knows, so the host says it. The last height it was told is kept, because the row goes away
  /// with its buttons while the dialog is still answering a hand.
  /// </summary>
  public void HoldAbove(double heightOfRowBelow)
  {
    if (heightOfRowBelow > 8)
      _keptRowHeight = heightOfRowBelow;
    Margin = new Thickness(SIDE_MARGIN, 0, SIDE_MARGIN, _keptRowHeight + ABOVE_ROW);
  }

  /// <summary>
  /// Give the dialog the model to speak for, and follow that model while the dialog stands: the room's own
  /// volume keys and its mute key move the sound without ever asking this dialog, and the line has to follow
  /// whatever the number has become.
  /// </summary>
  private void HoldModel(NowPlayingViewModel model)
  {
    if (ReferenceEquals(_model, model))
      return;

    if (_model != null)
      _model.PropertyChanged -= OnModelChanged;

    _model = model;
    DataContext = model;
    _model.PropertyChanged += OnModelChanged;
  }

  private void OnModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    Repaint();
  }

  /// <summary>
  /// Lay the filled part of the sound's line and its knob where the sound stands. The line is upright, so both
  /// grow from its foot: the filled part is as tall as the sound is high, and the knob stands at the head of
  /// that. Asked for whenever the number moved - by a hand on the line, a key standing on it, or the room's own
  /// volume keys - and whenever the line was laid out again.
  /// </summary>
  public void Repaint()
  {
    NowPlayingViewModel? model = _model;
    if (model == null || volumeRow == null)
      return;

    double height = volumeRow.Bounds.Height;
    if (height < 8)
      return;

    int volume = Math.Clamp(model.Volume, 0, 100);
    double at = height * volume / 100.0;
    volumePlayed.Height = at;

    double knob = volumeKnob.Bounds.Height;
    double fromFoot = at - knob / 2;
    if (fromFoot < 0)
      fromFoot = 0;
    if (fromFoot > height - knob)
      fromFoot = height - knob;
    // the knob hangs from the foot of its line by that distance, which is what its bottom margin means to a
    // panel that lays its children from the top down
    volumeKnob.Margin = new Thickness(0, 0, 0, fromFoot);
  }

  /// <summary>
  /// Put the sound at a fraction of the line: nothing of it for a press at its foot, all of it for a press at
  /// its head. Asked for by the hand through <see cref="SetVolumeWherePointed"/>, and asked for directly by the
  /// tests, which can hand over a fraction but not a place on a screen.
  /// </summary>
  public void SetVolumeAtFraction(double fraction)
  {
    NowPlayingViewModel? model = _model;
    if (model == null || volumeRow.Bounds.Height < 8)
      return;

    if (fraction < 0)
      fraction = 0;
    if (fraction > 1)
      fraction = 1;

    model.SetVolume((int)Math.Round(fraction * 100));
    Repaint();
  }

  /// <summary>One step quieter, the step the viewer set in the playback answers.</summary>
  private void OnVolumeDown(object? sender, RoutedEventArgs e)
  {
    StepVolume(-VOLUME_STEP);
  }

  /// <summary>One step louder.</summary>
  private void OnVolumeUp(object? sender, RoutedEventArgs e)
  {
    StepVolume(VOLUME_STEP);
  }

  /// <summary>
  /// Move the sound by one step and say where it landed, which is what the keys standing on the line and the
  /// two marks at its ends all do.
  /// </summary>
  private void StepVolume(int step)
  {
    NowPlayingViewModel? model = _model;
    if (model == null)
      return;

    model.SetVolume(model.Volume + step);
    Repaint();
    Say("Superlayer_Volume", "Volume " + model.Volume + " %", 2000);
  }

  /// <summary>Silence the film, and un-silence it. Said by the mark itself, which is the mute answer of the model.</summary>
  private void OnMute(object? sender, RoutedEventArgs e)
  {
    _model?.ToggleMute();
  }

  /// <summary>
  /// Where the viewer left the loudspeaker. The line of the sound answers a press and a drag anywhere along it,
  /// the arrow keys while a remote stands on it, and nothing else - the television's own volume keys belong to
  /// the room and reach it from anywhere, which is why this line does not have to answer the wheel as well.
  /// </summary>
  private void OnVolumePointer(object? sender, PointerPressedEventArgs args)
  {
    // The mark itself is not marked handled: the button keeps its own hold on the pointer for the length of the
    // press, which is what brings the moving and the releasing back here from anywhere on the screen.
    _dragging = true;
    SetVolumeWherePointed(args);
  }

  /// <summary>
  /// Dragging along the line while the press still holds moves the sound with the hand.
  /// </summary>
  private void OnVolumeMoved(object? sender, PointerEventArgs args)
  {
    if (!_dragging || !args.GetCurrentPoint(volumeRow).Properties.IsLeftButtonPressed)
      return;

    SetVolumeWherePointed(args);
  }

  /// <summary>
  /// The hand left the line. The reading of the new volume goes up now rather than on every pixel of the drag,
  /// because a number that flickers eighty times across the screen while a hand moves says nothing.
  /// </summary>
  private void OnVolumeReleased(object? sender, PointerCaptureLostEventArgs args)
  {
    if (!_dragging)
      return;

    _dragging = false;
    if (_model != null)
      Say("Superlayer_Volume", "Volume " + _model.Volume + " %", 2000);
  }

  /// <summary>
  /// Put the sound where the hand pointed, and follow the hand while it moves. The track, its filled part and
  /// the knob are all out of the way of the pointer, so the whole height of the line belongs to this: a viewer
  /// who aims at a third of the line means a third, not "one step quieter". The line stands upright, so the
  /// hand is read from its foot upward.
  /// </summary>
  private void SetVolumeWherePointed(PointerEventArgs args)
  {
    double height = volumeRow.Bounds.Height;
    if (height < 8)
      return;

    SetVolumeAtFraction(1.0 - args.GetPosition(volumeRow).Y / height);
  }

  /// <summary>
  /// Standing on the line of the sound, the keys across it mean the sound: left takes the volume down, right
  /// brings it up, by the same five percent the television's own volume keys move it. Up and down are left
  /// alone - they belong to the ring, and a control that claimed all four keys would be a trap a remote could
  /// only leave by guessing.
  /// </summary>
  private void OnVolumeKey(object? sender, KeyEventArgs args)
  {
    int step;
    switch (args.Key)
    {
      case Key.Left:
        step = -VOLUME_STEP;
        break;
      case Key.Right:
        step = VOLUME_STEP;
        break;
      default:
        return;
    }

    StepVolume(step);
    args.Handled = true;
  }

  /// <summary>
  /// Put the reading of the new level on the window's superlayer, which is where every other answer of the
  /// player is said - the dialog does not know which screen it was raised over.
  /// </summary>
  private void Say(string templateKey, string text, int autoHideMs)
  {
    (TopLevel.GetTopLevel(this) as ShellWindow)?.ShowSuperlayer(templateKey, text, autoHideMs);
  }
}
