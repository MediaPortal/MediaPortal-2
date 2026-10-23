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
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The wake screen. The model decides what is asked; this file is the clock and the thumbs: it turns the
  /// wake ring, breathes the halo, draws four characters of the model's PIN into the four boxes with the
  /// next one lit, shakes the card when the numbers were wrong, and takes keys - digits from anywhere,
  /// Backspace to take one back, the back key refused, Enter on a picked name taken.
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
    private DateTime _clockFrom;
    private int _shakeStep;
    private bool _started;
    private StartupViewModel? _model;

    public StartupView()
    {
      InitializeComponent();
      Focusable = true;

      // One transform each, handed to the controls the XAML named. The names belong to the picture; the
      // movement belongs to this file, and only the movement is written.
      ring.RenderTransform = _ringTurn;
      halo.RenderTransform = _haloBreath;
      pinCard.RenderTransform = _nudge;

      // Enter on a picked name takes it, like the server list on the connection screen; a double tap is the
      // same answer for the pointer.
      othersList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;
            if (_model != null && othersList.SelectedItem is UserInfo picked)
            {
              args.Handled = true;
              _model.PickOther(picked);
            }
          },
          RoutingStrategies.Tunnel);
      othersList.DoubleTapped += delegate
      {
        if (_model != null && othersList.SelectedItem is UserInfo picked)
          _model.PickOther(picked);
      };

      // The keys of the wake. They are claimed on the way in, before the focused control hears them: this
      // screen is not a text field, digits belong to the four boxes and the back key belongs to nobody.
      AddHandler(KeyDownEvent, OnStartupKey, RoutingStrategies.Tunnel);
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
    }

    /// <summary>One heartbeat of the wake: the ring at a steady turn, the halo breathing a sine under it.</summary>
    private void Tick()
    {
      double seconds = (DateTime.UtcNow - _clockFrom).TotalSeconds;
      _ringTurn.Angle = seconds * 150.0 % 360.0;
      double breath = 1.0 + 0.07 * Math.Sin(seconds * 2.4);
      _haloBreath.ScaleX = breath;
      _haloBreath.ScaleY = breath;
    }

    private void OnModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(StartupViewModel.Pin))
        PaintBoxes();
      else if (e.PropertyName == nameof(StartupViewModel.NeedsPin)
          || e.PropertyName == nameof(StartupViewModel.Switching))
        Focus();
    }

    /// <summary>
    /// The four boxes: the typed numbers stand in theirs, the next empty one is the one the orange border
    /// waits on, and behind them the filled ones keep a softer ring. One rule, drawn from the model's string.
    /// </summary>
    private void PaintBoxes()
    {
      if (_model == null)
        return;

      Border[] boxes = { box0, box1, box2, box3 };
      TextBlock[] digits = { digit0, digit1, digit2, digit3 };
      string pin = _model.Pin;
      for (int i = 0; i < boxes.Length; i++)
      {
        digits[i].Text = i < pin.Length ? pin[i].ToString() : "";
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
    /// Every key this screen owns. Digits go to the boxes from wherever they are typed; Backspace takes the
    /// newest one back; the back key is refused outright - while the overlay stands it closes the overlay,
    /// because there the back key at least has an honest meaning.
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
        if (args.Key == Key.Back && _model.NeedsPin)
          _model.Backspace();
        else if (_model.Switching)
          _model.StayAsWas();
        return;
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

  /// <summary>
  /// The one word a row of "who then?" adds beside a name: whether that person will ask for four numbers.
  /// </summary>
  public sealed class PinOrFreeConverter : IValueConverter
  {
    public static readonly PinOrFreeConverter Instance = new PinOrFreeConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      return value is true ? "asks for a PIN" : "walks right in";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
