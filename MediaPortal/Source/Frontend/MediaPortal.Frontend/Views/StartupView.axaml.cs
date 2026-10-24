/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The wake screen. The model decides what is asked; this file is the clock and the thumbs: it turns the
  /// wake ring for its one turn and lets it go, breathes the halo, stands one dot in each box a number has
  /// reached with the next one lit - never the number itself - shakes the card when the numbers were wrong,
  /// and takes the keys: digits from anywhere, Backspace or Delete to take one back, the arrows and Tab
  /// between the boxes and the picture, and the back key refused.
  /// </summary>
  /// <remarks>
  /// The animation runs on a dispatcher timer that counts milliseconds, the way <see cref="DissolveImage"/>
  /// counts its dissolve: a movement this short has to be known rather than transitioned, and a timer that
  /// is started on attach and stopped on detach cannot leave a background thread turning a ring nobody can
  /// see. The same reason the boxes are painted from the model's string rather than bound one by one: the
  /// highlight of the next box is one rule ("the box at the PIN's length"), and one rule belongs in one
  /// place.
  /// </remarks>
  public partial class StartupView : UserControl, Workflow.IFullScreenScreen
  {
    private readonly RotateTransform _ringTurn = new RotateTransform();
    private readonly ScaleTransform _haloBreath = new ScaleTransform(1, 1);
    private readonly TranslateTransform _nudge = new TranslateTransform();

    private DispatcherTimer? _clock;
    private DispatcherTimer? _shake;
    private DispatcherTimer? _fade;
    private DateTime _clockFrom;
    private int _shakeStep;
    private int _fadeStep;
    private bool _ringDone;
    private bool _started;
    private StartupViewModel? _model;
    private readonly Workflow.WorkflowManager? _workflow;

    /// <summary>
    /// One turn of the ring: 360° at the wake's 150° a second. The animation is shown for exactly this much
    /// of the screen's clock and then leaves, so it is a greeting rather than a screensaver - but it is
    /// always shown, whatever the server reads happen to take.
    /// </summary>
    private const double PASS_SECONDS = 2.4;

    public StartupView()
    {
      InitializeComponent();
      Focusable = true;

      // One transform each, handed to the controls the XAML named. The names belong to the picture; the
      // movement belongs to this file, and only the movement is written.
      ring.RenderTransform = _ringTurn;
      halo.RenderTransform = _haloBreath;
      pinCard.RenderTransform = _nudge;

      // The keys of the wake. They are claimed on the way in, before the focused control hears them: this
      // screen is not a text field, digits belong to the four boxes and the back key belongs to nobody.
      AddHandler(KeyDownEvent, OnStartupKey, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// The wake as the application runs it: the model that decides what is asked, and the workflow the wall of
    /// faces is raised on. Without a workflow - a wake built for a test, or for the designer - the screen still
    /// shows the question and the boxes; only the household has nowhere to be asked.
    /// </summary>
    public StartupView(StartupViewModel model, Workflow.WorkflowManager? workflow)
        : this()
    {
      _workflow = workflow;
      DataContext = model;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
      base.OnDataContextChanged(e);
      if (_model != null)
        _model.PropertyChanged -= OnModelChanged;
      _model = DataContext as StartupViewModel;
      if (_model == null)
        return;
      _model.PropertyChanged += OnModelChanged;
      _model.PinRejected += Shake;
      PaintBoxes();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
      base.OnAttachedToVisualTree(e);
      _clockFrom = DateTime.UtcNow;
      _clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
      _clock.Tick += delegate { Tick(); };
      _clock.Start();
      Focus();
      if (_model != null && !_started)
      {
        _started = true;
        _ = _model.StartAsync();
      }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
      base.OnDetachedFromVisualTree(e);
      _clock?.Stop();
      _clock = null;
      _shake?.Stop();
      _shake = null;
      _fade?.Stop();
      _fade = null;
    }

    /// <summary>
    /// One heartbeat of the wake: the ring at a steady turn, the halo breathing a sine under it - and, once
    /// the machine has finished reading and its turn is behind it, the ring's way out.
    /// </summary>
    private void Tick()
    {
      double seconds = (DateTime.UtcNow - _clockFrom).TotalSeconds;
      _ringTurn.Angle = seconds * 150.0 % 360.0;
      double breath = 1.0 + 0.07 * Math.Sin(seconds * 2.4);
      _haloBreath.ScaleX = breath;
      _haloBreath.ScaleY = breath;

      // The turn belongs to the wake, not to the reading: while the model still holds the wallpaper to
      // itself the ring carries on, because then it is the whole screen. The moment there is something else
      // to look at - the question, the household - the ring gets its one turn of the screen's clock and
      // steps out, so the animation is never cut by a fast server and never wears out a slow reader.
      if (!_ringDone && _model != null && !_model.Busy && seconds >= PASS_SECONDS)
        FadeRingOut();
    }

    /// <summary>The ring's way out: a third of a second of fading, then the panel is collapsed for good.</summary>
    private void FadeRingOut()
    {
      _ringDone = true;
      _fadeStep = 0;
      _fade = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
      _fade.Tick += delegate
      {
        _fadeStep++;
        wakeRing.Opacity = Math.Max(0.0, 1.0 - _fadeStep / 8.0);
        if (_fadeStep < 8)
          return;

        wakeRing.IsVisible = false;
        _fade?.Stop();
        _fade = null;
      };
      _fade.Start();
    }

    private void OnModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(StartupViewModel.Pin))
        PaintBoxes();
      else if (e.PropertyName == nameof(StartupViewModel.Switching))
      {
        // The household is a dialog the shell raises OVER this screen rather than a panel standing on it, which
        // is what puts the wake behind it out of focus and lets the frame's glow fall off into a dark room
        // instead of into the wake's own ring. So the model saying "faces" means this screen asks the question,
        // and its saying "no faces" means the question is answered and the keys come home.
        if (_model == null)
          return;

        if (_model.Switching)
          AskHousehold();
        else
        {
          PutHouseholdAway();
          Focus();
        }
      }
      else if (e.PropertyName == nameof(StartupViewModel.NeedsPin))
        Focus();
    }

    /// <summary>
    /// Raises the wall of faces as the dialog it is. Asked on the next pass of the dispatcher, because the model
    /// can decide to open the wall while this screen is still being attached to the window, and a workflow asked
    /// to move over a screen that does not know its own size yet is asked at a ground it cannot photograph.
    /// </summary>
    private void AskHousehold()
    {
      Workflow.WorkflowManager? workflow = _workflow;
      if (workflow == null)
        return;

      Dispatcher.UIThread.Post(delegate
      {
        if (_model == null || !_model.Switching || workflow.Current?.StateId == "household")
          return;
        workflow.Push(new Workflow.WorkflowFrame("household", _model.Heading, _model));
      }, DispatcherPriority.Loaded);
    }

    /// <summary>Takes the wall away if it is still up. Pops only its own stop, so a wake that answered by
    /// sending the viewer somewhere else never pops somebody else's screen.</summary>
    private void PutHouseholdAway()
    {
      Workflow.WorkflowFrame? current = _workflow?.Current;
      if (current != null && current.StateId == "household")
        _workflow?.Pop();
    }

    /// <summary>
    /// The four boxes: one plain dot stands in each box a number has reached, the next empty one is the one
    /// the orange border waits on, and behind them the filled ones keep a softer ring. The number itself is
    /// never painted - a living room reads what a television shows. One rule, drawn from the model's string.
    /// </summary>
    private void PaintBoxes()
    {
      if (_model == null)
        return;

      Border[] boxes = { box0, box1, box2, box3 };
      Ellipse[] dots = { dot0, dot1, dot2, dot3 };
      string pin = _model.Pin;
      for (int i = 0; i < boxes.Length; i++)
      {
        dots[i].IsVisible = i < pin.Length;
        boxes[i].Classes.Remove("next");
        boxes[i].Classes.Remove("filled");
        if (i < pin.Length)
          boxes[i].Classes.Add("filled");
        else if (i == pin.Length)
          boxes[i].Classes.Add("next");
      }
    }

    /// <summary>Four numbers said no: the card shakes for half a second and the boxes stand empty again.</summary>
    private void Shake()
    {
      if (_shake != null)
        return;

      _shakeStep = 0;
      _shake = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
      _shake.Tick += delegate
      {
        _shakeStep++;
        if (_shakeStep > 9)
        {
          _nudge.X = 0;
          _shake?.Stop();
          _shake = null;
          return;
        }
        double decay = 1.0 - _shakeStep / 10.0;
        _nudge.X = (_shakeStep % 2 == 0 ? -1.0 : 1.0) * 16.0 * decay;
      };
      _shake.Start();
    }

    /// <summary>
    /// Every key this screen owns. Digits go to the boxes from wherever they are typed - including from the
    /// picture's focus, so walking over there costs nothing typed; Backspace and Delete take the newest one
    /// back; the arrows and Tab walk the two stops this screen has, the four boxes and the round picture;
    /// and the back key is refused outright - while the overlay stands it closes the overlay, because there
    /// the back key at least has an honest meaning.
    /// </summary>
    private void OnStartupKey(object? sender, KeyEventArgs args)
    {
      if (_model == null)
        return;

      if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key)
          || args.Key == Key.Escape
          || args.Key == Key.Back)
      {
        args.Handled = true;
        if ((args.Key == Key.Back || args.Key == Key.Delete) && _model.NeedsPin)
          _model.Backspace();
        else if (_model.Switching)
          _model.StayAsWas();
        return;
      }

      if (args.Key == Key.Delete && _model.NeedsPin)
      {
        // The key a household calls "delete" undoes the last number, which is what it does everywhere else.
        args.Handled = true;
        _model.Backspace();
        return;
      }

      // The wake has two places to stand on: the four boxes and the round picture beside them. The boxes are
      // no control that could hold the focus itself - the screen carries the PIN - so the walk between the
      // two is written here rather than left to the focus manager, which would find one stop and no other.
      // The overlay keeps its own arrows: the list underneath walks them.
      if (_model.NeedsPin && !_model.Switching)
      {
        bool? toPicture = args.Key switch
        {
          Key.Left or Key.Up => true,
          Key.Right or Key.Down => false,
          Key.Tab => !userPicture.IsFocused,
          _ => null
        };
        if (toPicture != null)
        {
          args.Handled = true;
          if (toPicture.Value)
            userPicture.Focus();
          else
            Focus();
          return;
        }
      }

      string? digit = DigitOf(args.Key);
      if (digit != null && _model.NeedsPin && !_model.Switching)
      {
        args.Handled = true;
        _model.Press(digit);
      }
    }

    /// <summary>The digit a key means, top row or pad, or <c>null</c> for every other key.</summary>
    private static string? DigitOf(Key key)
    {
      if (key >= Key.D0 && key <= Key.D9)
        return ((char)('0' + (key - Key.D0))).ToString();
      if (key >= Key.NumPad0 && key <= Key.NumPad9)
        return ((char)('0' + (key - Key.NumPad0))).ToString();
      return null;
    }
  }
}
