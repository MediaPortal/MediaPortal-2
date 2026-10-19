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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

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
    private readonly Theming.ChromeInfo _chrome =
        new Theming.ChromeInfo(action => Avalonia.Threading.Dispatcher.UIThread.Post(action));
    private readonly ContentControl _header = new ContentControl();
    private readonly ContentControl _footer = new ContentControl();
    private readonly ContentControl _superlayer = new ContentControl();
    private Avalonia.Threading.DispatcherTimer? _superlayerTimer;

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
      get { return _screenArea.Content as Control; }
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
      Theming.SkinEngine.Changed += WearSkinChrome;
      Theming.FrontendTheme.Changed += WearSkinChrome;
      WearSkinChrome(null, EventArgs.Empty);

      Grid layout = new Grid();
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Star));
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      Grid.SetRow(_header, 0);
      Grid.SetRow(_trail, 1);
      Grid.SetRow(_screenArea, 2);
      Grid.SetRow(_footer, 3);
      layout.Children.Add(_header);
      layout.Children.Add(_trail);
      layout.Children.Add(_screenArea);
      layout.Children.Add(_footer);

      // The superlayer floats over all of it - header, trail, screen - exactly as the reference's volume and
      // busy layers covered the whole master window.
      Panel shell = new Panel();
      shell.Children.Add(layout);
      shell.Children.Add(_superlayer);
      Content = shell;

      // The back key of the remote and the Escape of the keyboard, heard after every screen has declined it.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
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

            if (args.Key != Key.Escape && args.Key != Key.Back)
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
      if (Settings.SkinSettingsBase.LoadFor(Theming.FrontendTheme.CurrentName(this), null, out _)
          is Settings.NereusSkinSettings nereus)
      {
        showTime = nereus.ShowTime;
        showDate = nereus.ShowDate;
      }
      _chrome.ShowTime = showTime;
      _chrome.ShowDate = showDate;

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

      // The head line's calendar speaks the language the same save named.
      _chrome.RefreshClock();
    }

    /// <summary>
    /// The chrome shows what the skin offers, except where the screen itself covers the television: a full
    /// screen film wore no master chrome in either reference skin, and a header over the picture is exactly
    /// the mistake they were built to avoid.
    /// </summary>
    private void UpdateChromeVisibility()
    {
      bool fullScreen = (_workflow.Current?.View as Control) is Workflow.IFullScreenScreen;
      _header.IsVisible = _header.Template != null && !fullScreen && !_viewFullBleed;
      _footer.IsVisible = _footer.Template != null && !fullScreen && !_viewFullBleed;
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

    private void ShowCurrentFrame()
    {
      Workflow.WorkflowFrame? frame = _workflow.Current;
      if (frame == null)
        return;

      if (frame.View == null && _viewFactory != null)
        frame.View = _viewFactory(frame);
      _screenArea.Content = frame.View;
      // A film is its own master screen - the chrome of the skin steps back while it is on (see IFullScreenScreen).
      UpdateChromeVisibility();
      // Alone, the window is the application, whatever screen that is; one stop deeper, the stop is worth naming.
      Title = _workflow.Stack.Count <= 1 || string.IsNullOrEmpty(frame.Label)
          ? "MediaPortal 2"
          : "MediaPortal 2 - " + frame.Label;
      // The header's words: the reference's master header named the screen it was showing, and at home it said
      // the application's name - the same sentence the window title speaks, without its prefix.
      _chrome.ScreenTitle = Title.StartsWith("MediaPortal 2 - ", StringComparison.Ordinal)
          ? Title.Substring("MediaPortal 2 - ".Length)
          : "MediaPortal";

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
      };

      if (_shown)
        activate();
      else
        _pendingActivation = activate;
    }

    private bool _shown;
    private Action? _pendingActivation;

    private void ScreenOpened(object? sender, EventArgs args)
    {
      _shown = true;
      _chrome.StartClock();
      Action? pending = _pendingActivation;
      _pendingActivation = null;
      if (pending != null)
        pending();
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
      _trail.IsVisible = _workflow.Stack.Count > 1;
      _trail.Margin = new Avalonia.Thickness(16, 8, 16, 0);
    }
  }
}
