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

      Title = "MediaPortal 2";
      Width = 1280;
      Height = 720;
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      // A media centre runs on a television, and a television has no desktop next to the picture.
      WindowState = WindowState.Maximized;
      Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x18));
      Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF4));

      _trail.Orientation = Orientation.Horizontal;
      _trail.Focusable = false;

      Grid layout = new Grid();
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));
      layout.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Star));
      Grid.SetRow(_trail, 0);
      Grid.SetRow(_screenArea, 1);
      layout.Children.Add(_trail);
      layout.Children.Add(_screenArea);
      Content = layout;

      // The back key of the remote and the Escape of the keyboard, heard after every screen has declined it.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Escape && args.Key != Key.Back)
              return;
            if (args.Handled)
              return; // A screen answered it about its own business; the way out has been taken.
            if (!_workflow.Pop())
              // At the root back means "leave the application", the same answer the home screen gave alone.
              Close();
            args.Handled = true;
          },
          RoutingStrategies.Bubble);
    }

    /// <summary>
    /// A screen whose frame left the stack is told so, once, and in the order the stack closed them - nearest
    /// first, the way the viewer walked past them backwards.
    /// </summary>
    private void CloseRemovedScreen(Workflow.WorkflowFrame frame)
    {
      (frame.View as Workflow.IWorkflowScreen)?.OnScreenClosed();
    }

    private void ShowCurrentFrame()
    {
      Workflow.WorkflowFrame? frame = _workflow.Current;
      if (frame == null)
        return;

      if (frame.View == null && _viewFactory != null)
        frame.View = _viewFactory(frame);
      _screenArea.Content = frame.View;
      // Alone, the window is the application, whatever screen that is; one stop deeper, the stop is worth naming.
      Title = _workflow.Stack.Count <= 1 || string.IsNullOrEmpty(frame.Label)
          ? "MediaPortal 2"
          : "MediaPortal 2 - " + frame.Label;

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
