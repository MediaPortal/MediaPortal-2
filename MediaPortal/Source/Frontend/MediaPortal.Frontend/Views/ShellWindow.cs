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
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The one window of the front end: the workflow's stack shown as a screen, with the trail of stops above it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Screens are not windows and opening one is not <c>Show()</c> - the old client proved where that ends, and so
  /// did the first version of this one: a stack of windows behind each other, back buttons that closed the wrong
  /// one, and no way to see where you were. Here the workflow manager keeps the stack, this window shows the
  /// control of the frame on top, and the back key pops the stack - which is why "back" leads to exactly the
  /// screen the viewer came from, still scrolled where they left it, every time.
  /// </para>
  /// <para>
  /// Above the screen sits the breadcrumb trail, one clickable label per open stop, for the same reason the old
  /// skin engine drew its <c>WorkflowNavigationBar</c>: a viewer who knows where they want to go should not have
  /// to press back once per stop to get there. It stays out of the way while the stack is one deep - at home no
  /// one needs to be told they are at home.
  /// </para>
  /// <para>
  /// The back key arrives as a bubbling event rather than the tunnel the screens use for their own keys: a screen
  /// that can answer Escape - closing its details panel, dismissing its search - hears it first and the shell only
  /// pops what nobody claimed.
  /// </para>
  /// </remarks>
  public sealed class ShellWindow : Window
  {
    private readonly Workflow.WorkflowManager _workflow;
    private readonly Func<Workflow.WorkflowFrame, Control?>? _viewFactory;
    private readonly StackPanel _trail = new StackPanel();
    private readonly ContentControl _screenArea = new ContentControl();

    /// <summary>
    /// The layer a question is raised on: a frame whose view is an <see cref="Workflow.IDialogScreen"/> stands
    /// here, over the screen it was asked from, which stays exactly where it was - lit, scrolled, selected. The
    /// reference's dialogs were never masters of their own, and a dialog that replaced the screen behind it would
    /// hide the very thing the viewer is being asked about.
    /// </summary>
    private readonly ContentControl _dialogLayer = new ContentControl();

    /// <summary>
    /// The screen behind a dialog, held where the shell can see it: the header, the trail, the screens and the
    /// footer in their rows, which is the whole picture a question is asked over and therefore the whole picture
    /// that has to go out of focus.
    /// </summary>
    private readonly Grid _layout = new Grid();

    /// <summary>
    /// The screen behind a dialog thrown out of focus: a photograph of <see cref="_layout"/>, taken at a small
    /// fraction of its size when a question goes up and stretched back over the window for the length of it.
    /// The live screen hides under that photograph, which is what the viewer wants - a question is asked over
    /// the screen, not in front of a moving one. The panel behind the photograph is the window's own colour,
    /// because a photograph of a visual only holds what that visual paints and a layout leaves its empty rows
    /// unpainted.
    /// </summary>
    private readonly Border _backdrop = new Border
    {
      IsHitTestVisible = false,
      IsVisible = false
    };

    /// <summary>
    /// The photograph itself, and it is a shape filled with the bitmap as a brush rather than an image control:
    /// an image control paints a bitmap at the picture's own pixels and leaves the rest of its bounds alone, so
    /// a photograph taken a fifth as big as the screen lands as a small blurred square in the corner. A brush
    /// belongs to the surface it fills and is stretched over all of it, which is the whole point of the picture.
    /// </summary>
    private readonly Rectangle _backdropPicture = new Rectangle
    {
      IsHitTestVisible = false
    };

    /// <summary>
    /// The photograph currently worn by <see cref="_backdropPicture"/>, kept so that the next question can throw
    /// the very same ground away or hand it back sharp, and so that the bitmap is freed rather than left behind.
    /// </summary>
    private Bitmap? _backdropShot;

    /// <summary>The stop the current photograph was taken of, so a second dialog over the same ground reuses it.</summary>
    private Workflow.WorkflowFrame? _backdropShotOf;
    private readonly Theming.ChromeInfo _chrome =
        new Theming.ChromeInfo(action => Avalonia.Threading.Dispatcher.UIThread.Post(action));
    private readonly ContentControl _header = new ContentControl();
    private readonly ContentControl _footer = new ContentControl();
    private readonly ContentControl _superlayer = new ContentControl();
    private Avalonia.Threading.DispatcherTimer? _superlayerTimer;

    /// <summary>
    /// The on-screen keyboard's shelf: a layer over the screens, below the screensaver's black, that holds
    /// the keyboard dialog of whatever field asked for it. The reference showed the keyboard as a dialog of
    /// the skin; this is the same dialog, hung where our shell can hang one.
    /// </summary>
    private readonly ContentControl _keyboardLayer = new ContentControl();

    /// <summary>The window's own colour before any theme speaks one - the front end's factory look.</summary>
    private static readonly IBrush FactoryBackground = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x18));

    /// <summary>
    /// The application's window: an empty workflow the caller fills, and the factory that builds a screen for a
    /// frame the first time the viewer reaches that stop.
    /// </summary>
    public ShellWindow(Workflow.WorkflowManager workflow, Func<Workflow.WorkflowFrame, Control?> viewFactory)
    {
      if (workflow == null)
        throw new ArgumentNullException(nameof(workflow));
      _workflow = workflow;
      _viewFactory = viewFactory;
      BuildChrome();
      ShowCurrentFrame();
    }

    /// <summary>
      /// A shell around one screen with no workflow to speak of: for the screens' own tests and for the single-
      /// screen modes, which should still see exactly the chrome a viewer sees and not a bare window.
    /// </summary>
    public ShellWindow(Control content)
    {
      if (content == null)
        throw new ArgumentNullException(nameof(content));
      _workflow = new Workflow.WorkflowManager();
      BuildChrome();
      _workflow.Push(new Workflow.WorkflowFrame("screen", "", content) { View = content });
    }

    /// <summary>Where the viewer is. Tests and opening code navigate through this; the window shows it.</summary>
    public Workflow.WorkflowManager Workflow
    {
      get { return _workflow; }
    }

    /// <summary>
    /// The control of the screen the viewer is looking at, or <c>null</c> before there is one.  Probes and tests
    /// that want a named control inside the screen ask the window for this rather than digging through the chrome.
    /// </summary>
    public Control? Screen
    {
      // What the viewer is standing on: the dialog while one is up, the screen otherwise.
      get { return (_dialogLayer.IsVisible ? _dialogLayer.Content : _screenArea.Content) as Control; }
    }

    private void BuildChrome()
    {

      // The window learns about every move through the workflow, whoever pushes it - both constructors, and the
      // opening code that fills the stack after the window exists.
      _workflow.Changed += ShowCurrentFrame;
      _workflow.Removed += CloseRemovedScreen;
      Opened += ScreenOpened;
      Closed += delegate
      {
        Theming.SkinEngine.Changed -= WearSkinChrome;
        Theming.FrontendTheme.Changed -= WearSkinChrome;
      };

      Title = "MediaPortal 2";
      Width = 1280;
      Height = 720;
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      // A media centre runs on a television, and a television has no desktop next to the picture - unless
      // this machine's own settings answered otherwise, which is exactly what its "full screen" row is.
      // The window follows that answer live too: switching it is something a viewer wants to see happen.
      ApplyWindowMode();
      Settings.ClientSettings.Changed += OnClientSettingsChanged;
      Closed += delegate { Settings.ClientSettings.Changed -= OnClientSettingsChanged; };
      Background = FactoryBackground;
      Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF4));

      _trail.Orientation = Orientation.Horizontal;
      _trail.Focusable = false;

      // The master chrome of the skin: a header over the screens and a footer under them, the way the
      // reference's masters wrapped every screen. Both take their look - and only their look - from the skin's
      // chrome file; while the palette wears no skin, the templates are not there and the rows stay away.
      _header.Name = "ChromeHeader";
      _header.Focusable = false;
      _header.DataContext = _chrome;
      _footer.Name = "ChromeFooter";
      _footer.Focusable = false;
      _footer.DataContext = _chrome;
      _superlayer.Name = "ChromeSuperlayer";
      _superlayer.Focusable = false;
      _superlayer.IsVisible = false;
      _screenArea.Name = "ChromeScreenArea";
      _backdrop.Name = "ChromeBackdrop";
      _backdrop.Child = _backdropPicture;
      RenderOptions.SetBitmapInterpolationMode(_backdropPicture, BitmapInterpolationMode.HighQuality);
      Theming.SkinEngine.Changed += WearSkinChrome;
      Theming.FrontendTheme.Changed += WearSkinChrome;
      WearSkinChrome(null, EventArgs.Empty);

      _layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      _layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      _layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Star));
      _layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      Grid.SetRow(_header, 0);
      Grid.SetRow(_trail, 1);
      Grid.SetRow(_screenArea, 2);
      Grid.SetRow(_footer, 3);
      _layout.Children.Add(_header);
      _layout.Children.Add(_trail);
      _layout.Children.Add(_screenArea);
      _layout.Children.Add(_footer);

      // The superlayer floats over all of it - header, trail, screen - exactly as the reference's volume and
      // busy layers covered the whole master window.
      Panel shell = new Panel();

      // The ground the pointer stands on, and it has to be there for a reason the picture cannot give. Avalonia
      // asks the element under the pointer what the pointer should look like and hands the platform whatever that
      // answer is - including "no answer", which the Windows backend answers with its own arrow, written into the
      // window's class so that Windows goes on painting it. So the moment the element the pointer was standing on
      // is taken away - which is every time the home content is read again, because the covers it stood on are
      // built anew - the arrow is back on the television for a moment, whatever the window itself was told to
      // wear. A ground that is always there and that always carries an answer of its own means there is never a
      // moment without one: see WearPointerLook, which is what dresses it. See also the pointer's rest in
      // WirePointerRest, which is what puts the arrow away in the first place.
      shell.Background = Avalonia.Media.Brushes.Transparent;
      _pointerGround = shell;
      WearPointerLook(false);

      shell.Children.Add(_layout);

      // The out-of-focus photograph of that ground, over it and under the question: while a dialog is up the
      // viewer sees the picture instead of the live screen, so nothing behind the letters moves about. See
      // BlurScreenBehind for the one moment that picture is taken and for the skins that want no such thing.
      shell.Children.Add(_backdrop);

      // The dialog layer, over the master and under the superlayer: a volume reading belongs above a dialog as
      // much as above a film, and a dialog belongs above the screen it is a question about.
      _dialogLayer.IsVisible = false;
      shell.Children.Add(_dialogLayer);
      shell.Children.Add(_superlayer);

      // The keyboard dialog over the screens - above the superlayer, under the screensaver's black: a field
      // that asked to be typed in is answered, and the room leaving still blacks the whole picture including
      // the keys.
      _keyboardLayer.Name = "ChromeKeyboard";
      _keyboardLayer.IsVisible = false;
      shell.Children.Add(_keyboardLayer);

      // The screensaver's black, above even the superlayer: a volume reading glowing over a screen that
      // decided the room left is the opposite of what a screensaver is for. It only ever paints while the
      // idle watch says the room is silent.
      _screensaverLayer = new Avalonia.Controls.Border
      {
        Background = Avalonia.Media.Brushes.Black,
        IsVisible = false
      };
      shell.Children.Add(_screensaverLayer);
      Content = shell;

      // The first question of a run can be asked before this window has ever been laid out, and a ground with no
      // size yet cannot be photographed. The next layout pass knows better, and one pass after the question is a
      // blink no viewer is waiting for.
      LayoutUpdated += delegate
      {
        if (_backdropShot == null && _backdropShotOf != null)
          PhotographGround();
      };

      WireScreensaver();
      WirePointerRest();

      // The back key of the remote and the Escape of the keyboard, heard after every screen has declined it.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            // The witness key: what the application paints, saved where the operator can look at it.
            if (args.Key == Key.F12)
            {
              LogScreenTo(ScreenshotService.NextTempPath());
              args.Handled = true;
              return;
            }

            if (args.Handled)
            {
              // A screen answered it about its own business; but the volume keys below belong to the room,
              // not the screen, so fall through only for them.
              if (!IsVolumeKey(args.Key))
                return;
            }

            if (IsVolumeKey(args.Key))
            {
              ViewModels.NowPlayingViewModel? playback = (Application.Current as App)?.Playback;
              if (playback == null)
                return;

              int step = args.Key == Key.VolumeUp || args.Key == Key.OemPlus ? 5 : -5;
              playback.SetVolume(Math.Max(0, Math.Min(100, playback.Volume + step)));
              if (ShowSuperlayer("Superlayer_Volume", "Volume " + playback.Volume + " %", 2000))
                args.Handled = true;
              return;
            }

            if (!Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key))
              return;
            if (!_workflow.Pop())
              // At the root back means "leave the application", the same answer the home screen gave alone.
              Close();
            args.Handled = true;
          },
          RoutingStrategies.Bubble);
    }

    /// <summary>The television's volume keys, and the keyboard's stand-ins for a keyboard without them.</summary>
    private static bool IsVolumeKey(Key key)
    {
      return key == Key.VolumeUp || key == Key.VolumeDown || key == Key.OemPlus || key == Key.OemMinus;
    }

    /// <summary>
    /// A screen whose frame left the stack is told so, once, and in the order the stack closed them - nearest
    /// first, the way the viewer walked past them backwards.
    /// </summary>
    private void CloseRemovedScreen(Workflow.WorkflowFrame frame)
    {
      (frame.View as Workflow.IWorkflowScreen)?.OnScreenClosed();
    }

    /// <summary>
    /// What the skin's chrome shows about this window: the clock, the screen's name, and what is playing.
    /// The application feeds it; the chrome.xaml templates of the worn skin read it.
    /// </summary>
    public Theming.ChromeInfo Chrome
    {
      get { return _chrome; }
    }

    /// <summary>
    /// Put on - or take off - the header and footer the worn skin defines. The window listens to the skin
    /// engine, so a theme switch on the couch swaps the chrome live, with no help from whoever switched.
    /// </summary>
    private void WearSkinChrome(object? sender, EventArgs args)
    {
      _header.Template = FindTemplate("ChromeHeaderTemplate");
      _footer.Template = FindTemplate("ChromeFooterTemplate");

      // What the head line is allowed to say is one of the worn skin's own settings - the reference's
      // ShowTime and ShowDate - and only Nereus has an opinion about it; every other face keeps the
      // clock and the day it always showed.
      bool showTime = true;
      bool showDate = true;
      bool showTemperature = true;
      bool showWeatherCondition = true;
      Settings.SkinSettingsBase? worn =
          Settings.SkinSettingsBase.LoadFor(Theming.FrontendTheme.CurrentName(Avalonia.Application.Current!),
              null, out _);
      if (worn is Settings.NereusSkinSettings nereus)
      {
        showTime = nereus.ShowTime;
        showDate = nereus.ShowDate;
        showTemperature = nereus.ShowTemperature;
        showWeatherCondition = nereus.ShowWeatherCondition;
      }
      _chrome.ShowTime = showTime;
      _chrome.ShowDate = showDate;
      _chrome.ShowTemperature = showTemperature;
      _chrome.ShowWeatherCondition = showWeatherCondition;

      // How the clock and the day are spelled belongs to the skin too - the reference kept both formats in
      // its skin settings, not in the application's - so this is where they cross over, and the head line is
      // said again in them straight away. A skin that speaks no formats leaves the reference's standing.
      _chrome.WearFormats(worn?.TimeFormat, worn?.DateFormat);

      // The reference's master chrome sat on the screen's own background - the header was never a band of its
      // own colour - so the templates paint themselves with the theme's screen colour, live, through a
      // DynamicResource; the window's factory background only ever shows while no theme speaks one.
      // The window itself follows suit: the chrome rows carry a margin, and a gap at the corner of the
      // television that kept the factory's colour while the screen wore another would say the theme never
      // arrived - which is precisely what the render gate looks for.
      //
      // A picture beats a colour here: the reference skins stood on their own backgrounds - WMC's blue wash,
      // Nereus' dark fanart - and a theme that carries a picture is asked for one, so a theme may name a
      // Background file in its manifest and the whole television then stands on it. Without one, the colour.
      object? picture = FindResource("SkinBackgroundImage");
      Background = picture is Avalonia.Media.IImageBrushSource src
          ? new Avalonia.Media.ImageBrush(src)
          {
            Stretch = Avalonia.Media.Stretch.UniformToFill,
            AlignmentX = Avalonia.Media.AlignmentX.Center,
            AlignmentY = Avalonia.Media.AlignmentY.Center
          }
          : FindResource("ScreenBackgroundBrush") as IBrush ?? FactoryBackground;
      UpdateChromeVisibility();
      Console.WriteLine("chrome:   header=" + (_header.Template != null) + " footer=" + (_footer.Template != null)
          + " visible=" + _header.IsVisible + " state=" + WindowState + " background=" + Background?.GetType().Name);
    }

    /// <summary>
    /// Full screen fills the television; the answer off leaves the window maximised on the desktop. Read
    /// from the file every time, because the file is where the settings screen wrote the answer.
    /// </summary>
    private void ApplyWindowMode()
    {
      WindowState = Settings.ClientSettings.Load(null, out _).FullScreen
          ? WindowState.FullScreen
          : WindowState.Maximized;
    }

    private void OnClientSettingsChanged(object? sender, EventArgs args)
    {
      ApplyWindowMode();

      // The screensaver's wait is the same kind of answer as the window's, read from the file the settings
      // row just wrote; turning it off while the screen is black wakes the screen, which is what turning it
      // off means.
      _idle.WaitMinutes = Settings.ClientSettings.Load(null, out _).ScreensaverMinutes;
      _idle.Touch();
      ApplyScreensaver();

      // The head line's calendar speaks the language the same save named.
      _chrome.RefreshClock();
    }

    // The screensaver's three parts: the watch that counts the silence, the black layer, and the quiet
    // poll that puts the layer on. The watch starts with the answer the file has, which is "off" until a
    // viewer says otherwise.
    private readonly Input.IdleWatch _idle =
        new Input.IdleWatch(Settings.ClientSettings.Load(null, out _).ScreensaverMinutes);
    private Avalonia.Controls.Border? _screensaverLayer;
    private Avalonia.Threading.DispatcherTimer? _idleTimer;

    /// <summary>
    /// The three ends a screensaver needs: the presses and moves that keep the room awake - heard in the
    /// tunnel before any screen sees them, and swallowed while the screen sleeps, so the waking press does
    /// not also press whatever stood under the black - a poll every half minute while the window is open,
    /// and the layer that shows the answer.
    /// </summary>
    private void WireScreensaver()
    {
      void Wake(RoutedEventArgs args)
      {
        bool wasAsleep = _idle.Asleep;
        _idle.Touch();
        if (!wasAsleep)
          return;
        args.Handled = true;
        ApplyScreensaver();
      }

      AddHandler(KeyDownEvent,
          delegate (object? sender, RoutedEventArgs args) { Wake(args); },
          RoutingStrategies.Tunnel, true);
      AddHandler(PointerPressedEvent,
          delegate (object? sender, RoutedEventArgs args) { Wake(args); },
          RoutingStrategies.Tunnel, true);
      // The base argument type on purpose: the move route legitimately carries the plain PointerEventArgs,
      // and a handler that demands a richer one is an ArgumentException on the first movement of the mouse -
      // which is to say a dead application the moment someone touches the desk next to the remote.
      AddHandler(PointerMovedEvent,
          delegate (object? sender, RoutedEventArgs args) { Wake(args); },
          RoutingStrategies.Tunnel, true);

      Opened += delegate
      {
        if (_idleTimer == null)
        {
          _idleTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
          _idleTimer.Tick += delegate
          {
            _idle.Poll();
            ApplyScreensaver();
          };
        }
        _idleTimer.Start();
      };
      Closed += delegate
      {
        if (_idleTimer != null)
          _idleTimer.Stop();
      };
    }

    /// <summary>Show or hide the black, following the watch; a screen going to sleep also puts the superlayers away.</summary>
    private void ApplyScreensaver()
    {
      if (_screensaverLayer == null || _screensaverLayer.IsVisible == _idle.Asleep)
        return;

      _screensaverLayer.IsVisible = _idle.Asleep;
      if (_idle.Asleep)
        HideSuperlayer();
    }

    // The pointer's own rest: the watch that says when the mouse has been left alone long enough, the beat that
    // asks it, and the one cursor this window wears when the answer is "away". The window is the only thing in the
    // front end that says what the pointer looks like - no screen, no skin and no drawing asks for a cursor of its
    // own - so whatever is written here is what a viewer sees, wherever the pointer happens to be standing.
    private readonly Input.PointerRest _pointerRest = new Input.PointerRest();
    private Avalonia.Input.Cursor? _pointerAway;
    private Avalonia.Input.Cursor? _pointerShown;

    // The one element under all the screens that the pointer can always be standing on. See WearPointerLook.
    private Avalonia.Controls.Control? _pointerGround;
    private Avalonia.Threading.DispatcherTimer? _pointerBeat;

    // The word on the window that the skin reads: put on when the pointer goes out of sight and taken off when it
    // comes back, so that every dressing answering ":pointerover" can fall silent with the pointer instead of
    // going on claiming that a tile is stood on. Named here, written by the styles: see the comment at the top of
    // the Nereus skin's styles file, which is where the arrangement is spelled out for whoever dresses a skin.
    internal const string PointerRestingClass = "pointer-resting";
    private bool _pointerStyling;

    /// <summary>
    /// Puts the mouse pointer out of sight after the quiet the watch asks for and brings it back on a movement that
    /// can only be a hand. Heard in the tunnel, before any screen sees the pointer: the moves are what the watch
    /// counts, and a press or a wheel is somebody who wants the pointer now rather than after its threshold.
    /// </summary>
    private void WirePointerRest()
    {
      AddHandler(PointerMovedEvent,
          delegate (object? sender, PointerEventArgs args)
          {
            // The plain argument type, as with the screensaver's wake: this route legitimately carries
            // PointerEventArgs, and a handler demanding a richer one is an exception on the first movement.
            if (_pointerRest.Move(args.GetPosition(this)))
            {
              BringPointerBack();
            }
            else if (_pointerRest.Resting)
            {
              // A report that is not a hand still says that the mouse is being listened to, and it arrives about
              // twice a second on its own - so the curtain is tightened on every one of them for free. That is
              // what keeps a curtain that something showed through from staying shown: the next shiver of the
              // mouse draws it again.
              Input.PointerCurtain.Draw();
            }
          },
          RoutingStrategies.Tunnel, true);

      void Stir(object? sender, RoutedEventArgs args)
      {
        if (_pointerRest.Stir())
          BringPointerBack();
      }

      AddHandler(PointerPressedEvent, Stir, RoutingStrategies.Tunnel, true);
      AddHandler(PointerWheelChangedEvent, Stir, RoutingStrategies.Tunnel, true);

      // While the pointer rests, every layout pass tightens the curtain again. That is where the framework
      // changes its mind about the pointer - in the middle of laying a home content out anew - and a fold that
      // pass lifted is back before the pass is over rather than half a second later.
      LayoutUpdated += delegate
      {
        if (_pointerRest.Resting)
          Input.PointerCurtain.Draw();
      };

      Opened += delegate
      {
        if (_pointerBeat == null)
        {
          _pointerBeat = new Avalonia.Threading.DispatcherTimer
          {
            Interval = TimeSpan.FromMilliseconds(500)
          };
          _pointerBeat.Tick += delegate
          {
            if (_pointerRest.Poll())
              PutPointerAway();
          };
        }
        _pointerBeat.Start();
      };
      Closed += delegate
      {
        // Gone with the window: the next Opened builds its own beat, so nothing here can restart a beat of a
        // window that is already closed.
        if (_pointerBeat != null)
        {
          _pointerBeat.Stop();
          _pointerBeat = null;
        }

        // A curtain outlives nobody. Left drawn, the count would keep the pointer off the television for whatever
        // takes this thread's picture over next.
        Input.PointerCurtain.Lift();
      };
    }

    /// <summary>
    /// Says what the pointer looks like, twice over, because the window alone cannot say it. The window is what
    /// the pointer is standing on most of the time and is asked directly; the ground under all the screens is
    /// what it is standing on the moment the element underneath is gone, and it is only asked if it carries an
    /// answer of its own - a value inherited from the window does not reach the platform from here. Without both,
    /// every home content rebuild - which tears the covers the pointer stood on down and builds new ones - let
    /// the platform fall back to its own arrow for a moment, right in the middle of a resting pointer.
    /// </summary>
    private void WearPointerLook(bool resting)
    {
      _pointerAway ??= new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None);
      _pointerShown ??= new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

      Cursor = resting ? _pointerAway : null;
      if (_pointerGround != null)
        _pointerGround.Cursor = resting ? _pointerAway : _pointerShown;
    }

    /// <summary>
    /// Takes the pointer away: the look first (see WearPointerLook) and then the word to the skin. A pointer that
    /// was never on screen has nothing to put away, which is why none of this is asked of the platform until the
    /// quiet has actually elapsed.
    /// </summary>
    private void PutPointerAway()
    {
      WearPointerLook(true);

      // And the curtain over it all. The look is the framework's promise about what the pointer would be painted
      // as, which the framework can and does forget for a frame while a home content is read again; the curtain
      // is the operating system's own answer to whether it is painted at all, and it does not forget.
      Input.PointerCurtain.Draw();

      // Said to the skin as well as to the platform, and this is the part the platform cannot say for us: the
      // pointer is invisible but it is still standing where it stood, so the tile underneath it still believes
      // it is being stood on and would go on wearing its lit edge with nobody there to see it. The class is how
      // the window tells the dressing that the standing has stopped; see the comment at the top of the Nereus
      // skin's styles file.
      if (!_pointerStyling)
      {
        _pointerStyling = true;
        Classes.Add(PointerRestingClass);
      }

      // Said out loud for the same reason the chrome and the skin are: a pointer that vanishes while nobody
      // pressed anything looks like a bug unless the front end says that it was the front end.
      Console.WriteLine("pointer:  out of sight after "
          + (_pointerRest.QuietMs / 1000).ToString(System.Globalization.CultureInfo.InvariantCulture)
          + "s of quiet");

      // The beat has nothing left to count until the pointer is back; the moves do the rest of the work.
      if (_pointerBeat != null)
        _pointerBeat.Stop();
    }

    /// <summary>Brings the pointer back to the window's normal look and starts the quiet again.</summary>
    private void BringPointerBack()
    {
      WearPointerLook(false);

      // Lifted before anything else is decided about the look: a pointer that is wanted and counting is the one
      // mistake a viewer does not forgive.
      Input.PointerCurtain.Lift();

      // Before the cursor is visible again, so that the first frame in which a viewer can see the pointer is
      // already the frame in which the tile under it is dressed for being stood on.
      if (_pointerStyling)
      {
        _pointerStyling = false;
        Classes.Remove(PointerRestingClass);
      }

      Console.WriteLine("pointer:  back - it moved past its threshold, or somebody pressed something");

      // The beat only exists while the window is open, and it is what notices the next quiet spell.
      _pointerBeat?.Start();
    }

    /// <summary>
    /// The chrome shows what the skin offers, except where the screen itself covers the television: a full
    /// screen film wore no master chrome in either reference skin, and a header over the picture is exactly
    /// the mistake they were built to avoid.
    /// </summary>
    private void UpdateChromeVisibility()
    {
      bool fullScreen = (_workflow.Current?.View as Control) is Workflow.IFullScreenScreen;
      // A dialog asks about the screen behind it; the master's header and footer are that screen's furniture and
      // stand back for the question, the same way they stand back for a film.
      bool dialog = (_workflow.Current?.View as Control) is Workflow.IDialogScreen;
      _header.IsVisible = _header.Template != null && !fullScreen && !dialog && !_viewFullBleed;
      _footer.IsVisible = _footer.Template != null && !fullScreen && !dialog && !_viewFullBleed;
    }

    /// <summary>
    /// The screen a dialog stands over: the nearest stop below this one that is not itself a dialog, or null
    /// when this stop is a screen rather than a dialog. A dialog opened from a dialog answers to the same screen
    /// underneath, because the second is only the first asked more precisely - the reference's dialogs stacked
    /// that way, over the master and never over each other's ground.
    /// </summary>
    private Workflow.WorkflowFrame? ScreenBehind(Workflow.WorkflowFrame frame)
    {
      if (!(frame.View is Workflow.IDialogScreen))
        return null;

      IReadOnlyList<Workflow.WorkflowFrame> stack = _workflow.Stack;
      for (int i = stack.Count - 1; i >= 0; i--)
      {
        if (!ReferenceEquals(stack[i], frame))
          continue;

        for (int below = i - 1; below >= 0; below--)
        {
          if (!(stack[below].View is Workflow.IDialogScreen))
            return stack[below];
        }
      }

      return null; // a dialog the workflow has nothing under: it takes the television like any screen would
    }

    /// <summary>
    /// Throws the screen a dialog is asked over out of focus, and hands it back sharp when the question is
    /// answered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference dimmed its master screen behind a dialog and left it sharp, which was enough over its still
    /// pictures. Here the home behind a question keeps moving about - covers arrive, the rail lights up, a shelf
    /// scrolls - and moving detail under the letters of a question is the one thing a dialog must not put up
    /// with. A photograph of that ground says what the reference's black said (this is the front of the stage)
    /// and still lets the viewer recognise where they were standing.
    /// </para>
    /// <para>
    /// A photograph, and not the framework's own blur: the framework draws an effect into a layer of its own at
    /// the size the visual had in dependent pixels and stretches that layer over the screen's real pixels, so on
    /// a television at 145% the blurred home comes out magnified, blocky and out of step with itself, its edges
    /// fading into the window's black where the layer runs out. A photograph cannot drift, because it is the
    /// screen - taken at the size the ground is really drawn in, shrunk by the skin's number, and stretched back
    /// over the very same ground, so every blurred pixel sits exactly where its sharp one sat. Shrunk and
    /// softened, that is: see <see cref="SoftFocus"/>, without which the picture is a mosaic and not a blur.
    /// </para>
    /// <para>
    /// Taking it once, as the question goes up, is also what a dialog wants: the ground behind a question has no
    /// business being recomputed sixty times a second, and a viewer who is being asked something is not watching
    /// the shelves behind them scroll. How far the ground is shrunk is the skin's business, read off its metrics
    /// under <c>DialogBackdropBlur</c>; a skin that names no number - or names less than two - leaves the screen
    /// behind exactly as sharp as it always was, which is what a skin drawing its dialogs over a still background
    /// wants. A film behind a dialog is never photographed either: every one of its frames is new, so a picture
    /// of it is stale before it is drawn, and a dialog asked over a film is a question about the film.
    /// </para>
    /// </remarks>
    private void BlurScreenBehind(Workflow.WorkflowFrame? behind)
    {
      bool worthPhotographing = behind != null
          && !(behind.View is Workflow.IFullScreenScreen)
          && (Theming.SkinEngine.WornNumber("DialogBackdropBlur") ?? 0.0) >= 2.0;

      if (!worthPhotographing)
      {
        _backdropShotOf = null;
        PutBackdropAway();
        return;
      }

      if (_backdropShot != null && ReferenceEquals(_backdropShotOf, behind))
        return; // a second question over the same ground stands in front of the same thrown-away picture

      _backdropShotOf = behind;
      PhotographGround();
    }

    /// <summary>
    /// Takes the photograph of the ground, if the ground has a size to take it at yet.
    /// </summary>
    private void PhotographGround()
    {
      double shrink = Theming.SkinEngine.WornNumber("DialogBackdropBlur") ?? 0.0;
      Size ground = _layout.Bounds.Size;
      if (shrink < 2.0 || ground.Width < 2.0 || ground.Height < 2.0)
        return; // the layout pass below tries again as soon as the window knows how big it is

      // Taken at the size the ground is really drawn in and shrunk by the skin's number, so the blur keeps the
      // same character whether the television is told to draw one pixel or one and a half per dot.
      double scaling = RenderScaling <= 0.0 ? 1.0 : RenderScaling;
      PixelSize pixels = new PixelSize(
          Math.Max(1, (int) Math.Round(ground.Width * scaling / shrink)),
          Math.Max(1, (int) Math.Round(ground.Height * scaling / shrink)));
      Vector dpi = new Vector(96.0 * scaling / shrink, 96.0 * scaling / shrink);

      RenderTargetBitmap shot = new RenderTargetBitmap(pixels, dpi);
      shot.Render(_layout);

      // And then genuinely softened, which is the other half of a blur: a picture that is merely small is a
      // mosaic, and every sharp edge of the home comes back as a square the eye counts. Three box passes over
      // the small picture - half the skin's number of its own pixels wide, which is a couple of screen pixels
      // either way of everything - take those steps out while they are still cheap, and what is stretched back
      // is a soft focus rather than a coarse one.
      Bitmap soft = SoftFocus.Of(shot, Math.Max(1, (int) Math.Round(shrink / 2.0)));
      shot.Dispose();

      PutBackdropAway();
      _backdropShot = soft;
      _backdrop.Background = Background;
      _backdropPicture.Fill = new ImageBrush(soft) { Stretch = Stretch.Fill };
      _backdrop.IsVisible = true;
    }

    /// <summary>
    /// Hands the ground back: the photograph is put away, and with it the layer it was covering.
    /// </summary>
    private void PutBackdropAway()
    {
      _backdrop.IsVisible = false;
      _backdropPicture.Fill = null;
      if (_backdropShot != null)
      {
        _backdropShot.Dispose();
        _backdropShot = null;
      }
    }

    /// <summary>
    /// How deep this stop sits in the way through the front end, counting from one for the home screen. The stack
    /// is a read-only list, so the answer is a walk from the top rather than an index.
    /// </summary>
    private int DepthOf(Workflow.WorkflowFrame frame)
    {
      IReadOnlyList<Workflow.WorkflowFrame> stack = _workflow.Stack;
      for (int i = stack.Count - 1; i >= 0; i--)
      {
        if (ReferenceEquals(stack[i], frame))
          return i + 1;
      }

      return 0;
    }

    /// <summary>
    /// Whether the screen on top has asked for the whole television for itself for a moment - a photograph
    /// being looked at, a slideshow running - while its frame stays a normal screen of the workflow.
    /// </summary>
    private bool _viewFullBleed;

    /// <summary>
    /// Lets one screen say, without leaving its place in the workflow, that it is covering the television:
    /// the chrome is stowed while <paramref name="fullBleed"/> holds and comes back the moment it does not.
    /// The reference worked this way too - a raised photograph filled the set, and putting it back returned
    /// the master - and a header across somebody's picture is the mistake both skins were built to avoid.
    /// </summary>
    public void SetViewFullBleed(bool fullBleed)
    {
      if (_viewFullBleed == fullBleed)
        return;

      _viewFullBleed = fullBleed;
      UpdateChromeVisibility();
    }

    /// <summary>
    /// Whether the header is currently floating over the screen instead of standing above it.
    /// </summary>
    private bool _headerOverlay;

    /// <summary>
    /// Lets one screen take the header out of its row and float it over the top of the screen instead.
    /// The reference's home did exactly this: its clock, date and weather sat at the very top of the
    /// television (master_home.xaml's top-right Grid, WindowButtonsPanel at the left), painted over the
    /// fanart that fills the whole screen - the header row of the DockPanel was empty precisely so the
    /// picture reached the edge. A home that docks its header cuts that picture off one hundred pixels
    /// short of the top, which is the difference this asks for.
    /// </summary>
    public void SetChromeHeaderOverlay(bool overlay)
    {
      if (_headerOverlay == overlay)
        return;

      _headerOverlay = overlay;

      // The layout grid gives the header its own row above the screen; floating means sharing the screen's
      // row from above. ZIndex because the grid paints its children in the order they were added, and the
      // header was added before the screens it now has to stand on. Hit tests pass through it: a floating
      // strip of clock that swallowed the clicks of the tiles under its top edge would be a header that
      // stole from the screen it only decorates.
      Avalonia.Controls.Grid.SetRow(_header, overlay ? 2 : 0);
      _header.ZIndex = overlay ? 10 : 0;
      _header.IsHitTestVisible = !overlay;
      _header.VerticalAlignment = overlay
          ? Avalonia.Layout.VerticalAlignment.Top
          : Avalonia.Layout.VerticalAlignment.Stretch;
    }

    private static object? FindResource(string key)
    {
      Application? app = Application.Current;
      if (app == null)
        return null;
      return app.TryFindResource(key, out object? found) ? found : null;
    }

    private static Avalonia.Controls.Templates.IControlTemplate? FindTemplate(string key)
    {
      return FindResource(key) as Avalonia.Controls.Templates.IControlTemplate;
    }

    /// <summary>
    /// Lay one of the skin's superlayers over the whole window - the volume reading, the busy spinner - saying
    /// <paramref name="text"/>. Answers whether the skin knows that layer at all; when <paramref name="autoHideMs"/>
    /// is given the layer takes itself off afterwards, which is how the reference's volume bar forgot itself.
    /// </summary>
    public bool ShowSuperlayer(string templateKey, string text, int? autoHideMs = null)
    {
      Avalonia.Controls.Templates.IControlTemplate? template = FindTemplate(templateKey);
      if (template == null)
        return false;

      _superlayer.Template = template;
      _superlayer.Content = text;
      _superlayer.IsVisible = true;

      _superlayerTimer?.Stop();
      _superlayerTimer = null;
      if (autoHideMs.HasValue)
      {
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(autoHideMs.Value) };
        timer.Tick += delegate
        {
          timer.Stop();
          _superlayer.IsVisible = false;
        };
        _superlayerTimer = timer;
        timer.Start();
      }
      return true;
    }

    /// <summary>Take the superlayer off - the busy spinner's end, or a fresh key beating the volume reading to it.</summary>
    public void HideSuperlayer()
    {
      _superlayerTimer?.Stop();
      _superlayerTimer = null;
      _superlayer.IsVisible = false;
    }

    /// <summary>
    /// Whether the on-screen keyboard stands over the screen right now. Screens ask this before they answer a
    /// key of their own - while the keys are out, the keys own the picture.
    /// </summary>
    public bool KeyboardOpen => _keyboardLayer.IsVisible;

    /// <summary>
    /// Shows the on-screen keyboard for one text field, in the layout the machine's language speaks. The
    /// reference opened this dialog from the editor itself; here the field's shell window hosts it as a layer,
    /// so the field stays visible above its own keys.
    /// </summary>
    public void ShowVirtualKeyboard(TextBox editor)
    {
      if (editor == null)
        return;

      var keyboard = new VirtualKeyboardView(editor,
          ViewModels.VirtualKeyboardLayout.ForCulture(Settings.ClientSettings.UiCulture().Name));
      keyboard.CloseRequested += delegate
      {
        CloseVirtualKeyboard();
        // The reference hands the edit back to the editor; the viewer who pressed Done goes on typing, or
        // presses on - the caret stayed where the keys left it.
        editor.Focus();
      };

      _keyboardLayer.Content = keyboard;
      _keyboardLayer.IsVisible = true;
    }

    /// <summary>Takes the keyboard away again. Pressing Done, pressing back, or walking to another screen.</summary>
    public void CloseVirtualKeyboard()
    {
      _keyboardLayer.IsVisible = false;
      _keyboardLayer.Content = null;
    }

    private void ShowCurrentFrame()
    {
      Workflow.WorkflowFrame? frame = _workflow.Current;
      if (frame == null)
        return;

      // Walking on closes the keyboard: a keyboard left over the next screen would be typing into a field
      // that is no longer there.
      if (_keyboardLayer.IsVisible)
        CloseVirtualKeyboard();

      if (frame.View == null && _viewFactory != null)
        frame.View = _viewFactory(frame);

      // A dialog is raised over the screen it was asked from, which stays lit, scrolled and selected behind it;
      // every other stop takes the screen area for itself and hands the dialog layer back.
      Workflow.WorkflowFrame? behind = ScreenBehind(frame);
      bool dialog = behind != null;
      _dialogLayer.Content = dialog ? frame.View : null;
      _dialogLayer.IsVisible = dialog;
      if (!dialog)
        _screenArea.Content = frame.View;
      else if (behind!.View != null)
        _screenArea.Content = behind.View;

      // What the dialog is asked over is thrown out of focus for the length of the question. See
      // BlurScreenBehind, which is where that photograph comes from, how far the skin says to shrink it, and why
      // a film is left alone.
      BlurScreenBehind(behind);

      // A film is its own master screen - the chrome of the skin steps back while it is on (see IFullScreenScreen).
      UpdateChromeVisibility();

      // What the television is showing is the screen that stayed lit: a dialog is a question about that screen,
      // not a new place the viewer walked to, so the window's name, the header's word and the weather all go on
      // answering for it.
      Workflow.WorkflowFrame shown = behind ?? frame;
      int depth = DepthOf(shown);

      // Alone, the window is the application, whatever screen that is; one stop deeper, the stop is worth naming.
      Title = depth <= 1 || string.IsNullOrEmpty(shown.Label)
          ? "MediaPortal 2"
          : "MediaPortal 2 - " + shown.Label;
      // The header's words: the reference's master header named the screen it was showing - and at home it
      // said nothing at all. Both rescued skins dock an empty HeaderPanel above their home (master_home.xaml,
      // the Grid with only column definitions in it), because at home the fanart is the head line and no
      // word of the application's own belongs over it. Deeper screens keep their names.
      _chrome.ScreenTitle = depth <= 1 ? string.Empty : shown.Label ?? string.Empty;

      // The reference read the weather on master_home and nowhere else, so the slot belongs to the same
      // one-deep-or-less answer that decides the window's name: the home head line carries it, deeper
      // screens do not.
      _chrome.ShowWeatherSlot = depth <= 1;

      RebuildTrail();

      Control? view = frame.View as Control;
      if (view != null && view.IsVisible == false)
        return; // A factory that refused the stop leaves nothing to activate; the frame stays as it is.

      // Activation is the part of opening a screen that needs a window the viewer can actually look at: focusing
      // a control inside a window that has not opened yet asks the focus manager for nothing. The old screens
      // heard this as their Opened event; the shell owes every screen the same moment.
      Workflow.WorkflowFrame opened = frame;
      Action activate = delegate
      {
        if (opened != _workflow.Current)
          return; // The viewer walked on while the window was still opening; the new stop is the one to wake.
        if (opened.View is Workflow.IWorkflowScreen screen)
          screen.OnScreenActivated();
        else
          (opened.View as Control)?.Focus();
        if (dialog && view != null)
          HoldTheKeyboardForTheDialog(view);
      };

      if (_shown)
        activate();
      else
        _pendingActivation = activate;
    }

    /// <summary>
    /// Makes sure that a question raised over a screen also holds the keyboard it is asked with.
    /// </summary>
    /// <remarks>
    /// A dialog carrying no focus is not merely unfocused - it is a lie about the screen behind it: every arrow the
    /// viewer presses walks the screen that only stands behind the question as a picture, and every Enter presses a
    /// button nobody meant. Where a dialog's keys start is the dialog's own decision, made in
    /// <see cref="Workflow.IWorkflowScreen.OnScreenActivated"/>; this is the net under that decision, so a dialog
    /// written without one is a dialog that can still be answered. It is asked twice - once now and once when the
    /// layout has settled - because a focus request aimed at a control that is not joined to a window yet answers
    /// nothing at all, and a dialog's buttons arrive out of the skin's template, which is a pass later than the call.
    /// </remarks>
    private void HoldTheKeyboardForTheDialog(Control dialogView)
    {
      void Ask()
      {
        if (!_dialogLayer.IsVisible || !ReferenceEquals(_dialogLayer.Content, dialogView))
          return; // the viewer walked on while the layout was settling; the newest stop decides from here
        if (this.FocusManager.GetFocusedElement() is Visual held && StandsInside(held, dialogView))
          return; // the dialog already answers for its own keys, which is all this was for
        Control? first = FirstControlTakingFocus(dialogView);
        first?.Focus();
      }

      Ask();
      Avalonia.Threading.Dispatcher.UIThread.Post(Ask, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Whether <paramref name="element"/> is the dialog itself or stands somewhere inside it.</summary>
    private static bool StandsInside(Visual element, Control dialogView)
    {
      for (Visual? at = element; at != null; at = at.GetVisualParent())
      {
        if (ReferenceEquals(at, dialogView))
          return true;
      }

      return false;
    }

    /// <summary>
    /// The first control of a dialog that can take the focus, in the order the dialog draws them. A face that
    /// dresses its dialog can hand the keyboard to whichever control it drew first, which is the honest guess for
    /// a dialog that never said anything about its keys.
    /// </summary>
    private static Control? FirstControlTakingFocus(Control root)
    {
      if (root.Focusable && root.IsVisible && root.IsEnabled)
        return root;

      foreach (Control child in root.GetVisualChildren().OfType<Control>())
      {
        Control? found = FirstControlTakingFocus(child);
        if (found != null)
          return found;
      }

      return null;
    }

    private bool _shown;
    private Action? _pendingActivation;

    private void ScreenOpened(object? sender, EventArgs args)
    {
      _shown = true;
      _chrome.StartClock();
      StartChromeWeather();
      Action? pending = _pendingActivation;
      _pendingActivation = null;
      if (pending != null)
        pending();
      ArmAutoShot();
    }

    /// <summary>
    /// Writes what the window paints to a file - the honest screenshot, see <see cref="ScreenshotService"/>.
    /// </summary>
    private void LogScreenTo(string path)
    {
      Visual root = (Content as Visual) ?? this;
      Console.WriteLine("screenshot: " + ScreenshotService.Capture(root, path));
    }

    /// <summary>
    /// When MP2_AUTOSHOT names a file, the window photographs itself a few seconds after opening, so a
    /// machine nobody is sitting in front of can still say what its screen showed. MP2_AUTOSHOT_DELAY
    /// moves that moment: the home screen fills its content from the server, and a machine that needs a
    /// slower first refresh photographs an empty screen at six seconds.
    /// </summary>
    private void ArmAutoShot()
    {
      string path = Environment.GetEnvironmentVariable("MP2_AUTOSHOT");
      if (string.IsNullOrEmpty(path))
        return;

      int seconds = 6;
      string delay = Environment.GetEnvironmentVariable("MP2_AUTOSHOT_DELAY");
      if (!string.IsNullOrEmpty(delay) && int.TryParse(delay, out int asked) && asked > 0)
        seconds = asked;

      Avalonia.Threading.DispatcherTimer.RunOnce(
          delegate
          {
            LogScreenTo(path);
          },
          TimeSpan.FromSeconds(seconds));
    }

    private void RebuildTrail()
    {
      List<Control> crumbs = new List<Control>();
      for (int i = 0; i < _workflow.Stack.Count; i++)
      {
        Workflow.WorkflowFrame frame = _workflow.Stack[i];
        if (i > 0)
        {
          TextBlock arrow = new TextBlock();
          arrow.Text = "›";
          arrow.FontSize = 15;
          arrow.Margin = new Avalonia.Thickness(6, 0, 6, 0);
          arrow.VerticalAlignment = VerticalAlignment.Center;
          object? dim;
          if (this.TryFindResource("DimTextBrush", out dim) && dim is IBrush dimBrush)
            arrow.Foreground = dimBrush; // Resolved when the trail is built, which happens on every move.
          crumbs.Add(arrow);
        }

        bool isLast = i == _workflow.Stack.Count - 1;
        Button crumb = new Button();
        crumb.Content = frame.Label;
        crumb.FontSize = 15;
        crumb.Padding = new Avalonia.Thickness(8, 2, 8, 2);
        crumb.Focusable = false; // A breadcrumb you can walk to with the arrows is one more thing to walk past;
                                  // the keyboard has the back key for the same trip.
        crumb.IsTabStop = false;
        crumb.Opacity = isLast ? 1.0 : 0.65;
        Workflow.WorkflowFrame clickedFrame = frame;
        crumb.Click += delegate { _workflow.PopToState(clickedFrame.StateId); };
        crumbs.Add(crumb);
      }

      _trail.Children.Clear();
      foreach (Control crumb in crumbs)
        _trail.Children.Add(crumb);
      // A dialog is not a place the viewer walked to, so it earns no crumb; the trail stays as the screen
      // behind it left it.
      _trail.IsVisible = _workflow.Stack.Count > 1 && !(_workflow.Current?.View is Workflow.IDialogScreen);
      _trail.Margin = new Avalonia.Thickness(16, 8, 16, 0);
    }

    // ---- the home head line's weather, the reference's master_home slot -----------------------------------

    private System.Timers.Timer? _weatherTimer;
    private bool _weatherStarted;

    /// <summary>
    /// Start the home head line's weather: the first watched place, read once now and then every half hour -
    /// the reference's own <c>WEATHER_UPDATE_INTERVAL</c> - from the same client the weather screen uses, with
    /// the same key and the same culture, so the head line and the screen can never disagree about what a
    /// degree is. A viewer who watches no place gets no weather, which is the truth rather than a hole; a
    /// service that refuses leaves the head line as it stands, because last hour's reading is still better
    /// than a sentence about HTTP, and the weather screen is where a real complaint belongs.
    /// </summary>
    private void StartChromeWeather()
    {
      if (_weatherStarted)
        return;
      _weatherStarted = true;

      Settings.WeatherPlacesSettings places = Settings.WeatherPlacesSettings.Load(null, out _);
      if (places.Places.Count == 0)
        return;

      Settings.WeatherPlaceEntry first = places.Places[0];
      Api.WeatherApiClient client = new Api.WeatherApiClient(
          userApiKey: places.ApiKey,
          cultureName: Settings.ClientSettings.Load(null, out _).Culture);
      Api.WeatherCityBookmark city =
          new Api.WeatherCityBookmark(first.Name, first.Detail, first.Lat, first.Lon);
      System.Globalization.NumberFormatInfo numbers = Settings.ClientSettings.UiCulture().NumberFormat;

      async System.Threading.Tasks.Task ReadAsync()
      {
        try
        {
          Api.WeatherAnswer answer = await client.ReadAsync(city);
          if (answer.Now == null)
            return;

          string temperature = answer.Now.Temp.ToString("0", numbers) + "°" + (client.IsMetric ? "C" : "F");
          _chrome.ReportWeather(temperature, answer.Now.Icon);
        }
        catch (Exception)
        {
          // Silence is the head line's way with a weather service that will not answer.
        }
      }

      _weatherTimer = new System.Timers.Timer(30 * 60 * 1000) { AutoReset = true };
      _weatherTimer.Elapsed += (sender, e) => _ = ReadAsync();
      _weatherTimer.Start();
      Closed += delegate { _weatherTimer?.Stop(); };
      _ = ReadAsync();
    }
  }
}
