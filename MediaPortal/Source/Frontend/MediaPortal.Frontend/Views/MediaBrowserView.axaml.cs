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

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The titles of one part of the library, and the way out of them into the player.
  /// </summary>
  public partial class MediaBrowserView : UserControl, IWorkflowScreen
  {
    private readonly WorkflowManager? _workflow;
    private readonly Func<WorkflowFrame>? _nowPlayingScreen;
    private readonly Func<TitleViewModel, WorkflowFrame>? _insideScreen;
    private bool _firstActivation;

    public MediaBrowserView()
        : this(new MediaBrowserViewModel(), null)
    {
    }

    /// <param name="browser">The section to show.</param>
    /// <param name="workflow">
    /// The navigation this screen opens the playback and the titles-inside-a-title through, or <c>null</c> in
    /// tests and the designer, where there is no workflow to push onto.
    /// </param>
    /// <param name="nowPlayingScreen">
    /// How to build the playback screen's frame, or <c>null</c> on a machine with no player. The application owns
    /// the player, so the screen it shows is built there - a title opened from here and a title opened from the
    /// home screen are the same film at the same position, not two players.
    /// </param>
    /// <param name="insideScreen">
    /// How to build the frame of the list of titles inside one title - the tracks of the album that was pressed -
    /// or <c>null</c> on a server that has nothing of that kind. Pressing an album has to lead somewhere, and the
    /// screen it leads to belongs to the application for the same reason the playback screen does.
    /// </param>
    public MediaBrowserView(MediaBrowserViewModel browser, WorkflowManager? workflow = null,
        Func<WorkflowFrame>? nowPlayingScreen = null,
        Func<TitleViewModel, WorkflowFrame>? insideScreen = null)
    {
      DataContext = browser;
      _workflow = workflow;
      _nowPlayingScreen = nowPlayingScreen;
      _insideScreen = insideScreen;
      InitializeComponent();

      playButton.IsVisible = browser.Selected != null;

      var ring = new List<Control> { titlesList };
      if (browser.CanLoadMore)
        ring.Add(moreButton);
      DpadNavigation.Attach(this, ring);

      // Rows or tiles: the view owns the panel because a panel is a thing the view draws, while the pictures
      // inside it belong to the skin - so switching means setting the wrap panel and handing the skin a
      // different template key, both from what the view model just settled on. The first call matters: a
      // viewer whose grid setting says "tiles" should never see a row flash before the tiles come.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(MediaBrowserViewModel.Layout))
          ApplyLayout();
      };
      ApplyLayout();

      // A double-click on a row means the Enter that plays it (or opens the series behind it); the search box
      // keeps its Enter for the caret's sake and gets no second path. See HomeWindow for the general reason.
      MouseRemote.Attach(this, titlesList);

      // OK on the remote is Enter here, and Avalonia hands it to the focused control before anyone else; the
      // list does nothing with it, so it is caught on the way in and turns into "play this", which is what a
      // viewer means by it on a screen whose whole content is a list of things to watch.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!(DataContext is MediaBrowserViewModel viewModel))
              return;

            if (args.Key == Key.I || args.Key == Key.Apps)
            {
              // The "info" key of a remote, which is the Apps key in everything that reaches this code, plus the
              // letter for a keyboard. It reads about the selected title rather than playing it, which is the one
              // question a title list cannot answer from its own rows.
              viewModel.ToggleDetailsCommand.Execute(null);
              args.Handled = true;
            }
            else if (args.Key == Key.F && !searchBox.IsFocused)
            {
              // Finding is the one thing on this screen that needs the keyboard, so it is the one key that moves
              // the focus out of the list - and Enter or Escape brings it straight back, because the next thing a
              // viewer does with a result is walk through it with the arrows.
              searchBox.Focus();
              args.Handled = true;
            }
            else if (args.Key == Key.W && !searchBox.IsFocused)
            {
              // W for watched: everything, then what nobody has finished, then what somebody has. One key round
              // three states rather than two switches, because a remote reaches its keyboard through one button
              // and a viewer should be able to get back to "everything" without remembering which switch it was.
              viewModel.CycleSeenCommand.Execute(null);
              args.Handled = true;
            }
            else if (args.Key == Key.V && !searchBox.IsFocused)
            {
              // V for view: rows or the skin's tiles. What shape the tiles take is not this key's business - the
              // skin's own settings say that - so the key only has the one question left for the viewer.
              viewModel.SwitchViewCommand.Execute(null);
              args.Handled = true;
            }
            else if (searchBox.IsFocused && (args.Key == Key.Enter || args.Key == Key.Return))
            {
              // Enter means different things depending on where the caret is: "play this" on a row, "search for
              // this" in the box. The box gets asked first, or typing a title would start a film.
              viewModel.FindCommand.Execute(null);
              titlesList.Focus();
              args.Handled = true;
            }
            else if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              // Back means "away with the newest thing on the screen", which is the panel when one is open, and
              // the search when there is one. A viewer who pressed back three times expects to be back where they
              // started, not outside the screen.
              if (viewModel.DetailsVisible)
              {
                viewModel.CloseDetails();
                args.Handled = true;
                return;
              }
              if (searchBox.IsFocused || viewModel.HasSearch)
              {
                viewModel.ClearSearchCommand.Execute(null);
                searchBox.Focus();
                args.Handled = true;
                return;
              }
              if (viewModel.HasSeenFilter)
              {
                // The watched filter goes after the search rather than before it: the thing asked for most
                // recently is the thing a viewer means by "back", and a filter set before typing a title is the
                // older of the two.
                viewModel.ClearSeenCommand.Execute(null);
                args.Handled = true;
                return;
              }
              // Nothing is open to undo, so the key is not this screen's: it reaches the shell behind it, which
              // pops this screen off the workflow.
            }
            else if (args.Key == Key.Enter || args.Key == Key.Return || args.Key == Key.MediaPlayPause)
            {
              viewModel.PlayCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // The screen fills itself in the first time it appears; returning from the playback screen or from a list
      // of titles inside a title finds everything where the viewer left it.
      _firstActivation = true;
    }

    /// <summary>
    /// Runs every time the workflow shows this screen.  The first showing asks the server for the titles and
    /// starts watching the view model; every showing gives the rows the focus, so the first Enter of the visit
    /// means "play this" - see HomeView for the general reason.
    /// </summary>
    public void OnScreenActivated()
    {
      if (_firstActivation)
      {
        _firstActivation = false;
        if (DataContext is MediaBrowserViewModel viewModel)
        {
          viewModel.OpenCommand.Execute(null);
          viewModel.PlayRequested += OnPlayRequested;
          viewModel.ChildrenRequested += OnChildrenRequested;
        }
      }

      titlesList.Focus();
    }

    /// <summary>
    /// Stops the watching of the view model, which outlives this screen: a handler left on it would have a
    /// screen that is gone opening a playback screen from an event nobody caused.
    /// </summary>
    public void OnScreenClosed()
    {
      if (DataContext is MediaBrowserViewModel viewModel)
      {
        viewModel.PlayRequested -= OnPlayRequested;
        viewModel.ChildrenRequested -= OnChildrenRequested;
      }
    }

    /// <summary>
    /// Lays the shelf out in the shape the view model settled on: the skin's tiles wrapped across the screen,
    /// or the rows the view was written with.  Both halves belong together - tiles in a vertical strip and rows
    /// wrapped into a grid would each be a half-dressed screen - so one method says both.  Avalonia's own
    /// default panel is a StackPanel, which is what a reset back to plain finds again.
    /// </summary>
    private void ApplyLayout()
    {
      if (!(DataContext is MediaBrowserViewModel viewModel))
        return;

      SkinTemplates.SetTileTemplate(titlesList, viewModel.GridTemplateKey);

      if (viewModel.Layout == BrowserLayout.Grid)
        titlesList.ItemsPanel = TILE_WRAP_PANEL;
      else
        titlesList.ClearValue(ItemsControl.ItemsPanelProperty);
    }

    /// <summary>The tiles' panel: laid left to right and wrapping whole, which is what a grid is at heart.</summary>
    private static readonly FuncTemplate<Panel?> TILE_WRAP_PANEL =
        new FuncTemplate<Panel?>(() => new WrapPanel { Orientation = Orientation.Horizontal });

    private void OnPlayRequested(object? sender, TitleViewModel title)
    {
      if (_nowPlayingScreen == null || _workflow == null)
        return;

      WorkflowFrame? frame = _nowPlayingScreen();
      if (frame == null)
        return;

      if (_workflow.Current != null && _workflow.Current.StateId == frame.StateId)
        return;

      _workflow.Push(frame);
    }

    /// <summary>
    /// A row that holds other titles was pressed: the titles inside it are pushed over this one in the
    /// workflow, and this one stays where it was.
    /// </summary>
    /// <remarks>
    /// The album list is left open underneath rather than closed, because "back" on a remote has to arrive back on
    /// the shelf the viewer came from, and a shelf that was closed has to be read from the server again to be
    /// seen. It is the same reason the playback screen is pushed over a list instead of instead-of it.
    /// </remarks>
    private void OnChildrenRequested(object? sender, TitleViewModel parent)
    {
      if (_insideScreen == null || _workflow == null)
        return;

      WorkflowFrame? frame = _insideScreen(parent);
      if (frame != null)
        _workflow.Push(frame);
    }
  }
}
