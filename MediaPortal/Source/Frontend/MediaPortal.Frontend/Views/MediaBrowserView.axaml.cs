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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;

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

      // The arrow ring is the shelf and nothing else. The two buttons that used to stand beside it are gone: "Play"
      // was the Enter that the list already answers, and "More" asked for what the shelf now reads on its own as
      // the viewer comes near its end. The details panel beside the list takes no focus by design.
      DpadNavigation.Attach(this, new List<Control> { titlesList });

      // Rows or tiles: the view owns the panel because a panel is a thing the view draws, while the pictures
      // inside it belong to the skin - so switching means setting the wrap panel and handing the skin a
      // different template key, both from what the view model just settled on. The first call matters: a
      // viewer whose grid setting says "tiles" should never see a row flash before the tiles come.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        // The shape and the picture inside the shape both change which frame the list draws, so both dress
        // the rows again.
        if (args.PropertyName == nameof(MediaBrowserViewModel.Layout)
            || args.PropertyName == nameof(MediaBrowserViewModel.MediaKind)
            || args.PropertyName == nameof(MediaBrowserViewModel.GridTemplateKey))
        {
          ApplyLayout();
          ScheduleRowPainting();
        }
      };
      // A skin put on or taken off while this screen is open changes which frames answer the tile keys, and
      // the view - not the skin's attachment - is the one that decides what the list draws. The deal lasts
      // exactly as long as the screen is on the window, which is what keeps a closed shelf from being held
      // alive by the application's event.
      AttachedToVisualTree += delegate { SkinEngine.Changed += OnSkinChanged; };
      DetachedFromVisualTree += delegate { SkinEngine.Changed -= OnSkinChanged; };
      ApplyLayout();

      // The plot reads itself down the panel when the worn face says so - the reference's three scroll
      // switches, obeyed. See PlotAutoScroll for what each answer means.
      new PlotAutoScroll(plotScroller, browser);

      // A double-click on a row means the Enter that plays it (or opens the series behind it); the search box
      // keeps its Enter for the caret's sake and gets no second path. See HomeWindow for the general reason.
      MouseRemote.Attach(this, titlesList);

      // A row the screen has laid out is a row that could be painted, but only the ones inside the viewport - plus
      // a screenful in each direction, so that scrolling arrives at pictures rather than at grey - are worth asking
      // the server for. The reference hangs its pictures the same way round: FanArtImageSource is composed into a
      // container as that container is realised, and the texture is allocated afterwards. It matters in the tile
      // and cover views too, whose panel keeps a band of rows standing around the screen and re-dresses a
      // container as it moves from one title to the next: eleven hundred tiles that all asked for a poster would
      // be eleven hundred downloads wearing the shelf, whether they stand or not.
      titlesList.ContainerPrepared += delegate { ScheduleRowPainting(); };

      // Two things are done when the list is scrolled: the rows that came into sight are offered their pictures,
      // and a viewer who has come to the bottom of what is on the screen is answered with the next part of the
      // shelf. The second is a safety net rather than the plan - a shelf reads itself whole as soon as it is opened
      // - but it is what recovers a reading that a server interrupted.
      titlesList.AddHandler(ScrollViewer.ScrollChangedEvent, delegate
      {
        ScheduleRowPainting();

        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        double short_of_bottom = scroller.Extent.Height - (scroller.Offset.Y + scroller.Viewport.Height);
        if (short_of_bottom <= scroller.Viewport.Height)
          _ = browser.KeepReadingAsync();
      });

      // OK on the remote is Enter here, and Avalonia hands it to the focused control before anyone else; the
      // list does nothing with it, so it is caught on the way in and turns into "play this", which is what a
      // viewer means by it on a screen whose whole content is a list of things to watch.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!(DataContext is MediaBrowserViewModel viewModel))
              return;

            if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Menu, args.Key))
            {
              // The "Menu" key of a remote - F16 by default, whatever a viewer's remote really reports is read
              // from keybindings.json. The apps key is not the default here because this screen already wears
              // it for the details, and one key cannot answer two questions. It opens the media list menu of
              // the reference for the selected row.
              viewModel.ShowMenuCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Details, args.Key))
            {
              // The "info" key of a remote, which is the Apps key by default in everything that reaches this
              // code, plus the letter for a keyboard. It reads about the selected title rather than playing it,
              // which is the one question a title list cannot answer from its own rows.
              viewModel.ToggleDetailsCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Find, args.Key) && !searchBox.IsFocused)
            {
              // Finding is the one thing on this screen that needs the keyboard, so it is the one key that moves
              // the focus out of the list - and Enter or Escape brings it straight back, because the next thing a
              // viewer does with a result is walk through it with the arrows.
              searchBox.Focus();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Watched, args.Key) && !searchBox.IsFocused)
            {
              // W for watched: everything, then what nobody has finished, then what somebody has. One key round
              // three states rather than two switches, because a remote reaches its keyboard through one button
              // and a viewer should be able to get back to "everything" without remembering which switch it was.
              viewModel.CycleSeenCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.View, args.Key) && !searchBox.IsFocused)
            {
              // V for view: rows or the skin's tiles. What shape the tiles take is not this key's business - the
              // Media button asks that - so the key only has the one question left for the viewer.
              viewModel.SwitchViewCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Media, args.Key) && !searchBox.IsFocused)
            {
              // M for media: the picture the tiles wear, poster to banner to thumbnail. The same question the
              // button beside the view button asks, reachable from the sofa.
              viewModel.SwitchMediaCommand.Execute(null);
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
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key))
            {
              // Back means "away with the newest thing on the screen", which is the title's menu when one is
              // standing open, then the search when there is one, then the watched filter, then the details
              // the screen was wearing. A viewer who pressed back three times expects to be back where they
              // started, not outside the screen.
              if (viewModel.MenuVisible)
              {
                viewModel.CloseMenu();
                titlesList.Focus();
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
                // The watched filter goes before the details for the same reason the search does: the thing
                // asked for most recently is the thing a viewer means by "back".
                viewModel.ClearSeenCommand.Execute(null);
                args.Handled = true;
                return;
              }
              if (viewModel.DetailsVisible)
              {
                viewModel.CloseDetails();
                args.Handled = true;
                return;
              }
              // Nothing is open to undo, so the key is not this screen's: it reaches the shell behind it, which
              // pops this screen off the workflow.
            }
            else if ((args.Key == Key.Enter || args.Key == Key.Return
                    || Settings.KeyBindings.Current.Is(Settings.KeyAction.PlayPause, args.Key))
                && !viewModel.MenuVisible)
            {
              // Enter is only "play the row" while nobody taller is wearing it: with the menu open the key
              // belongs to the button the focus rests on, and a dialog whose own key plays a film behind it
              // is the reason dialog boxes were invented.
              viewModel.PlayCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // The menu takes the focus when it comes up and hands it back to the shelf when it goes: a dialog the
      // focus never reaches is a dialog the remote cannot answer, and a shelf that stays behind its own scrim
      // is a shelf the viewer cannot walk while the dialog is up.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(MediaBrowserViewModel.MenuVisible))
          FocusMenu(browser);
      };

      // The screen fills itself in the first time it appears; returning from the playback screen or from a list
      // of titles inside a title finds everything where the viewer left it.
      _firstActivation = true;
    }

    /// <summary>
    /// Puts the focus on the answer the menu was opened for - the mark when there is one to give, the way out
    /// when there is nothing to give - and back on the rows when the menu goes away.
    /// </summary>
    private void FocusMenu(MediaBrowserViewModel browser)
    {
      if (browser.MenuVisible)
      {
        if (menuMarkButton.IsVisible)
          menuMarkButton.Focus();
        else
          menuBackButton.Focus();
      }
      else
        titlesList.Focus();
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

      // A shelf that comes back with its menu standing open gives the focus back to the menu: the viewer left
      // with the dialog up, and they come back to the dialog, not to the rows behind it.
      if (DataContext is MediaBrowserViewModel model && model.MenuVisible)
      {
        FocusMenu(model);
        return;
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

      // Ask the worn skin for the shape's own frame; when it answers none, fall back to this file's frame
      // for that shape. That fallback is the whole point: the three views used to exist only while a skin
      // that drew them was worn, which left every other screen with rows no matter what its viewer picked.
      titlesList.ItemTemplate = SkinEngine.TryTileTemplate(viewModel.GridTemplateKey)
          ?? BuiltinTemplate(viewModel.Layout);

      // A grid and a cover shelf are laid the same way - one after the other, wrapping whole - and only the
      // pictures inside differ.
      if (viewModel.Layout != BrowserLayout.List)
        titlesList.ItemsPanel = TILE_PANEL;
      else
        titlesList.ClearValue(ItemsControl.ItemsPanelProperty);

      // A panel swapped under a standing list is laid out again, and with it the scroller it is read through.
      _scroller = null;
    }

    /// <summary>
    /// Asks for the pictures of the rows the screen can see, once the layout pass that laid them out is over.
    /// </summary>
    /// <remarks>
    /// Posted rather than done where it is noticed, because a row is noticed as prepared before anything was
    /// measured: at that moment a container's bounds say nothing about whether it is inside the viewport. The
    /// screen paints a batch of rows at most per pass, however many rows the panel prepared on the way.
    /// </remarks>
    private void ScheduleRowPainting()
    {
      if (_paintScheduled)
        return;

      _paintScheduled = true;
      Dispatcher.UIThread.Post(delegate
      {
        _paintScheduled = false;
        PaintRowsTheScreenCanSee();
      }, DispatcherPriority.Loaded);
    }

    private bool _paintScheduled;

    /// <summary>
    /// Asks the shelf for the picture of every row it has laid out where the television can see it.
    /// </summary>
    /// <remarks>
    /// Needed because a frame can be swapped under rows that stay standing: the panel change realises its
    /// containers again, and a template change leaves the containers where they are and only what is drawn inside
    /// them changes. Both are answered by the same pass over what is on the screen - see
    /// <see cref="MediaBrowserViewModel.PaintRow"/> for why the picture belongs to the screen rather than to the
    /// list, and <see cref="StandsOnTheScreen"/> for what "on the screen" means here.
    /// </remarks>
    private void PaintRowsTheScreenCanSee()
    {
      if (!(DataContext is MediaBrowserViewModel viewModel))
        return;

      // A screenful above and below the visible band, because a poster that arrives while the viewer is still a
      // screen away is a poster that is there when they arrive, and one that arrives after they do is grey.
      Rect visible = titlesList.Bounds;
      Rect inSight = new Rect(visible.X, visible.Y - visible.Height, visible.Width, visible.Height * 3);

      foreach (Control? container in titlesList.GetRealizedContainers())
      {
        if (container?.DataContext is TitleViewModel row && StandsOnTheScreen(container, inSight))
          viewModel.PaintRow(row);
      }
    }

    /// <summary>
    /// Whether a laid out row stands inside the band of the list that is drawn on the television.
    /// </summary>
    private bool StandsOnTheScreen(Control container, Rect inSight)
    {
      Point? corner = container.TranslatePoint(new Point(0, 0), titlesList);
      if (!corner.HasValue)
        return false;
      return inSight.Intersects(new Rect(corner.Value, container.Bounds.Size));
    }

    private void OnSkinChanged(object? sender, EventArgs e)
    {
      ApplyLayout();
    }

    /// <summary>
    /// The scroller the list is read through, which lives inside the list's own template rather than beside it.
    /// </summary>
    /// <remarks>
    /// Looked for rather than handed over, because <see cref="ItemsControl"/> keeps its <see cref="ScrollViewer"/>
    /// to itself; the same hunt is what the home screen does for its menu. Found once and kept - and asked for
    /// again should the screen ever be retemplated, which is what a skin being put on does.
    /// </remarks>
    private ScrollViewer? ListScroller()
    {
      if (_scroller == null)
        _scroller = titlesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

      return _scroller;
    }

    private ScrollViewer? _scroller;

    /// <summary>The frame this file draws for a shape the worn skin does not answer.</summary>
    private IDataTemplate BuiltinTemplate(BrowserLayout layout)
    {
      string key = layout switch
      {
        BrowserLayout.Grid => "BuiltinTilesTemplate",
        BrowserLayout.Cover => "BuiltinCoversTemplate",
        _ => "BuiltinRowsTemplate"
      };

      if (_builtin.TryGetValue(key, out IDataTemplate? known))
        return known;

      IDataTemplate frame = (IDataTemplate)browser.FindResource(key)!;
      _builtin.Add(key, frame);
      return frame;
    }

    private readonly Dictionary<string, IDataTemplate> _builtin = new Dictionary<string, IDataTemplate>();

    /// <summary>
    /// The tiles' panel: laid left to right and wrapping whole, which is what a grid is at heart, but with only
    /// the rows standing that the screen could be drawing - see <see cref="VirtualizingTilePanel"/> for why a
    /// wrap panel laying out a whole film library is not the same thing.
    /// </summary>
    private static readonly FuncTemplate<Panel?> TILE_PANEL =
        new FuncTemplate<Panel?>(() => new VirtualizingTilePanel());

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
