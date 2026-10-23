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

using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that asks which server to talk to.
  /// </summary>
  /// <remarks>
  /// Who shows this screen and what happens after an address has been chosen is the application's decision, so
  /// nothing here closes or swaps windows in reaction to a save; the application watches the view model, which it
  /// built itself. What this file does add is what only a screen can add: the search starts when the screen opens,
  /// the focus lands on the list once something is in it, up and down walk the list while left and right leave it,
  /// and Enter means "this one" - on the field it tests the typed address, on the list it takes the highlighted
  /// server. A viewer holding a remote should never have to know what a Tab key is.
  /// </remarks>
  public partial class ConnectionView : UserControl, IWorkflowScreen
  {
    private readonly WorkflowManager? _workflow;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer. A screen over an empty address, which is what a first start shows.
    /// </summary>
    public ConnectionView()
        : this(new ConnectionViewModel(null))
    {
    }

    /// <param name="viewModel">The addresses to choose between.</param>
    /// <param name="workflow">
    /// The navigation this screen was opened through; its "Close" button pops this screen rather than closing a
    /// window, because there is only one window now.  <c>null</c> in tests and the designer.
    /// </param>
    /// <param name="offersClose">
    /// <c>false</c> when the screen stands embedded in the settings hub's pane: the hub's own trail and back key do
    /// the leaving there, and a button that would pop nothing should not stand on the pane.
    /// </param>
    public ConnectionView(ConnectionViewModel viewModel, WorkflowManager? workflow = null, bool offersClose = true)
    {
      DataContext = viewModel;
      _workflow = workflow;
      InitializeComponent();

      if (!offersClose)
        closeButton.IsVisible = false;

      // In the order they are on the screen. The list is first because it is what the viewer is meant to use; the
      // field is in the ring too, so a viewer who has to type an address can walk back into it and correct one
      // character without reaching for a mouse.
      _ring.Add(serversList);
      _ring.Add(serverUrlBox);
      _ring.Add(searchButton);
      _ring.Add(testButton);
      _ring.Add(saveButton);
      _ring.Add(closeButton);
      DpadNavigation.Attach(this, _ring);

      // A server that turns up later should be the thing under the viewer's thumb, so the focus moves to the list as
      // soon as it has something in it - but only while the field is still empty, because somebody who has started
      // typing is not going to be interrupted by a laptop on the other side of the house waking up.
      viewModel.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs args)
      {
        if (args.PropertyName != nameof(ConnectionViewModel.HasServers) || !viewModel.HasServers) return;
        if (serverUrlBox.IsFocused && string.IsNullOrEmpty(serverUrlBox.Text)) serversList.Focus();
      };

      // Up and down leave the field. Once there is text in it, the field takes left and right for the caret - which
      // is right, one wrong character should not mean retyping the whole address - but it has no use for a vertical
      // caret, and a television remote has no Tab key to fall back on. Claimed on the window while the key tunnels
      // in, which is where a handler is guaranteed to see it before the field does; a handler on the field itself was
      // measured to be too late, because by then the caret has already answered the key.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!serverUrlBox.IsFocused || (args.Key != Key.Up && args.Key != Key.Down))
              return;

            Control? next = DpadNavigation.NextReachableInRing(_ring, args.Key);
            if (next != null && next.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // The list is the opposite way round: up and down are how a viewer moves through it - the list does that by
      // itself, so nothing here takes those keys - and left or right is how they get out of it.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!serversList.IsKeyboardFocusWithin || (args.Key != Key.Left && args.Key != Key.Right))
              return;

            Control? next = DpadNavigation.NextReachableInRing(_ring, args.Key);
            if (next != null && next.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // Enter or the remote's OK button in the address field runs the test. Deliberately on the field and not on the
      // window: the field is where the caret is, and swallowing Enter anywhere else would take the activation key
      // away from whichever button happens to have the focus.
      serverUrlBox.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (DataContext is ConnectionViewModel model && model.TestCommand.CanExecute(null))
            {
              args.Handled = true;
              model.TestCommand.Execute(null);
            }
          },
          RoutingStrategies.Tunnel);

      // Enter on a highlighted server takes it. The entry is in the list only because that server's own API answered
      // a moment ago, so this is the one case where a key press can go straight to saving - which is what makes the
      // usual single-server house a two-key job: open the screen, press OK.
      serversList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (DataContext is ConnectionViewModel model && model.SaveCommand.CanExecute(null))
            {
              args.Handled = true;
              model.SaveCommand.Execute(null);
            }
          },
          RoutingStrategies.Tunnel);

      // A double-click on a found server means the Enter that takes it. Raised on the list rather than the
      // window because that is where this screen's Enter handler lives - see HomeWindow for the general reason.
      MouseRemote.Attach(serversList, serversList);

      // Back is the shell's key now: leaving without saving is exactly what popping this screen off the
      // workflow does, and the trail or the back key does it without this screen having to own the key.
    }

    /// <summary>
    /// Runs every time the workflow shows this screen: the search starts and the focus lands where the viewer
    /// is most likely to need it.
    /// </summary>
    public void OnScreenActivated()
    {
      if (DataContext is not ConnectionViewModel viewModel)
        return;

      // The field takes the focus first, so that a viewer with a keyboard can start typing at once; if the search
      // then turns up a server, the focus moves to the list below.
      FocusFirstChoice(viewModel);
      if (viewModel.AutoScan) viewModel.RescanCommand.Execute(null);
    }

    /// <summary>
    /// Puts the focus where the viewer is most likely to need it: on the list if the view model already knows a
    /// server, on the field if it does not.
    /// </summary>
    private void FocusFirstChoice(ConnectionViewModel viewModel)
    {
      if (viewModel.Servers.Count > 0) serversList.Focus();
      else serverUrlBox.Focus();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
      // The button says "leave this screen as it is", which in one window means popping it; whatever pushed it
      // stays untouched.
      _workflow?.Pop();
    }
  }
}
