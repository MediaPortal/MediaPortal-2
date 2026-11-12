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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.ViewModels;
using System;
using System.Linq;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// What a screen has to answer for its menu rows to be runnable by a rail: whether its menu is up, which page
  /// of the menu the rows are, and which plate of the rail the caret stands on.  Said as an interface rather than
  /// as the shelf's own view model, because the shelf is not the only screen wearing a rail - a wall of people
  /// wears the same one and answers the same three questions.
  /// </summary>
  internal interface IMenuRailOwner
  {
    /// <summary>Whether the screen is wearing its menu right now.</summary>
    bool MediaMenuVisible { get; }

    /// <summary>Which page of the menu the rows are: the commands, a page of layouts, a page of orders.</summary>
    string MediaMenuHeading { get; }

    /// <summary>Tells the screen's foot line what the stood row says and what it adds to that.</summary>
    void MenuTileStood(string? label, string? detail);
  }

  /// <summary>
  /// What a rail asks of the screen it hangs in, and nothing else: whose menu it is, where the caret is
  /// asked for, how its ground nods at a wall, and which row of the commands the viewer climbed down from.
  /// A screen that answers those four questions owns a working menu rail, which is why the shelf and the
  /// people screens can share one rail rather than each carry their own.
  /// </summary>
  internal interface IShelfRailHost
  {
    /// <summary>The screen whose menu the rows are, or nothing while the screen has no words to speak yet.</summary>
    IMenuRailOwner? RailOwner { get; }

    /// <summary>Puts the caret on a control of the screen and keeps asking for it until it is there.</summary>
    void RailCaret(Control target, Action? whenStood);

    /// <summary>Nods the rail and its ground toward the wall the viewer pointed at.</summary>
    void RailNod(Control ground, double diveX, double diveY);

    /// <summary>Says which row of the commands the viewer climbed down from, for the way back to stand on.</summary>
    void RailClimbedDown(int row);
  }

  /// <summary>
  /// The rail of a screen's own menu: the row of command plates, its ground, and everything the rail does -
  /// answering a button and a pointer, keeping its stood row out of the fade at either end, travelling like the
  /// reference's rail and paling into both its ends.  Two of these hang in the shelf, one down its left edge
  /// and one under the covers; a screen of people hangs the one that runs down.
  /// </summary>
  internal sealed class ShelfRail
  {
    /// <summary>How far a rail nods toward the wall a viewer pointed at, in points.</summary>
    internal const double RAIL_BOUNCE_DIVE = 22;

    private readonly ListBox _list;
    private readonly Control _ground;
    private readonly bool _sideways;
    private readonly IShelfRailHost _host;

    public ShelfRail(ListBox list, Control ground, bool sideways, IShelfRailHost host)
    {
      _list = list;
      _ground = ground;
      _sideways = sideways;
      _host = host;
    }


    /// <summary>
    /// Stands the caret on one row of a rail whose rows were just replaced underneath it - the same caret the
    /// menu's opening stands, asked of a rail that is on the screen already and only changed its rows.
    /// </summary>
    internal void StandOnRow(int row)
    {
      ListBox rail = _list;
      _host.RailCaret(rail, delegate
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
    internal void Wire()
    {
      ListBox list = _list;
      // The line at the screen's foot speaks of the plate the caret stands on, so it is told whenever the caret
      // walks: by the list's own selection, and by the focus arriving in a row - the screen's arrows hand the caret
      // from plate to plate by moving the focus, which leaves the list's selection where it was.
      list.SelectionChanged += delegate { TellFootLine(); };
      list.AddHandler(InputElement.GotFocusEvent, delegate { TellFootLine(); }, RoutingStrategies.Bubble);

      list.AddHandler(InputElement.KeyDownEvent, delegate (object? sender, KeyEventArgs args)
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
        if (StoodOnRow() is MediaBrowserMenuEntry row)
        {
          list.SelectedItem = row;
          RunRow(row);
          args.Handled = true;
        }
      }, RoutingStrategies.Bubble, true);

      list.AddHandler(InputElement.PointerPressedEvent, delegate (object? sender, PointerPressedEventArgs args)
      {
        // The press lands somewhere inside the row rather than on the row itself, so the row is asked for by
        // walking up from whatever was touched. A press on the list's own ground between two rows is no command.
        if (args.Source is Visual source)
        {
          ListBoxItem? item = source.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
          if (item?.DataContext is MediaBrowserMenuEntry row)
          {
            list.SelectedItem = row;
            RunRow(row);
            args.Handled = true;
          }
        }
      }, RoutingStrategies.Tunnel);

      // A row of the rail that comes by the caret is kept away from the rail's two ends, as the reference's rail
      // keeps its stood row a margin from both (see KeepRowInSight).  Only the rail that runs down answers:
      // the row of plates under covers has its own question to answer at its ends, and the viewer asked about the
      // side rail.
      if (!_sideways)
      {
        list.AddHandler(InputElement.GotFocusEvent, delegate (object? sender, RoutedEventArgs args)
        {
          // The caret sometimes lands on something inside the row rather than on the row itself, so the row is
          // asked for from whatever came into focus.
          ListBoxItem? row = args.Source as ListBoxItem
              ?? (args.Source as Visual)?.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
          if (row != null)
            KeepRowInSight(row);
        }, RoutingStrategies.Bubble);
      }

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
      bool sideways_rail = _sideways;
      Control rail_ground = _ground;
      rail_ground.AddHandler(InputElement.KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        if (Services.HomeTrace.Enabled)
          Services.HomeTrace.Log("rail.key", "key=" + args.Key + " handled=" + args.Handled
              + " items=" + list.Items.Count + " ground=" + rail_ground.Name);

        if (args.Handled || list.Items.Count == 0)
          return;

        // Under covers the shelf is reached from above (Up) and the wall lies BELOW the row of plates, so the
        // nod there dives downward into the floor the menu came out of.
        Key away_from_the_shelf = sideways_rail ? Key.Down : Key.Left;
        Key past_the_head = sideways_rail ? Key.Left : Key.Up;
        Key past_the_foot = sideways_rail ? Key.Right : Key.Down;
        object? stood = StoodOnRow();
        int standing = stood == null ? -1 : list.Items.IndexOf(stood);

        if (args.Key == away_from_the_shelf)
          _host.RailNod(rail_ground, sideways_rail ? 0 : -RAIL_BOUNCE_DIVE, sideways_rail ? RAIL_BOUNCE_DIVE : 0);
        else if (args.Key == past_the_head && standing <= 0)
          _host.RailNod(rail_ground, sideways_rail ? -RAIL_BOUNCE_DIVE : 0, sideways_rail ? 0 : -RAIL_BOUNCE_DIVE);
        else if (args.Key == past_the_foot && standing == list.Items.Count - 1)
          _host.RailNod(rail_ground, sideways_rail ? RAIL_BOUNCE_DIVE : 0, sideways_rail ? 0 : RAIL_BOUNCE_DIVE);
        else
          return;

        args.Handled = true;
      }, RoutingStrategies.Tunnel);

      if (Services.HomeTrace.Enabled)
        Services.HomeTrace.Log("rail.wire", "list=" + _list.Name + " ground=" + _ground.Name
            + " sideways=" + _sideways);
    }


    /// <summary>
    /// Presses the command a menu row carries, when it has one that can be pressed - and remembers the row when
    /// the press was a climb down from the commands into a subpage, which the back key asks for by name.
    /// </summary>
    internal void RunRow(MediaBrowserMenuEntry row)
    {
      ListBox list = _list;
      if (!row.Command.CanExecute(null))
        return;

      if ((_host.RailOwner)?.MediaMenuHeading == COMMANDS_HEADING)
        _host.RailClimbedDown(list.Items.IndexOf(row));

      row.Command.Execute(null);

      // Climbing down a page replaced every row the rail wore, caret included; by this moment the page stands
      // complete, so its first row is asked for at once.  A command that builds its page later - a filter still
      // counting its values - is answered by the heading handler instead, which wakes when the page really comes.
      if (_host.RailOwner is IMenuRailOwner owner
          && owner.MediaMenuVisible && owner.MediaMenuHeading != COMMANDS_HEADING)
        StandOnRow(0);
    }


    /// <summary>
    /// Tells the screen's foot which plate of a rail the caret stands on.  The plate is asked for the same way every
    /// other answer on this rail asks for it - the row the focus is in, falling back to the list's own selection -
    /// because the screen's arrows move the caret without asking the list to select.
    /// </summary>
    internal void TellFootLine()
    {
      ListBox list = _list;
      if (_host.RailOwner is not IMenuRailOwner owner)
        return;

      MediaBrowserMenuEntry? plate = StoodOnRow() as MediaBrowserMenuEntry;
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
    internal object? StoodOnRow()
    {
      ListBox list = _list;
      if ((TopLevel.GetTopLevel(_list) as Window)?.FocusManager.GetFocusedElement() is Visual stood)
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
    /// The rail's own scroller, looked for on the tree every time it is asked for: the panel the rail hangs in is
    /// taken out of the tree while the menu is shut (see <c>LayTheMenuPanelsDown</c>) and a list that was
    /// retemplated - which is what putting a skin on does - brings a new one with it.
    /// </summary>
    private ScrollViewer? TheScroller()
      => _list.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();


    /// <summary>
    /// Dresses the menu's rail for the two things a viewer noticed it was missing: the edge fade over its head and
    /// its foot, and the glide its rows travel on.  Without the first a row the caret walked above the rail's head
    /// is cut off there by a straight edge and a menu longer than the room says nothing at its foot (viewer,
    /// 2026-11-10); without the second every step of the caret lands the wall of rows on its new place in one
    /// frame, which the same viewer called a hard jump (2026-11-10).
    /// </summary>
    /// <remarks>
    /// Asked every time the rail stands, and not once for the view's life, because the panel is laid down between
    /// two openings and only the dress that is written on the scroller that stands there now can be seen.  The
    /// glide goes on permanently, as the shelf's does (see <c>InstallShelfGlide</c>): the reference lerps
    /// every offset change of this very list through its storyboards, a caret step and a wheel notch alike
    /// (<c>OtherControlsOverrides.xaml:1019-1027</c>).
    /// </remarks>
    internal void Dress(int tries = 12)
    {
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? rail = TheScroller();
        if (rail == null)
        {
          // The panel was stood up but its list has not been put together yet - which is what happens when a menu
          // is opened on a screen that has never measured it.  Asked again on the next pass, for as long as the
          // panel stands; a rail that never brings a scroller with it is a rail without rows anyway.
          if (tries > 0 && _ground.IsVisible)
            Dress(tries - 1);
          return;
        }

        FitGlide(rail);

        // The reading is only asked again for a scroller it does not already watch; one that stands again keeps
        // the scroller its template made the first time, and its reading with it.
        if (ReferenceEquals(_railFade?.Band, rail))
          return;

        // The rail holds its own reading: a mask that nobody keeps a hold of is a mask that is never renewed,
        // and the rail lives as long as its screen does.
        _railFade = new BandEndFade(rail, sideways: false);

        // A row's own height is not known until the list has laid it out, so the last row of a page that arrived
        // a moment ago is measured at less than it will be: the margin was kept against a rail shorter than the
        // one that stood after.  Told when the measure moves, the margin is put on again.
        rail.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { AskMarginAgain(rail); });
        AnswerFollow(rail);

        _railFade.Update();
        _railStoodRow = null;
      }, DispatcherPriority.Loaded);
    }


    /// <summary>
    /// Makes sure the rail that stands right now is dressed, and dresses it if it is not.  Asked of every caret
    /// that arrives in the rail, because it is cheap - one walk over a rail of a few rows - and because the one
    /// moment the dress is asked for (the panel standing up) can meet a list that has not been put together yet.
    /// </summary>
    private void EnsureDressed(ScrollViewer rail)
    {
      FitGlide(rail);

      if (ReferenceEquals(_railFade?.Band, rail))
        return;

      _railFade = new BandEndFade(rail, sideways: false);

      // As in Dress: the measure of a page that is still arriving is not the measure it ends at, and the
      // margin has to be put on against the one that stands.
      rail.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { AskMarginAgain(rail); });
      AnswerFollow(rail);

      _railFade.Update();
    }


    /// <summary>
    /// Answers the rail's "bring this into view" itself, so that a row the caret walked to arrives inside the
    /// reference's margin instead of flush against an end of the rail.
    /// </summary>
    /// <remarks>
    /// A list follows its caret by asking its scroller to bring the row into view, and the scroller's answer is the
    /// least it can do: the row's edge lands on the room's edge, which is inside the fade worn there - the very row
    /// the viewer stands on would dissolve (2026-11-10, the complaint about the rail).  The reference keeps a
    /// hundred and ninety points between the stood row and either end of this same rail
    /// (<c>OtherControlsOverrides.xaml:1019</c>), and asks for it while bringing the row into view - which is the
    /// moment answered here, because a scroller knows nothing of the fade it wears.  Heard with the handled asked
    /// for as well: the scroller's own answer is written by a handler of its class, which is served before this
    /// one, so stepping on it is what leaves the margin standing.
    /// </remarks>
    private void AnswerFollow(ScrollViewer rail)
    {
      if (ReferenceEquals(_railFollowAnswered, rail))
        return;

      _railFollowAnswered = rail;
      rail.AddHandler(Control.RequestBringIntoViewEvent,
          delegate(object? sender, RequestBringIntoViewEventArgs e)
      {
        // The ask often comes from something inside the row rather than from the row, and what a viewer reads as
        // stood is the whole row.
        Visual? asker = e.Source as Visual;
        ListBoxItem? row = asker as ListBoxItem
            ?? asker?.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
        if (row == null)
          return;

        KeepRowInSight(row);
        e.Handled = true;
      }, RoutingStrategies.Bubble, true);
    }


    /// <summary>
    /// Asks for the rail's margin to be put on again on the next pass rather than from inside the report of a
    /// scroll.  Written while the scroller is still telling everyone where it moved, an offset is swallowed - and
    /// that is exactly the moment the list's own answer to the caret arriving lands the stood row into the fade,
    /// so the answer has to come a pass later.
    /// </summary>
    private void AskMarginAgain(ScrollViewer rail)
    {
      Dispatcher.UIThread.Post(delegate
      {
        Control? stood = _railStoodRow;
        if (stood != null && ReferenceEquals(TheScroller(), rail))
          GlideOntoRow(rail, stood);
      }, DispatcherPriority.Background);
    }


    /// <summary>Puts the reference's travel on the rail, at the length its own scroll animation runs.</summary>
    private void FitGlide(ScrollViewer rail)
    {
      // Asked whether a dress is worn rather than whether both sides are empty: a scroller that wears nothing and
      // a memory of nothing are not the same thing, and reading them as one leaves the rail bare forever.
      if (_wornRailGlide != null && ReferenceEquals(rail.Transitions, _wornRailGlide))
        return;

      Transitions glide = new Transitions();
      glide.Add(new VectorTransition
      {
        Property = ScrollViewer.OffsetProperty,
        Duration = RAIL_GLIDE,
        // The easing the skin's own vertical menu scroller wears
        // (Skins/Nereus/themes/default/styles.xaml:349, the reference's VerticalMediaScrollViewerStyle), which
        // is also what makes the home menu's column feel as though it is set down rather than fired.
        Easing = new Avalonia.Animation.Easings.CubicEaseOut()
      });
      _wornRailGlide = glide;
      rail.Transitions = glide;
    }


    /// <summary>
    /// Keeps the row the caret stands on away from the rail's two ends, with the reference's margin between the row
    /// and either of them.  A scroller left to itself does bring a row into sight, but the minimum: the row lands
    /// flush against the end it came in at - inside the edge fade that is worn there, which would dissolve the very
    /// row the viewer is standing on.  The reference's rail holds its stood row a hundred and ninety points from
    /// both ends (<c>OtherControlsOverrides.xaml:1019</c>), which is also more than the ninety-odd points the fade
    /// zone of a rail of this height takes.
    /// </summary>
    internal void KeepRowInSight(Control row)
    {
      ScrollViewer? rail = TheScroller();
      if (rail == null)
        return;

      // The dress is asked for here as well as when the panel stands up: a menu opened on a screen that has never
      // measured its rail meets a list that has not been put together yet, and the caret arriving is the first
      // moment the scroller it was dressed in certainly stands there.
      EnsureDressed(rail);
      _railStoodRow = row;

      // The list's own answer to a caret arriving is the bare minimum - it lands the row flush against the end it
      // came in at, which is inside the fade worn there.  That answer is not fought here (it arrives later than any
      // pass made now, and it arrives through the rail's own ask, which the view answers with the margin - see
      // AnswerFollow); this pass is what carries the caret the moment it arrives, and the two answers read
      // as one travel because the glide retargets a running travel rather than restarting it.
      GlideOntoRow(rail, row);

      // Asked once more after the layout has had its say: the list's own answer to the caret arriving lands the row
      // flush against an end during that pass, and the glide retargets the running travel rather than restarting
      // it, so the two answers read as one travel (see KeepCardInSight).
      Dispatcher.UIThread.Post(delegate { GlideOntoRow(rail, row); }, DispatcherPriority.Loaded);

      // And once more after that answer has been painted towards: the list brings a row in by its own rule, which
      // stands the row's top at the very last line of the room - the row is then technically in sight and wholly
      // inside the fade - and it does so on the pass the layout is closed, below the one above.  Asking at a
      // priority under the layout's own leaves the margin standing.
      Dispatcher.UIThread.Post(delegate { GlideOntoRow(rail, row); }, DispatcherPriority.Background);
    }


    /// <summary>One pass of the rail's margin: writes the rail's offset if the row does not already lie inside it.</summary>
    private static void GlideOntoRow(ScrollViewer rail, Control row)
    {
      Point? at = row.TranslatePoint(new Point(0, 0), rail);
      if (at == null || rail.Viewport.Height <= 0.0)
        return;

      double viewport = rail.Viewport.Height;
      double top = at.Value.Y - rail.Padding.Top;
      double foot = top + row.Bounds.Height;

      // A rail shorter than a row and two margins cannot hold the margin at both ends, so it is asked for and given
      // up towards the middle, which leaves the row standing centred - the reference's own answer.
      double margin = Math.Max(0.0, Math.Min(RAIL_SCROLL_MARGIN, (viewport - row.Bounds.Height) / 2.0));
      double offset = rail.Offset.Y;

      if (top < margin)
        offset -= margin - top;
      else if (foot > viewport - margin)
        offset += foot - (viewport - margin);

      offset = Math.Clamp(offset, 0.0, Math.Max(0.0, rail.Extent.Height - viewport));
      if (Math.Abs(offset - rail.Offset.Y) > 0.5)
        rail.Offset = new Vector(rail.Offset.X, offset);
    }


    /// <summary>
    /// The reference's margin between a stood menu row and either end of its rail
    /// (<c>OtherControlsOverrides.xaml:1019</c>, the <c>ScrollMargin="190,190"</c> of the rail's items panel).
    /// </summary>
    private const double RAIL_SCROLL_MARGIN = 190.0;


    /// <summary>
    /// The length of the reference's rail travel: its <c>ScrollAnimation</c> storyboard runs four hundred
    /// milliseconds over every scroll of the list (<c>OtherControlsOverrides.xaml:1023</c>).
    /// </summary>
    private static readonly TimeSpan RAIL_GLIDE = TimeSpan.FromMilliseconds(400);




    /// <summary>
    /// The menu rail's own reading, kept beside the others because it is the same dress read on the other axis -
    /// and because it is thrown away and asked again whenever the rail is stood up on a new scroller.
    /// </summary>
    private BandEndFade? _railFade;


    /// <summary>
    /// The travel the rail's scroller was last dressed in, kept so that a rail standing again is not dressed twice
    /// - a transition that is replaced restarts nothing, but it throws away the one it is running.
    /// </summary>
    private Transitions? _wornRailGlide;


    /// <summary>
    /// The row the caret last stood on in the rail.  Kept because the rail's measure can move after the caret
    /// arrived - a page whose rows were only just laid out is measured taller a pass later - and the margin has to
    /// be put on against the measure that ends up standing.
    /// </summary>
    private Control? _railStoodRow;


    /// <summary>
    /// The rail whose "bring this into view" was answered for, so that a rail standing again is not asked twice -
    /// a second answer would scroll the same row a second time in the same frame.
    /// </summary>
    private ScrollViewer? _railFollowAnswered;


    /// <summary>The heading the menu wears while its commands page stands up (see the view model's BuildMediaMenu).</summary>
    internal const string COMMANDS_HEADING = "Menu";

  }

  /// <summary>
  /// The nod a screen gives a key that points at a wall: the lean of the plate the caret stands on toward that
  /// wall and the spring back.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Kept beside the rail it belongs to because more than one screen wears that rail: the shelf of titles nods its
  /// side rail, its rail under covers, its bands and its fields, and a wall of people nods the same rail the shelf
  /// hands over. A second hand writing the same answer would be a second feel, and the viewer reads the feel.
  /// </para>
  /// <para>
  /// The nod rides a <see cref="TranslateTransform"/> of its own on the plate that nods, which is the one thing
  /// it may not share: a room that slides open its rail travels on its own transform (and on a transition), and a
  /// nod written onto that transform closes the rail it was meant to answer.
  /// </para>
  /// </remarks>
  internal sealed class ShelfNod
  {
    /// <summary>The lean toward the wall and the spring back, both in milliseconds.</summary>
    private const int DIVE_MS = 90;
    private const int SPRING_MS = 430;

    private DispatcherTimer? _timer;
    private TranslateTransform? _worn;

    /// <summary>
    /// The lean of the last nod, which the nod's own transform cannot witness: the transform stays on the plate
    /// after the spring and says nothing about which way the plate leaned.  A test that has to tell a nod left
    /// from a nod right reads these instead.
    /// </summary>
    internal double LastNodX { get; private set; }

    /// <inheritdoc cref="LastNodX" />
    internal double LastNodY { get; private set; }

    /// <summary>
    /// Where the nodding plate currently stands. A test - and the render probe - read these to know that a wall
    /// was pointed at: while no wall has been pointed at since the last nod came to rest, the plate stands at
    /// nothing at all.
    /// </summary>
    internal double BounceX => _worn?.X ?? 0;

    /// <inheritdoc cref="BounceX" />
    internal double BounceY => _worn?.Y ?? 0;

    /// <summary>
    /// Nods a plate the told number of points toward the wall the viewer pointed at and springs it back.
    /// </summary>
    /// <remarks>
    /// A wall is answered rather than passed over, because a screen that says nothing when a key points at
    /// nothing is a screen where the viewer cannot tell a wall from a broken key. The nod runs on the front
    /// end's own timer, the way <c>MediaBrowserView.StandCaret</c> asks for a caret: ninety milliseconds of lean
    /// out - squared off, so the first frames carry most of it and the wall is named at once - and four hundred
    /// and thirty of easeOutBack home, which carries the plate a tenth of the dive past its rest before it
    /// settles, which is what makes the answer a bounce rather than a shuffle back.
    /// </remarks>
    /// <param name="face">The plate's whole ground: the panel that carries the list, its heading and its glow.</param>
    /// <param name="diveX">The lean along X, positive to the right; zero for a wall above or below.</param>
    /// <param name="diveY">The lean along Y, positive downward; zero for a wall beside the plate.</param>
    internal void Nod(Control face, double diveX, double diveY)
    {
      if (Services.HomeTrace.Enabled)
        Services.HomeTrace.Log("rail.nod", "face=" + face.Name + " dive=" + diveX.ToString("F0") + ","
            + diveY.ToString("F0"));

      // One nod at a time. A key pressed while the previous nod is still travelling starts a new one from
      // wherever the plate stands, because that is where the plate is and a viewer leaning on a key repeatedly
      // should see the plate lean with every press.
      _timer?.Stop();

      if (_worn != null && !ReferenceEquals(face.RenderTransform, _worn))
      {
        // Another plate wore the last nod - walking from a grid into covers does that, and one nodder shared by a
        // screen's rail, bands and fields does it often. Put that one back to rest before nodding this one, so no
        // plate is left hanging off its ground.
        _worn.X = 0;
        _worn.Y = 0;
        _worn = null;
      }

      TranslateTransform nod = face.RenderTransform as TranslateTransform;
      if (nod == null)
      {
        nod = new TranslateTransform();
        face.RenderTransform = nod;
      }
      _worn = nod;
      LastNodX = diveX;
      LastNodY = diveY;

      double fromX = nod.X;
      double fromY = nod.Y;
      long started = Environment.TickCount64;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _timer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(_timer, timer))
          return; // a newer nod drives this plate from here on

        long elapsed = Environment.TickCount64 - started;
        if (elapsed >= DIVE_MS + SPRING_MS)
        {
          timer.Stop();
          nod.X = 0;
          nod.Y = 0;
          return;
        }

        if (elapsed <= DIVE_MS)
        {
          double u = elapsed / (double)DIVE_MS;
          double lean = 1 - (1 - u) * (1 - u);
          nod.X = fromX + (diveX - fromX) * lean;
          nod.Y = fromY + (diveY - fromY) * lean;
          return;
        }

        double v = (elapsed - DIVE_MS) / (double)SPRING_MS;
        double c1 = 1.70158;
        double c3 = c1 + 1;
        double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
        nod.X = diveX * (1 - back);
        nod.Y = diveY * (1 - back);
      };

      timer.Start();
    }
  }
}