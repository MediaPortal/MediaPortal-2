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
  public partial class ConnectionWindow : Window
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer. A screen over an empty address, which is what a first start shows.
    /// </summary>
    public ConnectionWindow()
        : this(new ConnectionViewModel(null))
    {
    }

    public ConnectionWindow(ConnectionViewModel viewModel)
    {
      DataContext = viewModel;
      InitializeComponent();

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

      Opened += delegate
      {
        // The field takes the focus first, so that a viewer with a keyboard can start typing at once; if the search
        // then turns up a server, the focus moves to the list below.
        FocusFirstChoice(viewModel);
        if (viewModel.AutoScan) viewModel.RescanCommand.Execute(null);
      };

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
            if (!serversList.IsFocused || (args.Key != Key.Left && args.Key != Key.Right))
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

      // Back leaves the screen as it is. Choosing a server is not something a screen can be forced into, and the
      // address the viewer did not save is the one the front end keeps using.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
              Close();
          },
          RoutingStrategies.Tunnel);
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
      Close();
    }
  }
}
