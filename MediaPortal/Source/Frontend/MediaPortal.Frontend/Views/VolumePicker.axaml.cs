using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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

  /// <summary>How far the dialog leans against an edge and springs back - the player panel's own numbers.</summary>
  private const double DIALOG_BOUNCE_DIVE = 22;
  private const int DIALOG_BOUNCE_DIVE_MS = 90;
  private const int DIALOG_BOUNCE_SPRING_MS = 430;

  private NowPlayingViewModel? _model;
  private bool _dragging;
  private double _keptRowHeight;
  private DispatcherTimer? _bounceTimer;
  private TranslateTransform? _bounceTransform;

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

    // The dialog is an upright column, and the four keys answer to that column rather than to a ring: up and
    // down step from mark to mark - over the line of the level, which is a thing a hand points at rather than a
    // mark a remote stands on - and a key aimed past the head, the foot or either side of a one-mark-wide
    // dialog leans the whole dialog against that edge and springs it back, which is how the player's panel
    // answers the same question. Claimed on the way in, so the answer is the dialog's before any ring or list
    // of the screen behind it can take the key.
    AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
        {
          if (args.Handled || !IsOpen)
            return;

          bool upward = args.Key == Key.Up;
          bool downward = args.Key == Key.Down;
          bool leftward = args.Key == Key.Left;
          bool rightward = args.Key == Key.Right;
          if (!upward && !downward && !leftward && !rightward)
            return;

          // Only while the caret stands inside the dialog; the screen behind it keeps its own walk.
          if (!(args.Source is Visual stood) || !IsWithinDialog(stood))
            return;

          // The keys across the line of the sound belong to the sound itself, as they always have.
          if ((leftward || rightward) && ReferenceEquals(stood, volumeControl))
            return;

          if (upward || downward)
          {
            MoveToMark(downward ? 1 : -1, stood);
          }
          else
          {
            // A dialog one mark wide has nothing to the side of the mark it stands on.
            Bounce(leftward ? -1 : 1, 0);
          }

          args.Handled = true;
        },
        RoutingStrategies.Tunnel);
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
    // Stood on the first mark of the dialog rather than on its line: a viewer who came for the sound stands
    // first where a press does something, and the line of the level is read from there.
    volumeUpButton.Focus();
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
  /// The direction the dialog last leaned against an edge, in points: positive off the bottom or the right,
  /// negative off the top or the left, zero when it rests. A seam for the tests, like the player panel's own.
  /// </summary>
  internal double VolumeBounceDiveY { get; private set; }

  /// <summary>The sideways mate of <see cref="VolumeBounceDiveY"/>.</summary>
  internal double VolumeBounceDiveX { get; private set; }

  /// <summary>
  /// The marks a remote stands on, in the order they stand down the dialog. The line of the level is not among
  /// them: it is the sound itself, pointed at or dragged by a hand and answered by the marks either side of it,
  /// which is why stepping through the dialog walks from one mark to the next and over it.
  /// </summary>
  private Control[] Marks
  {
    get { return new Control[] { volumeUpButton, volumeDownButton, muteButton }; }
  }

  /// <summary>Whether <paramref name="element"/> is the dialog itself or stands inside it.</summary>
  private bool IsWithinDialog(Visual element)
  {
    for (Visual? at = element; at != null; at = at.GetVisualParent())
      if (ReferenceEquals(at, volumeDialog))
        return true;
    return false;
  }

  /// <summary>
  /// Step one mark up or down the dialog, and lean the dialog against its edge when nothing stands further.
  /// A caret that was put on the line of the level by a hand rather than by this walk steps to the nearest
  /// mark that the key asks for.
  /// </summary>
  private void MoveToMark(int downward, Visual stood)
  {
    Control[] marks = Marks;
    int at = -1;
    for (int i = 0; i < marks.Length; i++)
      if (ReferenceEquals(marks[i], stood))
        at = i;

    // Not standing on a mark means standing on the line of the level between them: the key then reaches for the
    // nearest mark that way, which is the mark under the line downward and the mark over it upward.
    int toward = at >= 0 ? at + downward : (downward > 0 ? 1 : 0);

    if (toward < 0 || toward >= marks.Length)
    {
      Bounce(0, downward);
      return;
    }

    marks[toward].Focus();
  }

  /// <summary>
  /// Lean the whole dialog against one of its edges and spring it back - the player panel's own answer to a key
  /// aimed past an edge, and read the same way: ninety milliseconds of lean squared off, four hundred and thirty
  /// of easeOutBack home, which carries it a tenth of the lean past its rest before it settles.
  /// </summary>
  /// <param name="directionX">+1 off the right edge, -1 off the left, 0 for a vertical nod.</param>
  /// <param name="directionY">+1 off the bottom edge, -1 off the top, 0 for a sideways nod.</param>
  private void Bounce(int directionX, int directionY)
  {
    // One lean at a time; a key pressed while the dialog is still travelling starts a fresh one from wherever
    // it stands, so a viewer leaning on the arrow sees the dialog lean with every press.
    _bounceTimer?.Stop();

    TranslateTransform nod = volumeDialog.RenderTransform as TranslateTransform;
    if (nod == null)
    {
      nod = new TranslateTransform();
      volumeDialog.RenderTransform = nod;
    }
    _bounceTransform = nod;

    double diveX = directionX * DIALOG_BOUNCE_DIVE;
    double diveY = directionY * DIALOG_BOUNCE_DIVE;
    double fromX = nod.X;
    double fromY = nod.Y;
    long started = Environment.TickCount64;
    VolumeBounceDiveX = diveX;
    VolumeBounceDiveY = diveY;

    DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
    {
      Interval = TimeSpan.FromMilliseconds(16)
    };
    _bounceTimer = timer;

    timer.Tick += delegate
    {
      if (!ReferenceEquals(_bounceTimer, timer))
        return; // a newer lean drives the dialog from here on

      long elapsed = Environment.TickCount64 - started;
      if (elapsed >= DIALOG_BOUNCE_DIVE_MS + DIALOG_BOUNCE_SPRING_MS)
      {
        timer.Stop();
        nod.X = 0;
        nod.Y = 0;
        return;
      }

      if (elapsed <= DIALOG_BOUNCE_DIVE_MS)
      {
        double u = elapsed / (double)DIALOG_BOUNCE_DIVE_MS;
        double lean = 1 - (1 - u) * (1 - u);
        nod.X = fromX + (diveX - fromX) * lean;
        nod.Y = fromY + (diveY - fromY) * lean;
        return;
      }

      double v = (elapsed - DIALOG_BOUNCE_DIVE_MS) / (double)DIALOG_BOUNCE_SPRING_MS;
      const double c1 = 1.70158;
      const double c3 = c1 + 1;
      double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
      nod.X = diveX * (1 - back);
      nod.Y = diveY * (1 - back);
    };
    timer.Start();
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
