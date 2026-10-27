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
        _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
        _chromeShell?.SetChromeHeaderOverlay(true);
      };
      DetachedFromVisualTree += delegate
      {
        SkinEngine.Changed -= OnSkinChanged;
        _chromeShell?.SetChromeHeaderOverlay(false);
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
        if (browser.Titles.Count == 0 || !NeedsCaretOnTheShelf())
          return;

        Dispatcher.UIThread.Post(delegate
        {
          if (NeedsCaretOnTheShelf())
            FocusShelf();
        }, DispatcherPriority.Loaded);
      };

      // The shelf answers the standing title with a glide rather than a jump: every step of the caret moves the
      // wall by the shape's own numbers, over the shape's own length of time.
      titlesList.SelectionChanged += delegate { GlideShelfToStoodTitle(); };

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
          SlideMenu(browser.MediaMenuVisible, browser.IsCoverLayout, false);
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
              // The "info" key of a remote, which reaches this code as Apps, and the letter for a keyboard. The
              // reference bound exactly this key to the media screen's own menu (master_mediamenu.xaml:400) - the
              // menu that holds the commands a shelf answers: finding, filtering, sorting, the view, the details
              // and the title's own menu - and bound it to closing it again when the menu is already out.
              viewModel.ToggleMediaMenuCommand.Execute(null);
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
              titlesList.Focus();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key))
            {
              // Back means "away with the newest thing on the screen", which is the title's menu when one is
              // standing open, then the search when there is one, then the watched filter, then the details
              // the screen was wearing. A viewer who pressed back three times expects to be back where they
              // started, not outside the screen.
              if (viewModel.MediaMenuVisible)
              {
                // Back puts the menu's current page away first, and the menu itself only when its commands page is
                // what stands there - which is the reference's own answer, its Escape binding being enabled solely
                // while the menu was open (master_mediamenu.xaml:401).
                viewModel.MediaMenuBackCommand.Execute(null);
                if (!viewModel.MediaMenuVisible)
                  titlesList.Focus();
                args.Handled = true;
                return;
              }
              if (viewModel.MenuVisible)
              {
                viewModel.CloseMenu();
                titlesList.Focus();
                args.Handled = true;
                return;
              }
              if (viewModel.SearchOpen && !viewModel.HasSearch && searchBox.IsFocused)
              {
                // The field was put on the screen and nothing was typed into it, so back takes the field away
                // again and gives the arrows back to the shelf. A field holding a search is a different thing and
                // is answered below.
                viewModel.SearchOpen = false;
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
              viewModel.HideMediaMenuCommand.Execute(null);
              titlesList.Focus();
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
        FocusShelf();
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
      // Posted, because the list has only just been made visible by the same answer and is not measurable yet.
      Dispatcher.UIThread.Post(delegate
      {
        list.Focus();
        if (list.Items.Count > 0)
          list.SelectedIndex = 0;
      }, DispatcherPriority.Loaded);
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
        if (args.Key != Key.Enter && args.Key != Key.Return && args.Key != Key.Space)
          return;

        if (list.SelectedItem is MediaBrowserMenuEntry row)
        {
          RunMenuRow(row);
          args.Handled = true;
        }
      }, RoutingStrategies.Bubble);

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
            RunMenuRow(row);
            args.Handled = true;
          }
        }
      }, RoutingStrategies.Tunnel);
    }

    /// <summary>Presses the command a menu row carries, when it has one that can be pressed.</summary>
    private void RunMenuRow(MediaBrowserMenuEntry row)
    {
      if (row.Command.CanExecute(null))
        row.Command.Execute(null);
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

      int standing = titlesList.SelectedIndex < 0 ? 0 : titlesList.SelectedIndex;

      if (titlesList.ContainerFromIndex(standing) is ListBoxItem container && container.IsEffectivelyVisible)
      {
        container.Focus();
        // The selection may well have stood here the whole time, in which case no SelectionChanged ever answered
        // the visit - the wall is stood at the caret by the caret itself, or it stays at the edge it came in at.
        GlideShelfToStoodTitle();
        return;
      }

      // The tiles are laid a pass after the screen is first measured, so the container the caret belongs in does
      // not exist yet at this moment. The list takes the caret meanwhile - which is not nothing, since the first
      // arrow key then moves the caret inside it - and the tile is asked for again once it is measurable.
      titlesList.Focus();
      Dispatcher.UIThread.Post(delegate
      {
        if (titlesList.ContainerFromIndex(standing) is ListBoxItem again
            && again.IsEffectivelyVisible && !again.IsFocused)
          again.Focus();
      }, DispatcherPriority.Loaded);
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
          return focused == titlesList;
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
        ScrollViewer.SetHorizontalScrollBarVisibility(titlesList, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(titlesList, ScrollBarVisibility.Disabled);
      }
      else
      {
        titlesList.ClearValue(Layoutable.VerticalAlignmentProperty);
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
    /// Takes any mask off the scroller the shelf is read through, so that the shelf's rows reach the screen's edges
    /// whole.
    /// </summary>
    /// <remarks>
    /// Kept as a call site because the scroller only exists once the list has been measured, so the answer has to
    /// be given again on a later pass; and because a skin that ever did want its rows to pale at the ends would
    /// want it here, as an overlay over the shelf rather than as a hole in it.
    /// </remarks>
    private void ApplyShelfFade(BrowserLayout layout)
    {
      // The scroller lives inside the list's template and only exists once the list has been measured, so the
      // answer is asked for again on the next pass rather than given up on.
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        // Rows dissolve at the ends rather than being cut there, which is what the home wall and the reference's
        // own menu column both do (styles.xaml gives the menu's ScrollViewer its fade over the top and bottom
        // twelve percent for exactly that reason).  Taken off, the shelf did not show its picture to the edges -
        // it showed a hard line across half a poster at the head and another at the foot, which is the "black
        // bar" the shelf has been blamed for.
        //
        // What it must never do is pale the title being stood on.  The band the shelf is kept inside (130 points
        // for rows, 400 for the grid, the reference's own ScrollMargin) already keeps a stood title out of a
        // twelve percent strip in the middle of a wall; the one place it cannot is where the wall has run out -
        // at its head the first row sits at the very top, inside the strip.  So the strip is put away while the
        // wall is at the end it would fade, and only there.  That is two answers per wall, not one per step: the
        // mask is only ever rewritten when the wall arrives at an end or leaves it, never while it travels.
        if (_shelfFade == null)
        {
          LinearGradientBrush fade = new LinearGradientBrush
          {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
          };
          fade.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
          fade.GradientStops.Add(new GradientStop(Colors.Black, SHELF_EDGE_FADE));
          fade.GradientStops.Add(new GradientStop(Colors.Black, 1.0 - SHELF_EDGE_FADE));
          fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
          _shelfFade = fade;
        }

        scroller.OpacityMask = _shelfFade;
      }, DispatcherPriority.Loaded);
    }

    /// <summary>How far in from an end of the shelf its rows dissolve: the twelve percent the menu column wears.</summary>
    private const double SHELF_EDGE_FADE = 0.12;

    private LinearGradientBrush? _shelfFade;

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
    /// Puts the reference's scroll animation on the scroller the shelf is read through: a transition on the offset
    /// the shelf moves by, of the length the shape's own storyboard had. This is the same mechanism the home wall
    /// glides on (HomeView's <c>nereusContentPane</c>), and for the same reason - a shelf whose offset is written
    /// outright walks one row per key press and stops dead at every one of them, which is what a viewer of a
    /// television calls a stutter.
    /// </summary>
    private void InstallShelfGlide(BrowserLayout layout)
    {
      (_, _, double milliseconds) = ShelfGlide(layout);
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        Transitions glide = new Transitions();
        glide.Add(new VectorTransition
        {
          Property = ScrollViewer.OffsetProperty,
          Duration = TimeSpan.FromMilliseconds(milliseconds)
        });
        scroller.Transitions = glide;
      }, DispatcherPriority.Loaded);
    }

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
      scroller.Offset = sideways
          ? new Vector(wanted, scroller.Offset.Y)
          : new Vector(scroller.Offset.X, wanted);
      _shelfGlide = wanted;
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
