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
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    /// <summary>
    /// The floor the markup wrote under the shelf, kept from the moment the view was stood up.
    /// </summary>
    /// <remarks>
    /// The covers lay their row against the bottom edge of the room and may not carry that floor, so the code
    /// which swaps the shapes also swaps the margin - and a <c>ClearValue</c> there throws the markup's own
    /// number away for good instead of handing it back: the local value the loader wrote is gone and nothing
    /// restores it, which is why a floor written in the markup was never seen on the screen.  Reading it once
    /// and putting that number back is what keeps the markup the one place the floor is written down.
    /// </remarks>
    private readonly Avalonia.Thickness _shelfFloor;

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
      _shelfFloor = titlesList.Margin;

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
          // Walking from covers into a grid moves the menu from the bottom edge of the room to its left edge. A
          // menu that was open stays open over the walk, and it is now open in the other direction - which is what
          // the third answer below is for: the room is put where the new direction wants it with nothing to slide,
          // because a viewer who changed the view already watched enough moving.
          if (args.PropertyName == nameof(MediaBrowserViewModel.Layout))
            SlideMenu(browser.MediaMenuVisible, browser.IsCoverLayout, true);
        }
      };
      // A skin put on or taken off while this screen is open changes which frames answer the tile keys, and
      // the view - not the skin's attachment - is the one that decides what the list draws. The deal lasts
      // exactly as long as the screen is on the window, which is what keeps a closed shelf from being held
      // alive by the application's event.
      AttachedToVisualTree += delegate
      {
        SkinEngine.Changed += OnSkinChanged;

        // The head band belongs to the screen and not to the window. The reference laid its media masters out in
        // three bands - a hundred points of head, the client area, two hundred and sixty of floor - and hung the
        // fanart over ALL THREE (master_bare_media.xaml:57-59, FanArtBackground with RowSpan 3), so its clock and
        // its screen name stood on the picture and the picture went to the very edge of the television. A header
        // kept in a row of its own above the screen does the opposite: the screen's fanart stops where its row
        // starts and a black bar lies over the head of the screen. So this screen asks for the header the same way
        // the home does - floating over the screen's own row, hit-test invisible - and lays it back to rest on the
        // way out, for a screen that plans its space under a header row would have its top cut off otherwise.
        //
        // And it asks for that head line to be put away while it stands here: its word is the word the viewer
        // already read on the tile he walked in through, and under it the marks of what the machines at home are
        // doing, the day, the weather and the clock. What the viewer came for is the wall and the picture of the
        // title he is standing on. The floor under the screen is not asked for - only the head line.
        _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
        _chromeShell?.SetChromeHeaderOverlay(true);
        _chromeShell?.SetChromeHeaderStowed(true);
      };
      DetachedFromVisualTree += delegate
      {
        SkinEngine.Changed -= OnSkinChanged;
        _chromeShell?.SetChromeHeaderOverlay(false);
        _chromeShell?.SetChromeHeaderStowed(false);
        _chromeShell = null;
      };
      ApplyLayout();

      // The caret comes in with the titles. The screen is activated while its shelf is still empty - the titles
      // are on their way from the server - so the shelf cannot answer the caret then, and a shelf that is on the
      // screen without the caret in it is a shelf no tile of which is lit: the ring, the growing picture and the
      // details beside the shelf all read the standing tile from the container the caret is in. So the caret goes
      // to the shelf the first time its tiles are laid, and to nobody else: a caret the viewer has since put on
      // the head, the details or a dialog of this screen is left where they put it.
      browser.Titles.CollectionChanged += delegate
      {
        if (browser.Titles.Count == 0)
          return;

        // Asked again when this pass has laid its tiles, whatever the caret looks like right now: during a
        // refill the containers are shells whose titles - and whose index - the panel has not handed out
        // yet, and a caret check taken mid-burst reads every shell as settled. The re-ask at the post below
        // is taken after the layout pass, where the answer means something.
        Dispatcher.UIThread.Post(delegate
        {
          if (NeedsCaretOnTheShelf())
            FocusShelf();
        }, DispatcherPriority.Loaded);
      };

      // The shelf answers the standing title with a glide rather than a jump: every step of the caret moves the
      // wall by the shape's own numbers, over the shape's own length of time. And every step re-arms the quiet
      // watching, because the walk itself - not only the landing - is when the recycling can knock the tile
      // under the caret away: a watchdog retired after the last still-standing breath has to be woken by the
      // next step, or a fast walk drops its caret between two tiles and nobody picks it up.
      titlesList.SelectionChanged += delegate
      {
        GlideShelfToStoodTitle();
        WatchShelfCaret();
      };

      // The reference's <c>RestoreFocus</c> seen from this side: the shelf remembers which title the caret last
      // stood on, so a visit to the screen's menu and back lands where the viewer left off rather than on
      // whichever tile the wall happens to have realized.
      titlesList.AddHandler(GotFocusEvent, delegate (object? sender, FocusChangedEventArgs args)
      {
        if (args.Source is ListBoxItem container
            && titlesList.ItemFromContainer(container) is object stood)
          _lastTileStand = stood;
      }, RoutingStrategies.Bubble, true);

      // THE SCREEN'S OWN MENU, which the reference slid in off the left edge for grid and list and off the bottom
      // edge for covers (master_mediamenu.xaml:124-186, Animations.xaml:8-38 - two hundred points in three
      // hundred and fifty milliseconds). Nothing is shown and nothing is hidden to open it: the room slides, and
      // the menu walks into the space that slides makes. The animation is the screen's own timer rather than a
      // storyboard for the same reason every other animation here is: a test can pump a timer, and a shelf whose
      // menu only moves on a storyboard is a shelf nobody has ever seen move.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(MediaBrowserViewModel.MediaMenuVisible))
        {
          // A menu put away is a stepped-from row forgotten: the next visit climbs down wherever the viewer
          // climbs down, not where the last one left a number behind.
          if (!browser.MediaMenuVisible)
            _menuSteppedFrom = -1;

          SlideMenu(browser.MediaMenuVisible, browser.IsCoverLayout, false);

          // And the caret goes with the menu, whoever opened it.  Walking off the leading edge of the shelf moved
          // the caret by itself; a menu that slid in while the caret stayed behind on a tile answered the one
          // button the viewer pressed with what the SHELF answers - the film started - and every command in the
          // rail stayed out of reach of the keyboard.
          FocusMediaMenu(browser.MediaMenuVisible, browser.IsCoverLayout);
        }
        else if (args.PropertyName == nameof(MediaBrowserViewModel.MediaMenuHeading)
            && browser.MediaMenuVisible)
        {
          // A page of the open menu replaced its rows - the viewer climbed down over "View" or back with the
          // back key - and every row the rail wore, caret included, went with the Clear that preceded them.
          // Heading is the word the menu speaks when it rebuilds (see BuildMediaMenu), and while the menu stands
          // open it is the only word: the menu itself never closed, so the handler above never wakes.
          // Down a page, the caret belongs on its first row, which is what the menu's own opening does.  Back up
          // to the commands, the caret goes to the row the viewer climbed down from - the way back is only half
          // an answer if the viewer stands on nothing at its end.
          bool up_to_the_commands = browser.MediaMenuHeading == COMMANDS_HEADING;
          int wanted = up_to_the_commands && _menuSteppedFrom >= 0 ? _menuSteppedFrom : 0;
          if (up_to_the_commands)
            _menuSteppedFrom = -1;

          // Asked after this pass, because the heading is set between the clearing and the adding: asked in the
          // breath of the heading, the caret would select a row of an empty rail and stand on nothing again.
          ListBox rail = browser.IsCoverLayout ? mediaMenuListBottom : mediaMenuList;
          Dispatcher.UIThread.Post(delegate { StandCaretOnRailRow(rail, wanted); });
        }
      };
      // The menu's Search row and the Find key both ask for the caret to be in a field that is only on the screen
      // while somebody is searching, so the field is put on the screen and the caret moved in one motion.
      browser.SearchFocusRequested += delegate { RevealSearch(); };
      WireMenuList(mediaMenuList);
      WireMenuList(mediaMenuListBottom);

      // The room is divided between the shelf and the details column by proportion rather than by fixed points, so
      // the division is redone whenever the details column comes or goes (see ApplyColumns for why a thousand fixed
      // points - which is what the reference could afford - is wrong on a window of a different shape).
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(MediaBrowserViewModel.ShowsSideDetails))
          ApplyColumns();
      };
      // The division is a measurement, and a measurement is only as good as the width it was taken at: a window
      // that is dragged wider, or thrown full screen from a laptop lid, is divided again.
      SizeChanged += delegate { ApplyColumns(); };
      ApplyColumns();

      // The plot reads itself down the panel when the worn face says so - the reference's three scroll
      // switches, obeyed. See PlotAutoScroll for what each answer means.
      new PlotAutoScroll(plotScroller, browser);

      // A double-click on a row means the Enter that plays it (or opens the series behind it); the search box
      // keeps its Enter for the caret's sake and gets no second path. See HomeWindow for the general reason.
      MouseRemote.Attach(this, titlesList);

      // The right press on a title asks the title's own dialog - the viewer moved that dialog off the rail
      // and onto the thing it talks about. The press is caught as the context request it is (right button,
      // long press, or the menu key of a keyboard, all of them arrive here), the row under the pointer is
      // stood on first - a press about a tile is a press about THAT tile, not about whatever the caret
      // happened to stand on - and the dialog opens over the shelf.
      titlesList.AddHandler(ContextRequestedEvent, delegate (object? sender, ContextRequestedEventArgs args)
      {
        if (!(DataContext is MediaBrowserViewModel viewModel))
          return;

        if (args.Source is Visual source
            && source.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is TitleViewModel stood)
          viewModel.Selected = stood;

        if (viewModel.Selected == null)
          return;

        viewModel.HideMediaMenu();
        viewModel.ShowMenu();
        args.Handled = true;
      }, RoutingStrategies.Tunnel);

      // The dialog grows on a transform the code holds, because a transform is no control for the markup to
      // name - the same reason the room's slide is reached through the room itself.
      menuDialog.RenderTransform = _menuDialogScale;
      RenderOptions.SetBitmapInterpolationMode(menuGround, BitmapInterpolationMode.HighQuality);

      // A press beside the dialog puts it away: the dialog answers the title it was opened for, and a viewer
      // who presses beside it has stopped asking.
      // The press is watched on the scrim the dialog stands in, and the point of it is read against the
      // dialog's own bounds - a press inside those belongs to the dialog, whatever it hits there.
      menuScrim.AddHandler(PointerPressedEvent, delegate (object? sender, PointerPressedEventArgs args)
      {
        if (!(DataContext is MediaBrowserViewModel viewModel) || !viewModel.MenuVisible)
          return;

        Point where = args.GetPosition(menuDialog);
        if (where.X < 0 || where.Y < 0
            || where.X > menuDialog.Bounds.Width || where.Y > menuDialog.Bounds.Height)
          viewModel.CloseMenu();
      }, RoutingStrategies.Tunnel);

      // A row the screen has laid out is a row that could be painted, but only the ones inside the viewport - plus
      // a screenful in each direction, so that scrolling arrives at pictures rather than at grey - are worth asking
      // the server for. The reference hangs its pictures the same way round: FanArtImageSource is composed into a
      // container as that container is realised, and the texture is allocated afterwards. It matters in the tile
      // and cover views too, whose panel keeps a band of rows standing around the screen and re-dresses a
      // container as it moves from one title to the next: eleven hundred tiles that all asked for a poster would
      // be eleven hundred downloads wearing the shelf, whether they stand or not.
      titlesList.ContainerPrepared += delegate (object? sender, ContainerPreparedEventArgs e)
      {
        ScheduleRowPainting();
        // A travel asked for while the stood tile was not laid out yet waits for exactly this container; when it
        // arrives, the question that was thrown away is asked again, and the wall glides instead of leaving the
        // selection's own jump as the only movement the viewer sees.
        if (e.Index == _glideAwaitContainer)
        {
          _glideAwaitContainer = -1;
          Dispatcher.UIThread.Post(delegate { GlideShelfToStoodTitle(0); }, DispatcherPriority.Loaded);
        }
      };

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

        // Covers are read sideways, so for them the end of the shelf is the end of the extent's width and not of
        // its height; asking the wrong axis would keep reading a shelf that is already whole and never read the one
        // a cover viewer is coming to the end of.
        bool sideways = browser.Layout == BrowserLayout.Cover;
        double short_of_end = sideways
            ? scroller.Extent.Width - (scroller.Offset.X + scroller.Viewport.Width)
            : scroller.Extent.Height - (scroller.Offset.Y + scroller.Viewport.Height);
        double viewport = sideways ? scroller.Viewport.Width : scroller.Viewport.Height;
        if (short_of_end <= viewport)
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
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args.Key)
                && !searchBox.IsFocused)
            {
              // The "info" key of a remote, which reaches this code as Apps, and the letter for a keyboard.
              // It asks the title's own question: the dialog about the standing title, which the viewer
              // moved off the rail and onto the title itself - the same door the right press opens. While
              // the dialog is the newest thing on the screen the same key puts it away again; the rail's
              // way in and out is the walking, and the info key never belongs to it any more.
              if (viewModel.MenuVisible)
                viewModel.CloseMenuCommand.Execute(null);
              else
              {
                viewModel.HideMediaMenu();
                viewModel.ShowMenuCommand.Execute(null);
              }
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Details, args.Key)
                && !searchBox.IsFocused)
            {
              // The details question, which the reference kept behind its view mode dialog and this screen answers
              // from its menu's view page as well; the letter is the shortcut for a keyboard, and it asks the same
              // question the menu row asks.
              viewModel.ToggleDetailsCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Find, args.Key) && !searchBox.IsFocused)
            {
              // Finding is the one thing on this screen that needs the keyboard, so it is the one key that moves
              // the focus out of the list - and Enter or Escape brings it straight back, because the next thing a
              // viewer does with a result is walk through it with the arrows. The field itself is only on the
              // screen while somebody is searching, so this key both puts it on the screen and hands it the caret.
              viewModel.OpenSearch();
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
              // The caret goes onto a TILE, not merely into the list: a caret that gets no further than the list
              // wears no frame, and the viewer who just spelled a title wants to stand on a result.
              FocusShelf();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key))
            {
              // Back means "away with the newest thing on the screen", which is the title's menu when one is
              // standing open, then the search when there is one, then the watched filter.  The details are NOT
              // one of these and never were: the reference binds Escape to nothing on this screen at all - its
              // only Escape binding (master_mediamenu.xaml:402) is enabled solely while the media menu is open -
              // so a viewer pressing back on a wall wearing details means "away from this screen", which is the
              // answer the shell behind this handler gives.  A details layer used to be taken away here first,
              // which made every exit from this screen cost two presses of a key the viewer pressed once, and
              // the panel the first press "closed" is put back by the next visit anyway; the key that opens and
              // closes the panel is the details key, and the button that says so.
              if (viewModel.MediaMenuVisible)
              {
                // Back puts the menu's current page away first, and the menu itself only when its commands page is
                // what stands there - which is the reference's own answer, its Escape binding being enabled solely
                // while the menu was open (master_mediamenu.xaml:401).
                viewModel.MediaMenuBackCommand.Execute(null);
                // The caret goes back with the menu itself: the MediaMenuVisible handler answers a closing menu
                // by standing the caret on a tile (FocusMediaMenu -> FocusShelf).  Asking the LIST for the caret
                // here on top of that undid exactly that - the list wears no frame, so the wall the viewer walked
                // back to was a wall of tiles with none of them lit.
                args.Handled = true;
                return;
              }
              if (viewModel.MenuVisible)
              {
                // Again the caret travels with the MenuVisible handler (FocusMenu -> FocusShelf); the list must
                // not be asked for it afterwards, or the tile just lit is put back into the dark.
                viewModel.CloseMenu();
                args.Handled = true;
                return;
              }
              if (viewModel.SearchOpen && !viewModel.HasSearch && searchBox.IsFocused)
              {
                // The field was put on the screen and nothing was typed into it, so back takes the field away
                // again and gives the arrows back to the shelf. A field holding a search is a different thing and
                // is answered below.
                viewModel.SearchOpen = false;
                FocusShelf();
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
              // Nothing is open to undo, so the key is not this screen's: it reaches the shell behind it, which
              // pops this screen off the workflow.  The details panel is deliberately NOT closed here.  It was,
              // and that is the reason this screen used to cost two presses of a key the viewer pressed once: the
              // first press took a panel nobody had just opened, the screen looked the same but one layer poorer,
              // and only the second press took the viewer home.  Nereus binds Escape to nothing on this screen at
              // all (its single Escape binding, master_mediamenu.xaml:402, is enabled solely while the media menu
              // stands open), and the key that opens and closes the panel is the details key - which the button
              // beside the view button has always said out loud.
            }
            else if ((args.Key == Key.Enter || args.Key == Key.Return
                    || Settings.KeyBindings.Current.Is(Settings.KeyAction.PlayPause, args.Key))
                && !viewModel.MenuVisible && !viewModel.MediaMenuVisible)
            {
              // Enter is only "play the row" while nobody taller is wearing it: with the title's dialog or the
              // screen's menu open the key belongs to the row the focus rests on there, and a dialog whose own key
              // plays a film behind it is the reason dialog boxes were invented.
              viewModel.PlayCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // Walking off the leading edge of the shelf is how a viewer reaches the screen's own menu. The reference
      // hung that menu on the remote's Info key alone (master_mediamenu.xaml:400), but a shelf is walked, not
      // memorised: a viewer pointing left past the first row pointed at the menu, which is the thing standing to
      // the left of the shelf in the reference's own room (ClientGrid Margin "-200,0,0,-200"). Covers lay their
      // menu under the screen, so their leading edge is the top of the row. The way back is the opposite key,
      // because a menu a viewer cannot leave is a second screen they never asked for.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Handled || searchBox.IsFocused || !(DataContext is MediaBrowserViewModel viewModel))
              return;

            bool sideways = viewModel.Layout == BrowserLayout.Cover;
            Key off_the_shelf = sideways ? Key.Up : Key.Left;
            Key back_to_the_shelf = sideways ? Key.Down : Key.Right;
            ListBox menu = sideways ? mediaMenuListBottom : mediaMenuList;

            if (args.Key == off_the_shelf && titlesList.IsKeyboardFocusWithin)
            {
              // Only the leading edge asks the question, and the leading edge is the whole first column - not
              // the first title alone. A viewer standing on the fifth row and pointing left is pointing at the
              // menu, which is the thing standing to the left of the shelf; a viewer standing anywhere else in
              // a row is pointing at the tile next to them, and a menu that jumped out on every Left would be a
              // menu nobody can walk past.
              if (!StandingOnTheLeadingEdge(viewModel))
                return;

              if (viewModel.MediaMenuVisible)
                FocusMediaMenu(true, sideways);
              else
                viewModel.ShowMediaMenuCommand.Execute(null);
              args.Handled = true;
            }
            else if (args.Key == back_to_the_shelf && viewModel.MediaMenuVisible && menu.IsKeyboardFocusWithin)
            {
              // Hiding the menu already stands the caret back on the tile the viewer came from - the handler on
              // MediaMenuVisible does that through FocusMediaMenu(false) -> FocusShelf - and this key's answer
              // ENDS there.  It used to ask the list for the caret on top of that, which took it straight off the
              // tile again: the ring lives on the tile (:focus on the ListBoxItem), the list wears nothing, so
              // walking right out of the rail handed the viewer back a wall with no frame on any tile.
              viewModel.HideMediaMenuCommand.Execute(null);
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
        GrowDialog();
        if (menuMarkButton.IsVisible)
          menuMarkButton.Focus();
        else
          menuBackButton.Focus();
      }
      else
      {
        HandDialogGroundBack();
        FocusShelf();
      }
    }

    /// <summary>
    /// The photograph this dialog wears its ground in - its own copy of the shell's thrown-away picture, which
    /// the shell would otherwise keep alive over the whole layout and bury the dialog standing inside a screen.
    /// </summary>
    private Bitmap? _dialogGround;

    /// <summary>
    /// Photographs the ground and lays it under the dialog, at the skin's own number and softened the same way
    /// the workflow's dialogs soften it (<see cref="ShellWindow"/> took both halves from one recipe). The shot
    /// is taken with the scrim already at nought opacity, so it is the wall that is captured and never the
    /// dialog standing in front of it. A skin that names no blur leaves the picture away and the wall keeps
    /// living behind the wash.
    /// </summary>
    private void PhotographDialogGround()
    {
      HandDialogGroundBack();
      _dialogGround = _chromeShell?.PhotographGroundForOverlay();
      if (_dialogGround != null)
        menuGround.Fill = new ImageBrush(_dialogGround) { Stretch = Stretch.Fill };
    }

    /// <summary>Hands the wall back: the photograph goes away and the live screen underneath shows again.</summary>
    private void HandDialogGroundBack()
    {
      menuGround.Fill = null;
      if (_dialogGround != null)
      {
        _dialogGround.Dispose();
        _dialogGround = null;
      }
    }

    /// <summary>The opening times the dialog keeps: two tenths of fade over the ground, four of growth.</summary>
    private const int DIALOG_FADE_MS = 200;
    private const int DIALOG_GROW_MS = 400;

    private DispatcherTimer? _dialogGrowTimer;

    /// <summary>The dialog's own growth, read through the dialog - a transform is no control to name.</summary>
    private readonly ScaleTransform _menuDialogScale = new ScaleTransform(1, 1);

    /// <summary>
    /// Grows the dialog out of nothing at its centre - the
    /// screen it stands on fading up in two tenths while the dialog itself scales from nought to one in
    /// four, with a deceleration on it so the first frames carry most of the growth.
    /// </summary>
    /// <remarks>
    /// The growth rides a scale transform of its own and is put back to exactly one when it is over, so a
    /// dialog at rest is a dialog at the size the markup wrote - and a test that never lets a render frame
    /// pass still finds every logical thing of the dialog where the markup put it.
    /// </remarks>
    private void GrowDialog()
    {
      // One opening at a time; a dialog asked for again while growing simply grows again from where it is.
      _dialogGrowTimer?.Stop();

      menuScrim.Opacity = 0;
      _menuDialogScale.ScaleX = 0;
      _menuDialogScale.ScaleY = 0;

      // The ground goes out of focus before the question grows into it.
      PhotographDialogGround();

      long started = Environment.TickCount64;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _dialogGrowTimer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(_dialogGrowTimer, timer))
          return; // a newer opening drives this dialog from here on

        long elapsed = Environment.TickCount64 - started;
        double u = Math.Min(1, elapsed / (double)DIALOG_GROW_MS);
        double grow = 1 - (1 - u) * (1 - u);
        _menuDialogScale.ScaleX = grow;
        _menuDialogScale.ScaleY = grow;
        menuScrim.Opacity = Math.Min(1, elapsed / (double)DIALOG_FADE_MS);

        if (u >= 1)
        {
          timer.Stop();
          menuScrim.Opacity = 1;
        }
      };

      timer.Start();
    }

    /// <summary>The two hundred points the reference slid its room by (Animations.xaml:8-9).</summary>
    private const double MENU_SLIDE = 200;

    /// <summary>The same two hundred points, taken upwards, for the menu under a row of covers.</summary>
    private const double COVER_SLIDE = -200;

    /// <summary>Whether the room is currently standing on the cover menu's axis, which is the one a test reads.</summary>
    private bool _slideCovers;

    /// <summary>
    /// The shift the room slides along. The room's own name reaches the code behind, and its transform is read
    /// through the room: Avalonia hands out a field for every named control, and a transform is not a control but a
    /// plain AvaloniaObject, which is why the transform under the reference's <c>ClientGrid</c> was aimed at by
    /// <c>Storyboard.TargetProperty</c> rather than by name too.
    /// </summary>
    private TranslateTransform RoomSlide
    {
      get { return (TranslateTransform)room.RenderTransform; }
    }

    /// <summary>
    /// How far the room currently stands out of where it was built to stand, which is what a test and the render
    /// probe read to know that a menu is not merely marked visible but is actually standing on the screen.
    /// </summary>
    internal double SlideOffset => _slideCovers ? RoomSlide.Y : RoomSlide.X;

    /// <summary>
    /// Whether the shelf is watching its caret at this moment, which is what the walk is obliged to keep
    /// true: the watching retires after four quiet seconds of a still-standing tile, and every step of a walk
    /// has to wake it again, because the walk - not the still-standing - is the hour in which the caret gets
    /// knocked off its tile and the frame goes missing.
    /// </summary>
    internal bool IsWatchingShelfCaret => _shelfTileTimer?.IsEnabled == true;

    /// <summary>
    /// Slides the room by the two hundred points that make room for the screen's menu: sideways for the rail
    /// beside a grid or a list, upwards for the row of commands under a row of covers.
    /// </summary>
    /// <param name="open">Whether the menu is meant to stand on the screen when the room stops moving.</param>
    /// <param name="covers">Whether the menu is the cover layout's, which hangs under the screen.</param>
    /// <param name="atOnce">
    /// Whether to put the room where it belongs without sliding it, which is what walking from one layout into
    /// another does: a menu that was open over covers is open beside the grid the viewer just walked into, and the
    /// two menus move at right angles to each other, so the way from one to the other is a diagonal nobody asked
    /// to watch.
    /// </param>
    /// <remarks>
    /// The moving is the transition that the room's own transform carries, which is how the reference drove the
    /// same two hundred points: two storyboards, each of three hundred and fifty milliseconds, aimed at the
    /// <c>ClientGrid</c>'s <c>TranslateTransform</c> (Animations.xaml:8-38). See that markup for why a timer of the
    /// front end's own was tried and why it cannot be seen to move anywhere but on a real window.
    /// </remarks>
    private void SlideMenu(bool open, bool covers, bool atOnce)
    {
      // A menu that was opened and put away again before the room finished arriving is not special here: its way
      // back starts from wherever the room got to, because that is where the room's value is standing and a
      // transition always starts from where the value is.
      _slideCovers = covers;
      ApplySlide(open ? (covers ? COVER_SLIDE : MENU_SLIDE) : 0, atOnce);
      FocusMediaMenu(open, covers);
    }

    /// <summary>
    /// Stands the room at the told point of the current menu's axis, and at nothing at all on the other one: the
    /// two menus of one screen move at right angles to each other, and a room left two hundred points to the right
    /// while its viewer walks into covers would be a room with a shelf hanging off the screen's right edge.
    /// </summary>
    /// <param name="atOnce">
    /// Whether to arrive without sliding, which is done by taking the transitions off the transform for the length
    /// of the assignment: a transition starts by comparing the value it is handed with the one it held, and with
    /// nothing to compare them with the room is simply where it was told to be.
    /// </param>
    private void ApplySlide(double value, bool atOnce = false)
    {
      TranslateTransform slide = RoomSlide;
      Transitions? held = atOnce ? slide.Transitions : null;
      if (atOnce)
        slide.Transitions = null;

      slide.X = _slideCovers ? 0 : value;
      slide.Y = _slideCovers ? value : 0;

      if (atOnce)
        slide.Transitions = held;
    }

    /// <summary>
    /// Hands the focus to the menu that came on the screen and back to the shelf when it went, which is what the
    /// reference's <c>RestoreFocus</c> on both of its menu lists and its <c>SetFocusPrio</c> on the content panel
    /// between them were for.
    /// </summary>
    private void FocusMediaMenu(bool open, bool covers)
    {
      if (!open)
      {
        FocusShelf();
        return;
      }

      ListBox list = covers ? mediaMenuListBottom : mediaMenuList;

      // The caret goes to the first command rather than to the list and nothing in it: a viewer who opens a menu
      // is asking for a command, and the arrows and the one Enter that follows are the shortest way to any of them.
      // Asked for over several frames (StandCaret), because the rail was made visible by this very answer and
      // cannot hold a caret the frame it appears: asked once, the caret stayed on the tile behind the rail and
      // the button the viewer pressed played the film instead of pressing the row they were standing on.
      StandCaret(list, delegate
      {
        if (list.Items.Count > 0)
          list.SelectedIndex = 0;
      });
    }

    /// <summary>The heading the menu wears while its commands page stands up (see the view model's BuildMediaMenu).</summary>
    private const string COMMANDS_HEADING = "Menu";

    /// <summary>
    /// The row of the commands page that the viewer climbed down from into a subpage, or -1 while nobody is
    /// standing below.  The way back is only half an answer if the viewer comes back standing on nothing, and
    /// the row is the one thing the rebuilt page cannot remember by itself - the rows are new by then.
    /// </summary>
    private int _menuSteppedFrom = -1;

    /// <summary>
    /// Stands the caret on one row of a rail whose rows were just replaced underneath it - the same caret the
    /// menu's opening stands, asked of a rail that is on the screen already and only changed its rows.
    /// </summary>
    private void StandCaretOnRailRow(ListBox rail, int row)
    {
      StandCaret(rail, delegate
      {
        if (rail.Items.Count == 0)
          return;
        rail.SelectedIndex = row < rail.Items.Count ? row : rail.Items.Count - 1;
      });
    }

    /// <summary>
    /// Makes a menu list answer the two ways the reference's menu rows were answered: the button of the remote
    /// while the focus stands on a row, and a pointer landing on one. Every row carries its own command, and the
    /// reference handed that command to its item template with a <c>CommandBridge</c> rather than keeping a table
    /// of row numbers in the screen.
    /// </summary>
    private void WireMenuList(ListBox list)
    {
      list.AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        // The button every input device has: Enter on a keyboard, the space bar where a remote has no letters, and
        // the remote's own OK where it arrives as its own key. A rail of commands that cannot be pressed with any
        // of them is a rail for the mouse only.  Heard though the list handled the key, because the list DOES
        // handle it: a list walks its own caret with the arrows and answers the button itself by ending whatever
        // it was doing, and a handler that only listens to keys nobody used never presses a row.
        if (args.Key != Key.Enter && args.Key != Key.Return && args.Key != Key.Space && args.Key != Key.Select)
          return;

        // The row the caret stands on, and the list's own answer only where that says nothing: the arrows that
        // walk this rail are answered by the screen's own walk, which moves the caret without asking the list to
        // select, so a list left with its first row selected would press its first command from whichever row the
        // viewer had walked to.
        if (StoodOnRow(list) is MediaBrowserMenuEntry row)
        {
          list.SelectedItem = row;
          RunMenuRow(list, row);
          args.Handled = true;
        }
      }, RoutingStrategies.Bubble, true);

      list.AddHandler(PointerPressedEvent, delegate (object? sender, PointerPressedEventArgs args)
      {
        // The press lands somewhere inside the row rather than on the row itself, so the row is asked for by
        // walking up from whatever was touched. A press on the list's own ground between two rows is no command.
        if (args.Source is Visual source)
        {
          ListBoxItem? item = source.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
          if (item?.DataContext is MediaBrowserMenuEntry row)
          {
            list.SelectedItem = row;
            RunMenuRow(list, row);
            args.Handled = true;
          }
        }
      }, RoutingStrategies.Tunnel);

      // The wall a viewer pointed at answers with a nod. Three kinds of key point at one: the key that leads
      // away from the shelf - the left on a rail standing beside the shelf, the up on the rail under covers,
      // whose other side holds nothing at all - and the two keys that lead past an end of the rail, which are
      // walls only while the caret stands on that end. Every other key is the list's own arrow walking and is
      // left to it. The whole rail nods, ground and glow with it, because a row of plates sliding on their
      // ground is a row that fell off it.
      //
      // Heard on the rail's ground rather than on the list: a tunnel handler on the control a key arrives at is
      // never heard there (the control itself is only served on the way back up), and the caret sometimes
      // stands on the list and not on one of its rows - the ground is between the room and every key the rail
      // ever receives, whichever of them wears the caret, and it hears them before the list walks its caret,
      // which is the only moment where "this end is a wall" can still be answered.
      bool sideways_rail = ReferenceEquals(list, mediaMenuListBottom);
      Control rail_ground = sideways_rail ? bottomMenu : sideMenu;
      rail_ground.AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        if (args.Handled || list.Items.Count == 0)
          return;

        Key away_from_the_shelf = sideways_rail ? Key.Up : Key.Left;
        Key past_the_head = sideways_rail ? Key.Left : Key.Up;
        Key past_the_foot = sideways_rail ? Key.Right : Key.Down;
        object? stood = StoodOnRow(list);
        int standing = stood == null ? -1 : list.Items.IndexOf(stood);

        if (args.Key == away_from_the_shelf)
          BounceRail(rail_ground, sideways_rail ? 0 : -RAIL_BOUNCE_DIVE, sideways_rail ? -RAIL_BOUNCE_DIVE : 0);
        else if (args.Key == past_the_head && standing <= 0)
          BounceRail(rail_ground, sideways_rail ? -RAIL_BOUNCE_DIVE : 0, sideways_rail ? 0 : -RAIL_BOUNCE_DIVE);
        else if (args.Key == past_the_foot && standing == list.Items.Count - 1)
          BounceRail(rail_ground, sideways_rail ? RAIL_BOUNCE_DIVE : 0, sideways_rail ? 0 : RAIL_BOUNCE_DIVE);
        else
          return;

        args.Handled = true;
      }, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Presses the command a menu row carries, when it has one that can be pressed - and remembers the row when
    /// the press was a climb down from the commands into a subpage, which the back key asks for by name.
    /// </summary>
    private void RunMenuRow(ListBox list, MediaBrowserMenuEntry row)
    {
      if (!row.Command.CanExecute(null))
        return;

      if ((DataContext as MediaBrowserViewModel)?.MediaMenuHeading == COMMANDS_HEADING)
        _menuSteppedFrom = list.Items.IndexOf(row);

      row.Command.Execute(null);

      // Climbing down a page replaced every row the rail wore, caret included; by this moment the page stands
      // complete, so its first row is asked for at once.  A command that builds its page later - a filter still
      // counting its values - is answered by the heading handler instead, which wakes when the page really comes.
      if (DataContext is MediaBrowserViewModel owner
          && owner.MediaMenuVisible && owner.MediaMenuHeading != COMMANDS_HEADING)
        StandCaretOnRailRow(list, 0);
    }

    /// <summary>
    /// The row of a list that the caret stands on, or the list's own selected row when the caret is on the list
    /// itself rather than inside one of its rows.
    /// </summary>
    /// <remarks>
    /// Asked of the focused control and walked up, because on this screen the arrow keys are answered by the
    /// skin's own walk (see <see cref="Input.SkinFocusWalk"/>) rather than by the list, and a list whose selection
    /// the walk never touched keeps answering with the row it was opened on.  A rail the viewer walked to a command
    /// on and then pressed a button for must press THAT command.
    /// </remarks>
    private object? StoodOnRow(ListBox list)
    {
      if ((TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() is Visual stood)
      {
        // The caret can be on the row itself or on anything painted inside it, so the row is asked for by walking
        // up from the focused control - itself included.
        for (Visual? at = stood; at != null; at = at.GetVisualParent())
        {
          if (at is ListBoxItem container && container.DataContext != null)
            return container.DataContext;
        }
      }

      return list.SelectedItem;
    }

    /// <summary>
    /// Gives the caret to the tile or row the screen is about.  The reference kept the caret on its media list by
    /// way of <c>RestoreFocus</c> and <c>SetFocusPrio</c> (see <see cref="FocusMediaMenu"/>), and what that caret
    /// answers with is the whole of the skin's walking: the ring of the tile a viewer stands on, its picture
    /// stepping nearer and the details beside the shelf all follow the container the caret is in.  A library that
    /// opens without the caret on its shelf therefore opens with no tile lit, no picture grown and - because the
    /// first arrow key then falls to the next focusable thing in the room, which is the screen's own menu standing
    /// two hundred points off the left edge - a viewer who cannot find out where they are.
    /// </summary>
    /// <remarks>
    /// The container is asked for rather than the list, and the container of the standing index rather than the
    /// first: the screen opens with a title selected (it is the title its head and its details speak of), and a
    /// caret on a different tile than the one being described is two answers to one question.  The list is the
    /// fallback for the moment before its containers exist, and is enough - a list with its caret in it moves that
    /// caret onto its selected row as soon as the rows are laid.
    /// </remarks>
    private void FocusShelf()
    {
      if (!titlesList.IsEffectivelyVisible)
        return;

      // The reference's <c>RestoreFocus</c>: the tile the viewer last stood on takes the caret back, so closing
      // the screen's menu hands back the wall exactly as they left it. A wall rebuilt under that title answers
      // false - the remembered title is no longer a tile of this wall - and the standing index asks the usual
      // patient question of the tiles instead.
      if (TryRestoreLastStand())
      {
        GlideShelfToStoodTitle();
        WatchShelfCaret();
        return;
      }

      int standing = titlesList.SelectedIndex < 0 ? 0 : titlesList.SelectedIndex;

      // A container the focus manager still names while it holds no caret is a stale claim, not a caret: a
      // rebuilt wall hands its recycled shells out to new titles underneath the caret. Only a container that
      // stands under the index and really wears the focus may answer the visit; a shell sends the caret the
      // waiting-room way, where it is asked of the tiles again.
      ListBoxItem claim = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement()
          as ListBoxItem;
      bool claim_is_stale = claim != null && !claim.IsKeyboardFocusWithin;

      if (!claim_is_stale
          && titlesList.ContainerFromIndex(standing) is ListBoxItem container && container.IsEffectivelyVisible)
      {
        _shelfTileTimer?.Stop();
        // The container can be laid and yet sit far outside the strip the viewer is looking at - a rebuilt wall
        // realizes containers while its scroll stands at the top. Bring the tile into the view FIRST, so the
        // frame the caret paints is a frame the viewer can see; the glide below then only smooths the ride.
        if (standing < titlesList.Items.Count)
          titlesList.ScrollIntoView(titlesList.Items[standing]);
        FocusShelfTile(container);
        // The selection may well have stood here the whole time, in which case no SelectionChanged ever answered
        // the visit - the wall is stood at the caret by the caret itself, or it stays at the edge it came in at.
        GlideShelfToStoodTitle();
        if (!container.IsKeyboardFocusWithin)
        {
          // Measurable is not yet able to hold the caret; see StandCaret for why that is a question of frames.
          StandCaret(container, delegate { GlideShelfToStoodTitle(); });
        }
        // And even a container that took the caret can have it knocked out of it by the recycling the glide
        // starts a frame later - the watching covers every way out of this method, not just the waiting one.
        WatchShelfCaret();
        return;
      }

      // The tiles are laid a pass after the screen is first measured, so the container the caret belongs in does
      // not exist yet at this moment. The list takes the caret meanwhile - which is not nothing, since the first
      // arrow key then moves the caret inside it - and the tile is asked for again once it is laid.  Laid, though,
      // is the word: a virtualised wall only owns containers near its viewport, and a caret asked for at a title
      // the wall has never scrolled to would wait a fifth of a second for a container that was never coming.
      // So the wall is told to bring that title into view first - which is what the glide does for the picture
      // anyway - and only then is the tile asked for.
      titlesList.Focus();
      if (standing < titlesList.Items.Count)
        titlesList.ScrollIntoView(titlesList.Items[standing]!);
      WatchShelfCaret();
    }

    private DispatcherTimer? _shelfTileTimer;

    /// <summary>
    /// The title the caret last stood a tile of - the shelf's memory for the reference's <c>RestoreFocus</c>.
    /// An object rather than a container: containers are the wall's to recycle, the titles are the viewer's.
    /// </summary>
    private object? _lastTileStand;

    /// <summary>
    /// Puts the caret back on the tile of the title it last stood on, and says whether it could.
    /// </summary>
    /// <remarks>
    /// The remembered title is looked up by what it is, not by where the caret came from - a rebuilt wall hands
    /// its containers out like hats, and the container the caret wore may now present a different title or
    /// none. A title the wall no longer wears forgets the memory; a title it does wear is stood at again, its
    /// tile brought into view and asked for through the one door that escapes the focus manager's stale claims.
    /// </remarks>
    private bool TryRestoreLastStand()
    {
      if (_lastTileStand == null)
        return false;

      int index = titlesList.Items.IndexOf(_lastTileStand);
      if (index < 0)
      {
        _lastTileStand = null;
        return false;
      }

      if (titlesList.SelectedIndex != index)
        titlesList.SelectedIndex = index;

      // Bring the tile into view FIRST: mid-rebuild a container can exist and be "visible" while sitting far
      // outside the viewport (or at stale coordinates), and a caret on such a tile is a caret nobody sees.
      titlesList.ScrollIntoView(titlesList.Items[index]);

      if (titlesList.ContainerFromIndex(index) is not ListBoxItem tile || !tile.IsEffectivelyVisible)
        return false;

      FocusShelfTile(tile);
      return tile.IsKeyboardFocusWithin;
    }

    /// <summary>
    /// Keeps the caret on the tile of the standing title while the wall might still knock it off.
    /// </summary>
    /// <remarks>
    /// Landing the caret is not the end of the story: a rebuilt wall recycles its containers a beat AFTER the
    /// caret was placed - the focused container is pulled out of the tree by the very scroll that brought its
    /// title into view, and the focus manager quietly hands the caret back to the bare list, which wears no
    /// frame. Nothing announces that fall when the wall stopped changing (a sort that only re-asks the same
    /// titles never fires a second refill), so an asker that stops after one landing leaves the viewer on
    /// nothing again. This asks instead until the caret stands where it belongs or leaves the shelf: one tick
    /// says "settled" (or "the viewer took the caret elsewhere - a dialog, the rail, the search field") and
    /// ends the watching; every other tick puts the caret back on the tile of the standing title.
    /// </remarks>
    /// <summary>
    /// Whether the told focused element belongs to one of the screen's two rails - the rail itself, one of its
    /// rows, or a button inside one. A closed rail is off the window but not off the focus manager's reach.
    /// </summary>
    private bool HoldsRailCaret(object? focused)
    {
      for (Visual? at = focused as Visual; at != null; at = at.GetVisualParent())
      {
        if (ReferenceEquals(at, mediaMenuList) || ReferenceEquals(at, mediaMenuListBottom))
          return true;
      }

      return false;
    }

    /// <summary>
    /// Puts the caret back on the tile of the standing title while that tile stands where the viewer can
    /// see it, brings it back from just outside the laid band when the wall was merely rebuilt under the
    /// caret, and LETS THE CARET REST when the viewer has rolled the wall away from it.
    /// </summary>
    /// <remarks>
    /// A rescue that only shakes its head at a container which is not there waits forever: a virtualised
    /// wall lays its tiles out around what it SHOWS, so a wall that is not looking at the standing title
    /// will never lay that tile again by itself - and after a rebuild the standing tile is exactly such a
    /// container for a beat. Fetching it is what scrolling into view does. But a fetch is a wall that runs
    /// to wherever the caret was left, and measured on the live tree, that fetch yanked a wall the viewer
    /// had just scrolled away back to its first tile and back again with every wheel notch - a rescue that
    /// fights the viewer is worse than no rescue. So the fetch is measured: near the laid band (one wall's
    /// worth of containers either side) it is a rebuild the caret fell through, and the wall goes to get
    /// the caret; further than that, the viewer left the caret behind on purpose and the watching is told
    /// so - it runs its quiet length out rather than fetching, so a viewer who rolls back within that
    /// length finds the frame lit again without pressing anything. The next step of a walk re-arms it:
    /// the selection wakes the watching again and the glide takes the wall to the new standing tile
    /// itself. Focusing a tile whose picture has left the window is given up for the same reason - asking
    /// a control for the focus asks Avalonia to scroll it into view on the way, and that single call is
    /// the whole snap-back.
    /// </remarks>
    private bool RescueShelfCaret()
    {
      int stand = titlesList.SelectedIndex < 0 ? 0 : titlesList.SelectedIndex;
      if (titlesList.ContainerFromIndex(stand) is ListBoxItem tile && StandsOnTheShownWall(tile))
      {
        FocusShelfTile(tile);
        return true;
      }

      if (stand >= titlesList.Items.Count || CaretStillWithinReach(stand))
      {
        if (stand < titlesList.Items.Count)
          titlesList.ScrollIntoView(titlesList.Items[stand]);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Whether a container's picture stands inside the window the wall is read through.
    /// </summary>
    private bool StandsOnTheShownWall(Control tile)
    {
      ScrollViewer? scroller = ListScroller();
      if (scroller == null)
        return true;

      Point? at = tile.TranslatePoint(new Point(), scroller);
      if (at == null)
        return true;

      Rect sight = new Rect(0, 0, scroller.Viewport.Width, scroller.Viewport.Height);
      Rect picture = new Rect(at.Value, tile.Bounds.Size);
      return sight.Intersects(picture);
    }

    /// <summary>
    /// Whether the standing index stands within one wall's worth of containers of the band the wall has
    /// laid: close enough that the tile's absence reads as a rebuild rather than as a viewer who rolled
    /// away. A wall with nothing laid at all answers yes - that IS the rebuild.
    /// </summary>
    private bool CaretStillWithinReach(int stand)
    {
      int low = int.MaxValue;
      int high = int.MinValue;
      foreach (Control? container in titlesList.GetRealizedContainers())
      {
        int at = titlesList.IndexFromContainer(container);
        if (at < 0)
          continue;

        low = Math.Min(low, at);
        high = Math.Max(high, at);
      }

      if (high < low)
        return true;

      int reach = Math.Max(1, high - low + 1);
      return stand >= low - reach && stand <= high + reach;
    }

    private void WatchShelfCaret()
    {
      _shelfTileTimer?.Stop();

      // Asked for at once first: the caller has just scrolled the standing title into view, and scrolling
      // realizes the container right here - a caret that lands in this breath is one that lands before
      // whoever asked has looked again.
      TryStandOnShelfTile(titlesList.SelectedIndex < 0 ? 0 : titlesList.SelectedIndex);

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(32)
      };
      _shelfTileTimer = timer;
      int left = 3000;

      // The wall keeps sliding for a second after a landing - and the tile the caret stands on is exactly the
      // one that slide recycles. A watchdog that stops at the first tick therefore dies long before the fall
      // it exists for, so a tile under the caret buys quiet watching instead of an end: only four seconds of
      // undisturbed standing retire it, every rescue rewinds it, and a caret that went anywhere that ISN'T
      // this shelf - the rail, a dialog, the search field, another screen - retires it at once.
      int quiet = 120;

      timer.Tick += delegate
      {
        object? fallen = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement();

        if (left-- <= 0)
        {
          timer.Stop();
          return;
        }

        // A caret the closed rail still holds is a fall too: the rail slid off the screen but its list - or
        // the button of the row that was clicked - stayed focusable, and Avalonia hands the caret to whatever
        // was clicked last once the click settles, which lands a beat AFTER the close already handed the caret
        // to the shelf. Left there, the viewer stands on an invisible row off the window and the wall wears no
        // frame - which is the whole complaint in one sentence. The rescue can fetch a tile the wall rolled
        // out of sight while it stands within reach of the laid band - a rescue that merely asks for a
        // container which is not there waits forever - but a caret left far behind by the viewer's own
        // scrolling is not a fall; see RescueShelfCaret for where the fetch ends and letting-rest begins.
        if (!(DataContext is MediaBrowserViewModel screen) || !screen.MediaMenuVisible)
        {
          if (HoldsRailCaret(fallen))
          {
            if (RescueShelfCaret())
              quiet = 120;
            else if (--quiet <= 0)
              timer.Stop();
            return;
          }
        }

        if (ReferenceEquals(fallen, titlesList))
        {
          // The fall this watchdog exists for: the caret slipped back onto the bare list, which wears no
          // frame. The tile of the standing title - read fresh, since the selection may have moved - takes
          // the caret again and the quiet counting starts over. A viewer who rolled the wall clean away
          // from the caret is not a fall though; the rescue says so, and the watching runs its length out.
          if (RescueShelfCaret())
            quiet = 120;
          else if (--quiet <= 0)
            timer.Stop();
          return;
        }

        if (fallen is ListBoxItem tile
            && !ReferenceEquals(tile, titlesList)   // ListBox IS A ListBoxItem - exclude the list itself
            && titlesList.ItemFromContainer(tile) != null)
        {
          if (--quiet <= 0)
            timer.Stop();
          return;
        }

        // A shell the recycling stripped of its title (index -1) is a fall too: the caret stands on a body
        // that presents nobody and wears no frame. A rail row reads the same way from here, so the rails are
        // asked first - while the menu stands in sight its rows are legitimate caret-holders.
        if (fallen is ListBoxItem shell && !HoldsRailCaret(shell)
            && titlesList.IndexFromContainer(shell) < 0)
        {
          if (RescueShelfCaret())
            quiet = 120;
          else if (--quiet <= 0)
            timer.Stop();
          return;
        }

        timer.Stop();
      };

      timer.Start();
    }

    /// <summary>
    /// Puts the caret on a tile of the shelf, escaping the limbo the focus manager can leave it in.
    /// </summary>
    /// <remarks>
    /// After a rebuild the focus manager can still <em>name</em> a container as the screen's focused element
    /// while the container itself lost its focus state - a stale claim, not a caret. Asked to focus the very
    /// element it already claims to hold, the focus manager answers "done" and changes nothing, so the tile
    /// never lights and every asker who checks for success believes it. The caret is therefore put on the list
    /// first - which is a different element and does move - and only then asked of the tile, where it lands
    /// for real.
    /// </remarks>
    private void FocusShelfTile(ListBoxItem tile)
    {
      if (ReferenceEquals((TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement(), tile)
          && !tile.IsKeyboardFocusWithin)
        titlesList.Focus();

      tile.Focus();
    }

    /// <summary>
    /// Puts the caret on the tile of the standing title when that tile is laid, and says whether it did.
    /// </summary>
    private bool TryStandOnShelfTile(int standing)
    {
      int place = standing < titlesList.Items.Count ? standing : titlesList.Items.Count - 1;
      if (place >= 0 && titlesList.ContainerFromIndex(place) is ListBoxItem tile
          && tile.IsEffectivelyVisible)
      {
        FocusShelfTile(tile);
        return tile.IsKeyboardFocusWithin;
      }

      return false;
    }

    /// <summary>Frames a caret is asked for before the screen stops asking (a fifth of a second).</summary>
    private const int CARET_TRIES = 12;

    private DispatcherTimer? _caretTimer;

    /// <summary>
    /// Puts the caret on a control of this screen and keeps asking for it until it is there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A control that has not been measured yet cannot hold the caret and says so by answering
    /// <c>Focus()</c> with a failure and nothing else.  That is the state every control here is in at the moment
    /// the screen decides the caret belongs to it - the menu was told to come on in the same breath, the tiles
    /// came off the server one frame ago, the screen itself is still sliding in.  Asked once, the caret therefore
    /// stayed wherever it was: behind a rail the viewer had just opened, so its commands could not be reached
    /// with a button and the shelf answered them instead, or behind a shelf the viewer had just walked into, so
    /// no tile was lit and the keys found nobody.  Asked once per frame for a fifth of a second, it lands the
    /// moment the control can hold it.
    /// </para>
    /// <para>
    /// A caret the viewer moved in the meantime is not fought over: a control that has the caret ends the asking
    /// (see the first condition of the tick), and a field the viewer is typing into always wins, because a
    /// screen that pulls the caret out of a search box while somebody is spelling a title into it is unusable.
    /// </para>
    /// </remarks>
    /// <param name="target">The control the caret belongs on.</param>
    /// <param name="whenStood">
    /// What to do once the caret is standing there, which is where the follow-up that only makes sense with the
    /// caret in place belongs - the wall being stood at its tile, the first row of a rail being taken.
    /// </param>
    private void StandCaret(Control target, Action? whenStood)
    {
      _caretTimer?.Stop();

      target.Focus();
      if (target.IsKeyboardFocusWithin)
      {
        whenStood?.Invoke();
        return;
      }

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _caretTimer = timer;
      int left = CARET_TRIES;

      timer.Tick += delegate
      {
        if (target.IsKeyboardFocusWithin)
        {
          timer.Stop();
          whenStood?.Invoke();
          return;
        }

        if ((TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() is TextBox
            || left-- <= 0)
        {
          timer.Stop();
          return;
        }

        target.Focus();
      };

      timer.Start();
    }

    /// <summary>How far a rail nods toward the wall a viewer pointed at, in points.</summary>
    private const double RAIL_BOUNCE_DIVE = 22;

    /// <summary>The lean toward the wall and the spring back, both in milliseconds.</summary>
    private const int RAIL_BOUNCE_DIVE_MS = 90;
    private const int RAIL_BOUNCE_SPRING_MS = 430;

    private DispatcherTimer? _railBounceTimer;
    private TranslateTransform? _railBounceTransform;

    /// <summary>
    /// Where the nodding rail currently stands. A test - and the render probe - read these to know that a wall
    /// was pointed at: while no wall has been pointed at since the last nod came to rest, the rail stands at
    /// nothing at all.
    /// </summary>
    internal double RailBounceX => _railBounceTransform?.X ?? 0;

    /// <inheritdoc cref="RailBounceX" />
    internal double RailBounceY => _railBounceTransform?.Y ?? 0;

    /// <summary>
    /// Nods a rail the told number of points toward the wall the viewer pointed at and springs it back.
    /// </summary>
    /// <remarks>
    /// A wall is answered rather than passed over, because a screen that says nothing when a key points at
    /// nothing is a screen where the viewer cannot tell a wall from a broken key. The nod runs on the front
    /// end's own timer, the way <see cref="StandCaret"/> asks for a caret: ninety milliseconds of lean out -
    /// squared off, so the first frames carry most of it and the wall is named at once - and four hundred and
    /// thirty of easeOutBack home, which carries the rail a tenth of the dive past its rest before it settles,
    /// which is what makes the answer a bounce rather than a shuffle back.
    /// </remarks>
    /// <param name="rail">The rail's whole ground: the panel that carries the list, its heading and its glow.</param>
    /// <param name="diveX">The lean along X, positive to the right; zero for a wall above or below.</param>
    /// <param name="diveY">The lean along Y, positive downward; zero for a wall beside the rail.</param>
    private void BounceRail(Control rail, double diveX, double diveY)
    {
      // One nod at a time. A key pressed while the previous nod is still travelling starts a new one from
      // wherever the rail stands, because that is where the rail is and a viewer leaning on a key repeatedly
      // should see the rail lean with every press.
      _railBounceTimer?.Stop();

      if (_railBounceTransform != null && !ReferenceEquals(rail.RenderTransform, _railBounceTransform))
      {
        // The other rail wore the last nod - walking from a grid into covers does that. Put it back to rest
        // before nodding this one, so no rail is left hanging off its ground.
        _railBounceTransform.X = 0;
        _railBounceTransform.Y = 0;
        _railBounceTransform = null;
      }

      TranslateTransform nod = rail.RenderTransform as TranslateTransform;
      if (nod == null)
      {
        nod = new TranslateTransform();
        rail.RenderTransform = nod;
      }
      _railBounceTransform = nod;

      double fromX = nod.X;
      double fromY = nod.Y;
      long started = Environment.TickCount64;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _railBounceTimer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(_railBounceTimer, timer))
          return; // a newer nod drives this rail from here on

        long elapsed = Environment.TickCount64 - started;
        if (elapsed >= RAIL_BOUNCE_DIVE_MS + RAIL_BOUNCE_SPRING_MS)
        {
          timer.Stop();
          nod.X = 0;
          nod.Y = 0;
          return;
        }

        if (elapsed <= RAIL_BOUNCE_DIVE_MS)
        {
          double u = elapsed / (double)RAIL_BOUNCE_DIVE_MS;
          double lean = 1 - (1 - u) * (1 - u);
          nod.X = fromX + (diveX - fromX) * lean;
          nod.Y = fromY + (diveY - fromY) * lean;
          return;
        }

        double v = (elapsed - RAIL_BOUNCE_DIVE_MS) / (double)RAIL_BOUNCE_SPRING_MS;
        double c1 = 1.70158;
        double c3 = c1 + 1;
        double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
        nod.X = diveX * (1 - back);
        nod.Y = diveY * (1 - back);
      };

      timer.Start();
    }

    /// <summary>
    /// Whether the shelf has to be asked for the caret: nothing has the caret, or the caret stands on the shelf's
    /// own list without having reached a tile of it. Anything else the caret stands on inside this screen - a
    /// tile, the head, the details, a field the viewer is typing into - is where the caret stays.
    /// </summary>
    private bool NeedsCaretOnTheShelf()
    {
      Visual? focused = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() as Visual;
      if (focused == null)
        return true;

      bool inside = false;
      for (Visual? at = focused; at != null; at = at.GetVisualParent())
      {
        if (ReferenceEquals(at, this))
          inside = true;
        if (ReferenceEquals(at, titlesList))
        {
          // The list itself is the caret's waiting room, not its place: a caret that got no further than the list
          // never lit a tile, which is what the caret is on the shelf for.
          if (ReferenceEquals(focused, titlesList))
            return true;

          // And a container is the caret's place only while it is THE tile the wall stands on. A rebuilt wall
          // hands its recycled shells out underneath the caret: the shell keeps answering keys, still shows a
          // title - another one now - and the focus manager never lets go of it, while the tile the viewer is
          // supposed to stand on wears no frame somewhere else or off the screen. A caret that drifted onto
          // such a shell counts as nowhere, and the shelf asks for the standing tile again.
          int stand = titlesList.SelectedIndex < 0 ? 0 : titlesList.SelectedIndex;
          return !ReferenceEquals(focused, titlesList.ContainerFromIndex(stand));
        }
      }

      return !inside;
    }

    /// <summary>
    /// Puts the search field on the screen and moves the caret into it. The field is only laid out while somebody
    /// is searching (the reference kept it off the screen until a viewer asked for it, and the shelf has no room to
    /// spare), so it has to be measured before it can be focused - which is why the caret is asked for on the next
    /// pass rather than in this one.
    /// </summary>
    private void RevealSearch()
    {
      Dispatcher.UIThread.Post(delegate { searchBox.Focus(); }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Runs every time the workflow shows this screen.  The first showing asks the server for the titles and
    /// starts watching the view model; every showing gives the rows the focus, so the first Enter of the visit
    /// means "play this" - see HomeView for the general reason.
    /// </summary>
    public void OnScreenActivated()
    {
      // The head band is asked for again on every showing: a walk from the home hands the header back to its own
      // row on the way out of that screen, and depending on the order the two screens were swapped that hand-back
      // can land after this screen asked for the header to float.
      _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
      _chromeShell?.SetChromeHeaderOverlay(true);
      _chromeShell?.SetChromeHeaderStowed(true);

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
      // ...and asked for again once the room has been measured. The screen is activated before its shelf has been
      // laid out at all - the titles are on their way from the server - and a caret handed to a list that is not
      // measured yet is a caret nobody took, which leaves the first arrow key of the visit to fall to the next
      // focusable thing in the room: the screen's own menu, standing two hundred points off the left edge.
      Dispatcher.UIThread.Post(delegate
      {
        if (NeedsCaretOnTheShelf())
          FocusShelf();
      }, DispatcherPriority.Loaded);
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
    /// The share of the room the details column is offered: the reference's own split, measured at the width it
    /// drew for - a thousand of its nineteen hundred and twenty points are its details, which is this share.
    /// </summary>
    private const double DETAILS_SHARE = 0.52;

    /// <summary>How wide the details column is allowed to grow, which is the reference's thousand points.</summary>
    private const double DETAILS_MAX = 1000;

    /// <summary>Under this width the column stops being a column and is only a strip, so it is not given less.</summary>
    private const double DETAILS_MIN = 340;

    /// <summary>
    /// The width a shelf is never allowed to fall under: two tiles of the poster shape the shelf is most often
    /// worn in. A details column that leaves less than that lays the shelf out in one long line of single tiles,
    /// which is a list wearing a grid's clothes.
    /// </summary>
    private const double SHELF_MIN = 470;

    /// <summary>What the details column was last told to be, so that a resize that changes nothing changes nothing.</summary>
    private double? _detailsColumnWidth;

    /// <summary>
    /// Divides the room between the shelf and the details column.
    /// </summary>
    /// <remarks>
    /// The reference handed its details column a thousand fixed points and its media area whatever was left, which
    /// was a fair thing to do on a screen of a known width. This front end is stood on desktop windows of every
    /// shape as well, and a details column that keeps its thousand points on a small window leaves the shelf a
    /// gutter to lay its tiles in - tiles that then wrap into one narrow column, so that a viewer on a laptop sees
    /// a single file of posters where the television shows a wall of them.
    /// </remarks>
    /// <remarks>
    /// The column is therefore measured in points of its own rather than left to the grid's proportions, and for
    /// two reasons: a proportion is a share of whatever is left after the content of the other columns has taken
    /// what it insists on, so a wide picture or a long word in the details silently takes points from the shelf,
    /// while a told number of points cannot be argued with. The number is the share of the room the reference
    /// divided, held between the width below which the column is no longer a column and the reference's own
    /// thousand - and never so much that the shelf is left narrower than two tiles, which is the one question a
    /// library screen is asked. With the details away (covers, or a viewer who turned them off) the column is
    /// nothing at all and the shelf has the room to itself, which is what the reference's cover layout did with
    /// its details centred above the covers instead of beside them.
    /// </remarks>
    private void ApplyColumns()
    {
      if (!(DataContext is MediaBrowserViewModel viewModel))
        return;

      // The room's own width, which the shell hands this view when it lays the screen under its header. Before
      // that has happened the reference's own width is assumed, and the first measure corrects it.
      double room = Bounds.Width > 0 ? Bounds.Width : 1920;
      double details = 0;
      if (viewModel.ShowsSideDetails)
      {
        details = Math.Min(Math.Clamp(room * DETAILS_SHARE, DETAILS_MIN, DETAILS_MAX), room - SHELF_MIN);
        if (details < 0)
          details = 0;
      }

      if (_detailsColumnWidth.HasValue && Math.Abs(_detailsColumnWidth.Value - details) < 0.5)
        return;

      _detailsColumnWidth = details;
      contentGrid.ColumnDefinitions = new ColumnDefinitions
      {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(new GridLength(details))
      };
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
      IDataTemplate frame = SkinEngine.TryTileTemplate(viewModel.GridTemplateKey)
          ?? BuiltinTemplate(viewModel.Layout);
      bool frame_changed = !ReferenceEquals(_wornFrame, frame);
      _wornFrame = frame;
      titlesList.ItemTemplate = frame;

      // A wall that changes the frame its tiles wear has to throw its standing tiles away.  A virtualizing panel
      // keeps the containers it stood up and re-hangs them on the next title, and a re-hung container keeps the
      // tree it was generated with - so a shelf switched from posters to thumbnails grew thumbnails where its
      // tiles had already stood and posters where they had not, and the mixing stayed until the viewer walked
      // every row twice.  Clearing the source once rebuilds every tile from the new frame; it costs one
      // realization, and it happens when the viewer presses the picture button, not while a wall travels.
      if (frame_changed)
      {
        System.Collections.IEnumerable? titles = titlesList.ItemsSource;
        object? stood_on = viewModel.Selected;
        titlesList.ItemsSource = null;
        titlesList.ItemsSource = titles;
        // Emptying the source empties the selection with it, and the selection answers the details, the fanart
        // and the caret - so the tile the viewer was standing on is stood on again, which is also what makes the
        // wall glide back to it rather than leaving the viewer at the head of a rebuilt wall.
        if (stood_on != null && titlesList.SelectedItem == null)
          titlesList.SelectedItem = stood_on;
      }

      // Tiles dress themselves - plate, picture, name and ring - so the list may not paint its selected row
      // panel under them; a shelf of rows is read by that panel and keeps it.
      titlesList.Classes.Set("nereus-shelf-tiles", viewModel.Layout != BrowserLayout.List);
      titlesList.Classes.Set("nereus-shelf-rows", viewModel.Layout == BrowserLayout.List);

      // A grid and a list are laid one under the other, the grid wrapping whole tiles across the screen and the
      // list keeping its strip of rows. Covers are the third shape and the only one the reference turned on its
      // side: their row runs along the bottom edge of the room, their details are centred above it, and the
      // screen's menu waits under the edge of the screen for the room to be lifted up to it.
      if (viewModel.Layout == BrowserLayout.Cover)
      {
        titlesList.ItemsPanel = COVER_PANEL;
        titlesList.VerticalAlignment = VerticalAlignment.Bottom;
        // The floor the grid and the list hand back to stand their wall in the middle of the room is not the
        // covers' floor: their row lies along the bottom edge of the room itself, and lifting it by that floor
        // would float the row off the edge it was laid against.
        titlesList.Margin = new Avalonia.Thickness(0);
        ScrollViewer.SetHorizontalScrollBarVisibility(titlesList, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(titlesList, ScrollBarVisibility.Disabled);
      }
      else
      {
        titlesList.ClearValue(Layoutable.VerticalAlignmentProperty);
        // Not a ClearValue: that would take the markup's floor away for good, and the wall would stand on the
        // bottom edge whatever the markup says.  The number is the markup's and is put back as it was read.
        titlesList.Margin = _shelfFloor;
        ScrollViewer.SetVerticalScrollBarVisibility(titlesList, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(titlesList, ScrollBarVisibility.Disabled);
        if (viewModel.Layout != BrowserLayout.List)
          titlesList.ItemsPanel = TILE_PANEL;
        else
          titlesList.ClearValue(ItemsControl.ItemsPanelProperty);
      }

      // A panel swapped under a standing list is laid out again, and with it the scroller it is read through - its
      // fade and its glide both live on that scroller, so both are put back on and the shelf is stood where the
      // title being stood on belongs in the new shape's band.
      _scroller = null;
      ApplyShelfFade(viewModel.Layout);
      InstallShelfGlide(viewModel.Layout);
      GlideShelfToStoodTitle();
    }

    /// <summary>
    /// Puts an edge fade on the scroller the shelf is read through, and holds it to one question at each of the
    /// wall's two ends: has the wall left anything standing out of the picture there?
    /// </summary>
    /// <remarks>
    /// The reference fades nothing.  <c>MediaStyles.xaml:5205</c>, <c>:5229</c> and <c>:5253</c> give their three
    /// shapes a <c>ScrollMargin</c> and a negative <c>Margin</c> and no mask whatever - and the only mask its home
    /// content does wear, <c>HomeContentStyles.xaml:1868</c>, is painted black at all four stops over a presenter
    /// that itself wears a solid black <c>DummyOpacityMask</c>, which is a mask that hides nothing at all.  So the
    /// fade here is this front end's own (twelve percent, borrowed from the menu column of styles.xaml) and it is
    /// answerable to the one thing a viewer can say about it: a row that has been walked out of the picture may
    /// dissolve at the edge it went out by, but the first row of a wall that stands at the top of the screen has
    /// been walked out of nothing and stands whole - and so does the last row once the wall has run to its end.  A
    /// blend laid over both ends whatever the wall was doing, which is what this method used to wear, took the head
    /// off the first row of every wall there ever was.
    /// <para>
    /// So each end is asked, on every scroll, how much of the wall stands outside the picture: nothing out is no
    /// fade at that end, a whole fade zone's worth out is the fade at full depth, and what lies between lies
    /// between.  That reading is taken off a ladder of eight steps rather than off the offset itself, so a wall
    /// travelling through its middle - where both ends have long been out of the picture - rewrites nothing, and
    /// the writes are the handful of steps taken while leaving one end and arriving at the next.
    /// </para>
    /// </remarks>
    private void ApplyShelfFade(BrowserLayout layout)
    {
      // Covers are walked sideways, so their ends are the left and the right edge; every other shape is walked
      // down.  The depths the mask remembers belong to an axis, so a wall turned from one to the other forgets them.
      bool sideways = layout == BrowserLayout.Cover;
      if (sideways != _fadeRunsSideways)
      {
        _fadeRunsSideways = sideways;
        _fadeBefore = _fadeAfter = UNREAD;
      }

      // The scroller lives inside the list's template and only exists once the list has been measured, so the
      // answer is asked for again on the next pass rather than given up on.
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        if (!ReferenceEquals(_fadedScroller, scroller))
        {
          // A retemplated list - which is what putting a skin on is - brings a new scroller, and the mask and the
          // remembered depths both go with the old one.
          _fadedScroller = scroller;
          _fadeBefore = _fadeAfter = UNREAD;
          scroller.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { UpdateShelfFade(); });
        }

        UpdateShelfFade();
      }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Says what each end of the wall is hiding and, if that is not what the mask already says, writes it on.
    /// </summary>
    private void UpdateShelfFade()
    {
      ScrollViewer? scroller = _fadedScroller;
      if (scroller == null)
        return;

      double view = _fadeRunsSideways ? scroller.Viewport.Width : scroller.Viewport.Height;
      if (view <= 0)
        return;

      double extent = _fadeRunsSideways ? scroller.Extent.Width : scroller.Extent.Height;
      double offset = _fadeRunsSideways ? scroller.Offset.X : scroller.Offset.Y;

      // What stands outside the picture at either end.  A wall shorter than its window stands outside neither of
      // them and is faded at neither.
      double zone = SHELF_EDGE_FADE * view;
      double before = FadeDepth(offset, zone);
      double after = FadeDepth(extent - view - offset, zone);
      if (before == _fadeBefore && after == _fadeAfter)
        return;

      _fadeBefore = before;
      _fadeAfter = after;

      // A wall that has left nothing out of the picture at either end wears no mask at all, which is both the
      // honest answer and the cheap one: an unmasked shelf is a shelf whose pixels are not put through a brush.
      if (before == 0 && after == 0)
      {
        scroller.OpacityMask = null;
        return;
      }

      LinearGradientBrush fade = _shelfFade ??= NewShelfFade();
      fade.StartPoint = new RelativePoint(_fadeRunsSideways ? 0.0 : 0.5,
          _fadeRunsSideways ? 0.5 : 0.0, RelativeUnit.Relative);
      fade.EndPoint = new RelativePoint(_fadeRunsSideways ? 1.0 : 0.5,
          _fadeRunsSideways ? 0.5 : 1.0, RelativeUnit.Relative);

      // A mask hides by its own transparency, so an end that is hiding nothing is written opaque and an end that
      // has walked a whole fade zone of wall out of the picture is written clear.
      fade.GradientStops[0].Color = FadeColour(before);
      fade.GradientStops[3].Color = FadeColour(after);
      scroller.OpacityMask = fade;
    }

    /// <summary>
    /// How deeply one end of the wall fades: nothing out of the picture is no fade, a fade zone's worth out of it
    /// is the full fade, and the way between is walked in eight steps so that a travelling wall only rewrites its
    /// mask where the depth genuinely moved.
    /// </summary>
    private static double FadeDepth(double hidden, double zone)
    {
      if (hidden <= 0 || zone <= 0)
        return 0;

      double gone = Math.Min(1.0, hidden / zone);
      return Math.Round(gone * FADE_STEPS) / FADE_STEPS;
    }

    /// <summary>The mask's colour for an end fading to the given depth.</summary>
    private static Color FadeColour(double depth)
      => Color.FromArgb((byte)Math.Round((1.0 - depth) * 255.0), 0xFF, 0xFF, 0xFF);

    /// <summary>The shelf's mask with both ends standing opaque; the depths are written on it afterwards.</summary>
    private static LinearGradientBrush NewShelfFade()
    {
      LinearGradientBrush fade = new LinearGradientBrush
      {
        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
      };
      fade.GradientStops.Add(new GradientStop(Colors.White, 0));
      fade.GradientStops.Add(new GradientStop(Colors.White, SHELF_EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1.0 - SHELF_EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1));
      return fade;
    }

    /// <summary>
    /// How far in from an end of the shelf its rows dissolve once a whole fade zone of wall has gone out of the
    /// picture: the twelve percent this front end's menu column wears.
    /// </summary>
    private const double SHELF_EDGE_FADE = 0.12;

    /// <summary>How finely the depth an end fades is read off the wall: eight steps between none and all.</summary>
    private const double FADE_STEPS = 8;

    /// <summary>A depth no end has been asked about yet, so that the first reading is always written.</summary>
    private const double UNREAD = -1;

    private LinearGradientBrush? _shelfFade;

    private ScrollViewer? _fadedScroller;

    private double _fadeBefore = UNREAD;

    private double _fadeAfter = UNREAD;

    private bool _fadeRunsSideways;

    /// <summary>The frame the shelf's tiles were last told to wear; a change of it empties the wall (ApplyLayout).</summary>
    private IDataTemplate? _wornFrame;

    /// <summary>
    /// The covers' panel: the tiles' own panel turned, which is to say a strip that lays one cover beside the
    /// next and keeps standing only the ones the screen could be drawing.
    /// </summary>
    private static readonly FuncTemplate<Panel?> COVER_PANEL =
        new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { Orientation = Orientation.Horizontal });

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
      // screen away is a poster that is there when they arrive, and one that arrives after they do is grey. For
      // covers the band runs sideways instead, which is the direction their shelf is walked in.
      Rect visible = titlesList.Bounds;
      Rect inSight = viewModel.Layout == BrowserLayout.Cover
          ? new Rect(visible.X - visible.Width, visible.Y, visible.Width * 3, visible.Height)
          : new Rect(visible.X, visible.Y - visible.Height, visible.Width, visible.Height * 3);

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
    /// <summary>
    /// Whether the highlighted title stands on the edge of the shelf beyond which the screen's own menu waits:
    /// the whole first column for a grid or a wall of tiles, any row at all for a list - whose rows are one
    /// column wide, so every row touches the left edge - and the head of the row for covers, which run along
    /// the bottom of the room. A shelf that has not laid itself out yet has one column, which is the answer
    /// that lets the very first title reach the menu, as it always did.
    /// </summary>
    private bool StandingOnTheLeadingEdge(MediaBrowserViewModel viewModel)
    {
      int standing = titlesList.SelectedIndex;
      if (standing < 0 || viewModel.Titles.Count == 0)
        return false;

      if (viewModel.Layout == BrowserLayout.Cover)
        return standing == 0;

      if (viewModel.Layout == BrowserLayout.List)
        return true;

      return standing % ShelfColumns() == 0;
    }

    /// <summary>
    /// How many tiles the shelf lays across, taken from the panel that lays them - which is the only one who
    /// knows, because it is the tile's own width against the shelf's that decides, not a number anybody sets.
    /// Asked, not remembered: switching the layout swaps the panel out from under any memory of it.
    /// </summary>
    private int ShelfColumns()
      => titlesList.GetVisualDescendants().OfType<VirtualizingTilePanel>().FirstOrDefault()?.Columns ?? 1;

    private ScrollViewer? ListScroller()
    {
      if (_scroller == null)
        _scroller = titlesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

      return _scroller;
    }

    private ScrollViewer? _scroller;

    /// <summary>
    /// The window's chrome, kept for the length of a visit: this screen asks the header to float over its own row
    /// so that its fanart reaches the top edge of the window, and hands it back on the way out.
    /// </summary>
    private ShellWindow? _chromeShell;

    /// <summary>
    /// The two numbers the reference gave each of its three shapes for moving a shelf: how far the stood title is
    /// kept from the end it came from and the end it is going to (<c>ScrollMargin</c>), and how long the walk
    /// between two places takes (<c>ScrollAnimation</c>). Both are in the skin's own points.
    /// </summary>
    /// <remarks>
    /// From <c>MediaStyles.xaml:5205</c> (the rows, 130 either side over three hundred milliseconds),
    /// <c>:5230</c> (the grid, four hundred either side over four hundred) and <c>:5253</c> (the covers, which run
    /// sideways and are given two hundred and eighty before and two hundred and sixty after, over four hundred
    /// with the reference's own acceleration and deceleration). A grid's band does not hold its tile - four hundred
    /// plus two hundred and seventy plus four hundred is more than a shelf of a thousand - and that is the point:
    /// the tile is then kept at the band's middle, which is how the reference's wall glides a whole row at a time
    /// instead of nudging a row's height every now and then.
    /// </remarks>
    private static (double Before, double After, double Milliseconds) ShelfGlide(BrowserLayout layout)
    {
      return layout switch
      {
        BrowserLayout.Cover => (280.0, 260.0, 400.0),
        BrowserLayout.List => (130.0, 130.0, 300.0),
        _ => (400.0, 400.0, 400.0)
      };
    }

    /// <summary>
    /// Dresses the shelf's scroller in the glide permanently, at the length the shape walks its wall.  The
    /// reference never takes its scroll animation off either: its AnimatedWrapPanel lerps EVERY offset change
    /// through the skin's storyboards (<c>MediaStyles.xaml:5205</c>, <c>AnimatedWrapPanel.cs</c>), a stepped
    /// caret and a mouse wheel notch alike, which is why its wall never jumps a row under the hand.  A previous
    /// attempt here wore the glide only for the length of one travel - that made the wheel answer in hard row
    /// jumps, a roughness the reference does not have, and the viewer's verdict put the smoothness above the
    /// crispness of a bare scroller.
    /// </summary>
    private void InstallShelfGlide(BrowserLayout layout)
    {
      (_, _, double milliseconds) = ShelfGlide(layout);
      _glideLength = TimeSpan.FromMilliseconds(milliseconds);
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        // A retemplated list brings a bare scroller; the glide goes onto whichever one stands there now.
        _glidingScroller = scroller;
        FitShelfGlide(scroller);
      }, DispatcherPriority.Loaded);
    }

    /// <summary>Puts the glide on the scroller, at the length currently asked for, reusing the running one.</summary>
    private void FitShelfGlide(ScrollViewer scroller)
    {
      if (ReferenceEquals(scroller.Transitions, _wornGlide) && _wornGlideLength == _glideLength)
        return;

      Transitions glide = new Transitions();
      glide.Add(new VectorTransition
      {
        Property = ScrollViewer.OffsetProperty,
        Duration = _glideLength
      });
      _wornGlide = glide;
      _wornGlideLength = _glideLength;
      scroller.Transitions = glide;
    }

    /// <summary>
    /// One travel of the wall: since the glide is worn permanently (see <see cref="InstallShelfGlide"/>), this
    /// only makes sure a scroller that arrived without one - or with one of another length, after a shape
    /// change - wears the right dress before the offset is written.  A travel that meets a running animation is
    /// handed to it by the transition itself, which retargets rather than restarting.
    /// </summary>
    private void WearShelfGlide(ScrollViewer scroller)
    {
      _glidingScroller = scroller;
      FitShelfGlide(scroller);
    }

    private TimeSpan _glideLength = TimeSpan.FromMilliseconds(400);

    private ScrollViewer? _glidingScroller;

    private Transitions? _wornGlide;

    /// <summary>The container index a thrown-away travel waits for; -1 when nobody is waiting.</summary>
    private int _glideAwaitContainer = -1;

    private TimeSpan _wornGlideLength = TimeSpan.Zero;

    /// <summary>
    /// Stands the shelf so that the title being stood on sits inside the band the shape was given, and lets the
    /// scroller's transition carry it there.
    /// </summary>
    /// <remarks>
    /// The band, not the middle: a title that is already inside it is left alone, which is what keeps a shelf of
    /// rows still for the first few steps of a walk and starts it moving once the standing row reaches the band's
    /// edge - the reference's own behaviour, and the reason its shelves never crawled row by row. Where a shape's
    /// band cannot hold its own tile (the grid's four hundred points either side of a shelf that is a thousand
    /// tall), the tile is stood at the band's middle instead, so that the wall answers every step with the same
    /// glide rather than with the two branches pulling against each other.
    /// </remarks>
    private void GlideShelfToStoodTitle(int tries = 2)
    {
      if (!(DataContext is MediaBrowserViewModel viewModel))
        return;

      ScrollViewer? scroller = ListScroller();
      Control? stood = titlesList.SelectedIndex >= 0
          ? titlesList.ContainerFromIndex(titlesList.SelectedIndex) as Control
          : null;

      if (scroller == null || stood == null)
      {
        // A container that is not realized yet has no place to stand at, and the shelf is asked to move before its
        // tiles have been laid more often than not - the caret arrives with the titles, and so does this question.
        if (tries > 0)
        {
          Dispatcher.UIThread.Post(delegate { GlideShelfToStoodTitle(tries - 1); }, DispatcherPriority.Loaded);
        }
        else if (stood == null && titlesList.SelectedIndex >= 0)
        {
          // The retries ran out with the tile still unrealized.  Rather than drop the travel, the shelf remembers
          // which container it is waiting for and takes the glide up the moment that container is prepared.
          _glideAwaitContainer = titlesList.SelectedIndex;
        }

        return;
      }

      (double before, double after, _) = ShelfGlide(viewModel.Layout);
      bool sideways = viewModel.Layout == BrowserLayout.Cover;
      Point? at = stood.TranslatePoint(new Point(), scroller);
      if (at == null)
        return;

      double view = sideways ? scroller.Viewport.Width : scroller.Viewport.Height;
      double extent = sideways ? scroller.Extent.Width : scroller.Extent.Height;
      double start = sideways ? at.Value.X : at.Value.Y;
      double end = start + (sideways ? stood.Bounds.Width : stood.Bounds.Height);
      // The band, and the middle of the viewport belongs to it: a shelf of a window smaller than the reference's
      // television cannot hold four hundred points at either end of a tile, and a band that then hung its tile
      // below the bottom of a small window would be a wall that walks its stood title off the screen.
      double low = Math.Min(before, view / 2);
      double high = Math.Max(low, Math.Max(view - after, view / 2));
      double wanted = sideways ? scroller.Offset.X : scroller.Offset.Y;

      if (end - start > high - low)
      {
        // The band cannot hold the tile, so the tile is stood at the band's middle. The sign is the offset's own:
        // asking for more offset moves the wall up, so a tile standing further down than the middle has to be
        // brought up by asking for MORE and not less.
        wanted += (start + end) / 2 - (low + high) / 2;
      }
      else if (start < low)
      {
        wanted -= low - start;
      }
      else if (end > high)
      {
        wanted += end - high;
      }

      wanted = Math.Clamp(wanted, 0, Math.Max(0, extent - view));
      double standing_now = sideways ? scroller.Offset.X : scroller.Offset.Y;
      _shelfGlide = wanted;
      if (Math.Abs(wanted - standing_now) < 1.0)
      {
        // The stood title already stands inside its band: there is no travel to glide and the offset is not
        // written.  The glide stays on the scroller - it is worn permanently - but with nothing written there
        // is nothing for it to animate, and a wheel notch of the viewer's own is still its own business.
        return;
      }

      WearShelfGlide(scroller);
      scroller.Offset = sideways
          ? new Vector(wanted, scroller.Offset.Y)
          : new Vector(scroller.Offset.X, wanted);
    }

    /// <summary>
    /// Where along its travel axis the shelf was last asked to stand. What the scroller itself calls its offset is
    /// the place the glide has got to, which while a wall is travelling is nowhere near the place it was asked
    /// for; a test and a render probe both want the answer to "where is the wall being taken to".
    /// </summary>
    internal double ShelfGlideOffset => _shelfGlide;

    private double _shelfGlide;

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

