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
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The weather screen: the home wall's bands of cards stood in the room of a screen, side by side and
  /// scrolling sideways exactly as the home wall stands and scrolls them, walked the way the wall is walked.
  /// The cards wear the skin's own card classes, so their states - the quiet rest, the standing light, the
  /// white edge - are the styles.xaml's answer and are written nowhere here.
  /// </summary>
  /// <remarks>
  /// <para>
  /// What only a screen can add: its own menu, standing in the two hundred points off the left edge exactly
  /// as the news screen's does, carrying the three commands the home's weather entry wore as shortcut tiles -
  /// the place to read, the settings behind a door, and the refresh. Left at the wall's leading cards opens
  /// it; Right, Escape, or the media-menu key shuts it.
  /// </para>
  /// <para>
  /// The walk over the wall is the reference's own: the four <c>Located…</c> predicates translated in
  /// <see cref="SkinFocusWalk"/>, run over the realized cards, which is how the skin engine answers the arrow
  /// keys for every wall it lays out. An arrow that finds no card to its side lets the wall scroll instead,
  /// and Left finding nothing is the menu. The wall glides after the stood card by the shelf's band law, and
  /// its ends fade only while content stands out of the picture.
  /// </para>
  /// </remarks>
  public partial class WeatherView : UserControl, IWorkflowScreen
  {
    private readonly WeatherViewModel _weather;
    private readonly WorkflowManager? _workflow;
    private readonly Func<string, WorkflowFrame>? _screenFactory;

    /// <summary>Whether the room is standing open for the menu right now.</summary>
    private bool _menuShown;

    /// <summary>The window this screen is standing in, for as long as it stands there (see the header stow).</summary>
    private ShellWindow? _shell;

    /// <summary>
    /// The card the keyboard last stood on. The shell's net asks where this screen's keys start when it
    /// notices the window standing on none, and the answer is where the viewer was, not the first focusable
    /// control in tree order - which on this screen is the menu rail standing behind the left edge.
    /// </summary>
    private Button? _stoodCard;

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with nowhere to read from shows.
    /// </summary>
    public WeatherView()
        : this(new WeatherViewModel())
    {
    }

    /// <param name="weather">What to show: the watched places and the bands of the one being spoken for.</param>
    /// <param name="workflow">
    /// Where the menu's place row raises its dialog and its setup row pushes the settings, or null for a
    /// screen that cannot leave its place - the designer's and any caller that has no workflow to walk.
    /// </param>
    /// <param name="screenFactory">
    /// The builder of named screens, the same one the home's tiles ask through; without it the setup row does
    /// not sit on the menu at all, because a button that opens nothing is worse than no button.
    /// </param>
    public WeatherView(WeatherViewModel weather, WorkflowManager? workflow = null,
        Func<string, WorkflowFrame>? screenFactory = null)
    {
      DataContext = weather;
      _weather = weather;
      _workflow = workflow;
      _screenFactory = screenFactory;
      InitializeComponent();

      // A row of the menu that a pointer rests on and leaves is a row nobody pressed; a row the pointer
      // pressed once is the command. So the menu acts on selection only from a pointer, and the keyboard
      // acts on Enter - the same split the library rail and the news menu answer with.
      weatherMenuList.AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (args.GetCurrentPoint(weatherMenuList).Properties.IsLeftButtonPressed
                && weatherMenuList.SelectedItem is WeatherMenuEntry pressed)
            {
              args.Handled = true;
              RunMenuEntry(pressed);
            }
          },
          RoutingStrategies.Tunnel);

      // Every fresh reading rebuilds the bands, and the card the keyboard stood on goes with the old ones.
      // The window can only hear of a lost focus from a control that still stands in a tree - the card the
      // rebuild pulled out cannot speak - so the wall that rebuilds itself hands the keyboard back to itself.
      _weather.Bands.CollectionChanged += (_, _) => ReclaimWallKeyboard();

      AttachedToVisualTree += (_, _) =>
      {
        _weather.ScreenOpened();
        WireScroller();
        // The weather room wears no head line over it - no word, no marks, no day, no clock. The screen asks
        // the window to put that line away for the length of its visit and to bring it back on the way out,
        // the same way the news screen and the photograph screen hand the television back.
        (_shell = TopLevel.GetTopLevel(this) as ShellWindow)?.SetChromeHeaderStowed(true);
      };
      DetachedFromVisualTree += (_, _) =>
      {
        _shell?.SetChromeHeaderStowed(false);
        _shell = null;
      };

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            Control? standing = TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement() as Control;

            if (weatherMenuList.IsKeyboardFocusWithin)
            {
              // The menu has its own answers while it is open: Enter runs the row the caret stands on,
              // Right walks the caret back onto the wall and slides the menu away, and Left - which has
              // nothing to the left of a rail - is swallowed rather than handed to the wall, which would
              // otherwise pull the caret off the menu and leave the rail standing open with nobody on it.
              if (args.Key == Key.Enter || args.Key == Key.Return)
              {
                if (weatherMenuList.SelectedItem is WeatherMenuEntry chosen)
                {
                  args.Handled = true;
                  RunMenuEntry(chosen);
                }
              }
              else if (args.Key == Key.Right)
              {
                args.Handled = true;
                HideWeatherMenu();
                StandOnWall();
              }
              else if (args.Key == Key.Left)
              {
                args.Handled = true;
              }
              else if (args.Key == Key.Escape)
              {
                // The reference's own answer: the escape of a whole screen first puts away a menu that
                // stands open on it (master_mediamenu.xaml:402); only an already closed menu lets the
                // escape leave the screen.
                args.Handled = true;
                HideWeatherMenu();
                StandOnWall();
              }
              else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args.Key))
              {
                args.Handled = true;
                HideWeatherMenu();
                StandOnWall();
              }

              return;
            }

            if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args.Key))
            {
              // The reference's other half: the remote's info key (Apps, or the letter I) toggles the
              // screen's menu from wherever the caret stands (master_mediamenu.xaml:401).
              args.Handled = true;
              if (_menuShown)
              {
                HideWeatherMenu();
                StandOnWall();
              }
              else
              {
                ShowWeatherMenu();
              }

              return;
            }

            // A bare wall that holds the keyboard - dropped there by a click on its empty ground or by the
            // window handing its keys back - answers an arrow key by scrolling and shows no frame. The arrow
            // is therefore preceded by the step onto a card; the wall then walks from a caret anybody can
            // see, and this same key press is answered from there.
            if (ReferenceEquals(standing, wallScroller) && IsArrow(args.Key))
            {
              StandOnWall();
              standing = TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement() as Control;
            }

            if (!IsArrow(args.Key) || standing == null)
              return;

            Button? card = StandingCard(standing);
            if (card == null)
              return;

            Button? next = NextCard(card, args.Key);
            if (next != null)
            {
              args.Handled = true;
              next.Focus();
              return;
            }

            if (args.Key == Key.Left)
            {
              // Nothing stood to the left of this card, and the wall is the leftmost thing the room holds -
              // so Left here is the wish to see the screen's own commands, the same door the news screen's
              // papers and the library's list give every one of their rows.
              args.Handled = true;
              ShowWeatherMenu();
            }
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Where this screen's keys start when the shell notices the window standing on no focus at all. The
    /// card the viewer last stood on, if it is still standing; the wall's scroller otherwise.
    /// </summary>
    public Control? KeysStartAt()
    {
      return _stoodCard != null && _stoodCard.GetVisualParent() != null
          ? _stoodCard
          : (Control?) wallScroller;
    }

    /// <summary>
    /// Runs every time the workflow shows this screen: the places come off their file again and a brief older
    /// than the wall's quarter is read again (both in the model's own words), and the keyboard is handed to
    /// the card the viewer last stood on, or to the first card of the visit. See IWorkflowScreen.
    /// </summary>
    public void OnScreenActivated()
    {
      _weather.ScreenOpened();
      WireScroller();
      StandOnWall();
    }

    /// <summary>
    /// Runs once, when the frame leaves the workflow for good.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenClosed()
    {
      _weather.Dispose();
    }

    /// <summary>
    /// A card took the keyboard: it is now the card the screen remembers, and the wall slides until the card
    /// stands inside the band. The hand-written arrow walk and the platform's own focus move both end here.
    /// </summary>
    private void OnWallCardGotFocus(object? sender, RoutedEventArgs args)
    {
      if (sender is not Button card)
        return;

      _stoodCard = card;
      GlideCardIntoView(card);
    }

    // ----------------------------------------------------------------------------------------------- walking

    private static bool IsArrow(Key key)
    {
      return key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right;
    }

    /// <summary>
    /// The card the keyboard stands on, whether the focus landed on the card itself or on something it
    /// carries. Not a card at all - the bare scroller, the rail - answers no arrow.
    /// </summary>
    private static Button? StandingCard(Control? standing)
    {
      if (standing is Button { Classes: _ } button
          && button.Classes.Contains("nereus-card-tile"))
        return button;

      return standing?.GetVisualAncestors().OfType<Button>()
          .FirstOrDefault(card => card.Classes.Contains("nereus-card-tile"));
    }

    /// <summary>
    /// The card that lies next to <paramref name="from"/> in <paramref name="direction"/>, measured over
    /// every realized card of the wall in the wall's own numbers - the reference's
    /// <c>FindNextFocusElement</c> run over what the wall has dressed. A card the wall has not realized is
    /// not a candidate; the arrow that finds nothing then lets the wall scroll, and the cards that come into
    /// the picture answer the next press.
    /// </summary>
    private Button? NextCard(Button from, Key direction)
    {
      List<Button> cards = bandsList.GetVisualDescendants()
          .OfType<Button>()
          .Where(card => card.Classes.Contains("nereus-card-tile") && card.Focusable && card.IsVisible)
          .ToList();

      return SkinFocusWalk.Next(cards, card => BoundsIn(card, wallScroller),
          BoundsIn(from, wallScroller), direction);
    }

    private static Rect BoundsIn(Control card, Control relativeTo)
    {
      Point? at = card.TranslatePoint(new Point(), relativeTo);
      return at == null
          ? default
          : new Rect(at.Value, card.Bounds.Size);
    }

    /// <summary>
    /// Hands the keyboard to the card the viewer last stood on, or to the first card of the wall when this
    /// is the visit's first step. A wall whose cards are not dressed yet - the bands arrive with the read -
    /// is asked for again on the next layout pass, a bounded few times, and the scroller is the fallback for
    /// the moment before any card exists.
    /// </summary>
    private void StandOnWall(int tries = 6)
    {
      if (StandOnACard())
        return;

      wallScroller.Focus();
      if (tries <= 0)
        return;
      Dispatcher.UIThread.Post(
          delegate
          {
            if (wallScroller.IsKeyboardFocusWithin)
              StandOnWall(tries - 1);
          },
          DispatcherPriority.Loaded);
    }

    private bool StandOnACard()
    {
      List<Button> cards = RealizedCards();
      Button? target = _stoodCard != null && cards.Contains(_stoodCard) ? _stoodCard : cards.FirstOrDefault();
      return target != null && target.Focus();
    }

    /// <summary>
    /// Puts the keyboard back on the wall after a rebuild of the bands took the card it stood on away - and
    /// only when the keyboard stands on nobody: a dialog asked over this wall, another screen's control or a
    /// window already holding a fresh focus all keep what they have, so this can never start a focus fight.
    /// The shell notices an empty focus through the window's own lost-focus event, which a card the rebuild
    /// pulled out of the tree can no longer raise; a wall that rebuilds itself therefore keeps its own watch.
    /// </summary>
    private void ReclaimWallKeyboard()
    {
      void Ask()
      {
        if (_workflow != null && !ReferenceEquals(_workflow.Current?.View, this))
          return; // the viewer walked on while the wall was rebuilding; the newest stop decides from here

        TopLevel? top = TopLevel.GetTopLevel(this);
        if (top == null)
          return;

        Control? held = top.FocusManager.GetFocusedElement() as Control;
        // The keyboard stands on nobody if it came down with the old wall, if it was never placed,
        // or if it sits on the scroller - which is only ever a parking place, never an answer.
        // A control another window holds is by definition not this window's focused element, so
        // standing there already means somebody answers - the wall keeps its hands off.
        if (held != null && held != top && !ReferenceEquals(held, wallScroller))
          return;

        StandOnWall();
      }

      Dispatcher.UIThread.Post(Ask, DispatcherPriority.Loaded);
    }

    private List<Button> RealizedCards()
    {
      return bandsList.GetVisualDescendants()
          .OfType<Button>()
          .Where(card => card.Classes.Contains("nereus-card-tile") && card.Focusable && card.IsVisible)
          .ToList();
    }

    // ------------------------------------------------------------------------------------------------ menu

    /// <summary>
    /// Builds the rows this screen has to offer and slides the room to make room for them - the menu does
    /// not appear, the room makes room for it, which is the whole of the reference's opening animation. The
    /// rows are the three commands the home's weather entry carried as shortcut tiles; the settings row only
    /// sits on the menu when this application can answer that walk at all.
    /// </summary>
    private void ShowWeatherMenu(int caretRow = 0)
    {
      List<WeatherMenuEntry> entries = new List<WeatherMenuEntry>
      {
        // The white bar of the row that is the screen's standing answer: the wall always speaks for a place,
        // and the place row names which one - the same claim the layout row makes on the library's rail.
        new WeatherMenuEntry("location", "Location", true, WeatherMenuCommand.ChooseLocation),
      };
      if (_workflow != null && _screenFactory != null)
        entries.Add(new WeatherMenuEntry("setup", "Setup", false, WeatherMenuCommand.OpenSetup));
      entries.Add(new WeatherMenuEntry("refresh", "Refresh", false, WeatherMenuCommand.Refresh));

      weatherMenuList.ItemsSource = entries;
      weatherMenuList.SelectedIndex = Math.Clamp(caretRow, 0, entries.Count - 1);
      _menuShown = true;
      ((TranslateTransform)room.RenderTransform).X = 200;
      StandCaretOnMenu();
    }

    /// <summary>Slides the menu back off the edge of the screen, where it stands until the next Left.</summary>
    private void HideWeatherMenu()
    {
      _menuShown = false;
      ((TranslateTransform)room.RenderTransform).X = 0;
      // A closed rail must never keep the keyboard: a caret behind a wall walks an invisible list, which is
      // what the news screen measured live as "die Feeds haben keinen Fokusrahmen". Closing the rail
      // therefore carries the caret onto the wall it came from.
      if (weatherMenuList.IsKeyboardFocusWithin)
        StandOnWall();
    }

    /// <summary>
    /// Runs the command of the row that was pressed. Nothing here keeps the menu open - every row leads the
    /// caret somewhere the menu has no business standing over.
    /// </summary>
    private void RunMenuEntry(WeatherMenuEntry entry)
    {
      switch (entry.Command)
      {
        case WeatherMenuCommand.ChooseLocation:
          // The picker is a dialog, and the dialog answers to this screen's own model - the wall that is
          // standing here is the one whose place changes. So the frame carries this screen's view model,
          // exactly like the wake pushes its household wall with its own.
          HideWeatherMenu();
          _workflow?.Push(new WorkflowFrame("weatherplaces", "Weather places", _weather));
          return;

        case WeatherMenuCommand.OpenSetup:
          HideWeatherMenu();
          WorkflowFrame? frame = _screenFactory?.Invoke("settings");
          if (frame != null)
            _workflow?.Push(frame);
          return;

        default:
          HideWeatherMenu();
          _weather.RefreshCommand.Execute(null);
          StandOnWall();
          return;
      }
    }

    /// <summary>
    /// Hands the keyboard to the row the menu picked rather than to the bare rail - the same law the news
    /// menu stands by, so the first arrow of a visit walks a row rather than a page.
    /// </summary>
    private void StandCaretOnMenu(int tries = 6)
    {
      if (weatherMenuList.SelectedIndex >= 0
          && weatherMenuList.ContainerFromIndex(weatherMenuList.SelectedIndex) is Control row
          && row.Focus())
        return;

      weatherMenuList.Focus();
      if (tries <= 0)
        return;
      Dispatcher.UIThread.Post(
          delegate
          {
            if (weatherMenuList.IsKeyboardFocusWithin)
              StandCaretOnMenu(tries - 1);
          },
          DispatcherPriority.Loaded);
    }

    // -------------------------------------------------------------------------------------------- scrolling

    /// <summary>
    /// Gives the wall its edge fade and its gliding wheel, once. The scroller stands in the open here rather
    /// than inside a list's template, but the moment it can be measured is still a layout pass away, so the
    /// wiring is asked for again on the next pass until the viewport has a width.
    /// </summary>
    private bool _scrollerWired;

    private void WireScroller()
    {
      if (_scrollerWired)
        return;

      Dispatcher.UIThread.Post(delegate
      {
        if (wallScroller.Viewport.Width <= 0)
          return;

        _scrollerWired = true;
        _wallFade ??= new WallFade(wallScroller);
        InstallWallGlide(wallScroller);
      }, DispatcherPriority.Loaded);
    }

    private static void InstallWallGlide(ScrollViewer scroller)
    {
      if (scroller.Transitions?.Any(t => t is VectorTransition) == true)
        return;

      Transitions glide = new Transitions();
      glide.Add(new VectorTransition
      {
        Property = ScrollViewer.OffsetProperty,
        Duration = TimeSpan.FromMilliseconds(400)
      });
      scroller.Transitions = glide;
    }

    private WallFade? _wallFade;

    /// <summary>
    /// How far inside the window a stood card should be held: the wall's own ScrollMargin of four hundred
    /// points (MediaStyles.xaml), the number the library wall and the news columns glide by. A band this
    /// deep holds the stood card at the window's middle and keeps every other card inside the picture, which
    /// also leaves the platform's bring-into-view nothing to cut - the wall's own glide is then the only hand
    /// that moves it.
    /// </summary>
    private const double WALL_BAND = 400.0;

    /// <summary>
    /// Stands the wall so the card the keyboard is on sits inside the band, by the shelf's law: a card
    /// already inside the band leaves the wall still, a card wider than the band is stood at its middle,
    /// and the move is one local write to the offset - which is what the scroller's transition turns into a
    /// glide. The platform's bring-into-view alone is not enough: measured live on the shelf it walks by
    /// cuts, and a television viewer calls that a stutter.
    /// </summary>
    private void GlideCardIntoView(Control card, int tries = 2)
    {
      if (wallScroller.Viewport.Width <= 0)
      {
        if (tries > 0)
        {
          Dispatcher.UIThread.Post(delegate { GlideCardIntoView(card, tries - 1); },
              DispatcherPriority.Loaded);
        }

        return;
      }

      Point? at = card.TranslatePoint(new Point(), wallScroller);
      if (at == null)
        return;

      // Absolute, not relative: the offset is mid-travel while a glide runs, and a target built from a
      // moving start drifts with it - measured live as the wall bouncing twice over one standing card.
      double view = wallScroller.Viewport.Width;
      double extent = wallScroller.Extent.Width;
      double abs = at.Value.X + wallScroller.Offset.X;
      double width = card.Bounds.Width;
      double low = Math.Min(WALL_BAND, view / 2);
      double high = Math.Max(low, Math.Max(view - WALL_BAND, view / 2));
      double wanted = wallScroller.Offset.X;

      if (width > high - low)
        wanted = abs + width / 2 - (low + high) / 2;
      else if (abs - wanted < low)
        wanted = abs - low;
      else if (abs - wanted + width > high)
        wanted = abs + width - high;
      else
        return; // The card stands inside the band already: the wall stays still, as the shelf's does.

      wanted = Math.Clamp(wanted, 0, Math.Max(0, extent - view));
      if (Math.Abs(wanted - wallScroller.Offset.X) < 1.0)
        return;

      wallScroller.Offset = new Vector(wanted, wallScroller.Offset.Y);
    }

    /// <summary>
    /// The wall's edge fade: the shelf's rule, written for a wall that only runs sideways. The end of the
    /// wall fades exactly as deeply as that end has cards out of the picture - nothing out is no fade at all
    /// and no mask is worn, one fade zone's worth out is the full fade - and the reading is taken off a
    /// ladder of eight steps, so a wall travelling through its middle rewrites nothing. The first band stands
    /// free while nothing has scrolled past it, which is what the rule demands and what the reference's own
    /// wall - four opaque stops and a black ground, one honest "no fade" - allowed.
    /// </summary>
    private sealed class WallFade
    {
      private readonly ScrollViewer _scroller;
      private LinearGradientBrush? _brush;
      private double _before = UNREAD;
      private double _after = UNREAD;

      public WallFade(ScrollViewer scroller)
      {
        _scroller = scroller;
        scroller.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { Update(); });
        Update();
      }

      /// <summary>How far in from an end of the wall its cards dissolve once a whole zone has gone out.</summary>
      private const double ZONE = 0.12;

      /// <summary>How finely the depth an end fades is read: eight steps between none and all.</summary>
      private const double STEPS = 8;

      /// <summary>A depth no end has been asked about yet, so that the first reading is always written.</summary>
      private const double UNREAD = -1;

      private void Update()
      {
        double view = _scroller.Viewport.Width;
        if (view <= 0)
          return;

        double extent = _scroller.Extent.Width;
        double offset = _scroller.Offset.X;
        double zone = ZONE * view;
        double before = Depth(offset, zone);
        double after = Depth(extent - view - offset, zone);
        if (before == _before && after == _after)
          return;

        _before = before;
        _after = after;

        // A wall that has left nothing out of the picture at either end wears no mask at all, which is both
        // the honest answer and the cheap one.
        if (before == 0 && after == 0)
        {
          _scroller.OpacityMask = null;
          return;
        }

        LinearGradientBrush fade = _brush ??= NewFade();
        fade.GradientStops[0].Color = Colour(before);
        fade.GradientStops[3].Color = Colour(after);
        _scroller.OpacityMask = fade;
      }

      private static double Depth(double hidden, double zone)
      {
        if (hidden <= 0 || zone <= 0)
          return 0;

        double gone = Math.Min(1.0, hidden / zone);
        return Math.Round(gone * STEPS) / STEPS;
      }

      /// <summary>A mask hides by its own transparency: nothing hidden is opaque, a zone hidden is clear.</summary>
      private static Color Colour(double depth)
        => Color.FromArgb((byte)Math.Round((1.0 - depth) * 255.0), 0xFF, 0xFF, 0xFF);

      private static LinearGradientBrush NewFade()
      {
        LinearGradientBrush fade = new LinearGradientBrush
        {
          StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
          EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative)
        };
        fade.GradientStops.Add(new GradientStop(Colors.White, 0));
        fade.GradientStops.Add(new GradientStop(Colors.White, ZONE));
        fade.GradientStops.Add(new GradientStop(Colors.White, 1.0 - ZONE));
        fade.GradientStops.Add(new GradientStop(Colors.White, 1));
        return fade;
      }
    }
  }

  /// <summary>
  /// One row of the weather screen's own menu: the mark out of the skin's vectors, the name on the glass,
  /// the white bar of the screen's standing answer, and the command a press runs.
  /// </summary>
  public sealed class WeatherMenuEntry
  {
    public WeatherMenuEntry(string icon, string label, bool isCurrent, WeatherMenuCommand command)
    {
      Icon = icon;
      Label = label;
      IsCurrent = isCurrent;
      Command = command;
    }

    /// <summary>Name of the glyph under MediaMenu/, worn the way the library rail wears its commands.</summary>
    public string Icon { get; }

    public string Label { get; }

    public bool IsCurrent { get; }

    internal WeatherMenuCommand Command { get; }
  }

  /// <summary>What a row of the weather menu does when it is pressed.</summary>
  public enum WeatherMenuCommand
  {
    /// <summary>The dialog of watched places goes up over the wall.</summary>
    ChooseLocation,

    /// <summary>The settings stand up, and the places are edited behind their door.</summary>
    OpenSetup,

    /// <summary>The wall is asked again, whatever the held brief says.</summary>
    Refresh
  }
}
