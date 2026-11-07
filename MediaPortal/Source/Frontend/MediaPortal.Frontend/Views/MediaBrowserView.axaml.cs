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
using System.Runtime.CompilerServices;
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
    /// Whether the caret is still owed its place in a reading whose bands were empty when it came up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reading says it is up before its bands are on the screen - either because the server has not answered what
    /// the file holds, or because the reading's own ground has not been laid out yet, which happens every time it
    /// comes up.  So the ask that stands the caret at the head of the first band usually finds no card to stand on,
    /// and, the reading having kept no button of its own at its foot, it then finds nothing at all.
    /// </para>
    /// <para>
    /// That is the right answer for a moment - a caret sent looking for a card that is not drawn is a caret lost off
    /// the screen - but it must not stay the answer: in that first half second every arrow the viewer presses meets
    /// a screen that answers nobody.  The promise here therefore asks again over the layout rounds and again when the
    /// bands say they have something to show, and it stops the moment the caret stands on a card.
    /// </para>
    /// </remarks>
    private bool _caretWaitsForTheBands;

    private TitleViewModel? _layoutStoodOn;

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

      // Measuring switch: MP_NOGLASS=1 takes the command rail - the seven liquid-glass lenses - out of the screen.
      // It exists because a thing that cannot be seen can still be paid for: a lens is not a picture that is
      // skipped when it stands outside the window, it is a shader over the ground behind it, and whether the
      // renderer runs that shader for a rail nobody looks at is a question for the frame ledger, not for opinion.
      // With the rail away and the steps growing cheap, the glass was being paid for while invisible; with the
      // steps staying as expensive as they were, the glass is innocent and can be left alone.
      if (Environment.GetEnvironmentVariable("MP_NOGLASS") == "1")
      {
        foreach (Visual child in this.GetVisualDescendants())
        {
          if (child is Control rail && rail.Classes.Contains("media-menu"))
            rail.IsVisible = false;
        }
      }
      _shelfFloor = titlesList.Margin;

      // The drawn cover row wants its posters from its first frame on - whether the viewer walked into the shape or
      // the screen opened in it.  Wiring this only when the shape is switched left every cover but the one the
      // invisible list happened to paint without a picture.
      coverRow.PictureAsked ??= AskForCoverPicture;

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
          {
            SlideMenu(browser.MediaMenuVisible, browser.IsCoverLayout, true);
            // ...and the pointer's handle to it changes side with it: covers open their menu off the bottom of the
            // room and draw their own mark there, so the row of marks at the window's head gets none.
            PushMenuMark();
          }
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

        // And the pointer's handle to this screen's menu, which the window holds in its row of marks for as long
        // as this screen is the one being looked at.
        PushMenuMark();
      };
      DetachedFromVisualTree += delegate
      {
        SkinEngine.Changed -= OnSkinChanged;

        // An appointment kept for a screen that is being left would be kept for nobody: the panels go out of the
        // tree with the screen anyway, and nothing behind it should find this screen's menu in front of it.
        _menuPanelTimer?.Stop();

        _chromeShell?.SetChromeHeaderOverlay(false);
        _chromeShell?.SetChromeHeaderStowed(false);
        _chromeShell?.SetCornerMenuMark(null, null);
        _chromeShell = null;
      };
      ApplyLayout();

      // Neither menu lies where a viewer can see it until he asks for it, so neither is hung in the tree at the
      // start: the rows are built when the menu is opened (the view model fills them then) and are taken down
      // again with the panel when the room has slid back over them. See SlideMenu, which owns both halves.
      LayTheMenuPanelsDown();

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

      // The far keys belong to the drawn row while this shape is worn.  The list keeps the focus here - the
      // menus, the details and the keys all walk in through it - but its own answer to ENDE and POS1 is to carry
      // the far container into view, and that answer is a walk: on a shelf of a thousand titles it builds every
      // container between the two places, measured as eleven hundred containers and two seconds of one frame.
      // The painted row stands on no container, so it is asked instead, and its answer is a number.
      AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        if (args.Handled
            || DataContext is not MediaBrowserViewModel shaped
            || shaped.Layout != BrowserLayout.Carousel)
          return;

        switch (args.Key)
        {
          case Key.End:
            args.Handled = true;
            coverRow.StandAtEitherEnd(true);
            break;

          case Key.Home:
            args.Handled = true;
            coverRow.StandAtEitherEnd(false);
            break;
        }
      }, RoutingStrategies.Tunnel);

      // The shelf answers the standing title with a glide rather than a jump: every step of the caret moves the
      // wall by the shape's own numbers, over the shape's own length of time. And every step re-arms the quiet
      // watching, because the walk itself - not only the landing - is when the recycling can knock the tile
      // under the caret away: a watchdog retired after the last still-standing breath has to be woken by the
      // next step, or a fast walk drops its caret between two tiles and nobody picks it up.
      titlesList.SelectionChanged += delegate
      {
        // A group plate is not a title and no caret stands on it.  The containers disarm the plates themselves
        // (see ContainerPrepared), so reaching this line with a plate selected means something got past that -
        // a click through a wall mid-rebuild, or a step taken over a container that had not been disarmed yet.
        // The answer is the same either way: the caret goes back to the title it last stood on, and the plate
        // keeps its place in the row and says its word from there.
        if (titlesList.SelectedItem is GroupHeaderRow)
        {
          // Belt and braces beside the disarmed containers: a revert that is refused and echoed back must not
          // start the echo again, or the caret list and the drawn row would hand the plate to each other until
          // the stack dies - which is exactly how the frontend used to die on opening a grouped wall: the wall
          // selected plate zero, this revert said -1, the drawn row clamps -1 to its first cover, the binding
          // handed plate zero back, and the "no" came again. So the revert never says -1 while a title stands
          // anywhere within reach: it says the title nearest the plate, which the drawn row accepts as-is.
          if (_plate_revert_running)
            return;

          _plate_revert_running = true;
          try
          {
            int stood_on = titlesList.SelectedIndex;
            int goes_to = _lastTileStand == null
                ? NearestTitleRow(stood_on)
                : titlesList.Items.IndexOf(_lastTileStand);
            if (goes_to < 0)
              goes_to = NearestTitleRow(stood_on);

            if (goes_to != stood_on)
              titlesList.SelectedIndex = goes_to;
          }
          finally
          {
            _plate_revert_running = false;
          }

          return;
        }

        Services.HomeTrace.Log("selchg.begin", "index=" + titlesList.SelectedIndex
            + " realized=" + (titlesList.GetRealizedContainers()?.Count() ?? -1)
            + " asc=" + titlesList.AutoScrollToSelectedItem);

        // A caret step is deliberate motion; container preparation and remeasurement must settle directly or
        // they would keep retargeting the same animation while the virtualized wall is being rebuilt.
        _animate_shelf_shape_targets = true;
        try
        {
          GlideShelfToStoodTitle();
        }
        finally
        {
          _animate_shelf_shape_targets = false;
        }

        // The list answers its own selection too, by bringing the tile into view - and that answer is the smallest
        // nudge that shows the tile, not the middle of the picture a ring is built around.  Landing after the
        // glide above, it leaves the stood cover standing beside the middle, which is what the viewer reads as a
        // row that is bigger on one side than on the other.  Once that nudge has landed the wall is put back where
        // the shape asked for it; asked without the wait, this would be answered before the nudge and change
        // nothing.
        int stood_on_index = titlesList.SelectedIndex;
        WatchShelfWallHome(stood_on_index);

        WatchShelfCaret();

        // Nachladen, weil der Zeiger an das Ende des Gelesenen kommt - und nicht, weil ein Scrollversatz eine
        // Zahl berührt.  In der gemalten Form rührt der Versatz sich nicht, wenn der Betrachter läuft oder
        // springt: die Liste sieht man nicht, und ihr Zeiger wird nicht ins Bild getragen.  Ein Regal, das
        // früher endete als das Regal des Servers, wäre dann für den stehen geblieben, der ganz hinten steht.
        // Gefragt wird, wer dem letzten gelesenen Titel näher steht als eine Bildschirmvoll voraus.
        if (stood_on_index >= 0 && browser.HasMore
            && stood_on_index >= browser.Rows.Count - READ_AHEAD_TITLES)
        {
          Services.HomeTrace.Log("read.step", "index=" + stood_on_index + " titles=" + browser.Titles.Count);
          _ = browser.KeepReadingAsync();
        }

        Services.HomeTrace.Log("selchg.end", "index=" + stood_on_index
            + " realized=" + (titlesList.GetRealizedContainers()?.Count() ?? -1));
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
        // The layers this screen can raise over its wall answer the fade too: a hidden wall wears no mask
        // (see UpdateShelfFade), and it is these flags that say when the wall is hidden and when it is not.
        if (args.PropertyName == nameof(MediaBrowserViewModel.DetailsVisible)
            || args.PropertyName == nameof(MediaBrowserViewModel.SearchOpen)
            || args.PropertyName == nameof(MediaBrowserViewModel.SearchText)
            || args.PropertyName == nameof(MediaBrowserViewModel.Layout))
          UpdateShelfFade();

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

          // The handle in the window's row of marks pictures the move the next click makes, which is the other
          // move from the one the click just made.
          PushMenuMark();
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
      SizeChanged += delegate
      {
        ApplyColumns();
        // A window that changed size changed the middle of its picture, which is what both shapes hold their stood
        // cover at and what the shift of their whole row is measured from.
        WearShelfShape(true);
      };
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

      // The bands of cards under a title's picture hold more cards than fit the screen, and the caret
      // stands on a CARD - the question a viewer asks is WHICH track, and a ring around five cards answers
      // nothing.  A press lands on the card by itself (a focusable control takes the focus it is given), the
      // arrows walk from card to card, and the band slides along with the caret because that is what a scroller
      // does when the thing it holds walks out of sight.
      // The wheel over a band belongs to the band, but only while the band holds the caret: a viewer whose
      // pointer happens to lie over a band must still be able to scroll the reading of a title as a whole.
      foreach (ScrollViewer band in new[]
          {
              audioTracksScroller, subtitleTracksScroller, actorsScroller, directorsScroller,
              artistsScroller, albumArtistsScroller, composersScroller, collectionScroller
          })
      {
        // A band glides to where its caret goes instead of jumping there.  The reference lerps EVERY offset
        // change of its rows through the skin's storyboards - three hundred milliseconds with a ScrollMargin of a
        // hundred and thirty points at either end (MediaStyles.xaml:5205, driven by AnimatedWrapPanel.cs) - which
        // is why its wall never snaps a row past the eye, whether the offset moved because a caret walked or
        // because a hand turned a wheel.  The browser's own shelf wears the same dress (InstallShelfGlide).
        Transitions glide = new Transitions();
        glide.Add(new VectorTransition { Property = ScrollViewer.OffsetProperty, Duration = BAND_GLIDE });
        band.Transitions = glide;

        // Whichever way a card comes by the caret - an arrow, a press, a Tab from another control - the band
        // keeps it in sight with the reference's margin around it rather than flush against an end.
        band.AddHandler(GotFocusEvent,
            delegate (object? sender, RoutedEventArgs args)
            {
              if (args.Source is Control card && !ReferenceEquals(card, band))
                KeepCardInSight(band, card);
            }, RoutingStrategies.Bubble);

        band.AddHandler(PointerWheelChangedEvent,
            delegate (object? sender, PointerWheelEventArgs args)
            {
              if (Math.Abs(args.Delta.Y) < Math.Abs(args.Delta.X)
                  || !band.IsKeyboardFocusWithin)
                return;

              double amount = band.Viewport.Width * 0.6;
              band.Offset = new Vector(band.Offset.X - Math.Sign(args.Delta.Y) * amount, 0);

              // The wall now stands where the viewer put it, not where the last step asked for it.  Told so, the
              // next step writes its travel again instead of believing the wall is still on its way there.
              _shelfGlideIndex = -1;
              args.Handled = true;
            }, RoutingStrategies.Tunnel);

        // A band's cards walk out of its ends, and an end they walked out of dissolves the way the shelf's wall
        // dissolves at the end its titles went out by - the viewer named that dress for the media lists, and a
        // band of this screen is a media list of this screen (see FadeBandEnds).
        FadeBandEnds(band);
      }

      // The walking of the cards is asked on the way IN, before the scroller around them can spend an arrow on
      // its own offset: the caret moves first, and the band follows it.  The keys are asked literally rather than
      // through Settings.KeyBindings.Left/Right, because those carry the remote's navigation keys as well and a
      // band that ate them would be a wall that cannot be walked any more.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (WalksTrackCards(args))
              args.Handled = true;
          }, RoutingStrategies.Tunnel);

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
        ++_shelf_prepared;
        ScheduleRowPainting();

        // Measuring switch: MP_NOSHADOW=1 takes the ground shadow off every tile the shelf realises, once, as it
        // arrives.  A BoxShadow is rasterised together with its blur, so a wall that stands sixty tiles in a
        // shape carries sixty blurs with it wherever it goes - which is the kind of cost that has to be counted
        // before anybody is allowed to call it cheap or dear.
        Control prepared = e.Container;

        // Plates disarm the containers they wear: no caret walks onto a divider, nobody can click one, and no
        // tab stop stands in the row.  The answer is written on EVERY prepare because containers are recycled -
        // the shell that wore a plate a moment ago may wear a title now, and it has to be able to take the
        // caret again then.
        if (prepared is ListBoxItem prepared_item)
        {
          bool wears_plate = prepared.DataContext is GroupHeaderRow;
          prepared_item.Focusable = !wears_plate;
          prepared_item.IsTabStop = !wears_plate;
          prepared_item.IsHitTestVisible = !wears_plate;
        }

        if (NoShadowsWorn)
          TakeShadowsFrom(prepared);

        // A recycled container can still carry the previous title's side, scale and opacity. Clear that state
        // before its new index is available; the deferred row pass below dresses it from the correct index.
        if (DataContext is MediaBrowserViewModel viewModel
            && viewModel.Layout is BrowserLayout.Stacked or BrowserLayout.Carousel)
          BareTile(prepared, viewModel.Layout != BrowserLayout.Carousel);

        // A list which is looked at through a shape must not scroll itself, because the wall travels to where the
        // shape puts it - see KeepTileWhereTheWallStands.
        KeepTileWhereTheWallStands(prepared);

        // A travel asked for while the stood tile was not laid out yet waits for exactly this container; when it
        // arrives, the question that was thrown away is asked again, and the wall glides instead of leaving the
        // selection's own jump as the only movement the viewer sees.
        if (_glideAwaitTitle != null && ReferenceEquals(prepared.DataContext, _glideAwaitTitle))
        {
          _glideAwaitTitle = null;
          Dispatcher.UIThread.Post(delegate { GlideShelfToStoodTitle(0); }, DispatcherPriority.Background);
        }

        // The panel has not finished assigning this container its item/index yet. Defer one whole-row pass and
        // coalesce the posts: a single layout batch can prepare many containers, and each pass visits them all.
        // This is independent of whether the same container also completed a pending glide request.
        ScheduleShelfShape();
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
        bool sideways = browser.IsCoverLayout;
        double short_of_end = sideways
            ? scroller.Extent.Width - (scroller.Offset.X + scroller.Viewport.Width)
            : scroller.Extent.Height - (scroller.Offset.Y + scroller.Viewport.Height);
        double viewport = sideways ? scroller.Viewport.Width : scroller.Viewport.Height;
        if (short_of_end <= viewport)
        {
          Services.HomeTrace.Log("read.ask",
              string.Format(System.Globalization.CultureInfo.InvariantCulture,
                  "short={0:F0} viewport={1:F0} titles={2}", short_of_end, viewport, browser.Titles.Count));
          _ = browser.KeepReadingAsync();
        }
      });

      // The wall answers the keys that point past its edges with a nod rather than a step. A grid walks as the
      // two-dimensional wall it is: Right at the last column, Up at the top row and Down below the last row are
      // walls, and the wall leans against them and springs back. A list only ever has a first row and a last one.
      // And the sideways shapes - covers, stacked, carousel - lay their beginning at the left hand and their end
      // at the right, so THEY nod to Left at the first title, Up at the top-side of that beginning and Right at
      // the last; their Down stays the key that opens the menu under the screen (see the shelf's own menu
      // handler), which is why the floor is not one of their walls. Changing rows is Up and Down's business in a
      // grid, not Right's.
      //
      // Heard on the shelf's ground - the panel behind the list - because a tunnel handler on the control a key
      // arrives at is never heard there, and the wall's ground lies between the room and every key the wall
      // ever receives, whichever of its tiles wears the caret. The nod shakes the face the viewer can see:
      // under the carousel that is the painted row, for the list there wears nothing but the caret.
      Control wall_ground = (Control?)titlesList.Parent ?? titlesList;
      wall_ground.AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        if (args.Handled || titlesList.Items.Count == 0)
          return;

        if (DataContext is not MediaBrowserViewModel wall_owner)
          return;

        if (wall_owner.Layout != BrowserLayout.Grid && wall_owner.Layout != BrowserLayout.List
            && !wall_owner.IsCoverLayout)
          return;

        int stood = titlesList.SelectedIndex;
        if (stood < 0)
          return;

        bool sideways = wall_owner.IsCoverLayout;
        int columns = sideways || wall_owner.Layout == BrowserLayout.List
            ? 1
            : (titlesList.ItemsPanelRoot as VirtualizingTilePanel)?.Columns ?? 1;
        Control face = wall_owner.Layout == BrowserLayout.Carousel ? (Control)coverRow : titlesList;

        double diveX = 0;
        double diveY = 0;
        bool at_a_wall = false;

        switch (args.Key)
        {
          case Key.Right when sideways:
            at_a_wall = stood == titlesList.Items.Count - 1;
            diveX = RAIL_BOUNCE_DIVE;
            break;
          case Key.Left when sideways:
            at_a_wall = stood == 0;
            diveX = -RAIL_BOUNCE_DIVE;
            break;
          //  Up at the beginning of a sideways row is the top-side wall of that row.  (When the menu under the
          //  screen is UP, Up is the key that steps back onto the shelf - that handler speaks first, and this
          //  one only ever hears the key while the shelf itself wears the focus.)
          case Key.Up when sideways:
            at_a_wall = stood == 0;
            diveY = -RAIL_BOUNCE_DIVE;
            break;
          case Key.Right when wall_owner.Layout == BrowserLayout.Grid:
            at_a_wall = stood % columns == columns - 1 || stood == titlesList.Items.Count - 1;
            diveX = RAIL_BOUNCE_DIVE;
            break;
          //  A list is one column wide, so the right hand is a wall from every row.  (While the media menu
          //  stands open, Right is the key that steps back onto the shelf - that handler speaks first.)
          case Key.Right when wall_owner.Layout == BrowserLayout.List:
            at_a_wall = true;
            diveX = RAIL_BOUNCE_DIVE;
            break;
          case Key.Up when !sideways:
            at_a_wall = stood < columns;
            diveY = -RAIL_BOUNCE_DIVE;
            break;
          case Key.Down when !sideways:
            at_a_wall = stood + columns >= titlesList.Items.Count;
            diveY = RAIL_BOUNCE_DIVE;
            break;
          default:
            return;
        }

        if (!at_a_wall)
          return;

        BounceRail(face, diveX, diveY);
        args.Handled = true;
      }, RoutingStrategies.Tunnel);

      // OK on the remote is Enter here, and Avalonia hands it to the focused control before anyone else; the
      // list does nothing with it, so it is caught on the way in and turns into "play this", which is what a
      // viewer means by it on a screen whose whole content is a list of things to watch.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!(DataContext is MediaBrowserViewModel viewModel))
              return;

            if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Menu, args))
            {
              // The "Menu" key of a remote - F16 by default, whatever a viewer's remote really reports is read
              // from keybindings.json. The apps key is not the default here because this screen already wears
              // it for the details, and one key cannot answer two questions. It opens the media list menu of
              // the reference for the selected row.
              viewModel.ShowMenuCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args)
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
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Details, args)
                && !searchBox.IsFocused)
            {
              // The details question, which the reference kept behind its view mode dialog and this screen answers
              // from its menu's view page as well; the letter is the shortcut for a keyboard, and it asks the same
              // question the menu row asks.
              viewModel.ToggleDetailsCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Find, args) && !searchBox.IsFocused)
            {
              // Finding is the one thing on this screen that needs the keyboard, so it is the one key that moves
              // the focus out of the list - and Enter or Escape brings it straight back, because the next thing a
              // viewer does with a result is walk through it with the arrows. The field itself is only on the
              // screen while somebody is searching, so this key both puts it on the screen and hands it the caret.
              viewModel.OpenSearch();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Watched, args) && !searchBox.IsFocused)
            {
              // W for watched: everything, then what nobody has finished, then what somebody has. One key round
              // three states rather than two switches, because a remote reaches its keyboard through one button
              // and a viewer should be able to get back to "everything" without remembering which switch it was.
              viewModel.CycleSeenCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.View, args) && !searchBox.IsFocused)
            {
              // V for view: rows or the skin's tiles. What shape the tiles take is not this key's business - the
              // Media button asks that - so the key only has the one question left for the viewer.
              viewModel.SwitchViewCommand.Execute(null);
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Media, args) && !searchBox.IsFocused)
            {
              // M for media: the picture the tiles wear, poster to banner to thumbnail. The same question the
              // button beside the view button asks, reachable from the sofa.
              viewModel.SwitchMediaCommand.Execute(null);
              args.Handled = true;
            }
            else if (searchBox.IsFocused && (args.Key == Key.Enter || args.Key == Key.Return))
            {
              // Enter means different things depending on where the caret is: "play this" on a row, "search for
              // this" in the box. The box gets asked first, or typing a title would start a film - and the box is
              // asked about the enter key ALONE, never about the OK action, because the space bar is the letter
              // the viewer is still spelling with and Android's D-pad centre reports itself as exactly that.
              viewModel.FindCommand.Execute(null);
              // The caret goes onto a TILE, not merely into the list: a caret that gets no further than the list
              // wears no frame, and the viewer who just spelled a title wants to stand on a result.
              FocusShelf();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args))
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
              if (viewModel.InfoShown)
              {
                // The reading of a title is the newest thing on the screen and goes first, and the caret comes back
                // with the wall - which the InfoShown handler below does, so nothing is asked of the list here.
                viewModel.CloseInfo();
                args.Handled = true;
                return;
              }
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
            else if ((Settings.KeyBindings.Current.Is(Settings.KeyAction.Select, args)
                    || Settings.KeyBindings.Current.Is(Settings.KeyAction.PlayPause, args))
                && !viewModel.MenuVisible && !viewModel.MediaMenuVisible && !viewModel.InfoShown
                && !searchBox.IsFocused)
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
      // menu UNDER the screen, so their key points DOWN: lifting the caret off the row upward to reach a menu
      // lying below it is a key that has to be memorised, which is what the viewer called irritating. The way
      // back is the opposite key, because a menu a viewer cannot leave is a second screen they never asked for.
      // And pointing down again from the menu is pointing at the floor behind it, so the menu is nudged into the
      // floor and springs back - the same answer this front end gives every other wall.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Handled || searchBox.IsFocused || !(DataContext is MediaBrowserViewModel viewModel))
              return;

            bool sideways = viewModel.IsCoverLayout;
            Key off_the_shelf = sideways ? Key.Down : Key.Left;
            Key back_to_the_shelf = sideways ? Key.Up : Key.Right;
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

      // The reading of one title takes the caret with it and brings it back with the wall. It has to: the shelf is
      // off the screen while the reading stands, so a caret left behind on its list is standing on nothing, and
      // the viewer who finished reading would find the arrows answering nobody.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName != nameof(MediaBrowserViewModel.InfoShown))
          return;

        if (browser.InfoShown)
        {
          // The reading is on the screen a moment BEFORE its bands are - the band's own visibility is a binding
          // that has to reach the layout, and a card the layout never grew cannot take a caret.  So the caret is
          // not asked for once here but over the layout rounds, which is the same promise that waits for a server
          // that answers late.  Asking once was the mistake the viewer caught on 2026-11-03: for a title whose
          // answers the screen was holding already, nothing announced itself a second time, and the caret stood
          // nowhere at all - every arrow met a screen that answered nobody.
          _caretWaitsForTheBands = true;
          SeatTheCaretOnTheFirstCardOfTheReading(browser, CARET_SEATING_ROUNDS);
        }
        else
        {
          _caretWaitsForTheBands = false;
          FocusShelf();
        }
      };

      // What the bands answer, the caret is asked for a second time.  The reading stood itself up over empty
      // lists, so its first ask found no card to stand on; as soon as there is one, a caret the viewer never
      // moved walks onto the card at the head of the first band, which is where the reading meant to take it.
      browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (!_caretWaitsForTheBands || !browser.InfoShown)
          return;
        if (args.PropertyName != nameof(MediaBrowserViewModel.ShowsTracks)
            && args.PropertyName != nameof(MediaBrowserViewModel.ShowsPersons)
            && args.PropertyName != nameof(MediaBrowserViewModel.ShowsCollection))
          return;

        SeatTheCaretOnTheFirstCardOfTheReading(browser, CARET_SEATING_ROUNDS);
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
        // The lines are built from the model's table, so at the moment the dialog is switched on they are not on
        // the screen yet - the containers come with the layout. The focus therefore goes to them one round later,
        // the way the guide's own action dialog reaches its list, and lands on the way out until they arrive.
        if (!FocusFirstTitleAction())
          Dispatcher.UIThread.Post(delegate
          {
            if (!browser.MenuVisible)
              return;
            if (!FocusFirstTitleAction())
              menuBackButton.Focus();
          }, DispatcherPriority.Loaded);
      }
      else
      {
        HandDialogGroundBack();
        FocusShelf();
      }
    }

    /// <summary>
    /// Stands the caret on the first line of the title's dialog: a viewer who opened the dialog by pressing a row
    /// wants to press once more, not to walk first. False while the lines are not on the screen yet.
    /// </summary>
    private bool FocusFirstTitleAction()
    {
      foreach (Control child in menuActionList.GetVisualDescendants())
      {
        if (child is Button line && line.IsEnabled && line.Focus())
          return true;
      }

      return false;
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

      // Whether the menu's rows hang in the tree at all belongs here and not to a binding, for one reason: a
      // panel bound to the layout stays visible while the room stands back over it, and Avalonia measures,
      // arranges and paints an invisible control not at all. Bound to the layout, a shelf that has ever shown its
      // menu therefore carries its rows for the rest of its life two hundred points off the picture.
      if (open)
      {
        // Stood up before the room is told to move, so that the first frame of the slide already carries the
        // rows: a control made visible by the same word that starts the sliding is laid out before anything is
        // painted, and it is laid out at the edge it will slide out of, not on the screen.
        _menuPanelTimer?.Stop();
        StandMenuPanel(covers);
      }
      else if (sideMenu.IsVisible || bottomMenu.IsVisible)
      {
        // Only now has the menu a panel it could lose. It keeps it through the sliding, because the thing the
        // viewer is watching is the menu travelling back out of the edge along with the room, and the room is
        // what the reference moved.
        if (atOnce)
          LayTheMenuPanelsDown();
        else
          LayTheMenuPanelsDownWhenTheRoomStops();
      }

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

    /// <summary>The slide the markup declares, for the case of a transform carrying no transitions to ask.</summary>
    private static readonly TimeSpan DECLARED_SLIDE = TimeSpan.FromMilliseconds(350);

    /// <summary>Wakes once after the room stopped sliding, to take the menu's panels out of the tree.</summary>
    private DispatcherTimer? _menuPanelTimer;

    /// <summary>
    /// How long the room's slide takes, read off the very transitions the slide travels on. Read and not repeated:
    /// the moment the panels are laid down must be the moment the room stops moving, and a second copy of the
    /// number in this file is a second number that can be changed on its own.
    /// </summary>
    private TimeSpan RoomSlideLength
    {
      get
      {
        Transitions? carried = RoomSlide.Transitions;
        if (carried == null)
          return DECLARED_SLIDE; // a jump borrows the transitions off the transform for the length of the jump

        foreach (ITransition step in carried)
        {
          if (step is DoubleTransition glide
              && (glide.Property == TranslateTransform.XProperty || glide.Property == TranslateTransform.YProperty))
            return glide.Duration;
        }

        return DECLARED_SLIDE;
      }
    }

    /// <summary>
    /// Hangs this shelf's menu in the panel its shape lies behind - the rail at the room's left edge beside a
    /// grid or a list, the row of plates under its floor under covers - and takes the other one down, which is
    /// the layout law the markup used to carry as two bindings.
    /// </summary>
    private void StandMenuPanel(bool covers)
    {
      sideMenu.IsVisible = !covers;
      bottomMenu.IsVisible = covers;
    }

    /// <summary>
    /// Takes both panels out of the tree, which is what a shelf whose menu stands shut does with a menu it is not
    /// showing: the room slid back over it, and from here on its rows are only measured, arranged and painted
    /// again every time something else asks the room for a layout pass - for a viewer who is looking at titles.
    /// </summary>
    private void LayTheMenuPanelsDown()
    {
      sideMenu.IsVisible = false;
      bottomMenu.IsVisible = false;
    }

    /// <summary>
    /// Lays the panels down once the room has arrived back, which is the first moment the menu is really out of
    /// the picture. Earlier would take the picture of the sliding menu away with the panels.
    /// </summary>
    private void LayTheMenuPanelsDownWhenTheRoomStops()
    {
      if (_menuPanelTimer == null)
      {
        _menuPanelTimer = new DispatcherTimer(DispatcherPriority.Background);
        _menuPanelTimer.Tick += delegate
        {
          _menuPanelTimer!.Stop();

          // Called back while the room was still travelling: this appointment belonged to a menu that is
          // standing again by now, and a menu that stands keeps the panel it stands in.
          if (DataContext is MediaBrowserViewModel browser && browser.MediaMenuVisible)
            return;

          LayTheMenuPanelsDown();
        };
      }

      _menuPanelTimer.Interval = RoomSlideLength;
      _menuPanelTimer.Stop();
      _menuPanelTimer.Start();
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
      // The line at the screen's foot speaks of the plate the caret stands on, so it is told whenever the caret
      // walks: by the list's own selection, and by the focus arriving in a row - the screen's arrows hand the caret
      // from plate to plate by moving the focus, which leaves the list's selection where it was.
      list.SelectionChanged += delegate { TellFootLineAbout(list); };
      list.AddHandler(GotFocusEvent, delegate { TellFootLineAbout(list); }, RoutingStrategies.Bubble);

      list.AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        // The button every input device has: the enter key on a keyboard, the space bar where a remote has no
        // letters, and the remote's own OK where it arrives as its own key - which keys those are is the
        // table's business (KeyAction.Select). A rail of commands that cannot be pressed with any of them is a
        // rail for the mouse only.  Heard though the list handled the key, because the list DOES handle it: a
        // list walks its own caret with the arrows and answers the button itself by ending whatever it was
        // doing, and a handler that only listens to keys nobody used never presses a row.
        if (!Settings.KeyBindings.Current.Is(Settings.KeyAction.Select, args))
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
      // away from the shelf - the left on a rail standing beside the shelf, the DOWN on the rail under covers,
      // whose other side is the floor the menu slides out of - and the two keys that lead past an end of the
      // rail, which are walls only while the caret stands on that end. Every other key is the list's own arrow
      // walking and is left to it. The whole rail nods, ground and glow with it, because a row of plates sliding
      // on their ground is a row that fell off it.
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

        // Under covers the shelf is reached from above (Up) and the wall lies BELOW the row of plates, so the
        // nod there dives downward into the floor the menu came out of.
        Key away_from_the_shelf = sideways_rail ? Key.Down : Key.Left;
        Key past_the_head = sideways_rail ? Key.Left : Key.Up;
        Key past_the_foot = sideways_rail ? Key.Right : Key.Down;
        object? stood = StoodOnRow(list);
        int standing = stood == null ? -1 : list.Items.IndexOf(stood);

        if (args.Key == away_from_the_shelf)
          BounceRail(rail_ground, sideways_rail ? 0 : -RAIL_BOUNCE_DIVE, sideways_rail ? RAIL_BOUNCE_DIVE : 0);
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
    /// Tells the screen's foot which plate of a rail the caret stands on.  The plate is asked for the same way every
    /// other answer on this rail asks for it - the row the focus is in, falling back to the list's own selection -
    /// because the screen's arrows move the caret without asking the list to select.
    /// </summary>
    private void TellFootLineAbout(ListBox list)
    {
      if (DataContext is not MediaBrowserViewModel owner)
        return;

      MediaBrowserMenuEntry? plate = StoodOnRow(list) as MediaBrowserMenuEntry;
      owner.MenuTileStood(plate?.Label, plate?.Detail);
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

    /// <summary>Whether the plate revert is mid-flight, so its echo cannot re-enter it.</summary>
    private bool _plate_revert_running;

    /// <summary>
    /// The index of the title nearest the row a caret stumbled onto: the title below is preferred, because the
    /// row a wall stumbles on when it opens grouped is its first headline and the first film stands under it.
    /// A wall holding no title at all answers -1, and no caret stands anywhere in it.
    /// </summary>
    private int NearestTitleRow(int from)
    {
      for (int step = 1; step < titlesList.Items.Count; step++)
      {
        int below = from + step;
        if (below < titlesList.Items.Count && titlesList.Items[below] is not GroupHeaderRow)
          return below;

        int above = from - step;
        if (above >= 0 && titlesList.Items[above] is not GroupHeaderRow)
          return above;
      }

      return -1;
    }

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
          // The click's own handover: the tile the caret landed on is recycled by the layout the pressed
          // command just changed (details off widens the wall), and between the recycling and the click
          // settling the focus manager names NOTHING at all. That nothing is not a viewer who walked away -
          // nobody aims for a window without a caret - and the same click completes a beat later onto the
          // row of the rail that just slid off the screen, where the watching was the only hand left to
          // catch it. So the nothing is answered like the fall it is: the tile is asked for again.
          if (fallen == null)
          {
            if (titlesList.IsEffectivelyVisible && RescueShelfCaret())
              quiet = 120;
            else if (--quiet <= 0)
              timer.Stop();
            return;
          }

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

    /// <summary>
    /// How many titles must stand unread behind the caret before the screen asks the server for the next part
    /// of the shelf.
    /// </summary>
    /// <remarks>
    /// <para>
    /// About what the eye reaches: the drawn row paints ten covers each side of the stood one and a wall of
    /// tiles shows a few dozen, so a caret that comes this near the last read title is a viewer who is walking
    /// off the end of what the screen has.  Asking then - rather than at the last title itself - is what keeps
    /// the walk over the page boundary from standing in front of a gap while the answer is on its way.
    /// </para>
    /// </remarks>
    private const int READ_AHEAD_TITLES = 24;

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
      // The one shape this screen paints with its own hand is asked of the skin for nothing: the row the
      // viewer sees is drawn above the list, so the list wears this file's pictureless frame and its
      // containers stop asking for posters the viewer cannot see.
      IDataTemplate frame = viewModel.Layout == BrowserLayout.Carousel
          ? BuiltinTemplate(viewModel.Layout)
          : SkinEngine.TryTileTemplate(viewModel.GridTemplateKey) ?? BuiltinTemplate(viewModel.Layout);
      // Plates are laid by the same wall as the titles, and a frame drawn for a title cannot speak for a plate -
      // so the worn frame is worn INSIDE a dispatcher, which hands titles to it unchanged and hands the grouping's
      // plates their divider. Laying it out per shape because a divider between rows is a headline, a divider
      // between tiles is a standing card and a divider between covers is a book's spine.
      bool frame_changed = !ReferenceEquals(_wornFrame, frame) || _wornFrameLayout != viewModel.Layout;
      _wornFrame = frame;
      _wornFrameLayout = viewModel.Layout;
      titlesList.ItemTemplate = new ShelfFrameTemplate(frame, viewModel.Layout);
      TitleViewModel? stood_on = _layoutStoodOn
          ?? titlesList.SelectedItem as TitleViewModel
          ?? viewModel.Selected;
      _layoutStoodOn = stood_on;

      // A wall that changes the frame its tiles wear has to throw its standing tiles away.  A virtualizing panel
      // keeps the containers it stood up and re-hangs them on the next title, and a re-hung container keeps the
      // tree it was generated with - so a shelf switched from posters to thumbnails grew thumbnails where its
      // tiles had already stood and posters where they had not, and the mixing stayed until the viewer walked
      // every row twice.  Clearing the source once rebuilds every tile from the new frame; it costs one
      // realization, and it happens when the viewer presses the picture button, not while a wall travels.
      if (frame_changed)
      {
        System.Collections.IEnumerable? titles = titlesList.ItemsSource;
        titlesList.SetCurrentValue(ItemsControl.ItemsSourceProperty, null);
        titlesList.SetCurrentValue(ItemsControl.ItemsSourceProperty, titles);
      }

      // Tiles dress themselves - plate, picture, name and ring - so the list may not paint its selected row
      // panel under them; a shelf of rows is read by that panel and keeps it.
      titlesList.Classes.Set("nereus-shelf-tiles", viewModel.Layout != BrowserLayout.List);
      titlesList.Classes.Set("nereus-shelf-rows", viewModel.Layout == BrowserLayout.List);

      // A gathered row is painted differently from a wall of standing tiles: it casts no shadow but for the cover
      // stood on, and the picture inside its tiles keeps its place (styles.xaml, nereus-shelf-shaped).  Both
      // because a shape scales its covers, and a blur and a clipped zoom that are re-drawn under a scale are
      // what the viewer feels as the row catching - not the dressing, which was measured at a millimetre of a
      // millisecond for a whole row.
      titlesList.Classes.Set("nereus-shelf-shaped",
          viewModel.Layout is BrowserLayout.Stacked or BrowserLayout.Carousel);

      // The screen drawn by our own hand wears the row twice: the list still holds the caret, but the
      // viewer sees the painted covers.  Avalonia carries a new selection into view on its own, and
      // carrying a caret that jumped from the first title to the thousandth into view realises every
      // container between them - measured at twenty-two seconds in a single frame and eleven hundred
      // covers prepared for dressing.  The ledger does not have to see its caret; the row does, and
      // the row keeps its own place by the numbers it paints from.  The other shapes keep the scroll:
      // there the list IS the shelf the viewer walks, and a caret driven out of sight would be lost.
      titlesList.AutoScrollToSelectedItem = viewModel.Layout != BrowserLayout.Carousel;
      Services.HomeTrace.Log("shape.asc", "value=" + titlesList.AutoScrollToSelectedItem
          + " layout=" + viewModel.Layout);
      // A grid and a list are laid one under the other, the grid wrapping whole tiles across the screen and the
      // list keeping its strip of rows. Covers are the third shape and the only one the reference turned on its
      // side: their row runs along the bottom edge of the room, their details are centred above it, and the
      // screen's menu waits under the edge of the screen for the room to be lifted up to it.
      if (viewModel.IsCoverLayout)
      {
        titlesList.ItemsPanel = COVER_PANEL;
        titlesList.VerticalAlignment = VerticalAlignment.Bottom;
        // The floor the grid and the list hand back to stand their wall in the middle of the room is not the
        // covers' floor: their row lies along the bottom edge of the room itself, and lifting it by that floor
        // would float the row off the edge it was laid against.  Lying along the edge is not the same as being
        // cut by it, though - laid flat against the floor the row read as sawn off, so it stands on a floor of
        // its own: forty-five design pixels, which is the sixty the viewer counted on a screen drawn at 133%.
        titlesList.Margin = new Avalonia.Thickness(0, 0, 0, COVER_SHELF_FLOOR);
        // No bar under the row: the covers are walked with the caret and the room slides to the stood-on cover
        // by itself, so the bar only ever drew a bright line across the bottom of the picture.
        ScrollViewer.SetHorizontalScrollBarVisibility(titlesList, ScrollBarVisibility.Hidden);
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

      // THE COVER ROW, drawn.  For this one shape the covers are painted by a control of their own and the list
      // keeps nothing but the caret, so the list is put out of sight rather than out of the tree: the keys, the
      // menus and the details all answer to the list's selection, and a list that is collapsed cannot hold the
      // focus they walk in from.  Out of sight also means out of the dressing - the shape asks nothing of a row
      // nobody can see (see WearShelfShape), and the wall is asked to travel nowhere (see GlideShelfToStoodTitle),
      // which is the whole of the per-step cost this shape used to have.
      bool row_is_drawn = viewModel.Layout == BrowserLayout.Carousel;
      coverRow.IsVisible = row_is_drawn;
      coverRow.PictureAsked ??= AskForCoverPicture;
      titlesList.Opacity = row_is_drawn ? 0.0 : 1.0;

      // A panel swapped under a standing list is laid out again, and with it the scroller it is read through - its
      // fade and its glide both live on that scroller, so both are put back on and the shelf is stood where the
      // title being stood on belongs in the new shape's band.
      _scroller = null;
      ApplyShelfFade(viewModel.Layout);
      InstallShelfGlide(viewModel.Layout);
      GlideShelfToStoodTitle();

      // And the same questions are put again once the swapped panel has measured itself.  A wall asked where its
      // caret belongs before the new panel has grown knows neither how long the wall is nor where the cover it
      // stands on lies, so the first asking lands wherever the PREVIOUS layout left the wall - which for a viewer
      // walking from rows into a stack is a stack standing at some other dozen covers than the one they are
      // standing on, filling itself only when they walk the caret on.  The glide is fitted on a post of its own
      // (see InstallShelfGlide), so this is asked after that: a glide asked of a bare scroller is a jump.  The
      // glide itself dresses the shape; a second WearShelfShape here would repeat the full row pass.
      Dispatcher.UIThread.Post(delegate
      {
        if (_layoutStoodOn is TitleViewModel stood_on)
        {
          if (!viewModel.Titles.Contains(stood_on))
            _layoutStoodOn = null;
          else
          {
            viewModel.Selected = stood_on;
            titlesList.SetCurrentValue(SelectingItemsControl.SelectedItemProperty, stood_on);
            if (ReferenceEquals(titlesList.SelectedItem, stood_on))
              _layoutStoodOn = null;
          }
        }

        if (viewModel.Layout == BrowserLayout.Carousel && titlesList.SelectedItem is object selected)
        {
          titlesList.ScrollIntoView(selected);
          Dispatcher.UIThread.Post(delegate { GlideShelfToStoodTitle(); }, DispatcherPriority.Background);
        }
        else
          GlideShelfToStoodTitle();

        // A wall stood up under a shape it did not come from has to be carried to the middle of the picture the
        // same way a step of the caret carries it: the swapped panel measures its length after this method has
        // run, so the travel asked for here is answered by the watch below rather than by one more question.
        WatchShelfWallHome(titlesList.SelectedIndex);
      }, DispatcherPriority.Background);
    }

    // ── The shape the covers are put in ───────────────────────────────────────────────────────────────
    //
    // The cover family lays the same posters in the same row along the floor; what the three shapes differ in is
    // only what happens to a tile according to how far it stands from the title being stood on.  The walked row
    // leaves its tiles exactly as the panel put them down.  A carousel turns its neighbours away from the stood
    // cover and lets them overlap it.  A stack keeps its place in the middle of the room: the stood cover faces
    // the viewer, the covers still to come stand one behind the next to its right, and the ones already walked
    // turn out of the picture.
    //
    // Avalonia knows no 3D transform string - "RotateY(45deg)" and "Perspective(500)" are WinUI words and the
    // parser here answers them with a FormatException.  It does own Rotate3DTransform, and the carousel wears it.
    // Two things were learned about that object and both are kept in the code:
    //
    // · It leans about whatever centre it is given, and given none it leans about the corner the tile was laid at.
    //   That swings the far half of a cover sideways and shifts its whole height, which is why the neighbours of the
    //   stood cover stood at different heights and with a hole in the middle.  Every turn here is given the tile's
    //   own middle (WearCarouselTile), and a ring is a ring again.
    // · A tilted tile is no longer square to the screen, and a visual the compositor cannot draw inside its own
    //   axis-aligned rectangle is given a surface of its own to be rasterised into.  A carousel with its neighbours
    //   leaned therefore costs one surface per turned cover - measured, and accepted for the look.  A stack wears
    //   the same object at zero degrees, where the matrix is square again and nothing is paid.
    //
    // The measure, the place and the light still come out of the same walk, so one tile's whole look is written by
    // one method.
    //
    // One boundary governs all of it.  The panel that lays the wall only stands up the tiles that fall inside the
    // picture's window on that wall. The carousel now lays its overlap into the tile margins, so the panel's own
    // item centres, scroll extent and virtualized window all use the same step; it no longer has to move a laid
    // cover across the wall with a render transform. The stack still gathers its covers with transforms and asks
    // the panel for enough <see cref="CacheLength"/> to stand those covers up before they are seen. Both shapes
    // leave the wall's travel, fades and next-page reading to the panel that owns them.
    //
    // The stood cover is held in the middle of the picture by the wall's own travel in the middle of a shelf, and
    // by one shift of the whole row at either end, where the wall can travel no further.  Never by a shift per
    // place, which is the pull this boundary forbids.

    /// <summary>
    /// How far a carousel's neighbours turn away from the cover being stood on.  Well past a right angle's half:
    /// a neighbour seen at forty-five degrees still shows half of itself and the row reads as a fence of boards,
    /// while at these angles the neighbours are wedges the eye reads as a ring running out of the picture.
    /// </summary>
    private const double CAROUSEL_LEAN_DEGREES = 45.0;

    /// <summary>
    /// How much of a cover's height a carousel's neighbour gives up per place from the cover being stood on.
    /// Every number of the ring - height, light and the step down towards the edge of the picture - is read off
    /// the DISTANCE ALONE, never off which side a cover stands on: two covers standing equally far from the middle
    /// are then the same size by construction, which is the one thing a row seen from the middle has to be.
    /// </summary>
    private const double CAROUSEL_MEASURE_LOSS = 0.055;

    /// <summary>How far a carousel's covers may shrink before the row stops being a row of readable posters.</summary>
    private const double CAROUSEL_MEASURE_FLOOR = 0.66;

    /// <summary>How much of a cover's light a carousel's neighbour gives up per place from the stood cover.</summary>
    private const double CAROUSEL_LIGHT_LOSS = 0.13;

    /// <summary>How dark a carousel's covers may go at the edge of the picture.</summary>
    private const double CAROUSEL_LIGHT_FLOOR = 0.28;

    /// <summary>
    /// How far a carousel's neighbours step DOWN per place from the cover being stood on.  The row is a ring seen
    /// slightly from above, so its far parts fall away towards the floor instead of standing on one flat line.
    /// </summary>
    private const double CAROUSEL_DROP = 7.0;

    /// <summary>
    /// The camera distance every turn here projects through.  A cover of this skin is wide, and a camera standing
    /// close magnifies the edge a turned cover brings near far past the other one - a row of covers whose near
    /// edges are a third bigger than their far ones does not read as a ring any more.  This is the distance the
    /// reference looks its covers through, and at it the near edge of a turned cover is a little bigger, no more.
    /// </summary>
    private const double SHELF_CAMERA_DEPTH = 900.0;

    /// <summary>
    /// The centre-to-centre distance of carousel covers. The overlap is laid into each cover's horizontal margin,
    /// so the virtualizing panel measures the same spacing that the 3D row shows.
    /// </summary>
    /// <remarks>
    /// This is also the distance the panel lays them at; unlike the stack, the carousel does not translate each
    /// cover away from its virtualized slot.
    /// </remarks>
    private const double CAROUSEL_STEP = 160.0;

    /// <summary>
    /// Keeps a few carousel covers prepared beyond each edge without realizing another full screen of 3D tiles.
    /// Avalonia applies CacheLength on both sides of the viewport.
    /// </summary>
    private const double CAROUSEL_CACHE_LENGTH = 0.15;

    /// <summary>
    /// How far the covers of a stack are seen from one another, to the left of the front cover and to its right: a
    /// stack is a stack on both sides, and both of them run out to the edges of the picture.
    /// </summary>
    private const double STACK_STEP = 150.0;

    /// <summary>How much of its size a stacked cover loses for every place it stands behind the front one.</summary>
    private const double STACK_MEASURE_LOSS = 0.06;

    /// <summary>The smallest a stacked cover stands, however far behind the front one it stands.</summary>
    private const double STACK_MEASURE_FLOOR = 0.55;

    /// <summary>How much of its light a stacked cover loses for every place it stands behind the front one.</summary>
    private const double STACK_LIGHT_LOSS = 0.09;

    /// <summary>The least light a stacked cover keeps, however far behind the front one it stands.</summary>
    private const double STACK_LIGHT_FLOOR = 0.25;

    /// <summary>
    /// How much wall the panel is asked to stand up beside the picture while a shape is worn, in pictures' width:
    /// a screenful and a half on each side.  A shape gathers its covers towards the middle, so a cover the panel
    /// took down would leave a hole <em>inside</em> the gathered row rather than outside it, and the row has to be
    /// stood up wide enough that the covers at its two ends reach the edges of the picture.
    /// </summary>
    /// <remarks>
    /// This is only the guess for the moment before the row has been laid: as soon as two covers of it answer,
    /// <see cref="ShelfWiden"/> works the window out of the row itself (half a picture seen, read back into the
    /// pitch the panel lays in) and asks for that.  Kept below the panel's own ceiling of two either way: every
    /// cover standing beside the picture is a cover standing with its poster in memory, its texture on the
    /// graphics card and its shadow to be painted, and this screen was asked to get cheaper, not dearer.
    /// </remarks>
    private const double SHELF_REALIZED_BUFFER = 1.5;

    /// <summary>
    /// The breath put on the window a gathered row asks for, so that the cover standing at the edge of the
    /// picture is stood up whole rather than half.
    /// </summary>
    private const double SHELF_REALIZED_BREATH = 1.15;

    /// <summary>How long a tile takes to reach the place its shape asks for.</summary>
    private static readonly TimeSpan SHELF_SHAPE_TRAVEL = TimeSpan.FromMilliseconds(300);

    /// <summary>The shape the tiles on screen were last put in, so that leaving a shape can undress them once.</summary>
    private BrowserLayout _wornShelfShape = BrowserLayout.List;

    /// <summary>
    /// Whether the row on screen dims its own covers towards the edges - which the two gathered shapes do, every
    /// cover standing dimmer the further it is from the stood one.  Such a row wears no edge mask: a mask is an
    /// offscreen surface of the whole band for the compositor to paint again on every frame of a step, and the
    /// reference gives its shapes no mask at all (<c>MediaStyles.xaml:5205</c>, <c>:5229</c>, <c>:5253</c>).
    /// </summary>
    private bool RowFadesItsOwnCovers
        => _wornShelfShape is BrowserLayout.Stacked or BrowserLayout.Carousel;

    /// <summary>
    /// The two transforms one tile of a shape is leaned with, kept beside the tile itself: the measure and the
    /// place.  They are made once per tile and only ever written afterwards, which is what lets a tile walk from
    /// one place to the next instead of jumping - a transform thrown away and built again carries no memory of
    /// where it stood.
    /// </summary>
    /// <remarks>
    /// The turn is the one object here that is NOT square to the screen, and that costs: a tile leaned away from the
    /// viewer cannot be drawn inside its own axis-aligned rectangle, so the compositor rasterises it into a surface
    /// of its own - one per turned cover, every frame of a step.  That is the price of a carousel and it is paid
    /// knowingly; a stack, which wears the same object at zero degrees, pays nothing because its matrix is square
    /// again.
    /// <para>
    /// The turn is a matrix built HERE rather than a <c>Rotate3DTransform</c>, and the reason is a measurement and not
    /// a preference: leaning a cover 230 px wide by +45° through Avalonia's own 3D object projects it onto 143 px, and
    /// by -45° onto 185 px.  That object mirrors the perspective term of its matrix but carries the term standing for
    /// the lean's centre over unchanged, so the two neighbours of a stood cover come out of it a quarter apart in
    /// width however equally they were written - which is the "links ist alles größer als rechts" the viewer has been
    /// reporting since this carousel was first stood up, and no value written into that object can cure it.
    /// </para>
    /// <para>
    /// A coverflow leaning is one camera looking at covers turned about their own upright line.  What that does to the
    /// plane of a cover is written out in <see cref="Lean"/>, and because the writing follows the camera instead of
    /// approximating it, the two sides come back as mirror images of one another by construction.
    /// </para>
    /// </remarks>
    private sealed class LeanTurn
    {
      private double _angle;
      private double _upright;
      private double _camera_x;
      private double _camera_y;

      /// <summary>
      /// The object the tile wears, which carries the matrix the compositor draws with.  Every number written below
      /// puts a new matrix into it, and a matrix left standing at identity is square to the screen - which is why a
      /// stack, wearing this at zero degrees, pays nothing for the object being there.
      /// </summary>
      public MatrixTransform Worn { get; } = new MatrixTransform(Matrix.Identity);

      /// <summary>
      /// The upright line the cover turns about, in the cover's own ground.  Given none, a cover leans about the
      /// corner it was laid at, which swings its far half sideways and shifts its whole height.
      /// </summary>
      public double Upright
      {
        get => _upright;
        set => Write(ref _upright, value);
      }

      /// <summary>
      /// Where the camera stands, in the ground of the cover being turned: the middle of the PICTURE, which for a
      /// cover standing three places to the right of the stood one lies three places to its left.  A camera in front
      /// of every cover's own middle instead would flatten every cover alike and the row would read as a row of
      /// boards, not as a ring opening out of the middle.
      /// </summary>
      public double CameraX
      {
        get => _camera_x;
        set => Write(ref _camera_x, value);
      }

      /// <summary>How high the camera looks across the cover being turned.</summary>
      public double CameraY
      {
        get => _camera_y;
        set => Write(ref _camera_y, value);
      }

      /// <summary>
      /// How far the cover stands from facing the viewer.  Worn at zero degrees by every cover of a stack, and at
      /// zero degrees the matrix is square to the screen again, so a stack pays nothing for the object being there.
      /// </summary>
      public double Angle
      {
        get => _angle;
        set => Write(ref _angle, value);
      }

      private void Write(ref double stored, double value)
      {
        if (Math.Abs(stored - value) < 1e-09)
          return;

        stored = value;
        Worn.Matrix = Project();
      }

      /// <summary>
      /// The matrix of the lean: the cover is turned by <see cref="Angle"/> degrees about <see cref="Upright"/> and
      /// seen through a camera <see cref="SHELF_CAMERA_DEPTH"/> in front of the row, at
      /// <see cref="CameraX"/>/<see cref="CameraY"/>.
      /// </summary>
      /// <remarks>
      /// <para>
      /// A point (x, y) of the cover lies in the plane z = 0 before the turn.  Turning it about the upright line
      /// x = u by θ moves it to X = u + (x-u)·cos θ, Z = (x-u)·sin θ - so a positive θ pushes the half to the RIGHT
      /// of the upright away from the viewer, which is the half a cover standing to the right of the stood one has
      /// to give up.  The camera at (cx, cy, -D) projects that point onto the plane z = 0 through
      /// t = D / (D + Z), and since Z is a multiple of (x-u), the divisor is a polynomial of x ALONE.  A mapping of a
      /// plane whose divisor is linear in the coordinates is a projective matrix, so no corner-by-corner fitting is
      /// needed: dividing numerator and denominator by D and ordering the terms gives the nine numbers below, with
      /// k = sin θ / D standing for the turn's strength per pixel.
      /// <code>
      ///   w  = k·x + (1 - k·u)
      ///   x' = [ (cos θ + cx·k)·x + (cx·(1 - k·u) + u - cx - cos θ·u) ] / w
      ///   y' = [ (cy·k)·x + y + (cy·(1 - k·u) - cy) ] / w
      /// </code>
      /// Every term of the divisor and of the translation carries k, so changing θ's sign mirrors the WHOLE matrix
      /// about the upright line instead of leaving one term behind - which is precisely what a lean has to do and
      /// what <c>Rotate3DTransform</c> does not: measured here, that object projected a cover of 230 px onto 143 px
      /// at +45° and onto 185 px at -45°, so the two neighbours of a stood cover stood a quarter apart in width
      /// however equally they were written.
      /// </para>
      /// </remarks>
      private Matrix Project()
      {
        if (Math.Abs(_angle) < 0.001)
        {
          // Square to the screen: no surface for the compositor to rasterise, which is why a stack is free.
          return Matrix.Identity;
        }

        double radians = _angle * Math.PI / 180.0;
        double k = Math.Sin(radians) / SHELF_CAMERA_DEPTH;
        double cosine = Math.Cos(radians);
        double depth_at_upright = 1.0 - k * _upright;

        return new Matrix(
            cosine + _camera_x * k, 0.0, k,
            _camera_y * k, 1.0, 0.0,
            _camera_x * depth_at_upright + _upright - _camera_x - cosine * _upright,
            _camera_y * (depth_at_upright - 1.0),
            depth_at_upright);
      }
    }

    /// <summary>
    /// The three transforms one tile of a shape is worn with, kept beside the tile itself: the lean, the measure and
    /// the place.  They are made once per tile and only ever written afterwards, which is what lets a tile walk from
    /// one place to the next instead of jumping - a transform thrown away and built again carries no memory of where
    /// it stood.
    /// </summary>
    private sealed class ShelfShape
    {
      public ShelfShape()
      {
        Turn = new LeanTurn();
        Measure = new ScaleTransform(1.0, 1.0);
        Place = new TranslateTransform(0.0, 0.0);

        Group = new TransformGroup();
        Group.Children.Add(Turn.Worn);
        Group.Children.Add(Measure);
        // The place is applied last, in the tile's own ground: a place laid on top of a turn would be a place
        // measured along the turned tile's own slant, which drags a turned cover down as it turns.
        Group.Children.Add(Place);
      }

      /// <summary>The lean of a carousel's neighbour away from the cover being stood on.</summary>
      public LeanTurn Turn { get; }

      public ScaleTransform Measure { get; }
      public TranslateTransform Place { get; }
      public TransformGroup Group { get; }

      public double FromAngle { get; set; }
      public double FromScale { get; set; } = 1.0;
      public double FromX { get; set; }
      public double FromY { get; set; }
      public double FromOpacity { get; set; } = 1.0;
      public double TargetAngle { get; set; }
      public double TargetScale { get; set; } = 1.0;
      public double TargetX { get; set; }
      public double TargetY { get; set; }
      public double TargetOpacity { get; set; } = 1.0;
      public long StartedAt { get; set; }
      public bool IsAnimating { get; set; }
    }

    private readonly ConditionalWeakTable<Control, ShelfShape> _shelf_shapes =
        new ConditionalWeakTable<Control, ShelfShape>();

    private sealed class CarouselTileMargin
    {
      public CarouselTileMargin(Thickness original, double slotWidth, HorizontalAlignment contentAlignment)
      {
        Original = original;
        SlotWidth = slotWidth;
        ContentAlignment = contentAlignment;
      }

      public Thickness Original { get; }
      public double SlotWidth { get; }
      public HorizontalAlignment ContentAlignment { get; }
    }

    private readonly ConditionalWeakTable<Control, CarouselTileMargin> _carousel_tile_margins =
        new ConditionalWeakTable<Control, CarouselTileMargin>();

    /// <summary>Containers which were already told to leave the wall where the shape stands it.  One telling each.</summary>
    private readonly ConditionalWeakTable<Control, object> _tiles_told_to_stay =
        new ConditionalWeakTable<Control, object>();

    private DispatcherTimer? _shelf_shape_animation;

    private readonly List<Control> _animating_shelf_tiles = new List<Control>();

    private bool _animate_shelf_shape_targets;

    private bool _shelf_shape_asked;

    private bool _shelf_shape_scheduled;

    private bool _shelf_needs_realign;

    /// <summary>
    /// Coalesces container preparation into one shape pass after the panel has assigned its indices.
    /// </summary>
    private void ScheduleShelfShape()
    {
      if (_shelf_shape_scheduled)
        return;

      _shelf_shape_scheduled = true;
      Dispatcher.UIThread.Post(delegate
      {
        _shelf_shape_scheduled = false;
        if (!(DataContext is MediaBrowserViewModel viewModel))
          return;

        if (_shelf_needs_realign && viewModel.IsCoverLayout)
          GlideShelfToStoodTitle();
        else
          WearShelfShape();
        _shelf_needs_realign = false;
      }, DispatcherPriority.Loaded);
    }

    // How often the row has been asked about its shape in a row, without a measurement ever agreeing.  Asking is
    // a bet that the next breath reads better numbers than this one did; a breath that reads the same numbers
    // again proves the bet wrong, and after a handful of them the row is left as it was dressed.  Without this
    // ceiling a disagreement that cannot be resolved by waiting becomes a wake-up loop, and a wake-up loop at
    // Loaded priority takes the whole screen with it.
    private int _shelf_shape_quarrels;

    private const int SHELF_SHAPE_QUARRELS_ALLOWED = 6;

    // WHAT A STEP OF THE CARET COSTS.  A shelf that judders judders for one of three reasons - the view's own
    // dressing, the posters arriving, or the screen painting what it was given - and a viewer in front of a
    // television cannot tell them apart.  The first two are counted here and the third is the frame ledger beside
    // these lines (MP_TRACE_HOME, Services.HomeTrace): a step whose dressing and pictures cost nothing and whose
    // frames still came slow is a step the screen itself could not paint.
    private static readonly System.Diagnostics.Stopwatch SHELF_CLOCK = System.Diagnostics.Stopwatch.StartNew();
    /// <summary>
    /// Whether the tiles arrive without their ground shadow.  Off unless <c>MP_NOSHADOW=1</c>, for one measured
    /// run: what the shadow costs is a question the frame ledger answers and opinion does not.
    /// </summary>
    private static readonly bool NoShadowsWorn =
        Environment.GetEnvironmentVariable("MP_NOSHADOW") == "1";

    /// <summary>
    /// Whether every shape pass writes the place of every container into the trace file.  Off unless
    /// <c>MP_TRACE_SHELF=1</c>, because the line is built out of string concatenation and a row is dressed on every
    /// step of a walk.
    /// </summary>
    private static readonly bool ShelfTraced =
        Environment.GetEnvironmentVariable("MP_TRACE_SHELF") == "1";

    /// <summary>Takes the skin's ground shadow off one container's tiles.</summary>
    private static void TakeShadowsFrom(Control container)
    {
      foreach (Visual child in container.GetVisualDescendants())
      {
        if (child is Border shade && shade.Classes.Contains("home-tile-shadow"))
          shade.IsVisible = false;
      }
    }

    private int _shelf_prepared;
    private int _shelf_dress_passes;
    private int _shelf_dress_tiles;
    private long _shelf_dress_ms;

    /// <summary>
    /// Puts the standing row into the shape the screen wears, and - when somebody asked for the numbers - says
    /// afterward how much that cost.
    /// </summary>
    private void WearShelfShape(bool force = false, bool animate = false)
    {
      animate |= _animate_shelf_shape_targets;
      if (!Services.HomeTrace.Enabled)
      {
        DressShelfShape(force, animate);
        return;
      }

      long asked_at = SHELF_CLOCK.ElapsedMilliseconds;
      DressShelfShape(force, animate);
      ++_shelf_dress_passes;

      int covers = 0;
      if (titlesList != null)
      {
        foreach (Control? _ in titlesList.GetRealizedContainers())
          ++covers;
      }

      _shelf_dress_tiles = covers;
      _shelf_dress_ms += SHELF_CLOCK.ElapsedMilliseconds - asked_at;
    }

    /// <summary>
    /// Asks the standing row to be dressed again once the wall has measured what it stands in, once per asking.
    /// </summary>
    /// <remarks>
    /// A row whose covers and whose wall disagree on how long the row is was read across a change rather than
    /// measured - a panel swapped under a wall that has not re-measured itself yet - and dressing on the reading
    /// leaves the covers where they were read.  Nothing is fixed by waiting, so the asking is put behind the
    /// layout pass this message is standing in, the same priority a resized picture is answered at.
    /// </remarks>
    private void AskAboutTheShelfShapeAgain()
    {
      if (_shelf_shape_asked || _shelf_shape_quarrels >= SHELF_SHAPE_QUARRELS_ALLOWED
          || DataContext is not MediaBrowserViewModel)
        return;

      _shelf_shape_asked = true;
      ++_shelf_shape_quarrels;
      Dispatcher.UIThread.Post(
          delegate
          {
            _shelf_shape_asked = false;
            if (DataContext is MediaBrowserViewModel again && again.IsCoverLayout && titlesList != null)
            {
              GlideShelfToStoodTitle();
              WearShelfShape(true);
            }
          },
          DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Asks the panel how much wall it stands up beside the picture, and whether the picture is a window the wall
    /// may lean out of. The stack gathers covers with transforms, so it needs extra wall outside the picture; the
    /// carousel's overlap is already part of the panel's measured layout. Both shapes can extend beyond the slot
    /// the presenter would otherwise clip. A shelf walked plainly again is asked to stop both: extra covers are
    /// nobody's view, and a wall that leans out of its window while walked vertically paints rows over the screen.
    /// </summary>
    /// <remarks>
    /// The panel is asked for realised covers and for nothing else - deliberately not for ground at its ends.  A
    /// margin on the panel moves the panel's own content origin away from the wall's, and the panel works out
    /// which covers to stand up from the wall's travel; a panel standing on a margin of half a picture therefore
    /// realises its covers half a picture away from the covers the screen is showing. The room the ends of a
    /// gathered stack needs is paid for where it is spent - see the shift in <see cref="WearShelfShape"/>.
    /// </remarks>
    private void ShelfWiden(ScrollViewer? scroller, bool worn, double step)
    {
      if (ShelfPanel(scroller) is VirtualizingStackPanel panel)
      {
        // No shape, no cover standing beside the picture: the panel goes back to standing up exactly what the
        // picture shows, which is what a shelf walked in rows is asked to do and nothing else.
        double wanted = 0.0;

        if (worn)
        {
          wanted = _wornShelfShape == BrowserLayout.Carousel
              ? CAROUSEL_CACHE_LENGTH
              : SHELF_REALIZED_BUFFER;
          if (_wornShelfShape != BrowserLayout.Carousel)
          {
            // A transformed stack reaches the edge of the picture when the panel stands up as much wall beside the
            // picture as its gathering takes away: half a picture SEEN is half a picture times the laid step over
            // the seen step, LAID. Asking for more leaves posters, shadows and textures standing for nobody to
            // look at. Where nothing has been measured to go by, SHELF_REALIZED_BUFFER is the guess.
            double view = scroller?.Viewport.Width ?? 0.0;
            double laid = ShelfLaidPitch(out double place);

            if (view > 0.0 && step > 0.0 && laid > 0.0 && place > 0.0)
            {
              double to_the_edge = view / 2.0 * (laid / step) + place;
              wanted = Math.Clamp(to_the_edge / view * SHELF_REALIZED_BREATH, 0.25, 2.0);
            }
          }
        }

        if (panel.CacheLength != wanted)
          panel.CacheLength = wanted;
      }

      // The window the wall is seen through has to be open while a shape leans covers out of it.  The presenter
      // and the ScrollViewer that owns it both clip by default. Disabling only the presenter's clip still leaves
      // the viewer's rectangle as a hard edge around the turned covers, which cuts their overflow at the sides and
      // floor. Both clips are opened for a shape and restored when the shelf is walked plainly again.
      if (scroller != null && scroller.ClipToBounds == worn)
        scroller.ClipToBounds = !worn;

      if (scroller != null &&
          scroller.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>()
              .FirstOrDefault() is { } presenter &&
          presenter.ClipToBounds == worn)
        presenter.ClipToBounds = !worn;
    }

    /// <summary>
    /// How far the panel lays two neighbouring covers apart, and how wide one cover is; naught where the row has
    /// not been laid far enough to be asked.
    /// </summary>
    private double ShelfLaidPitch(out double place_width)
    {
      place_width = 0.0;
      double first = double.NaN;

      foreach (Control? tile in titlesList.GetRealizedContainers())
      {
        if (tile == null || !(tile.Bounds.Width > 0.0))
          continue;

        place_width = tile.Bounds.Width;

        if (double.IsNaN(first))
        {
          first = tile.Bounds.X;
          continue;
        }

        double pitch = tile.Bounds.X - first;
        return pitch > 0.0 ? pitch : 0.0;
      }

      return 0.0;
    }

    /// <summary>
    /// The panel the cover row is laid in.  A list's panel is the child of its presenter, and its presenter is what
    /// a scroller holds.
    /// </summary>
    private static Panel? ShelfPanel(ScrollViewer? scroller)
    {
      if (scroller?.Content is not Visual presenter)
        return null;

      return presenter.GetVisualChildren().OfType<Panel>().FirstOrDefault();
    }

    /// <summary>
    /// Puts every tile the shelf is standing in the shape the screen wears: the plain row, the carousel, or the
    /// stack.  Asked whenever the caret walks (the places belong to the distance from the stood title) and on every
    /// step of the shelf's travel while a shape is worn, because both shapes hang their covers off where the wall
    /// has travelled to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A shelf is <em>laid out</em> by a scrolling panel, and the panel is what decides which tiles stand up at all:
    /// a wall of five thousand titles cannot stand up all of them so that one may be looked at, and a tile the panel
    /// laid outside the picture's window is taken down again. The carousel's margins make its overlap part of the
    /// panel's own layout, while the stack still gathers covers with transforms. The latter is only honest while
    /// the panel stands up more wall than the picture shows, so a stacked shape asks the panel for that (<see
    /// cref="ShelfBuffer"/>); the carousel only needs its measured edge covers and a modest preheat buffer.
    /// </para>
    /// <para>
    /// The row underneath is never touched: it is laid, travelled, faded and read exactly as the walked row is.  The
    /// only thing a shape writes is where each cover is <em>seen</em> - one step of its own from the next, which the
    /// panel laid a whole place apart - and the whole row is shifted by the one amount that brings the stood cover
    /// to the middle of the picture.  In the middle of a shelf that amount is naught, because the wall's own travel
    /// keeps the stood cover there (<see cref="ShelfGlide"/>, whose band both shapes narrow to the middle alone); at
    /// either end of the shelf, where the wall can travel no further, it is what keeps the first cover and the last
    /// one from standing against the edge of the screen instead of in the middle.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Tells the shelf that a cover standing within the viewer's reach wants its poster.  Asking the server is the
    /// view model's business, so the drawn row is handed this instead of a client of its own; the answer arrives
    /// on the row and is decoded there, off the thread that draws.
    /// </summary>
    private void AskForCoverPicture(object item)
    {
      if (DataContext is MediaBrowserViewModel browser && item is TitleViewModel title)
        browser.PaintRow(title);
    }

    private void DressShelfShape(bool force, bool animate)
    {
      if (!(DataContext is MediaBrowserViewModel viewModel))
        return;

      BrowserLayout shape = viewModel.Layout;

      // Both shapes belong to the distance from the title being stood on, and the stack additionally holds its
      // place while the wall underneath it travels, so both are rewritten on every step of that travel; a wall
      // standing in any other shape has nothing to rewrite until something changes.
      bool wears = shape is BrowserLayout.Stacked or BrowserLayout.Carousel;
      if (!force && !wears && shape == _wornShelfShape)
        return;

      bool wore_a_shape = _wornShelfShape is BrowserLayout.Stacked or BrowserLayout.Carousel;
      _wornShelfShape = shape;

      // The carousel's covers are DRAWN now, by a control that owns the place the row stands at, so this shape
      // asks nothing of the list's containers: what a previous shape wrote on them is taken back once, the fade is
      // asked for the shape that is worn now, and the pass stops there.  A row nobody can see has no business
      // being measured twice per cover on every step of the caret, which is what this pass cost a wall of a
      // thousand covers - see CoverCarousel for why the row is drawn rather than dressed.
      if (shape == BrowserLayout.Carousel)
      {
        if (wears != wore_a_shape)
          UpdateShelfFade();

        if (wore_a_shape)
        {
          ShelfWiden(ListScroller(), false, 0.0);
          foreach (Control? tile in titlesList.GetRealizedContainers())
          {
            if (tile == null)
              continue;

            RestoreCarouselTileMargin(tile);
            BareTile(tile);
          }
        }

        return;
      }

      // The edge fade follows the shape: a gathered row dims every cover by its own distance from the stood one and
      // needs no mask over the band, while a plain wall has nothing else to hide its edges with.  So the fade is
      // asked again the moment the row changes sides, and not on every step - see UpdateShelfFade.
      if (wears != wore_a_shape)
        UpdateShelfFade();

      bool carousel_margins_changed = false;
      foreach (Control? tile in titlesList.GetRealizedContainers())
      {
        if (tile == null)
          continue;

        if (shape == BrowserLayout.Carousel)
          carousel_margins_changed |= SetCarouselTileMargin(tile);
        else if (wore_a_shape)
          carousel_margins_changed |= RestoreCarouselTileMargin(tile);
      }

      if (carousel_margins_changed)
      {
        // The panel's own extent and the centres it assigns its containers must agree with the carousel's
        // overlap before a shape pass can measure its step. Re-ask after the changed margins have been measured.
        _shelf_needs_realign = true;
        ScheduleShelfShape();
        return;
      }

      ScrollViewer? scroller = ListScroller();

      if (!wears)
      {
        // The wall stops standing outside the picture as soon as no shape gathers covers towards its middle
        // anymore, and the tiles lose what the shape wrote on them - once, and only when a shape really was worn:
        // tiles are asked for on every scroll, and clearing properties that were never written is a write per tile
        // per frame for nothing.
        ShelfWiden(scroller, false, 0.0);
        if (wore_a_shape)
        {
          foreach (Control? tile in titlesList.GetRealizedContainers())
          {
            if (tile != null)
              BareTile(tile);
          }
        }

        return;
      }

      if (scroller == null)
      {
        // Both shapes want the middle of the picture and the distance the wall has travelled, and neither number
        // exists before the tree has grown a scroller.  A tree that has not is asked again when it has - the
        // glide's own fitting asks the fade, and the fade asks this.
        _wornShelfShape = BrowserLayout.List;
        return;
      }

      int stood = titlesList.SelectedIndex;
      Control? stood_tile = stood >= 0 ? titlesList.ContainerFromIndex(stood) as Control : null;

      // What one place of this row is, and how far the panel lays two neighbouring places apart.  Both are read
      // and neither is known: the width belongs to the worn skin's tile, and the distance between two places
      // belongs to that tile and to whatever ground the row wears around it.  The narrowest distance between the
      // stood cover and any cover standing beside it is the step of the row, and a row that has only just stood up
      // its first cover has no distance to read yet - one cover of it is then a whole place of it.
      double place_width = stood_tile?.Bounds.Width ?? 0.0;
      double laid_step = 0.0;
      if (stood_tile != null)
      {
        foreach (Control? neighbour in titlesList.GetRealizedContainers())
        {
          if (neighbour == null || neighbour == stood_tile || neighbour.Bounds.Width <= 0.0)
            continue;

          double apart = Math.Abs(neighbour.Bounds.X - stood_tile.Bounds.X);
          if (apart > 0.0 && (laid_step <= 0.0 || apart < laid_step))
            laid_step = apart;
        }
      }

      if (laid_step <= 0.0)
        laid_step = place_width;

      double seen_step = shape == BrowserLayout.Carousel ? CAROUSEL_STEP : STACK_STEP;
      double view = scroller.Viewport.Width;

      // The panel is asked for the window the row can be seen in BEFORE its covers are dressed, and with the
      // shape's own step told to it: a row gathered to a narrow step has to be stood up wide, and the same row
      // at a wide step needs far less wall standing around it.
      ShelfWiden(scroller, true, seen_step);

      // How far the panel lays two neighbouring places apart is bounded by what the row can be: one cover of it
      // and whatever ground the skin wears beside a cover, so at most about twice a cover.  A wider reading did
      // not come from this row - it came from containers of the layout the shelf came from, or from a wall that
      // is still measuring itself - and dressing a row on a pitch of fifteen thousand pixels is dressing every
      // cover of it a hundred screens off the picture.  Where the reading is wider than a row of covers can be,
      // the row is dressed as the row it is: one cover beside the next.
      if (laid_step <= 0.0 || laid_step > place_width * 2.0)
        laid_step = place_width;

      // And where the covers and the wall still disagree on how long the row is, the disagreement is a moment
      // rather than a fact: a panel swapped under a standing wall has not told the wall its new length yet.  The
      // row is dressed on what was read - so that a viewer never stands above an undressed shape - and asked
      // again once the wall has measured what it stands in.
      //
      // Only where there is a row to disagree ABOUT, though.  With nothing stood under the caret there is no
      // cover and no length to read, so "the numbers disagree" is true of every breath and asking again answers
      // nothing - the next breath reads nothing either.  Woken that way, this pass wakes itself, the wake is
      // answered at Loaded priority, and Loaded priority is answered before the ordinary work of the thread:
      // measured, that was two thousand dresses a second, the whole screen at 50 ms a frame, and every poster
      // errand that had already been sent sitting in a queue the loop never let be answered.  A row that has not
      // stood its covers yet is woken by the container arriving (ContainerPrepared) and by the caret being put on
      // the shelf, both of which say the same thing with a fact instead of a guess.
      double laid_length = place_width > 0.0
          ? (titlesList.Items.Count - 1) * laid_step + place_width
          : 0.0;
      if (view > 0.0 && place_width > 0.0
          && Math.Abs(laid_length - scroller.Extent.Width) > Math.Max(laid_step, place_width))
      {
        AskAboutTheShelfShapeAgain();
      }
      else
      {
        _shelf_shape_quarrels = 0;
      }

      // The stack stands nearer its neighbours than the panel laid them, so its own length is shorter than the
      // wall's: at the ends the wall runs out of travel before the stood cover does, and the cover is left standing
      // at the edge of the picture instead of in its middle. What the wall could not travel is paid for here, in
      // one shift that moves the whole row along - never in a shift per place, which would pull the row apart.
      //
      // The shift is worked out from where the wall CAN travel to, not from where it happens to stand this frame:
      // the wall is still on its way when this is asked, and a shift read off the moving offset would be retargeted
      // by the transition on every frame of that travel.  That rubber-band is the jerk the screen shows after every
      // key, and it is the reason an earlier version of this shifted on every scroll event.
      double travel = scroller.Extent.Width - view;
      double shift = 0.0;
      if (travel > 0.0)
      {
        double wanted = (stood_tile?.Bounds.X ?? 0.0) - (view / 2.0 - place_width / 2.0);
        shift = Math.Min(Math.Max(wanted, 0.0), travel) - wanted;
        if (Math.Abs(shift) > view)
          shift = 0.0;
      }

      // Measuring: MP_TRACE_SHELF (with MP_TRACE_HOME, which opens the file) writes one line per dress - the place,
      // distance and picture of every container the row is standing.  A row that reads wrong on the screen can be
      // read off these numbers instead: distances that do not run -3 -2 -1 0 1 2 3 say the panel is standing the
      // titles somewhere else than the shape thinks, and two containers carrying the same number after "c" say the
      // same picture is standing twice.
      string? shelf_trace = ShelfTraced
          ? $"mode={shape} stood={stood} items={titlesList.Items.Count} laid={laid_step:0.#} seen={seen_step:0.#} shift={shift:0.#}"
          : null;

      // How big a turned cover may stand without climbing out of the slot it was laid in.  Worked out once from the
      // cover this skin draws and carried into every cover of the row.
      foreach (Control? tile in titlesList.GetRealizedContainers())
      {
        if (tile == null)
          continue;

        // A virtualizing panel can prepare a recycled container before its index map has caught up with the
        // DataContext it just assigned. Resolve the shape from the title the tile actually wears, so a tile
        // cannot inherit the distance of its previous neighbour for one pass of the carousel.
        int index = tile.DataContext is TitleViewModel title
            ? viewModel.Rows.IndexOf(title)
            : -1;
        if (index < 0)
        {
          // The panel is standing no title on this container at this moment.  It has no distance from the stood
          // cover, so it has no place in the shape to be written - see ContainerPrepared, which is where this
          // container comes from and why dressing it here used to put a cover of somebody else's title far out in
          // the row.  It is left exactly as the panel laid it and asked again when it has a title: the pass posted
          // from ContainerPrepared is that asking.
          if (shelf_trace != null)
            shelf_trace += $" | i- c{(tile.DataContext?.GetHashCode() ?? 0) & 0xffff}";
          continue;
        }

        int distance = stood < 0 ? 0 : index - stood;

        // Where the panel laid this cover and where its shape wants it seen: one step of the shape per place from
        // the stood cover, counted from the place the panel laid the stood cover in.  A tile's bounds are its place
        // before any transform, which is what keeps this reading from feeding on its own result.
        //
        // And that is all this number ever is - a distance between two places, never a distance measured from the
        // picture or from where the wall has travelled to.  A wall walked in the middle keeps its stood cover in
        // the middle by travelling one laid step per cover, and since every cover of a gathered row moves by what
        // the shape took away from its own laid step, the row glides one step of the shape per cover along with the
        // wall's own travel.  Writing anything that depends on that travel here - a shift that centres the row at
        // this very moment, say - would be retargeted by the transition on every frame of the travel, which is the
        // rubber-band the screen shows as a jerk after every key.
        double place = shift + distance * (seen_step - laid_step);

        if (shape == BrowserLayout.Carousel)
          WearCarouselTile(tile, distance, place, animate);
        else
          WearStackTile(tile, distance, place, animate);

        if (shelf_trace != null)
          shelf_trace += $" | i{index} d{distance} x{place:0} c{(tile.DataContext?.GetHashCode() ?? 0) & 0xffff}";
      }

      if (shelf_trace != null)
        Services.HomeTrace.Log("shelf", shelf_trace);
    }

    private void SetShelfTileTarget(Control tile, ShelfShape shape, double angle, double scale, double x, double y,
        double opacity, int zIndex, bool animate)
    {
      tile.RenderTransform = shape.Group;
      tile.ZIndex = zIndex;

      bool same_target = Math.Abs(shape.TargetAngle - angle) < 0.001
          && Math.Abs(shape.TargetScale - scale) < 0.001
          && Math.Abs(shape.TargetX - x) < 0.001
          && Math.Abs(shape.TargetY - y) < 0.001
          && Math.Abs(shape.TargetOpacity - opacity) < 0.001;
      if (same_target)
      {
        if (animate && shape.IsAnimating && _shelf_shape_animation?.IsEnabled == true)
          return;
        if (!animate && shape.IsAnimating)
        {
          shape.IsAnimating = false;
          _animating_shelf_tiles.Remove(tile);
          ApplyShelfTileState(tile, shape, 1.0);
        }
        return;
      }

      shape.FromAngle = shape.Turn.Angle;
      shape.FromScale = shape.Measure.ScaleX;
      shape.FromX = shape.Place.X;
      shape.FromY = shape.Place.Y;
      shape.FromOpacity = tile.Opacity;
      shape.TargetAngle = angle;
      shape.TargetScale = scale;
      shape.TargetX = x;
      shape.TargetY = y;
      shape.TargetOpacity = opacity;
      shape.StartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
      shape.IsAnimating = animate && (Math.Abs(shape.FromAngle - angle) >= 0.001
          || Math.Abs(shape.FromScale - scale) >= 0.001
          || Math.Abs(shape.FromX - x) >= 0.001
          || Math.Abs(shape.FromY - y) >= 0.001
          || Math.Abs(shape.FromOpacity - opacity) >= 0.001);

      if (shape.IsAnimating)
      {
        if (!_animating_shelf_tiles.Contains(tile))
          _animating_shelf_tiles.Add(tile);
        StartShelfShapeAnimation();
      }
      else
      {
        _animating_shelf_tiles.Remove(tile);
        ApplyShelfTileState(tile, shape, 1.0);
      }
    }

    private void StartShelfShapeAnimation()
    {
      if (_shelf_shape_animation == null)
      {
        _shelf_shape_animation = new DispatcherTimer(DispatcherPriority.Render)
        {
          Interval = TimeSpan.FromMilliseconds(16)
        };
        _shelf_shape_animation.Tick += ShelfShapeAnimationTick;
      }

      if (!_shelf_shape_animation.IsEnabled)
        _shelf_shape_animation.Start();
    }

    private void ShelfShapeAnimationTick(object? sender, EventArgs e)
    {
      if (titlesList == null || _animating_shelf_tiles.Count == 0)
      {
        _shelf_shape_animation?.Stop();
        return;
      }

      long now = System.Diagnostics.Stopwatch.GetTimestamp();
      double duration = SHELF_SHAPE_TRAVEL.TotalMilliseconds;
      for (int i = _animating_shelf_tiles.Count - 1; i >= 0; --i)
      {
        Control tile = _animating_shelf_tiles[i];
        if (!_shelf_shapes.TryGetValue(tile, out ShelfShape? shape) || !shape.IsAnimating)
        {
          _animating_shelf_tiles.RemoveAt(i);
          continue;
        }

        double elapsed = (now - shape.StartedAt) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double progress = Math.Clamp(elapsed / duration, 0.0, 1.0);
        double eased = 1.0 - Math.Pow(1.0 - progress, 3.0);
        ApplyShelfTileState(tile, shape, eased);
        if (progress >= 1.0)
        {
          shape.IsAnimating = false;
          _animating_shelf_tiles.RemoveAt(i);
        }
      }

      if (_animating_shelf_tiles.Count == 0)
        _shelf_shape_animation?.Stop();
    }

    private static void ApplyShelfTileState(Control tile, ShelfShape shape, double progress)
    {
      shape.Turn.Angle = Lerp(shape.FromAngle, shape.TargetAngle, progress);
      double scale = Lerp(shape.FromScale, shape.TargetScale, progress);
      shape.Measure.ScaleX = scale;
      shape.Measure.ScaleY = scale;
      shape.Place.X = Lerp(shape.FromX, shape.TargetX, progress);
      shape.Place.Y = Lerp(shape.FromY, shape.TargetY, progress);
      tile.Opacity = Lerp(shape.FromOpacity, shape.TargetOpacity, progress);
    }

    private static double Lerp(double from, double to, double progress)
        => from + (to - from) * progress;

    /// <summary>
    /// Stands one tile of a carousel: the cover being stood on faces the viewer whole, its neighbours turn away from
    /// it and stand smaller and dimmer the further they are, which is what makes the row read as a ring the viewer
    /// looks through the middle of.  Where a tile stands was settled before this was asked - see
    /// <see cref="WearShelfShape"/>.
    /// </summary>
    private void WearCarouselTile(Control tile, int distance, double place, bool animate)
    {
      ShelfShape shape = _shelf_shapes.GetValue(tile, _ => new ShelfShape());
      int away = Math.Abs(distance);
      int side = Math.Sign(distance);
      double centre_x = tile.Bounds.Width / 2.0;
      double half_height = tile.Bounds.Height / 2.0;

      // The lean of a ring: the cover stood on faces the viewer, EVERY other cover turns by the same amount away
      // from it - on whichever side it stands - about its OWN upright line, and is seen through the camera standing
      // in front of the middle of the picture.  For a cover three places to the right of the stood one that camera
      // stands three places to its LEFT, and that one number is what turns a row of boards into a ring: it is the
      // only quantity here which is not the same on both sides, and it is written so that it mirrors.
      //
      // Given no upright line a lean goes about the corner the tile was laid at, which swings the far half of a
      // cover sideways and shifts its whole height - neighbours standing at different heights and a hole where the
      // stood cover should be.
      shape.Turn.Upright = centre_x;
      shape.Turn.CameraX = centre_x - distance * CAROUSEL_STEP;
      shape.Turn.CameraY = half_height;
      double angle = away == 0 ? 0.0 : side * CAROUSEL_LEAN_DEGREES;

      // What a cover stands at, and how dark it stands, come out of its DISTANCE and of nothing else - not out of
      // which side of the stood cover it lies, and not out of the angle's own sign.  The camera's own foreshortening
      // needs no compensation and must be given none: it is worked out of the same numbers on both sides, mirrored
      // about the upright line, so a cover standing three places left and one standing three places right come off
      // the projection the same width.  Measuring a cover by hand from the turn's perspective (an earlier look here)
      // took the two sides differently and is what left one side at 0.68 of itself against the other's 1.12.
      double measure = away == 0
          ? 1.0
          : Math.Max(CAROUSEL_MEASURE_FLOOR, 1.0 - away * CAROUSEL_MEASURE_LOSS);

      // What a turned cover gives up is given up about its middle too, so a cover that shrinks stays in the slot the
      // row laid it in instead of climbing towards its corner.  A measure grows out of that corner, so the middle is
      // paid for in the place: measuring a tile of width w by s about its centre is the same as measuring it by s
      // and moving it back by (1 - s) · w/2.  The ring's far parts then fall away towards the floor with the
      // distance, which is a ring seen a little from above rather than a row of boards on one line.
      double x = place + (1.0 - measure) * centre_x;
      double y = (1.0 - measure) * half_height + away * CAROUSEL_DROP;

      // The light falls with the distance, which is the fade towards the edges of the picture that a gathered row
      // wears instead of a mask over the whole band - a mask is an offscreen surface of that band, repainted on
      // every frame of a step.
      double opacity = away == 0
          ? 1.0
          : Math.Max(CAROUSEL_LIGHT_FLOOR, 1.0 - away * CAROUSEL_LIGHT_LOSS);

      // The cover being stood on stands in front of its neighbours, and every cover nearer the middle in front of
      // the ones further out, which is what makes a ring overlap towards the viewer instead of behind it.
      // Only the focus and its immediate neighbours animate on a step. Further covers share the same angle,
      // scale and light; transitioning every one of them on every key needlessly invalidates their 3D surfaces.
      SetShelfTileTarget(tile, shape, angle, measure, x, y, opacity, 100 - away, animate && away <= 1);
    }

    private bool SetCarouselTileMargin(Control tile)
    {
      if (tile is not ListBoxItem item)
        return false;

      double width = item.Bounds.Width;
      if (width <= 0.0)
        return false;

      CarouselTileMargin original = _carousel_tile_margins.GetValue(
          tile, control => new CarouselTileMargin(control.Margin, width, item.HorizontalContentAlignment));
      double side = (CAROUSEL_STEP - original.SlotWidth) / 2.0;
      Thickness target = new Thickness(side, original.Original.Top, side, original.Original.Bottom);
      bool changed = false;
      if (!tile.Margin.Equals(target))
      {
        tile.Margin = target;
        changed = true;
      }

      if (item.HorizontalContentAlignment != HorizontalAlignment.Center)
      {
        item.HorizontalContentAlignment = HorizontalAlignment.Center;
        changed = true;
      }

      return changed;
    }

    private bool RestoreCarouselTileMargin(Control tile)
    {
      if (!_carousel_tile_margins.TryGetValue(tile, out CarouselTileMargin? original))
        return false;

      bool changed = false;
      if (!tile.Margin.Equals(original.Original))
      {
        tile.Margin = original.Original;
        changed = true;
      }

      if (tile is ListBoxItem item && item.HorizontalContentAlignment != original.ContentAlignment)
      {
        item.HorizontalContentAlignment = original.ContentAlignment;
        changed = true;
      }

      return changed;
    }

    /// <summary>
    /// Stands one tile of a stack: every cover upright and facing the viewer, the front one in the middle of the
    /// picture and the others stepping away from it to the left and to the right, each a little smaller and a
    /// little darker than the one before it, until they are gone into the fade at the edge of the screen.
    /// </summary>
    private void WearStackTile(Control tile, int distance, double place, bool animate)
    {
      ShelfShape shape = _shelf_shapes.GetValue(tile, _ => new ShelfShape());

      int away = Math.Abs(distance);

      // A stack does not turn; it draws its depth by standing its covers smaller the further back they are, which
      // is the same distance on either side of the front cover.  They are measured about their own centre, which is
      // where a cover's centre was when the row laid it, so a stacked cover shrinks into its own place instead of
      // sliding towards the cover in front of it - the centre being paid for in the place, as in a carousel.
      double measure = Math.Max(STACK_MEASURE_FLOOR, 1.0 - away * STACK_MEASURE_LOSS);
      double x = place + (1.0 - measure) * tile.Bounds.Width / 2.0;
      double y = (1.0 - measure) * tile.Bounds.Height / 2.0;

      double opacity = Math.Max(STACK_LIGHT_FLOOR, 1.0 - away * STACK_LIGHT_LOSS);
      SetShelfTileTarget(tile, shape, 0.0, measure, x, y, opacity, 100 - away, animate);
    }

    /// <summary>
    /// Tells one container that it must not ask the list to scroll itself while a gathered shape is worn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia answers its own selection: putting the caret on a tile ends in a bring-into-view of the panel, which
    /// moves the scroller to the smallest offset showing that tile.  For a walked list that is the whole point.  For
    /// a carousel it is a second opinion on where the wall stands, and it lands <em>after</em> the travel the shape
    /// asked for - a wall whose stood cover was carried to the middle is found sitting beside the middle again, with
    /// nothing to show for the travel but a jerk back.  The ask is therefore swallowed where it starts, which leaves
    /// the panel realising the container it was asked for (the glide measures that container) and refuses only the
    /// scrolling of it.
    /// </para>
    /// </remarks>
    private void KeepTileWhereTheWallStands(Control container)
    {
      if (_tiles_told_to_stay.TryGetValue(container, out object? told) && told != null)
        return;

      _tiles_told_to_stay.Add(container, new object());
      container.AddHandler(RequestBringIntoViewEvent, delegate (object? sender, RequestBringIntoViewEventArgs args)
      {
        if (DataContext is MediaBrowserViewModel viewModel && viewModel.IsCoverLayout)
          args.Handled = true;
      }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Takes a tile out of a shape: no turn, no measure, no place, no dimming, no foreground.</summary>
    private void BareTile(Control tile, bool restoreCarouselMargin = true)
    {
      _animating_shelf_tiles.Remove(tile);
      if (restoreCarouselMargin)
        RestoreCarouselTileMargin(tile);
      if (_shelf_shapes.TryGetValue(tile, out ShelfShape? shape))
      {
        shape.IsAnimating = false;
        shape.Turn.Angle = 0.0;
        shape.Measure.ScaleX = 1.0;
        shape.Measure.ScaleY = 1.0;
        shape.Place.X = 0.0;
        shape.Place.Y = 0.0;
        shape.TargetAngle = 0.0;
        shape.TargetScale = 1.0;
        shape.TargetX = 0.0;
        shape.TargetY = 0.0;
        shape.TargetOpacity = 1.0;
      }
      tile.ClearValue(RenderTransformProperty);
      tile.ClearValue(OpacityProperty);
      tile.ClearValue(ZIndexProperty);
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
      bool sideways = layout is BrowserLayout.Cover or BrowserLayout.Stacked or BrowserLayout.Carousel;
      if (sideways != _fadeRunsSideways)
      {
        _fadeRunsSideways = sideways;
        _fadeBefore = _fadeAfter = UNREAD;
      }

      // The scroller lives inside the list's template and only exists once the list has been measured, so the
      // answer is asked for again on the next pass rather than given up on.  A skin worn as a THEME lives in
      // the application's style bundles and arrives a pass later than the view's own, so this is not a one-shot
      // errand: the wall is asked again whenever a later pass changes the scroller under it.
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller = ListScroller();
        if (scroller == null)
          return;

        if (!ReferenceEquals(_fadedScroller, scroller))
        {
          // A retemplated list - which is what putting a skin on is - brings a new scroller, and the mask and the
          // remembered depths both go with the old one.  The wall keeps its fade while it travels, so the reading
          // of where it stands must not be refreshed on every frame of that travel: the handler only answers a
          // scroll that came without the screen starting a travel - the viewer's own wheel - and lets a travel
          // the screen started settle by its own means (see WearShelfGlide).
          _fadedScroller = scroller;
          _fadeBefore = _fadeAfter = UNREAD;
          scroller.AddHandler(ScrollViewer.ScrollChangedEvent, delegate
          {
            // Every frame that moves the wall feeds the rest watcher (see AwaitScrollRest).  A travel the screen
            // started wears its mask from before its first frame and is not asked again until it stands (see
            // WearShelfGlide); a scroll that comes without a travel - the viewer's own wheel - refreshes the
            // mask at once.
            _scrollSettled = true;
            if (!_shelfTravelling)
              UpdateShelfFade();
          });
        }

        UpdateShelfFade();
      }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Whether the titles' scroller is standing on the screen.  A shelf behind the search's full-screen
    /// question or the menu's wall of pictures is drawn no more than a window with its curtain down, so the
    /// wall behind it wears no mask at all - see <see cref="UpdateShelfFade"/>.  The details panel is NOT such
    /// a curtain: beside the tiles, over the covers, it leaves the wall in the picture, and its fade answers
    /// the scroll the same as ever.  IsVisible does not always follow the curtain (the screen asks its layers
    /// to stay put while it raises them), so the question is put to the layers themselves.
    /// </summary>
    private bool TitlesHiddenBehindALayer
    {
      get
      {
        if (!(DataContext is MediaBrowserViewModel viewModel))
          return false;
        return viewModel.ShowsSearch;
      }
    }

    /// <summary>
    /// Says what each end of the wall is hiding and, if that is not what the mask already says, writes it on.
    /// </summary>
    private void UpdateShelfFade()
    {
      // Both shapes are dressed from the places the panel laid their covers in, which one travel of the wall never
      // changes, so a scroll is NOT a reason to dress a shelf again: writing a transitioned property on every
      // frame of that travel retargets the transition every frame, which the screen shows as a row that jerks.
      // (The glide, the caret, a resizing picture and a container taking its place ARE such reasons - they ask
      // WearShelfShape themselves.)

      ScrollViewer? scroller = _fadedScroller;
      if (scroller == null)
        return;

      // A wall behind a closed door does not answer the door, and the doors this screen has are its own layers
      // (see TitlesHiddenBehindALayer).  While one of them stands over the wall, every pixel the wall's mask
      // costs is a pixel nobody sees - and the layer over the wall is the screen whose picture is tightest:
      // wall, panel and fanart all compete for one GPU.  So a covered wall wears no mask, and the layer going
      // down asks again - the remembered depths go with the mask, so the wall recomputes its fade before a
      // viewer can see it.  (The scroller's own IsVisible is deliberately not asked: it answers the question
      // of an unfinished tree too, and an unfinished tree is not a closed door.)
      //
      // A gathered row stands in the same queue for the same reason: it already dims every cover by its own
      // distance from the stood one, so the mask would hide nothing a cover does not hide itself, while making the
      // whole band a surface the compositor paints again on every frame of a step.  The reference agrees - its
      // carousel and its stack wear no fade over the row at all.
      if (TitlesHiddenBehindALayer || RowFadesItsOwnCovers)
      {
        if (scroller.OpacityMask != null || _fadeBefore != UNREAD)
        {
          scroller.OpacityMask = null;
          _fadeBefore = _fadeAfter = UNREAD;
        }
        return;
      }

      // A mask that was put away behind a layer goes back on before the wall is asked where it stands: the
      // remembered depths are gone with it, and the first frame of the wall should already be dressed.  A row that
      // fades its own covers is not such a wall, and stays uncovered.
      if (_shelfFade != null && scroller.OpacityMask == null && !RowFadesItsOwnCovers)
        scroller.OpacityMask = _shelfFade;

      double view = _fadeRunsSideways ? scroller.Viewport.Width : scroller.Viewport.Height;
      if (view <= 0)
        return;

      double extent = _fadeRunsSideways ? scroller.Extent.Width : scroller.Extent.Height;
      double offset = _fadeRunsSideways ? scroller.Offset.X : scroller.Offset.Y;

      // What stands outside the picture at either end.  A wall shorter than its window stands outside neither of
      // them and is faded at neither.  A wall that is travelling is read at where it will stand, not at where it
      // is, and what the reading asks for is put on before its first frame - see WearShelfTravel.
      double zone = EDGE_FADE * view;
      double before = FadeDepth(_shelfTravelling ? _shelfGlide : offset, zone);
      double after = FadeDepth(extent - view - (_shelfTravelling ? _shelfGlide : offset), zone);
      if (before == _fadeBefore && after == _fadeAfter)
        return;

      _fadeBefore = before;
      _fadeAfter = after;

      WearShelfFade(scroller);
    }

    /// <summary>The mask the wall wears while it travels. Never rewritten during a travel.</summary>
    private void WearShelfFade(ScrollViewer scroller)
    {
      if (_fadeBefore <= 0 && _fadeAfter <= 0)
      {
        if (scroller.OpacityMask != null)
          scroller.OpacityMask = null;
        return;
      }

      LinearGradientBrush fade = _shelfFade ??= NewEdgeFade();
      fade.StartPoint = new RelativePoint(_fadeRunsSideways ? 0.0 : 0.5,
          _fadeRunsSideways ? 0.5 : 0.0, RelativeUnit.Relative);
      fade.EndPoint = new RelativePoint(_fadeRunsSideways ? 1.0 : 0.5,
          _fadeRunsSideways ? 0.5 : 1.0, RelativeUnit.Relative);

      // A mask hides by its own transparency, so an end that is hiding nothing is written opaque and an end that
      // has walked a whole fade zone of wall out of the picture is written clear.
      fade.GradientStops[0].Color = FadeColour(_fadeBefore);
      fade.GradientStops[3].Color = FadeColour(_fadeAfter);
      scroller.OpacityMask = fade;
    }

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

    /// <summary>The edge mask with both ends standing opaque; the depths are written on it afterwards.</summary>
    private static LinearGradientBrush NewEdgeFade()
    {
      LinearGradientBrush fade = new LinearGradientBrush
      {
        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
      };
      fade.GradientStops.Add(new GradientStop(Colors.White, 0));
      fade.GradientStops.Add(new GradientStop(Colors.White, EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1.0 - EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1));
      return fade;
    }

    /// <summary>
    /// Puts the media lists' edge fade on one band of cards: the same dress <see cref="ApplyShelfFade"/> lays over
    /// the shelf's wall, asked of a band's two ends instead of the wall's and answering the one question the wall's
    /// ends answer - has anything walked out of the picture at this end?
    /// </summary>
    /// <remarks>
    /// A band runs sideways where the shelf runs down, so a band's mask is laid from its left edge to its right
    /// one; everything else is shared with the wall, down to the twelve percent and the eight steps, because two
    /// dresses on one screen are read as one dress or they are noticed.
    /// <para>
    /// The reading is renewed on the band's <c>ScrollChanged</c>, which is the one event that answers all three of
    /// the things that can change what an end is hiding: the offset (a card walked, a wheel turned), the viewport
    /// (the window was resized) and the extent (a band's cards arrived after the reading opened).  The first
    /// reading is asked on the pass the band's layout is finished, which is the first pass its extent is more than
    /// a guess.
    /// </para>
    /// </remarks>
    private void FadeBandEnds(ScrollViewer band)
    {
      BandEndFade fade = new BandEndFade(band);
      _bandFades.Add(fade);
      Dispatcher.UIThread.Post(fade.Update, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// One band's edge fade: the mask it wears and the depths its two ends were last read at.  The first card of a
    /// band that stands at its left edge has walked out of nothing and stands whole; a card the caret walked past
    /// an edge dissolves into that edge, and a band that still holds cards beyond an edge says so by dissolving the
    /// card it shows there - which is what the shelf's wall does at its own ends.
    /// </summary>
    private sealed class BandEndFade
    {
      private readonly ScrollViewer _band;

      private LinearGradientBrush? _mask;

      private double _before = UNREAD;

      private double _after = UNREAD;

      internal BandEndFade(ScrollViewer band)
      {
        _band = band;
        band.AddHandler(ScrollViewer.ScrollChangedEvent, delegate
        {
          Update();
        });
      }

      /// <summary>Says what each end of the band is hiding and, if the mask says otherwise, writes it on.</summary>
      internal void Update()
      {
        double view = _band.Viewport.Width;
        if (view <= 0)
          return;

        double zone = EDGE_FADE * view;
        double before = FadeDepth(_band.Offset.X, zone);
        double after = FadeDepth(_band.Extent.Width - view - _band.Offset.X, zone);
        if (before == _before && after == _after)
          return;

        _before = before;
        _after = after;

        // Neither end has anything standing out of the picture - the whole band fits its window - so it wears no
        // mask at all, exactly as a wall shorter than its window wears none.
        if (before <= 0 && after <= 0)
        {
          _band.OpacityMask = null;
          return;
        }

        LinearGradientBrush mask = _mask ??= NewEdgeFade();
        mask.StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        mask.EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative);
        mask.GradientStops[0].Color = FadeColour(before);
        mask.GradientStops[3].Color = FadeColour(after);
        _band.OpacityMask = mask;
      }
    }

    /// <summary>
    /// How far in from an end of a list its rows dissolve once a whole fade zone of it has gone out of the
    /// picture: the twelve percent this front end's menu column wears.
    /// </summary>
    private const double EDGE_FADE = 0.12;

    /// <summary>
    /// How long after the last started travel the wall is read once more even when its transition never
    /// answered.  A glide runs 300 to 400 ms; this settles a travel that went unanswered a beat after the
    /// longest of them would have arrived, so a wall that glides never settles mid-flight.
    /// </summary>
    private const double TRAVEL_SETTLE_MILLISECONDS = 550;

    /// <summary>How often the rest watcher asks whether the travelling wall has stopped moving.</summary>
    private const double TRAVEL_REST_BEAT_MILLISECONDS = 60;

    /// <summary>How finely the depth an end fades is read off the wall: eight steps between none and all.</summary>
    private const double FADE_STEPS = 8;

    /// <summary>A depth no end has been asked about yet, so that the first reading is always written.</summary>
    private const double UNREAD = -1;

    private LinearGradientBrush? _shelfFade;

    private ScrollViewer? _fadedScroller;

    private double _fadeBefore = UNREAD;

    private double _fadeAfter = UNREAD;

    private bool _fadeRunsSideways;

    /// <summary>
    /// The edge fade of every band of cards the reading holds, kept because a mask that nobody holds a reading
    /// for is a mask that is never renewed - each one watches its own band and remembers its own two ends.
    /// </summary>
    private readonly List<BandEndFade> _bandFades = new List<BandEndFade>();

    /// <summary>True while the wall is on a travel and the fade is worn for its destination.</summary>
    private bool _shelfTravelling;

    /// <summary>Settles a travel whose transition never answered (see TRAVEL_SETTLE_MILLISECONDS).</summary>
    private DispatcherTimer? _shelfTravelTimer;

    /// <summary>The frame the shelf's tiles were last told to wear; a change of it empties the wall (ApplyLayout).</summary>
    private IDataTemplate? _wornFrame;
    private BrowserLayout _wornFrameLayout;

    /// <summary>
    /// The covers' panel: the tiles' own panel turned, which is to say a strip that lays one cover beside the
    /// next and keeps standing only the ones the screen could be drawing.
    /// </summary>
    private static readonly FuncTemplate<Panel?> COVER_PANEL =
        new FuncTemplate<Panel?>(() => new VirtualizingStackPanel { Orientation = Orientation.Horizontal });

    /// <summary>
    /// The floor the cover row stands on: how far the row is lifted off the bottom edge of the room.  Laid flat
    /// against that edge the covers read as sawn off, which is what the viewer reported as sixty screen pixels
    /// of missing cover - forty-five of them design pixels on the screen this port is measured on, which draws
    /// at one and a third.
    /// </summary>
    private const double COVER_SHELF_FLOOR = 45;

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

      // THE DRAWN ROW ASKS FOR ITS OWN PICTURES.  In this one shape the list underneath is the caret and nothing
      // else - it stands at zero opacity - and the row above it asks for the handful of covers a viewer could be
      // looking at by itself (CoverCarousel.PictureAsked).  Asking here too was not a second opinion but a second
      // bill: a jump of the caret to the far end brings the list's own answer to "show the selected item" with it,
      // which lays out EVERY container of a thousand-title shelf - measured as realized=1136 where ten had stood -
      // and this pass then walked all of them and asked a poster for each.  Measured as 3038 asks, 3031 decodes
      // and one frame gap of 24 seconds on a shelf of eleven hundred films.
      if (viewModel.Layout == BrowserLayout.Carousel)
        return;

      int caret = titlesList.SelectedIndex;
      List<(int distance, TitleViewModel row)> in_sight = new List<(int distance, TitleViewModel row)>();

      foreach (Control? container in titlesList.GetRealizedContainers())
      {
        if (container?.DataContext is not TitleViewModel row)
          continue;

        int at = titlesList.IndexFromContainer(container);
        if (at < 0)
          continue;

        // The panel already limits this set to realized rows, including its deliberate preheat band. Sort that
        // bounded set by the selected item rather than translating transformed containers into viewport space:
        // the scroll transform is already part of TranslatePoint, so comparing that point with Offset would count
        // the walk twice and eventually stop asking for pictures farther down a cover shelf.
        in_sight.Add((caret < 0 ? 0 : Math.Abs(at - caret), row));
      }

      // Nearest the caret first.  The picture fetcher keeps four errands on the wire and queues the rest, so the
      // ORDER these are asked in decides what the viewer sees: asked in the order the panel laid them, a shelf
      // walked to the right spends its whole queue on the wall the viewer has already left and stands looking at
      // backup plates until the queue drains - which is what "the pictures are missing from the sixtieth cover
      // onwards" is made of, and no amount of widening the band can mend an order that starts at the wrong end.
      //
      // And only the nearest of them at all.  A panel that has laid out more containers than the television can
      // fill - its preheat band, or a panel asked to bring a distant caret into view - is not a statement about
      // what the viewer can see, and every picture asked for a row he cannot see is a place in the queue taken
      // from one he can.
      Services.HomeTrace.Log("paintrows", "sighted=" + in_sight.Count + " asking="
          + Math.Min(in_sight.Count, MAX_ROWS_PAINTED_AT_ONCE) + " caret=" + caret);

      foreach ((int _, TitleViewModel row) pair in
          in_sight.OrderBy(stand => stand.Item1).Take(MAX_ROWS_PAINTED_AT_ONCE))
      {
        viewModel.PaintRow(pair.row);
      }
    }

    /// <summary>
    /// How many rows are asked for their picture in one pass: about what a television fills with rows, twice
    /// over, so that the rows coming into view are already on their way when the viewer reaches them.
    /// </summary>
    private const int MAX_ROWS_PAINTED_AT_ONCE = 48;

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
    /// Whether the caret stands on one of the track cards under a title's picture and the pressed key means
    /// walking.  A card is looked at rather than chosen - nothing about it is ever started - so the arrows only
    /// move the caret, and they move it over the places this screen has for a caret at all: from card to
    /// card along a band, from a band up or down into the band above or below it, out of the head of one of the two
    /// people bands into the band beside it - those two share a row of the screen, so the row is walked as one row.
    /// A band that kept the caret to itself would be a band a viewer could
    /// get into and never out of.  Enter is taken and answered with nothing (Return being the very same key to
    /// Avalonia): this screen hands Enter to "play this" wherever else on it the key lands, and a film that
    /// starts because a viewer wanted to look at an audio track is an answer to a question nobody asked.  At the
    /// ends of a row, where the caret has nowhere left to walk, the band leans against that end and springs back
    /// (see <see cref="BounceBand"/>); above the first band the key is left alone, so that whatever the screen means
    /// by it there keeps its meaning.  Below the last band of cards there IS one more place for a caret on this
    /// reading - the collection's row of films, when the film belongs to a collection - so Down steps onto that, and
    /// the key is left alone only below it.
    ///
    /// That row is walked like every other band of this reading and answers one key more than the others do: a press
    /// on one of its posters READS the film it stands for, its posters being the collection's films and not a
    /// picture of them (<see cref="ReadsCollectionFilm"/>).  Every other card of this reading still answers the key
    /// with nothing, and no card of it anywhere ever starts a film.
    /// </summary>
    private bool WalksTrackCards(KeyEventArgs args)
    {
      ScrollViewer band =
          audioTracksScroller.IsKeyboardFocusWithin ? audioTracksScroller
          : subtitleTracksScroller.IsKeyboardFocusWithin ? subtitleTracksScroller
          : actorsScroller.IsKeyboardFocusWithin ? actorsScroller
          : directorsScroller.IsKeyboardFocusWithin ? directorsScroller
          : artistsScroller.IsKeyboardFocusWithin ? artistsScroller
          : albumArtistsScroller.IsKeyboardFocusWithin ? albumArtistsScroller
          : composersScroller.IsKeyboardFocusWithin ? composersScroller
          : collectionScroller.IsKeyboardFocusWithin ? collectionScroller
          : null;
      if (band == null)
        return false;

      // The cards are asked for by their class rather than by the item containers around them: an ItemsControl
      // hands out a container per item and the card is what the template grew inside it, so the thing the viewer
      // sees and presses is not the thing the list thinks it made.
      List<Border> cards = TrackCards(band);
      int at = cards.FindIndex(card => card.IsFocused);
      if (at < 0)
        return false;

      switch (args.Key)
      {
        case Key.Left:
          // Left walks the band, and out of the head of a band that shares its row with another it walks into the
          // tail of the band beside it: the two people bands of a film, and the three of a song, each share one row
          // of the screen, so the row is walked as the one row it is.
          if (at == 0)
          {
            ScrollViewer? beside = BandLeftOfTheRow(band);
            if (beside != null && StandsOnTrackCard(beside, int.MaxValue, args))
              break;

            // The head of the row is here and the arrow asked for the card before it, so the band leans against
            // that end and springs back - the answer this screen's rails already give a key that points at a wall.
            BounceBand(band, true);
            break;
          }

          cards[at - 1].Focus();
          break;
        case Key.Right:
          // The same row read the other way round, and no further: at the tail of the last band of the row the
          // caret has nowhere to go, and the band leans against its end instead.
          if (at == cards.Count - 1)
          {
            ScrollViewer? beyond = BandRightOfTheRow(band);
            if (beyond != null && StandsOnTrackCard(beyond, 0, args))
              break;

            BounceBand(band, false);
            break;
          }

          cards[at + 1].Focus();
          break;
        case Key.Home:
          if (at == 0)
            return false;
          cards[0].Focus();
          break;
        case Key.End:
          if (at == cards.Count - 1)
            return false;
          cards[cards.Count - 1].Focus();
          break;
        case Key.Up:
          // Up out of a band walks into the band above at the same place ALONG it: a viewer who came down at the
          // third card expects to come back up at the third card, not at the band's first.  Above the first band
          // this screen has nothing else for a caret to take - its wall is put away while the reading of a title is
          // up - so the key is left unanswered rather than swallowed.
          return StandsOnTrackCard(BandAbove(band), at, args);
        case Key.Down:
          {
            // Down goes the same way: into the band below at the same place along it.  Below the last band of
            // tracks and people this reading still has one place for a caret when the film belongs to a collection
            // - its row of films, which stands where the reference stood its button under the bands
            // (MediaInfoVideo.xaml:457-530) - and BandBelow answers that.  With no band below the arrow is left
            // alone, exactly as it is above the first band.
            return StandsOnTrackCard(BandBelow(band), at, args);
          }
        case Key.Enter:
          if (band != collectionScroller)
            break;

          // Taken whether or not there was a film to read behind the card, because Enter means "play this"
          // everywhere else on this screen and a poster that answered its own press with nothing would hand that
          // very key to the player.
          args.Handled = true;
          ReadsCollectionFilm(cards[at]);
          break;
        default:
          return false;
      }

      return true;
    }

    /// <summary>
    /// Reads the film one poster of the collection's row stands for, which is what a viewer standing on it and
    /// pressing it asks for.
    /// </summary>
    /// <param name="card">The card of the row, keyed or pointed at.</param>
    /// <remarks>
    /// <para>
    /// The reference's screen for a collection was a list of its films whose entries opened the film's information
    /// (<c>MediaItemListMovieCollectionDetails.inc</c>), so a film of the collection stood under a viewer's hand is
    /// a film they are asking about - and it is read, exactly as the wall's own dialog button reads a title, by
    /// <see cref="MediaBrowserViewModel.OpenCollectionFilmAsync"/>.  Nothing is started by it, whatever the same key
    /// means elsewhere on this screen.
    /// </para>
    /// <para>
    /// The caret is left where it stands.  The row belongs to the collection and not to the film being read, so
    /// reading the next film of it leaves the row standing and the ring on the same poster
    /// (see <c>MediaBrowserViewModel.FillDetailsCollection</c>), which is what a viewer walking a collection with
    /// the arrows wants: the row stays, the words beside it change.
    /// </para>
    /// </remarks>
    private void ReadsCollectionFilm(Control card)
    {
      if (DataContext is MediaBrowserViewModel viewModel
          && card.DataContext is CollectionFilmCard film)
        _ = viewModel.OpenCollectionFilmAsync(film);
    }

    /// <summary>
    /// Reading the film whose poster in the collection's row was pressed with a pointer, which is the same ask as
    /// the key of the card under it.
    /// </summary>
    private void OnCollectionFilmTapped(object? sender, RoutedEventArgs args)
    {
      if (sender is Control card)
      {
        card.Focus();
        ReadsCollectionFilm(card);
      }
    }

    /// <summary>
    /// Leans a band of cards against the end the caret asked to walk past and springs it back.
    /// </summary>
    /// <remarks>
    /// The caret is on the card at the band's end, an arrow asked for the card beyond it, and the screen has no
    /// such card: the honest answer is the band itself saying that it is at its end, which is what
    /// <see cref="BounceRail"/> is on the home screen for.  A band's scroller cannot answer that itself - it holds
    /// its offset inside its extent and cannot be pushed past an end, which is exactly what a lean is - so the lean
    /// goes on the row of cards inside the band, where the band's own edge cuts it off at the end it leans into.
    /// The people row is one row of two bands, so a lean is only ever asked where the ROW ends: the head of the
    /// actors' band and the tail of the director's.
    /// </remarks>
    /// <param name="band">The band whose cards are to lean.</param>
    /// <param name="pastLeft">Whether the caret asked past the band's left end; false for its right one.</param>
    private void BounceBand(ScrollViewer band, bool pastLeft)
    {
      if (band.Content is Control cards)
        BounceRail(cards, pastLeft ? -RAIL_BOUNCE_DIVE : RAIL_BOUNCE_DIVE, 0);
    }

    /// <summary>
    /// The row of people bands a band of cards belongs to: a film's two, or a song's three, in the order the screen
    /// lays them out from left to right.
    /// </summary>
    /// <remarks>
    /// Exactly one of the two rows is ever drawn - a song names no actors and a film names no composers - so the
    /// question is answered by which band the caret stands in rather than by asking the screen what it is showing.
    /// </remarks>
    private IList<ScrollViewer> RowOfTheBand(ScrollViewer band)
    {
      if (band == artistsScroller || band == albumArtistsScroller || band == composersScroller)
        return new ScrollViewer[] { artistsScroller, albumArtistsScroller, composersScroller };

      return new[] { actorsScroller, directorsScroller };
    }

    /// <summary>
    /// The band of cards left of the given one WITHIN ITS ROW, or null where the row begins here.
    /// </summary>
    private ScrollViewer? BandLeftOfTheRow(ScrollViewer band)
    {
      IList<ScrollViewer> row = RowOfTheBand(band);
      for (int at = row.IndexOf(band) - 1; at >= 0; at--)
        if (row[at].IsEffectivelyVisible)
          return row[at];

      return null;
    }

    /// <summary>
    /// The band of cards right of the given one WITHIN ITS ROW, or null where the row ends here.
    /// </summary>
    private ScrollViewer? BandRightOfTheRow(ScrollViewer band)
    {
      IList<ScrollViewer> row = RowOfTheBand(band);
      for (int at = row.IndexOf(band) + 1; at < row.Count; at++)
        if (row[at].IsEffectivelyVisible)
          return row[at];

      return null;
    }

    /// <summary>
    /// The band of cards above the given one, or null where the screen has none above it.
    /// </summary>
    /// <remarks>
    /// The people row is ONE row of two bands, so whatever band of cards the screen draws above that row is the
    /// band a caret walking up out of it comes to - and a title whose file carries no subtitles has no subtitle
    /// band to come up to, which is why this is asked of the screen instead of being written down as a pair.
    /// </remarks>
    private ScrollViewer? BandAbove(ScrollViewer band)
    {
      if (band == subtitleTracksScroller)
        return audioTracksScroller;

      if (StandsInAPeopleRow(band))
        return subtitleTracksScroller.IsEffectivelyVisible ? subtitleTracksScroller
            : audioTracksScroller.IsEffectivelyVisible ? audioTracksScroller
            : null;

      // The collection's row is the last band of this reading, so whatever band of cards the screen drew above it
      // is the band a caret walking up out of the row comes back to.
      return band == collectionScroller ? LastBandOfTheReading() : null;
    }

    /// <summary>
    /// Whether the given band is one of the bands of a row of people: a film's actors and director, or a song's
    /// artist, album artist and composer.  Both rows are walked as one row and both stand under the same bands of
    /// tracks, so the two questions - what is above them, what is below - have the same answer for both.
    /// </summary>
    private bool StandsInAPeopleRow(ScrollViewer band)
    {
      return band == actorsScroller || band == directorsScroller
          || band == artistsScroller || band == albumArtistsScroller || band == composersScroller;
    }

    /// <summary>
    /// The band of cards below the given one, or null where the caret already stands in the last row of the
    /// reading - below which the reading has nothing left for a caret to stand on.
    /// </summary>
    private ScrollViewer? BandBelow(ScrollViewer band)
    {
      ScrollViewer? people = FirstBandOfThePeopleRow();

      if (band == audioTracksScroller)
        return subtitleTracksScroller.IsEffectivelyVisible
            ? subtitleTracksScroller
            : people ?? CollectionBand();

      if (band == subtitleTracksScroller)
        return people ?? CollectionBand();

      return StandsInAPeopleRow(band) ? CollectionBand() : null;
    }

    /// <summary>
    /// The collection's row of films, or null where this reading drew none - a film naming no collection, or a
    /// library holding no film of the one it names.  Asked and not remembered because both answers arrive a moment
    /// after the reading stands up, and a caret sent into a band that is not drawn is a caret lost off the screen.
    /// </summary>
    private ScrollViewer? CollectionBand() =>
        collectionScroller.IsEffectivelyVisible ? collectionScroller : null;

    /// <summary>
    /// The first band of the people row that is on the screen, which is the band a caret walking DOWN into that row
    /// is stood on.
    /// </summary>
    /// <remarks>
    /// Usually the actors.  A library that named no actors but named a director leaves the row's left half away,
    /// and a caret sent into a band that is not drawn is a caret lost off the screen.
    /// </remarks>
    private ScrollViewer? FirstBandOfThePeopleRow()
    {
      if (actorsScroller.IsEffectivelyVisible)
        return actorsScroller;
      if (directorsScroller.IsEffectivelyVisible)
        return directorsScroller;

      // A song's row, which stands in the same place the film's two bands do and is drawn instead of them.
      if (artistsScroller.IsEffectivelyVisible)
        return artistsScroller;
      if (albumArtistsScroller.IsEffectivelyVisible)
        return albumArtistsScroller;

      return composersScroller.IsEffectivelyVisible ? composersScroller : null;
    }

    /// <summary>
    /// The first band of cards this reading draws, or null when it draws none.  Which band that is depends on what
    /// the title's file and the library have to say - a title without subtitles has no subtitle band to walk into.
    /// </summary>
    private ScrollViewer? FirstBandOfTheReading()
    {
      if (audioTracksScroller.IsEffectivelyVisible)
        return audioTracksScroller;
      if (subtitleTracksScroller.IsEffectivelyVisible)
        return subtitleTracksScroller;

      return FirstBandOfThePeopleRow();
    }

    /// <summary>
    /// The band of cards at the foot of this reading, or null when it draws none.  This is the band a caret walking
    /// UP out of the collection's row of films walks back into, and - like <see cref="FirstBandOfTheReading"/> -
    /// which band that is depends on what the title's file and the library had to say.
    /// </summary>
    private ScrollViewer? LastBandOfTheReading()
    {
      if (composersScroller.IsEffectivelyVisible)
        return composersScroller;
      if (albumArtistsScroller.IsEffectivelyVisible)
        return albumArtistsScroller;
      if (artistsScroller.IsEffectivelyVisible)
        return artistsScroller;
      if (directorsScroller.IsEffectivelyVisible)
        return directorsScroller;
      if (actorsScroller.IsEffectivelyVisible)
        return actorsScroller;
      if (subtitleTracksScroller.IsEffectivelyVisible)
        return subtitleTracksScroller;

      return audioTracksScroller.IsEffectivelyVisible ? audioTracksScroller : null;
    }

    /// <summary>
    /// Whether the caret stands on a card of the reading right now.
    /// </summary>
    /// <remarks>
    /// Asked by the promise that keeps the caret waiting for the bands: a caret that already found a card is not
    /// to be moved by the arrival of a later answer, and a caret that did not - nowhere at all, because the card
    /// it was asked for could not take it yet - is.
    /// </remarks>
    private bool StandsOnACardOfTheReading()
    {
      Control? stood = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
      return stood != null && stood.Classes.Contains("trackcard");
    }

    /// <summary>
    /// How many layout rounds the caret is re-asked for while the head card of the first band is still being built.
    /// </summary>
    /// <remarks>
    /// Six rounds, because two things have to happen before there is a card to stand on and each of them costs a
    /// round of its own: the reading's own ground has to be laid out for a band to count as visible at all, and the
    /// band then has to hand out a container per track.  Few enough that a reading whose cards never arrive stops
    /// asking instead of hunting forever.
    /// </remarks>
    private const int CARET_SEATING_ROUNDS = 6;

    /// <summary>
    /// Walks a caret still waiting for its bands onto the card at the head of the reading's first band.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A band announces that it has something to show one step ahead of the panel's own work: the word that it is
    /// visible is written, and the card that carries it is built with the layout that follows.  Asking for the card
    /// in the same breath as that word therefore usually finds none, which is why the ask is repeated over the
    /// layout rounds until a card stands there to take the caret.
    /// </para>
    /// <para>
    /// A caret the viewer moved in the meantime is left where the viewer put it: once the caret stands on any card
    /// of the reading the promise is kept, and the arrival of a later answer - the people, say, after the tracks -
    /// may not pull it back to the head of the first band.
    /// </para>
    /// </remarks>
    /// <param name="reader">The reading the caret belongs to.</param>
    /// <param name="rounds">How many layout rounds the ask may still be repeated over.</param>
    private void SeatTheCaretOnTheFirstCardOfTheReading(MediaBrowserViewModel reader, int rounds)
    {
      if (!_caretWaitsForTheBands || !reader.InfoShown)
        return;

      ScrollViewer? band = FirstBandOfTheReading();
      List<Border> cards = band != null ? TrackCards(band) : new List<Border>();
      if (cards.Count > 0)
      {
        _caretWaitsForTheBands = false;
        if (!StandsOnACardOfTheReading())
          StandCaret(cards[0], null);
        return;
      }

      if (rounds <= 0)
      {
        // Nothing of the tracks or the people ever arrived, which leaves the collection's row of films as the one
        // thing this reading drew below its story - a film naming a collection while its file carries no tracks and
        // the library knows no people for it.  Asked only in the last round on purpose: while a round is left, a
        // band may still build itself, and a caret that had already taken a card would be held there by the promise
        // above.
        List<Border> films = TrackCards(collectionScroller);
        if (films.Count > 0 && !StandsOnACardOfTheReading())
        {
          _caretWaitsForTheBands = false;
          StandCaret(films[0], null);
        }
        return;
      }

      Dispatcher.UIThread.Post(
          delegate { SeatTheCaretOnTheFirstCardOfTheReading(reader, rounds - 1); },
          DispatcherPriority.Loaded);
    }

    /// <summary>
    /// How long a band takes to slide its cards along, and how far inside its own ends the stood card is held.
    /// Both are the reference's own numbers for its rows: three hundred milliseconds and a <c>ScrollMargin</c> of
    /// a hundred and thirty points at either end (<c>MediaStyles.xaml:5205</c>).
    /// </summary>
    private static readonly TimeSpan BAND_GLIDE = TimeSpan.FromMilliseconds(300);

    private const double BAND_SCROLL_MARGIN = 130.0;

    /// <summary>
    /// Sliding a band so that the card the caret stands on lies inside it with the reference's margin between the
    /// card and the ends of the band.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A scroller left to itself does bring a newly focused card into sight, but it does the minimum: the card
    /// lands flush against the end it came in at, with nothing of the picture beside it to say where the band
    /// goes on.  The reference keeps a stood item a margin's width from both ends instead, which is what makes
    /// walking a row read as a walk rather than as a card appearing at an edge.
    /// </para>
    /// <para>
    /// A band narrower than a card and two margins cannot hold the margin at both ends - a band of people cards is
    /// two cards wide - so the margin is asked for and then given up towards the middle, which leaves the card
    /// standing centred.  That is what the reference's grid band does with its four hundred points of margin on
    /// either side of a shelf that is shorter than the sum: the tile is held at the band's middle.
    /// </para>
    /// </remarks>
    private static void KeepCardInSight(ScrollViewer band, Control card)
    {
      GlideBandOntoCard(band, card);

      // A card coming into focus from a long way off also asks the band to bring it in, and that answer lands the
      // card flush against an end - during the focus itself, or at the layout that follows it.  So the margin is
      // asked for once more when the layout has had its say; the glide retargets the running slide rather than
      // restarting it, which is what keeps the whole move one smooth move.
      Dispatcher.UIThread.Post(delegate { GlideBandOntoCard(band, card); }, DispatcherPriority.Loaded);
    }

    /// <summary>One pass of the margin: writes the band's offset if the card does not already lie inside it.</summary>
    private static void GlideBandOntoCard(ScrollViewer band, Control card)
    {
      Point? at = card.TranslatePoint(new Point(0, 0), band);
      if (at == null || band.Viewport.Width <= 0.0)
        return;

      double viewport = band.Viewport.Width;
      double left = at.Value.X - band.Padding.Left;
      double right = left + card.Bounds.Width;
      double margin = Math.Max(0.0, Math.Min(BAND_SCROLL_MARGIN, (viewport - card.Bounds.Width) / 2.0));
      double offset = band.Offset.X;

      if (left < margin)
        offset -= margin - left;
      else if (right > viewport - margin)
        offset += right - (viewport - margin);

      offset = Math.Clamp(offset, 0.0, Math.Max(0.0, band.Extent.Width - viewport));
      if (Math.Abs(offset - band.Offset.X) > 0.5)
        band.Offset = new Vector(offset, band.Offset.Y);
    }

    /// <summary>
    /// Standing the caret on the card at the given place of another band, as far along it as that band has
    /// cards - asking for a place beyond its end is asking for its last card.  A band which is not on the screen
    /// (a title whose file carries no subtitles has no subtitle band at all) answers false, and the caller keeps
    /// the key's other answer.
    /// </summary>
    private static bool StandsOnTrackCard(ScrollViewer? band, int at, KeyEventArgs args)
    {
      if (band == null || !band.IsEffectivelyVisible)
        return false;

      List<Border> cards = TrackCards(band);
      if (cards.Count == 0)
        return false;

      cards[Math.Min(at, cards.Count - 1)].Focus();
      args.Handled = true;
      return true;
    }

    /// <summary>
    /// The track cards of one band, in the order they stand - which is the order a viewer walks them.
    /// </summary>
    private static List<Border> TrackCards(Visual band) =>
        band.GetVisualDescendants().OfType<Border>()
            .Where(card => card.Classes.Contains("trackcard"))
            .ToList();

    /// <summary>
    /// Whether the highlighted title stands on the edge of the shelf beyond which the screen's own menu waits:
    /// the whole first column for a grid or a wall of tiles, any row at all for a list - whose rows are one
    /// column wide, so every row touches the left edge - and any cover at all, because a cover shelf is one
    /// row laid along the bottom of the room: every tile in it touches the edge the menu waits behind. A shelf
    /// that has not laid itself out yet has one column, which is the answer that lets the very first title
    /// reach the menu, as it always did.
    /// </summary>
    private bool StandingOnTheLeadingEdge(MediaBrowserViewModel viewModel)
    {
      int standing = titlesList.SelectedIndex;
      if (standing < 0 || viewModel.Titles.Count == 0)
        return false;

      // Standing on the first cover alone was the old answer, and it was a trap: a viewer who had walked one
      // tile to the right found the key that had opened the menu a moment ago opening nothing at all, which
      // is a menu that disappears the moment the shelf is used.
      if (viewModel.IsCoverLayout)
        return true;

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
    /// Hands the window the pointer's handle to this screen's menu, or takes it back.
    /// </summary>
    /// <remarks>
    /// A shelf whose menu stands in the rail at its room's LEFT edge is opened off that edge, and the pointer's
    /// handle for it belongs to the row of marks the window keeps at its top - in front of the way back, because
    /// that is the order the two questions come in: "what is this screen's menu?" before "how do I leave?".
    /// Covers, stacks and the three dimensional row open their menu off the room's BOTTOM edge instead; their
    /// handle is drawn by the screen itself, riding up with the room, and the window's row is left holding
    /// nothing - a left-pointing arrow over a shelf whose menu lies under its covers points at an edge that has
    /// never had a menu behind it.
    /// </remarks>
    private void PushMenuMark()
    {
      if (_chromeShell == null)
        return;

      if (DataContext is not MediaBrowserViewModel browser || browser.IsCoverLayout)
      {
        _chromeShell.SetCornerMenuMark(null, null);
        return;
      }

      // The icon's name is the catalog's, and the catalog is the skin's; the window is the one wearing it.
      _chromeShell.SetCornerMenuMark("MediaMenu/" + browser.MenuMarkIcon, browser.ToggleMediaMenu);
    }

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
    private static (double Before, double After, double Milliseconds) ShelfGlide(BrowserLayout layout,
        double view = 0.0)
    {
      return layout switch
      {
        // The walked row of posters keeps the reference's band: a cover standing inside it is left where it is, and
        // one that walks out of it is carried to the edge it walked out by.
        BrowserLayout.Cover => (280.0, 260.0, 400.0),

        // A carousel holds its stood cover in the middle of the picture, which the reference reaches by the same
        // means and not by moving its covers: its ScrollMargin is half a screen at both ends, so the band narrows
        // to the middle alone and every step of the caret is answered by the wall carrying the cover back there.
        BrowserLayout.Carousel => (view / 2.0, view / 2.0, 400.0),

        // A stack is a stack on both sides of its front cover, so it wants the same thing a carousel wants: the
        // stood cover held in the middle of the picture by the wall's own travel, and the covers of both its stacks
        // standing up around it - which the panel is asked to do beside the picture, not by a band that would keep
        // the front cover away from the middle.
        BrowserLayout.Stacked => (view / 2.0, view / 2.0, 400.0),

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
    /// handed to it by the transition itself, which retargets rather than restarting.  The travel is also where
    /// the fade starts to be worn for its destination: the mask is written once, before the first frame of the
    /// travel is painted, and is not touched again until the wall stands (see AwaitScrollRest).
    /// </summary>
    private void WearShelfGlide(ScrollViewer scroller)
    {
      _glidingScroller = scroller;
      FitShelfGlide(scroller);

      // Where the wall stands as the travel takes it up; the rest watcher wants to know whether the transition
      // ever stirred (see AwaitScrollRest).
      double from = _fadeRunsSideways ? scroller.Offset.X : scroller.Offset.Y;

      _shelfTravelling = true;

      // The mask for the destination is put on before the first frame of the travel is painted, so the wall
      // never glides bare.
      UpdateShelfFade();

      // A travel that outlives its answer - the transition replaced under it, a wheel turn of the viewer's own
      // that answers nothing - must not strand the mask in "travelling" forever: at the latest when this timer
      // fires, the wall reads itself once more at where it actually stands and wears that from then on.
      _shelfTravelTimer ??= NewShelfTravelTimer();
      _shelfTravelTimer.Stop();
      _shelfTravelTimer.Start();

      // The arrival itself ends the travel as soon as the offset stops moving, which settles a finished glide
      // a good beat before the timer would and covers a wheel turn of the viewer's own as well.
      _scrollSettled = false;
      _ = AwaitScrollRest(scroller, _fadeRunsSideways, from);
    }

    /// <summary>
    /// Watches a travelling scroller until its offset stops changing, and ends the travel then.  Every frame of
    /// a running transition feeds the flag; when a beat passes without one, the wall stands - wherever it
    /// stands, which covers arrivals, interruptions and wheel turns alike.
    /// </summary>
    private async Task AwaitScrollRest(ScrollViewer scroller, bool sideways, double from)
    {
      bool beat = false;
      while (true)
      {
        try
        {
          // No ConfigureAwait(false) here: the loop reads the scroller's offset below, and a ScrollViewer is a
          // UI-thread object. Stranded on a pool thread that read throws, the fire-and-forget task faults
          // unobserved, and the finalizer drops the process over it - so the wait resumes where it started.
          await Task.Delay(TimeSpan.FromMilliseconds(TRAVEL_REST_BEAT_MILLISECONDS));
        }
        catch (Exception)
        {
          return;
        }

        if (!ReferenceEquals(scroller, _glidingScroller) || !_shelfTravelling)
          return;

        if (_scrollSettled)
        {
          // A beat that found movement: the wall is still on its way, ask again after the next one.
          _scrollSettled = false;
          beat = true;
          continue;
        }

        if (beat)
        {
          // A quiet beat after a moving one - or a wall that never stirred at all: a clock that runs no frames
          // (headless tests, a window with nothing invalidating it) starts no transition, and such a travel is
          // dead as it was born.  Either way the wall stands where it stands; end the travel and read it there.
          Dispatcher.UIThread.Post(delegate { FinishShelfTravel(); });
          return;
        }

        // A first beat without any movement yet need not mean rest - the transition can take a beat to gather
        // itself.  But a wall that has not stirred a hair's breadth from where the travel found it, when the
        // travel asked it to walk, has begun no travel at all; waiting longer asks a question the clock will
        // never answer.  Give that wall one more beat to prove itself moving, and settle it otherwise.
        double now = sideways ? scroller.Offset.X : scroller.Offset.Y;
        if (Math.Abs(now - from) < 1.0 && Math.Abs(from - _shelfGlide) > 1.0)
          beat = true;
      }
    }

    /// <summary>Set by the scroller's scroll handler on every frame it brings; the rest watcher eats it.</summary>
    private bool _scrollSettled;

    private DispatcherTimer NewShelfTravelTimer()
    {
      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background)
      {
        Interval = TimeSpan.FromMilliseconds(TRAVEL_SETTLE_MILLISECONDS)
      };
      timer.Tick += delegate
      {
        timer.Stop();
        FinishShelfTravel();
      };
      return timer;
    }

    /// <summary>
    /// Ends the travel and asks the wall once what its ends are hiding now that it stands.  A travel that was
    /// interrupted mid-flight is read at where the wall actually ended up, not at where the glide was headed.
    /// </summary>
    private void FinishShelfTravel()
    {
      if (!_shelfTravelling)
        return;

      _shelfTravelTimer?.Stop();
      _shelfTravelling = false;
      ScrollViewer? scroller = _fadedScroller;
      double standing = scroller == null
          ? _shelfGlide
          : _fadeRunsSideways ? scroller.Offset.X : scroller.Offset.Y;
      _shelfGlide = standing;

      // The mask is only ever worn for a destination, so a travel that ends is not done until the wall has
      // been asked once more at the place it actually stands - and that asking must survive a headless or
      // otherwise timer-less clock, where neither the rest watcher's beat nor the settle timer ever comes due.
      // Asking late is the one state a wall must never be left in: it stands, but wears another place's fade.
      if (Dispatcher.UIThread.CheckAccess())
        UpdateShelfFade();
      else
        Dispatcher.UIThread.Post(delegate { UpdateShelfFade(); }, DispatcherPriority.Render);
    }

    private TimeSpan _glideLength = TimeSpan.FromMilliseconds(400);

    private ScrollViewer? _glidingScroller;

    private Transitions? _wornGlide;

    /// <summary>The title whose container a thrown-away travel is waiting for.</summary>
    private TitleViewModel? _glideAwaitTitle;

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

      // One line per step of the caret, saying what the stretch since the last one cost: the tiles the panel stood
      // up in it, the passes this view spent dressing them and what they took, and what the posters took - which
      // thread they were read on is in those numbers as waiting.  The frames that carried the stretch are asked for
      // as well, so a slow step can be read against the frames it was drawn in.
      if (tries == 2 && Services.HomeTrace.Enabled)
      {
        Services.HomeTrace.Log("shelfstep",
            "index=" + titlesList.SelectedIndex + " layout=" + viewModel.Layout
            + " realized=" + _shelf_dress_tiles + " prepared=" + _shelf_prepared
            + " dress=" + _shelf_dress_passes + "x" + _shelf_dress_ms + "ms "
            + ArtworkConverter.TakePictureReadings() + " "
            + ViewModels.Artwork.TakeFetchReadings());
        Services.HomeTrace.NextFrames("shelfstep");

        _shelf_prepared = 0;
        _shelf_dress_passes = 0;
        _shelf_dress_ms = 0;
      }

      // The places a shape gives its tiles belong to the distance from the title being stood on, so the walk that
      // brings the wall to that title is also the walk that re-lays the shape.
      WearShelfShape();

      // The drawn cover row carries itself: it owns the one number the row stands at and springs to the cover the
      // caret answers to, so the wall's travel - the scroller, its band and the watch that used to re-centre it -
      // has nothing to do for this shape.  Asking it anyway would have the invisible wall glide along beside the
      // drawn row and spend the step's time on a row nobody can see.
      if (viewModel.Layout == BrowserLayout.Carousel)
        return;

      ScrollViewer? scroller = ListScroller();
      Control? stood = titlesList.SelectedIndex >= 0
          ? titlesList.ContainerFromIndex(titlesList.SelectedIndex) as Control
          : null;
      bool stood_is_realized = stood != null
          && titlesList.GetRealizedContainers().Any(container => ReferenceEquals(container, stood));

      if (scroller == null || stood == null || !stood_is_realized)
      {
        // A container that is not realized yet has no place to stand at, and the shelf is asked to move before its
        // tiles have been laid more often than not - the caret arrives with the titles, and so does this question.
        // ScrollIntoView is what brings an off-window title into the virtualized panel; ContainerFromIndex alone
        // can return a recycled shell whose bounds still belong to another part of the wall.
        if (titlesList.SelectedIndex >= 0 && titlesList.SelectedIndex < titlesList.Items.Count
            && titlesList.Items[titlesList.SelectedIndex] is TitleViewModel stood_row)
        {
          _glideAwaitTitle = stood_row;
          titlesList.ScrollIntoView(stood_row);
        }

        if (tries > 0)
        {
          Dispatcher.UIThread.Post(delegate { GlideShelfToStoodTitle(tries - 1); }, DispatcherPriority.Background);
        }

        return;
      }

      if (!WantedShelfWall(out double wanted, out bool sideways))
        return;

      double standing_now = sideways ? scroller.Offset.X : scroller.Offset.Y;
      if (Math.Abs(wanted - standing_now) < 1.0)
      {
        // The stood title already stands inside its band: there is no travel to glide and the offset is not
        // written.  The glide stays on the scroller - it is worn permanently - but with nothing written there
        // is nothing for it to animate, and a wheel notch of the viewer's own is still its own business.
        _shelfGlide = wanted;
        _shelfGlideIndex = titlesList.SelectedIndex;
        return;
      }

      AskShelfWall(scroller, wanted, sideways);
    }

    /// <summary>
    /// Writes a travel onto the wall's scroller, and only when the wall has not already been asked for that travel.
    /// The wall wears a glide, and a glide is restarted by EVERY write to the offset it animates - so a second
    /// asking of the same target, taken while the wall is still on its way and therefore standing nowhere near the
    /// number asked for, begins the travel again from half way along.  Measured, one step of the caret asked the
    /// same target eleven times over the container passes that step brings with it, and the wall crept a quarter of
    /// its length and stopped: a stood cover standing beside the middle of the picture, and a step that reads as a
    /// stutter rather than a glide.
    /// </summary>
    private void AskShelfWall(ScrollViewer scroller, double wanted, bool sideways)
    {
      int index = titlesList.SelectedIndex;
      bool already_asked = _shelfGlideIndex == index && Math.Abs(_shelfGlide - wanted) < 0.5;
      _shelfGlide = wanted;
      _shelfGlideIndex = index;

      if (already_asked)
        return;

      WearShelfGlide(scroller);
      scroller.Offset = sideways
          ? new Vector(wanted, scroller.Offset.Y)
          : new Vector(scroller.Offset.X, wanted);
    }

    /// <summary>
    /// Where the wall has to stand for the cover being stood on to answer to its band, read off the wall as it is
    /// laid out now.  Asked without dressing a row, so that the question put after a layout pass costs one reading
    /// of the stood container instead of a walk over every cover of the row - and asked out of the same band the
    /// glide is written with, so that the two can never disagree about where the middle of the picture is.
    /// </summary>
    private bool WantedShelfWall(out double wanted, out bool sideways)
    {
      wanted = 0.0;
      sideways = false;

      if (!(DataContext is MediaBrowserViewModel viewModel))
        return false;

      ScrollViewer? scroller = ListScroller();
      if (scroller == null || titlesList.SelectedIndex < 0)
        return false;
      if (titlesList.ContainerFromIndex(titlesList.SelectedIndex) is not Control stood)
        return false;
      if (!titlesList.GetRealizedContainers().Any(container => ReferenceEquals(container, stood)))
        return false;

      Point? at = stood.TranslatePoint(new Point(), scroller);
      if (at == null)
        return false;

      sideways = viewModel.IsCoverLayout;
      double view = sideways ? scroller.Viewport.Width : scroller.Viewport.Height;
      double extent = sideways ? scroller.Extent.Width : scroller.Extent.Height;
      double start = sideways ? at.Value.X : at.Value.Y;
      double end = start + (sideways ? stood.Bounds.Width : stood.Bounds.Height);

      // The band, and the middle of the viewport belongs to it: a shelf of a window smaller than the reference's
      // television cannot hold four hundred points at either end of a tile, and a band that then hung its tile
      // below the bottom of a small window would be a wall that walks its stood title off the screen.
      (double before, double after, _) = ShelfGlide(viewModel.Layout, view);
      double low = Math.Min(before, view / 2);
      double high = Math.Max(low, Math.Max(view - after, view / 2));
      wanted = sideways ? scroller.Offset.X : scroller.Offset.Y;

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
      return true;
    }

    /// <summary>
    /// Watches one step of the caret until the wall has come to rest in its band.  The list answers its own
    /// selection as well, by the smallest nudge that shows the tile, and that answer lands AFTER the glide has been
    /// asked for - leaving the stood cover standing beside the middle of the picture, which is what the viewer reads
    /// as a row that is bigger on one side than on the other.  A watch of a few posted questions cannot catch that:
    /// the wall wears a glide, so it is on its way for the length of that glide, and only a wall that has come to
    /// REST away from its band has been moved by somebody else.  Hence a short timer, and hence a wall that is
    /// asked for nothing while it is honestly travelling.
    /// </summary>
    private void WatchShelfWallHome(int stood_on_index)
    {
      _glide_check_index = stood_on_index;
      _glide_check_left = 20;
      _shelfWallSeen = double.NaN;

      if (_glide_check == null)
      {
        _glide_check = new DispatcherTimer(DispatcherPriority.Background)
        {
          Interval = TimeSpan.FromMilliseconds(60)
        };
      }

      _glide_check.Tick -= CheckShelfWallHome;
      _glide_check.Tick += CheckShelfWallHome;
      _glide_check.Start();
    }

    /// <summary>One look of the watch: has the wall rested, and if so, is it standing where its band wants it?</summary>
    /// <remarks>
    /// Deliberately cheap per look - one reading of the stood container and one of the scroller, never a pass over
    /// the row - because a step of the caret walks the wall through a dozen container passes of its own.
    /// </remarks>
    private void CheckShelfWallHome(object? sender, EventArgs e)
    {
      if (_glide_check == null || titlesList == null || _glide_check_index != titlesList.SelectedIndex
          || --_glide_check_left < 0)
      {
        _glide_check?.Stop();
        return;
      }

      if (!WantedShelfWall(out double wanted, out bool sideways))
        return;

      ScrollViewer? scroller = ListScroller();
      if (scroller == null)
      {
        _glide_check.Stop();
        return;
      }

      double standing_now = sideways ? scroller.Offset.X : scroller.Offset.Y;
      if (Math.Abs(standing_now - wanted) < 1.0)
      {
        _glide_check.Stop();
        return;
      }

      // A wall that is STILL TRAVELLING is corrected at once, without waiting for it to rest.  The offset is worn
      // by a transition, and a transition handed a new number retargets - it does not restart - so a correction
      // written during the travel is woven into the same movement and is never seen.  Waiting for the rest first,
      // which is what this watch did, lands the same correction a beat AFTER the wall has stood, and that second
      // movement is what the viewer reported as a re-centring jerk at every step.
      bool resting = !double.IsNaN(_shelfWallSeen) && Math.Abs(_shelfWallSeen - standing_now) < 0.5;
      _shelfWallSeen = standing_now;
      if (!resting && !_shelfTravelling)
        return;

      // The travel the wall was last asked for is no longer the one it is standing at, so the asking may write.
      _shelfGlideIndex = -1;
      AskShelfWall(scroller, wanted, sideways);
    }

    /// <summary>
    /// Where along its travel axis the shelf was last asked to stand. What the scroller itself calls its offset is
    /// the place the glide has got to, which while a wall is travelling is nowhere near the place it was asked
    /// for; a test and a render probe both want the answer to "where is the wall being taken to".
    /// </summary>
    internal double ShelfGlideOffset => _shelfGlide;

    private double _shelfGlide;

    /// <summary>The title the wall's last asked travel belongs to.</summary>
    private int _shelfGlideIndex = -1;

    /// <summary>Where the wall stood when the re-centring last looked at it, to tell a travel from a rest.</summary>
    private double _shelfWallSeen = double.NaN;

    /// <summary>Timer that watches one step of the caret until the wall has come to rest in its band.</summary>
    private DispatcherTimer? _glide_check;

    /// <summary>The title the wall home is being watched for.</summary>
    private int _glide_check_index = -1;

    /// <summary>How many more looks the watch of one step takes before it leaves the wall alone.</summary>
    private int _glide_check_left;

    /// <summary>The frame this file draws for a shape the worn skin does not answer.</summary>
    /// <summary>
    /// The frame the wall wears: the skin's tile frame for the titles, and a divider for the plates a grouping
    /// stands in the row.  A dispatcher rather than a third template, because the wall owns exactly one
    /// <c>ItemTemplate</c> and the grouping's plates share that wall with the titles - and because the titles'
    /// answer must stay the worn skin's business, untouched, whether or not anything is grouped.
    /// </summary>
    /// <remarks>
    /// The plate says its word in white and opacity, like everything else the shelf lights: it is not a title,
    /// takes no caret and wears no selection, so it has no second state and nothing to say in colour. Each shape
    /// of the wall gets the divider that shape can carry: a headline over a strip of rows, a standing card in a
    /// wall of tiles, and a book's spine among covers - three readings of the same one sentence, "here a group
    /// of the shelf begins".
    /// </remarks>
    private sealed class ShelfFrameTemplate : Avalonia.Controls.Templates.IRecyclingDataTemplate
    {
      private readonly Avalonia.Controls.Templates.IDataTemplate _titles;
      private readonly BrowserLayout _layout;

      // The gap a tile leaves around its face, read from the FIRST tile the worn frame ever builds. A plate may
      // not change the spacing of a wall it is dropped into, and every wall in this front end lays its tiles
      // uniformly - so the tiles themselves say what their gap is, and the plate wears exactly that. It is read
      // once, the first time the frame answers a title, and it then belongs to this frame: the dispatcher is
      // rebuilt whenever the frame or the picture changes, and a fresh dispatcher asks the fresh frame afresh.
      private Thickness? _tile_margin;
      private bool _margin_learned;

      public ShelfFrameTemplate(Avalonia.Controls.Templates.IDataTemplate titles, BrowserLayout layout)
      {
        _titles = titles;
        _layout = layout;
      }

      public bool Match(object? data)
      {
        // The wall lays both kinds of item through this one door; Build answers the difference.
        return data is GroupHeaderRow || _titles.Match(data);
      }

      public Control Build(object? data)
      {
        if (data is GroupHeaderRow plate)
          return BuildPlate(plate);
        Control tile = _titles.Build(data);
        LearnMargin(tile);
        return tile;
      }

      // What the worn frame could do with a recycled shell it can still do: the titles pass straight through to
      // it, unchanged. A plate answers for itself only - the card that says "Action" cannot be told to say
      // "Drama" in its sleep - so a plate always arrives as its own fresh card. Saying nothing of recycling at
      // all would have taken the frame's own recycling away from every tile on the wall.
      public Control? Build(object? data, Control? existing)
      {
        if (data is GroupHeaderRow plate)
          return BuildPlate(plate);

        Control? tile = _titles is Avalonia.Controls.Templates.IRecyclingDataTemplate recycling
            ? recycling.Build(data, existing)
            : _titles.Build(data);
        if (tile != null)
          LearnMargin(tile);
        return tile;
      }

      private Control BuildPlate(GroupHeaderRow plate)
      {
        if (_layout == BrowserLayout.List)
          return RowPlate(plate);
        if (_layout is BrowserLayout.Cover or BrowserLayout.Stacked or BrowserLayout.Carousel)
          return SpinePlate(plate);
        return CardPlate(plate);
      }

      /// <summary>
      /// The first thing the worn frame builds for a title carries the gap every tile of this wall leaves around
      /// its face - the templates hang that gap on their root (a poster breathes 8,8,8,14, a banner 8,6,8,10, the
      /// built-in frame 8 on all sides), and every wall lays its tiles uniformly. Once learned, the gap belongs to
      /// the wall, and every plate wears it, so the spacing between media cannot change when a grouping stands in
      /// it. Plates built before the first tile (a group's headline always leads its tiles) are remembered and
      /// dressed as soon as the answer arrives.
      /// </summary>
      private readonly List<WeakReference<Border>> _plates_awaiting_margin = new List<WeakReference<Border>>();

      private void LearnMargin(Control tile)
      {
        if (_margin_learned)
          return;

        _margin_learned = true;
        _tile_margin = tile.Margin;

        foreach (WeakReference<Border> held in _plates_awaiting_margin)
        {
          if (held.TryGetTarget(out Border? awaiting))
            awaiting.Margin = _tile_margin.Value;
        }
        _plates_awaiting_margin.Clear();
      }

      /// <summary>A headline: the word, its count beside it, and a hairline under the line.</summary>
      private static Control RowPlate(GroupHeaderRow plate)
      {
        StackPanel line = new StackPanel
        {
          Orientation = Orientation.Horizontal,
          Margin = new Thickness(2, 16, 2, 4)
        };

        line.Children.Add(new TextBlock
        {
          Text = plate.Label,
          FontSize = 24,
          FontWeight = FontWeight.SemiBold,
          Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
          VerticalAlignment = VerticalAlignment.Center
        });

        if (plate.CountWord.Length > 0)
        {
          line.Children.Add(new TextBlock
          {
            Text = plate.CountWord,
            FontSize = 18,
            Opacity = 0.55,
            Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
            Margin = new Thickness(10, 3, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
          });
        }

        return new StackPanel
        {
          Children =
          {
            line,
            new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#26FFFFFF")) }
          }
        };
      }

      /// <summary>
      /// A card the measure of the tile beside it - whatever that is this time, poster, thumbnail or banner - with
      /// its word standing in the middle. The card names no size of its own: the wall measures it with the cell its
      /// tiles just made and arranges it into that cell, so a headline is as big as a tile and no bigger. What it
      /// does copy from the tiles is their margin, so the gaps of the wall stay exactly what they were, and their
      /// face - the same shadow and plate the media tiles wear.
      /// </summary>
      private Control CardPlate(GroupHeaderRow plate)
      {
        StackPanel inside = new StackPanel
        {
          HorizontalAlignment = HorizontalAlignment.Center,
          VerticalAlignment = VerticalAlignment.Center,
          Margin = new Thickness(12)
        };

        inside.Children.Add(new TextBlock
        {
          Text = plate.Label,
          FontSize = 22,
          FontWeight = FontWeight.SemiBold,
          TextAlignment = TextAlignment.Center,
          TextWrapping = TextWrapping.Wrap,
          Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF"))
        });

        if (plate.CountWord.Length > 0)
        {
          inside.Children.Add(new TextBlock
          {
            Text = plate.CountWord,
            FontSize = 16,
            Opacity = 0.55,
            Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
          });
        }

        Border face = new Border
        {
          Classes = { "home-tile-face" },
          Background = new SolidColorBrush(Color.Parse("#222222")),
          ClipToBounds = true,
          Child = inside
        };

        Border outer = new Border
        {
          Classes = { "home-tile-shadow" },
          // Until the wall's first tile has answered its gap, breathe like the wall most walls are.
          Margin = _tile_margin ?? new Thickness(8),
          Child = face
        };

        if (!_margin_learned)
          _plates_awaiting_margin.Add(new WeakReference<Border>(outer));

        return outer;
      }

      /// <summary>
      /// A book's spine among covers: the word read from top to bottom, one letter standing on the next, in a
      /// card the width and height of a cover - which is exactly how a divider between albums says which shelf
      /// a run of books belongs to. The letters are stacked by hand rather than wrapped, because a long value
      /// must not climb out of the cover's height - the word's size gives way instead, down to what a viewer
      /// can still read from the sofa.
      /// </summary>
      private static Control SpinePlate(GroupHeaderRow plate)
      {
        string stacked = string.Join("\n", plate.Label.Select(letter => letter.ToString()));
        int lines = plate.Label.Length;
        double size = lines <= 1 ? 30.0 : Math.Clamp(320.0 / lines, 15.0, 28.0);

        TextBlock word = new TextBlock
        {
          Text = stacked,
          FontSize = size,
          FontWeight = FontWeight.SemiBold,
          TextAlignment = TextAlignment.Center,
          Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
          HorizontalAlignment = HorizontalAlignment.Center,
          VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
          Width = 260,
          Height = 410,
          Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
          BorderBrush = new SolidColorBrush(Color.Parse("#26FFFFFF")),
          BorderThickness = new Thickness(1),
          CornerRadius = new CornerRadius(10),
          Child = word
        };
      }
    }

    private IDataTemplate BuiltinTemplate(BrowserLayout layout)
    {
      string key = layout switch
      {
        BrowserLayout.Grid => "BuiltinTilesTemplate",
        // The shape drawn by our own hand wears a frame of its own under the row: the covers are painted, so
        // the containers underneath ask for no picture (see BuiltinCoversBlindTemplate).  The two shapes
        // besides it put the same covers differently but wear the same frame; only the class the list wears
        // changes, and the class is what leans the tiles.
        BrowserLayout.Carousel => "BuiltinCoversBlindTemplate",
        BrowserLayout.Cover or BrowserLayout.Stacked => "BuiltinCoversTemplate",
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
