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
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// Walking the Nereus home with a keyboard or a remote: an arrow goes where it points, the menu rail is a
  /// place a viewer can stand on, and a band remembers where its viewer last stood.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These are measurements on a shown screen rather than unit tests of a helper, because every part of the
  /// claim lives somewhere else: the wall's scale, the shortcut column's size, the mosaic's mixed squares, the
  /// rail's virtualized entries and the key handler's idea of what is focusable. The first version of this
  /// screen scored candidates by the distance between their <i>centres</i>, which is invisible in a wall of
  /// equal squares and wrong in a wall of unequal ones - the shortcut squares are a third of the size of the
  /// covers beside them, so their centres came out nearer, and up from the top row of covers left the wall for
  /// a shortcut. The reference measures rects (<c>FrameworkElement.FindNextFocusElement</c>), and that is what
  /// <see cref="Input.SkinFocusWalk"/> ports.
  /// </para>
  /// <para>
  /// The wall these tests walk is the one the music entry actually builds - the shortcut column, a
  /// <c>square1</c> band whose six covers are three different sizes, and a <c>square2</c> band beside it - so
  /// the squares above, below and beside the one a test stands on are the sizes a viewer sees.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestNereusWallNavigation
  {
    private const string AUDIO_ACTION = "30715d73-4205-417f-80aa-e82f0834171f";

    /// <summary>
    /// Up from the top row of covers leaves the wall for the row of buttons the face hangs above it - the power,
    /// the person, the cog and the search glass - which is the one thing that really does lie above the wall.
    /// Under the old centre measure this key stepped sideways onto a shortcut whose middle sat a few lines
    /// higher, and the row stayed a picture a mouse alone could touch.
    /// </summary>
    [AvaloniaTest]
    public void UpFromTheTopRowOfCoversLeavesTheWallForTheRowOfButtonsAboveIt()
    {
      OnWall(delegate (Wall wall)
      {
        Button top = wall.Covers
            .OrderBy(button => wall.Abs(button).Y)
            .ThenBy(button => wall.Abs(button).X)
            .First();
        wall.Stand(top);
        wall.Press(Key.Up);

        Control? stood = wall.Focused;
        Assert.That(wall.TopRow, Contains.Item(stood),
            "the arrow that had no cover above it stood on one of the buttons at the head of the screen; it came "
            + "back with '" + wall.NameOf(stood) + "'");
        Assert.That(wall.Abs(stood!).Bottom, Is.LessThanOrEqualTo(wall.Abs(top).Top + 1),
            "and the button it picked lies above the cover, not merely nearer its middle");
      });
    }

    /// <summary>
    /// The way the row is actually reached from where a viewer stands: up from the highest shortcut. Nothing of
    /// the wall is above that square, and the row is - so the key leaves the wall for it.
    /// </summary>
    [AvaloniaTest]
    public void UpFromTheHighestShortcutStandsOnTheRowOfButtons()
    {
      OnWall(delegate (Wall wall)
      {
        Button highest = wall.Shortcuts.OrderBy(button => wall.Abs(button).Y).First();
        wall.Stand(highest);
        wall.Press(Key.Up);

        Control? stood = wall.Focused;
        Assert.That(wall.TopRow, Contains.Item(stood),
            "up from the highest shortcut reached the row of buttons over the screen; it came back with '"
            + wall.NameOf(stood) + "'");
        Assert.That(wall.Abs(stood!).Bottom, Is.LessThanOrEqualTo(wall.Abs(highest).Top + 1),
            "the button stood on is above the shortcut the viewer came from");
      });
    }

    /// <summary>
    /// The row is reached from the head of the menu too, because that is where it stands: beside the first
    /// entry. From every other entry the menu's Right key still returns the viewer to the square they last stood
    /// on, which is the courtesy the reference's RestoreFocusItem was for.
    /// </summary>
    [AvaloniaTest]
    public void RightFromTheHeadOfTheMenuReachesTheRowOfButtons()
    {
      OnWall(delegate (Wall wall)
      {
        Control? head = wall.RailEntry(0);
        Assert.That(head, Is.Not.Null, "the menu had no first entry to stand on");
        Assert.That(wall.Abs(wall.TopRow[0]).Top, Is.LessThan(wall.Abs(head!).Bottom),
            "the row does stand beside the head of the menu, or this test asks for the wrong key");

        wall.Stand(head!);
        Assert.That(head!.IsFocused || head.IsKeyboardFocusWithin,
            "standing on the head of the menu leaves the focus on it");
        wall.Press(Key.Right);

        Assert.That(wall.TopRow, Contains.Item(wall.Focused),
            "right from the head of the menu walked onto the row beside it; it came back with '"
            + wall.NameOf(wall.Focused) + "', with the head at " + wall.Abs(head!) + " and the row at "
            + string.Join(", ", wall.TopRow.Select(button => wall.NameOf(button) + " " + wall.Abs(button))));
      });
    }

    /// <summary>
    /// Once on the row the arrows keep their word: right and left walk along it and down goes back onto the
    /// wall, so a remote never leaves the viewer standing at the top of the screen with no way down.
    /// </summary>
    [AvaloniaTest]
    public void AlongTheRowOfButtonsTheArrowsWalkItAndGoBackDownToTheWall()
    {
      OnWall(delegate (Wall wall)
      {
        Assert.That(wall.TopRow.Length, Is.GreaterThan(1),
            "the row has to hold more than one button for walking it to mean anything");
        Button first = wall.TopRow[0];

        wall.Stand(first);
        wall.Press(Key.Right);
        Assert.That(wall.Focused, Is.SameAs(wall.TopRow[1]),
            "right walked to the button beside it; it came back with '" + wall.NameOf(wall.Focused) + "'");

        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.SameAs(first),
            "and left walked back; it came back with '" + wall.NameOf(wall.Focused) + "'");

        wall.Press(Key.Down);
        Control? down = wall.Focused;
        Assert.That(down is Button square && square.Tag is HomeContentTile,
            "down went back onto the wall; it came back with '" + wall.NameOf(down) + "'");
      });
    }

    /// <summary>
    /// Up inside the mosaic goes to the cover above the one the viewer stands on - the cover whose bottom edge
    /// the viewer would touch by going up - and not to any of the shortcuts down the left of the screen.
    /// </summary>
    [AvaloniaTest]
    public void UpInsideTheMosaicGoesToTheCoverAboveAndNotToAShortcutBesideIt()
    {
      OnWall(delegate (Wall wall)
      {
        // The mosaic's first table stands a small cover over another small cover in its third column; that pair
        // is what the test walks, since it is the one pair whose answer cannot be argued about.
        Button lower = wall.Covers.First(cover => wall.NameOf(cover) == "Album 3");
        Button above = wall.Covers.First(cover => wall.NameOf(cover) == "Album 2");
        Assert.That(wall.Abs(above).Bottom, Is.LessThanOrEqualTo(wall.Abs(lower).Top + 1),
            "the wall really does hold a cover over this one, or the test says nothing");
        Assert.That(wall.Abs(above).Left, Is.GreaterThanOrEqualTo(wall.Abs(lower).Left - 1),
            "and over it in the same column");

        wall.Stand(lower);
        wall.Press(Key.Up);

        Assert.That(wall.Focused, Is.SameAs(above),
            "up went to the cover above, where the arrow points; it came back with "
            + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// Down inside the mosaic is the same measurement the other way: the cover under the standing one, which in
    /// the reference's table is a big square under a small one - unequal sizes being exactly where a centre
    /// measure goes wrong, since the big square's centre sits further down and further left than the small
    /// one's edge is up.
    /// </summary>
    [AvaloniaTest]
    public void DownInsideTheMosaicGoesToTheSquareUnderIt()
    {
      OnWall(delegate (Wall wall)
      {
        Button small = wall.Covers.First(cover => wall.NameOf(cover) == "Album 3");
        Button big = wall.Covers.First(cover => wall.NameOf(cover) == "Album 6");
        Assert.That(wall.Abs(big).Top, Is.GreaterThanOrEqualTo(wall.Abs(small).Bottom - 1));
        Assert.That(wall.Abs(big).Height, Is.GreaterThan(wall.Abs(small).Height),
            "a square twice as deep as the one above it is what makes this worth testing");
        Assert.That(wall.Abs(big).X + wall.Abs(big).Width / 2,
            Is.LessThan(wall.Abs(small).X + wall.Abs(small).Width / 2),
            "and its centre stands to the left of the small one's, which is what a centre measure trips over");

        wall.Stand(small);
        wall.Press(Key.Down);

        Assert.That(wall.Focused, Is.SameAs(big),
            "down went to the square under it; it came back with " + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The last row is the end of the wall, and the end of the wall is the end of the way down. What stands under
    /// the wall is a tile of the menu - another place, one the viewer reaches with Left and not with an arrow that
    /// pointed at media - so that key used to hand the viewer out of the content they were reading. It answers
    /// where it was pressed instead: the whole content pane leans toward the edge that was asked for and comes
    /// back, and the square it leaned from is still the square the viewer stands on.
    /// </summary>
    [AvaloniaTest]
    public void DownPastTheBottomOfTheWallLeansTheContentAndKeepsTheSquare()
    {
      OnWall(delegate (Wall wall)
      {
        // The lowest square of the whole wall, covers and shortcuts together, so that nothing of the wall can
        // lie under the one this test stands on.
        Button bottom = wall.Covers.Concat(wall.Shortcuts)
            .OrderByDescending(square => wall.Abs(square).Bottom)
            .First();
        wall.Stand(bottom);
        bool answered = wall.Press(Key.Down);

        Assert.That(answered, Is.True,
            "down past the last row was answered by this screen instead of being left to the focus navigation "
            + "every control falls back on");
        Assert.That(wall.Focused, Is.SameAs(bottom),
            "and the viewer is still standing on the square they asked from; it came back with "
            + wall.NameOf(wall.Focused));

        // How far the lean travels is the screen's own beating, and a headless dispatcher never counts beats -
        // no timer of any kind ticks in it. What a test can hold the screen to is the first line of the answer:
        // the pane was put out of its place toward the edge that was asked for, which is the difference between
        // a refusal that says something and one that only declines to move.
        Assert.That(wall.ContentLean, Is.GreaterThan(6.0),
            "the whole content leaned toward the edge that was asked for; it leaned "
            + wall.ContentLean.ToString("0.0") + " lines");

        // And the wall is not a trap: the way out of it is still the one that was always there.
        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.Not.SameAs(bottom),
            "the arrow that does lead somewhere still leads, after a refusal; it came back with "
            + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The row of corner buttons is the head of the screen, and there is no row above the head of the screen.
    /// What the geometric walk answers an upward arrow with there is a tile of the menu standing somewhere above
    /// and to the side, and standing the viewer on that scrolled the rail up under their feet - a step to
    /// another place, taken by a key that meant "one row higher". The row answers for itself now, as the wall
    /// does at its foot: it leans up out of its place and settles back, and the button stays the button.
    /// </summary>
    [AvaloniaTest]
    public void UpFromTheRowOfCornerButtonsLeansTheRowAndKeepsTheButton()
    {
      OnWall(delegate (Wall wall)
      {
        Assert.That(wall.TopRow.Length, Is.GreaterThan(1),
            "the face hung a row of corner buttons to walk; it hung " + wall.TopRow.Length + " of them");

        // A button in the middle of the row rather than at either end, so that this is about the top edge of the
        // screen and not about an edge of the row, which another test already walks.
        Button stood = wall.TopRow[1];
        wall.Stand(stood);
        bool answered = wall.Press(Key.Up);

        Assert.That(answered, Is.True,
            "up from the head of the screen was answered by this screen instead of being left to the walk, which "
            + "knows a menu tile that stands higher than these buttons");
        Assert.That(wall.Focused, Is.SameAs(stood),
            "and the viewer is still standing on the button they asked from; it came back with "
            + wall.NameOf(wall.Focused));
        Assert.That(wall.Focused, Is.Not.SameAs(wall.RailEntry(0)),
            "the menu rail did not take the focus, which is the scroll the viewer complained about");

        // As at the foot of the wall, a headless dispatcher never counts the beats of the return; the first line
        // of the answer is what a test can hold the screen to. A tenth of the screen's height is the row's own
        // travel, and the first step is the largest of its six.
        Assert.That(wall.TopRowLean, Is.LessThan(-6.0),
            "the row leaned off the top edge; it leaned " + wall.TopRowLean.ToString("0.0") + " lines");

        // And the row is not a trap either: along it, an arrow still walks the four buttons.
        wall.Press(Key.Right);
        Assert.That(wall.Focused, Is.SameAs(wall.TopRow[2]),
            "the arrow that does lead somewhere still leads, after a refusal; it came back with "
            + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// Left across the wall reaches the menu rail, and reaches it on an entry: the rail is not a decoration a
    /// keyboard cannot stand on. From the rail, Right returns the viewer to the square they came from rather
    /// than to the top of the wall.
    /// </summary>
    [AvaloniaTest]
    public void TheMenuRailIsReachedOnAnEntryAndGivesTheWayBack()
    {
      OnWall(delegate (Wall wall)
      {
        // The covers' own left edge: from anywhere else in the mosaic the next thing to the left is another
        // cover, which is right, and the menu would never be asked about.
        Button cover = wall.Leftmost.First();
        wall.Stand(cover);
        wall.Press(Key.Left); // into the shortcut column, which is the next thing to the left
        Assert.That(wall.NameOf(wall.Focused), Does.StartWith("shortcut "),
            "left from the covers steps onto the shortcuts beside them, not past them; it came back with "
            + wall.NameOf(wall.Focused));

        wall.Press(Key.Left);
        Control? entry = wall.Focused;
        Assert.That(entry, Is.InstanceOf<ListBoxItem>(),
            "and another left stands on the menu itself, on an entry of it - it came back with "
            + wall.NameOf(entry));
        Assert.That(entry!.IsFocused, Is.True);

        wall.Press(Key.Right);
        Assert.That(wall.NameOf(wall.Focused), Does.StartWith("shortcut "),
            "and right goes back onto the wall rather than further along the menu; it came back with "
            + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The band a viewer leaves remembers the square they last stood on, which is the reference's
    /// <c>RestoreFocus</c>: walking the shortcuts down to its third square, stepping across to the covers and
    /// back from a row where that square is not the one the geometry points at has to land on the square that
    /// was stood on before.
    /// </summary>
    [AvaloniaTest]
    public void ABandRemembersTheSquareThatWasLastStoodOn()
    {
      OnWall(delegate (Wall wall)
      {
        Button first = wall.Shortcuts
            .OrderBy(button => wall.Abs(button).Y)
            .ThenBy(button => wall.Abs(button).X)
            .First();
        Assert.That(wall.NameOf(first), Is.EqualTo("shortcut Genre"),
            "the music rail starts at its genre, which is where a viewer arriving from the menu lands");

        wall.Stand(first);
        wall.Press(Key.Down);
        wall.Press(Key.Down);
        Assert.That(wall.NameOf(wall.Focused), Is.EqualTo("shortcut Album"),
            "two downs walk the column to its third square; it came back with " + wall.NameOf(wall.Focused));

        // A cover high up on the wall's left edge, where the shortcut the measurement would pick is not the one
        // that was stood on. If they agreed, this test would prove nothing.
        Button high = wall.Leftmost.First();
        Button alongside = wall.Shortcuts
            .OrderByDescending(shortcut => Math.Min(wall.Abs(shortcut).Bottom, wall.Abs(high).Bottom)
                - Math.Max(wall.Abs(shortcut).Top, wall.Abs(high).Top))
            .First();
        Assert.That(wall.NameOf(alongside), Is.Not.EqualTo("shortcut Album"),
            "the square beside that cover is " + wall.NameOf(alongside) + ", so the answer below can only "
            + "come from the band remembering");

        wall.Stand(high);
        wall.Press(Key.Left);
        Assert.That(wall.NameOf(wall.Focused), Is.EqualTo("shortcut Album"),
            "coming back to the shortcuts stands on the square that was last stood on, because that is the "
            + "whole point of remembering; it came back with " + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The menu is one place with one standing entry, so an arrow that walks off the wall into it lands on that
    /// entry - the one whose wall the viewer is looking at, which is the one they last stood on - and not on
    /// whichever entry happens to hang beside the square they pressed from. The measurement decides whether the
    /// arrow leads into the menu at all; it does not get to choose which entry of the menu, because a menu does
    /// not have one entry per height, it has one entry per menu item.
    /// </summary>
    [AvaloniaTest]
    public void EnteringTheMenuLandsOnTheStandingEntryAndNotBesideTheSquare()
    {
      OnWall(delegate (Wall wall)
      {
        int standing = wall.StandingEntry;
        Assert.That(standing, Is.GreaterThanOrEqualTo(0),
            "the menu stands on an entry from the moment a wall is showing, since the wall is its content");

        // A shortcut the rail can be reached from, and whose nearest entry is not the standing one. If the two
        // agreed for every shortcut, this test would pass while proving nothing, so it says so out loud.
        Button? fromShortcut = wall.Shortcuts
            .Where(shortcut => wall.NearestEntryIndexTo(shortcut) >= 0
                && wall.NearestEntryIndexTo(shortcut) != standing)
            .OrderBy(shortcut => wall.Abs(shortcut).Y)
            .FirstOrDefault();
        Assert.That(fromShortcut, Is.Not.Null,
            "the standing entry stands beside every shortcut of the wall, so nothing here could tell the "
            + "standing entry from the entry the geometry would have picked");

        Button from = fromShortcut!;
        int beside = wall.NearestEntryIndexTo(from);
        wall.Stand(from);
        wall.Press(Key.Left);

        Assert.That(wall.Focused, Is.InstanceOf<ListBoxItem>(),
            "left out of the shortcut column stands the viewer on the menu; it came back with "
            + wall.NameOf(wall.Focused));
        Assert.That(wall.Focused, Is.SameAs(wall.RailEntry(standing)),
            "and on the entry the menu is standing on rather than on the one beside the square they pressed "
            + "from - the square was " + wall.NameOf(from) + ", the entry beside it is number " + beside
            + " (" + wall.NameOf(wall.RailEntry(beside)!) + "), the menu stands on number " + standing
            + ", and the key came back with " + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The menu hands back the entry the viewer was last stood on, which is the reference's
    /// <c>RestoreFocus</c> for the menu: a viewer who walked the menu, went out to a wall and comes back does not
    /// start again from wherever the wall happens to point at.
    /// </summary>
    [AvaloniaTest]
    public void TheMenuRemembersTheEntryThatWasLastStoodOn()
    {
      OnWall(delegate (Wall wall)
      {
        int standing = wall.StandingEntry;

        // Onto the menu, and along it to another entry. Walking the menu with the arrows selects as it goes,
        // which is what makes the walked entry the standing one - the same thing the reference's list does.
        Button cover = wall.Leftmost.First();
        wall.Stand(cover);
        wall.Press(Key.Left);
        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.InstanceOf<ListBoxItem>(),
            "two lefts out of the covers stand the viewer on the menu; it came back with "
            + wall.NameOf(wall.Focused));

        wall.Press(Key.Down);
        Control? walked = wall.Focused;
        Assert.That(walked, Is.InstanceOf<ListBoxItem>(),
            "an arrow along the menu moves along its entries; it came back with " + wall.NameOf(walked));
        Assert.That(wall.StandingEntry, Is.Not.EqualTo(standing),
            "and walking the menu stands it on the entry walked to, since this face's menu fills its content as "
            + "the highlight moves; it is still standing on " + wall.StandingEntry.ToString());

        // Back out onto the wall and in again, from a square the geometry aims somewhere else.
        wall.Press(Key.Right);
        Assert.That(wall.NameOf(wall.Focused), Does.StartWith("shortcut "),
            "right goes back onto the wall; it came back with " + wall.NameOf(wall.Focused));
        Control? cameBackTo = wall.Focused;

        // The walk would answer from this square with the entry beside it, which is not the one that was stood
        // on - if it were, the assertion below would pass while proving nothing.
        int beside = wall.NearestEntryIndexTo(cameBackTo);
        Assert.That(beside, Is.Not.EqualTo(wall.IndexInRail(walked)),
            "the entry beside " + wall.NameOf(cameBackTo) + " is the one that was last stood on, so this square "
            + "cannot tell the memory from the measurement");

        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.SameAs(walked),
            "and coming back into the menu from " + wall.NameOf(cameBackTo) + " stands the viewer on the entry "
            + "they last stood on, which is " + wall.NameOf(walked) + ", not on the entry beside that square; it "
            + "came back with " + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The home content is scrolled by its sections - "Last added", "Unwatched" - and never by its squares, which
    /// is how the reference reads it: a section is looked at whole, from its heading to its last cover. So where
    /// the wall stands follows the section the viewer is in and not the square under their feet: walking along a
    /// section leaves the wall where it stands, and walking into a section stands that section before the right
    /// edge of the screen with the gap the face writes between two sections breathing after it, instead of being
    /// sliced in half by that edge as it was while the wall only drifted after the focused square.
    /// </summary>
    [AvaloniaTest]
    public void WalkingIntoTheNextSectionStandsItWholeBeforeTheRightEdge()
    {
      OnWall(delegate (Wall wall)
      {
        // The highest square of the mosaic, so that a step down stays inside the same section.
        Button top = wall.Covers
            .Where(square => wall.SectionOf(square) == "Latest added")
            .OrderBy(square => wall.Abs(square).Top)
            .First();
        wall.Stand(top);

        double resting = wall.OffsetX;
        wall.Press(Key.Down);
        for (int i = 0; i < 3; i++)
          Pump();

        Assert.That(wall.SectionOf(wall.Focused), Is.EqualTo("Latest added"),
            "a step down from the top square stays inside the section it started in; it landed on "
            + wall.NameOf(wall.Focused));
        Assert.That(wall.OffsetX, Is.EqualTo(resting).Within(0.5),
            "and walking along a section leaves the wall standing where it stands - it scrolled by "
            + (wall.OffsetX - resting).ToString("0.0"));

        // Over the first section's edge. The fixture's wall begins with the shortcut column, so the section beside
        // the mosaic still stands whole before the right edge where the wall begins - and a section that is
        // already whole is not shoved around to reach for an edge it is clear of.
        string WalkRightOutOf(string section)
        {
          for (int step = 0; step < 12 && wall.SectionOf(wall.Focused) == section; step++)
            wall.Press(Key.Right);

          return wall.SectionOf(wall.Focused);
        }

        string entered = WalkRightOutOf("Latest added");
        Assert.That(entered, Is.EqualTo("Unplayed"),
            "walking right out of the mosaic walks into the section beside it; the walk came back with "
            + wall.NameOf(wall.Focused));

        // The wall glides to wherever a section belongs; wait for the glide instead of reading it halfway.
        for (int i = 0; i < 5; i++)
          Pump();

        Assert.That(wall.OffsetX, Is.EqualTo(resting).Within(0.5),
            "a section that already stands whole before the right edge of the screen leaves the wall where it "
            + "stands; the wall slid by " + (wall.OffsetX - resting).ToString("0.0"));

        // One section further along, where the screen really does cut a section in half.
        entered = WalkRightOutOf("Unplayed");
        Assert.That(entered, Is.EqualTo("Recently added"),
            "walking on reaches the third section of the wall; the walk came back with "
            + wall.NameOf(wall.Focused));

        for (int i = 0; i < 5; i++)
          Pump();

        double edge = wall.PaneEdgeX;
        double drawn = wall.DrawnRightOf(entered);
        double between = wall.DrawnLeftOf(entered) - wall.DrawnRightOf("Unplayed");

        // The section's own box, found by the squares it holds: the box carries the face's gap as its trailing
        // margin, so where the box ends is where a section ends for the eye.
        Rect box = wall.BandsInPane()
            .OrderBy(candidate => Math.Abs(candidate.Left - wall.DrawnLeftOf(entered)))
            .First();

        Assert.That(wall.OffsetX, Is.GreaterThan(resting),
            "walking over the edge of a section the screen cannot show moves the wall, where a step along a "
            + "section and a step into a whole section did not");
        Assert.That(edge - box.Right, Is.EqualTo(0).Within(0.5),
            "the section that was walked into stands whole before the right edge of the screen, ending with the "
            + "gap the face writes between two sections; its box ended at " + box.Right.ToString("0.0")
            + " of a screen ending at " + edge.ToString("0.0"));
        Assert.That(edge - drawn, Is.GreaterThan(20.0),
            "so that the section's last cover is not pressed flat against the edge of the television but keeps "
            + "the room the face writes behind a section; it stands " + (edge - drawn).ToString("0.0")
            + " lines clear of the edge, where two sections keep " + between.ToString("0.0") + " between them");

        Rect stood = wall.InPane(wall.Focused);
        Assert.That(stood.Right, Is.LessThanOrEqualTo(edge + 0.5),
            "and the square stood upon lies inside the screen rather than hanging past its edge");

        double placed = wall.OffsetX;
        wall.Press(Key.Right);
        for (int i = 0; i < 3; i++)
          Pump();

        Assert.That(wall.SectionOf(wall.Focused), Is.EqualTo(entered),
            "a step right inside the section that was just stood stays inside it; it landed on "
            + wall.NameOf(wall.Focused));
        Assert.That(wall.OffsetX, Is.EqualTo(placed).Within(0.5),
            "and walking along a section does not slide it cover by cover - the wall answers the section it "
            + "stands in and not the square under the viewer's feet; it slid by "
            + (wall.OffsetX - placed).ToString("0.0"));
      });
    }

    /// <summary>
    /// Where the home content leaves the screen on the left it used to end on a straight line beside the menu,
    /// which is the one edge of the wall that is never meant to be seen: the wall slides behind the menu, so its
    /// covers should thin out and be gone before they reach it. That needs the pane to reach into the gap between
    /// menu and content - and the gap to keep belonging to the wall, so that nothing on the screen moves - and it
    /// needs a mask rather than a veil of black, because what lies behind the wall is the entry's own picture,
    /// which is exactly what a departing cover should sink into.
    /// </summary>
    [AvaloniaTest]
    public void TheWallThinsOutInsideTheGapAndIsGoneWhereTheMenuBegins()
    {
      OnWall(delegate (Wall wall)
      {
        // The wall's own box begins at the content gap, measured inside the pane. That is what proves the pane
        // reaches into the gap: the forty is no longer the pane's place but the wall's own margin, so nothing on
        // the screen moved and yet the wall now has room to dissolve in.
        Rect first = wall.BandsInPane()[0];
        Assert.That(first.Left, Is.EqualTo(HomeViewModel.CONTENT_GAP).Within(0.5),
            "the standing wall keeps the content gap as its own margin, so a section at the wall's beginning "
            + "still stands its forty from the menu; the wall's box begins at " + first.Left.ToString("0.0"));

        double columnEnds = wall.Abs(wall.MenuColumn).Left
            + wall.MenuColumn.ColumnDefinitions[0].Width.Value;
        Assert.That(wall.Abs(wall.Pane).Left, Is.EqualTo(columnEnds).Within(0.5),
            "and the pane begins exactly where the menu's column ends, so that the dissolving stretch of wall "
            + "lies behind the menu instead of being cut off beside it; the pane begins at "
            + wall.Abs(wall.Pane).Left.ToString("0.0") + " and the menu ends at " + columnEnds.ToString("0.0"));

        Avalonia.Media.IBrush? mask = wall.PaneMask;
        Assert.That(mask, Is.InstanceOf<Avalonia.Media.LinearGradientBrush>(),
            "the wall's left edge is thinned by a mask - a veil of black would hide the picture behind the wall "
            + "instead of letting the cover sink into it");

        Avalonia.Media.LinearGradientBrush ramp = (Avalonia.Media.LinearGradientBrush) mask!;
        Assert.That(ramp.StartPoint.Unit, Is.EqualTo(Avalonia.RelativeUnit.Absolute),
            "measured in the screen's own numbers, so that the thinning keeps its width whatever the wall holds");
        Assert.That(ramp.EndPoint.Unit, Is.EqualTo(Avalonia.RelativeUnit.Absolute), "and at its end");
        Assert.That(ramp.EndPoint.Point.X, Is.LessThanOrEqualTo(HomeViewModel.CONTENT_GAP + 0.5),
            "the whole of it lies inside the content gap, so that a cover is fully gone - and, standing still, "
            + "fully whole - before the menu begins; the ramp ends at "
            + ramp.EndPoint.Point.X.ToString("0.0"));
        Assert.That(ramp.GradientStops[0].Color.A, Is.EqualTo(0),
            "clear at the pane's left edge, which is where the menu stands");
        Assert.That(ramp.GradientStops[ramp.GradientStops.Count - 1].Color.A, Is.EqualTo(255),
            "and whole again at the content's beginning, so that a resting section is not dimmed");
      });
    }

    /// <summary>
    /// The wall ends, and the menu does not begin where the wall ends: the menu is the place a viewer steps to
    /// with Left, and an arrow still pointing right past the last square points at nothing. The measurement answers
    /// such a key with a menu entry standing somewhere off that edge, and standing the viewer there threw them out
    /// of the entry whose wall they were reading - so the wall answers for itself, as it does at its foot: it is
    /// thrown off its right end back into the screen, and the square stays the square.
    /// </summary>
    [AvaloniaTest]
    public void RightPastTheLastSquareOfTheWallLeansTheContentAndKeepsTheSquare()
    {
      OnWall(delegate (Wall wall)
      {
        // The square hanging furthest right, so that nothing of the wall can lie to the right of the one this
        // test stands on.
        Button last = wall.Covers.Concat(wall.Shortcuts)
            .OrderByDescending(square => wall.InPane(square).Right)
            .First();
        wall.Stand(last);
        bool answered = wall.Press(Key.Right);

        Assert.That(answered, Is.True,
            "right past the last square was answered by this screen instead of being left to the focus navigation "
            + "every control falls back on");
        Assert.That(wall.Focused, Is.SameAs(last),
            "and the viewer is still standing on the square they asked from; it came back with "
            + wall.NameOf(wall.Focused));
        Assert.That(wall.Focused, Is.Not.InstanceOf<ListBoxItem>(),
            "and above all they were not thrown out of the wall onto the menu");

        // As at the foot of the wall, a headless dispatcher never counts beats, so what a test can hold the screen
        // to is the first line of the answer: the content was put out of its place, toward the edge that was
        // asked for, which here means back into the screen.
        Assert.That(wall.ContentLeanSideways, Is.LessThan(-6.0),
            "the content was thrown off the edge that was asked for; it moved "
            + wall.ContentLeanSideways.ToString("0.0") + " lines across the screen");

        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.Not.SameAs(last),
            "the arrow that does lead somewhere still leads, after a refusal; it came back with "
            + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// A wall is stood at the share of the screen the face gives it, which is seldom one to the unit, so every
    /// number the wall is moved by has to be read in the screen's own numbers and not in the numbers the controls
    /// carry. Mixing the two is what once left the last cover standing flat against the right edge of the
    /// television: the wall's place was measured in the pane, its width in itself, and the two only agree while
    /// the wall happens to stand unscaled.
    /// </summary>
    [AvaloniaTest]
    public void TheLastSectionStandsItsGapBeforeTheEdgeOnAWallTheFaceHasShrunk()
    {
      OnWall(delegate (Wall wall)
      {
        // The square furthest right, stood on directly: what this test is about is where the wall stands a section,
        // and not about the refusal that follows the last square - which leans the whole content out of its place
        // and would be measured along with it.
        Control lastSquare = wall.Covers.Concat<Control>(wall.Shortcuts)
            .OrderByDescending(square => wall.InPane(square).Right)
            .First();
        wall.Stand(lastSquare);

        string last = wall.SectionOf(wall.Focused);
        Assert.That(last, Is.EqualTo("Recently added"),
            "the square furthest right on the wall belongs to its last section; it came back with "
            + wall.NameOf(wall.Focused) + " in " + last);

        for (int i = 0; i < 6; i++)
          Pump();

        Assert.That(wall.ContentLeanSideways, Is.EqualTo(0).Within(0.5),
            "and the wall stands where the face put it, unleaned, while it is measured; it leans "
            + wall.ContentLeanSideways.ToString("0.0"));

        double edge = wall.PaneEdgeX;
        double drawn = wall.DrawnRightOf(last);
        Rect box = wall.BandsInPane()
            .OrderBy(candidate => Math.Abs(candidate.Left - wall.DrawnLeftOf(last)))
            .First();

        Assert.That(wall.OffsetX, Is.GreaterThan(0),
            "a wall wider than its screen has to be scrolled to be read; it stood at "
            + wall.OffsetX.ToString("0.0"));
        Assert.That(edge - box.Right, Is.EqualTo(0).Within(0.5),
            "the last section stands where a section stands, its box ending at the edge of the screen; its box "
            + "ended at " + box.Right.ToString("0.0") + " of a screen ending at " + edge.ToString("0.0"));
        Assert.That(edge - drawn, Is.GreaterThan(20.0),
            "and its last cover is not pressed flat against the edge of the television; it stands "
            + (edge - drawn).ToString("0.0") + " lines clear of it");
      }, height: 1400);
    }

    /// <summary>
    /// The square the menu hands back on Right is the one the viewer was standing on - the reference's
    /// <c>NereusHomeListView.RestoreFocusItem</c>, which existed so that a viewer returning to the menu did not
    /// have to walk the wall from its beginning.
    /// </summary>
    [AvaloniaTest]
    public void TheMenuSendsTheViewerBackToTheSquareTheyCameFrom()
    {
      OnWall(delegate (Wall wall)
      {
        Button cover = wall.Leftmost.First();
        wall.Stand(cover);
        wall.Press(Key.Left);
        Control? shortcut = wall.Focused;
        wall.Press(Key.Left);
        Assert.That(wall.Focused, Is.InstanceOf<ListBoxItem>(), "the viewer is on the menu");

        wall.Press(Key.Right);
        Assert.That(wall.Focused, Is.SameAs(shortcut),
            "right from the menu goes back onto the wall where the viewer left it, not to its first square; it "
            + "came back with " + wall.NameOf(wall.Focused));
      });
    }

    /// <summary>
    /// The shown music home: the shortcut column the entry asks for, a <c>square1</c> band of six covers in
    /// three sizes, a <c>square2</c> band beside it and a third one past that, on a television-sized window
    /// wearing the worn skin. The third band is what makes the wall wider than the screen, so that the wall has
    /// somewhere to scroll to. A <paramref name="height"/> other than the television's stands the wall at the
    /// share of the screen the face would give it there, which is how a scaled wall gets tested.
    /// </summary>
    private static void OnWall(Action<Wall> assert, double height = 1080)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == AUDIO_ACTION);
        home.SelectedEntry = entry;
        home.ShowContentAsync(entry).GetAwaiter().GetResult();

        // The bands the server would answer with, as the wall itself lays them out: six covers in the mosaic
        // the reference's first square table draws, then four in the second one.
        HomeContentGroup latest = new HomeContentGroup("Latest added", "square1", 760, 6);
        for (int index = 1; index <= 6; index++)
          latest.AddTitle(NewTitle("Album " + index));
        home.ContentGroups.Add(latest);

        HomeContentGroup unplayed = new HomeContentGroup("Unplayed", "square2", 650, 4);
        for (int index = 7; index <= 10; index++)
          unplayed.AddTitle(NewTitle("Album " + index));
        home.ContentGroups.Add(unplayed);

        // One band more than the screen can show. A wall no wider than the screen it stands on can never be
        // scrolled, and what the wall does at its right edge - standing a section whole before that edge - is
        // only to be seen on a wall that has to scroll.
        HomeContentGroup recent = new HomeContentGroup("Recently added", "square2", 650, 4);
        for (int index = 11; index <= 14; index++)
          recent.AddTitle(NewTitle("Album " + index));
        home.ContentGroups.Add(recent);

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Width = 1920;
        window.Height = height;
        window.Show();
        Pump();

        ScrollViewer wallPane = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");
        ListBox rail = window.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "menuList");

        Button[] squares = wallPane.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Focusable && button.IsEffectivelyVisible && button.IsEnabled)
            .ToArray();
        Assert.That(squares, Is.Not.Empty, "the wall showed no tile at all");

        // The face's own row of buttons above the wall. It carries no names, only the Tag its press reads,
        // and the screen reaches it the same way a remote does - by measurement, not by a lookup.
        ContentControl overlays = window.GetVisualDescendants().OfType<ContentControl>()
            .Single(face => face.Name == "homeOverlays");
        Button[] topRow = overlays.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Tag is string mark && mark.Length > 0
                && button.Focusable && button.IsEffectivelyVisible && button.IsEnabled)
            .OrderBy(button => button.Bounds.X)
            .ToArray();
        Assert.That(topRow, Is.Not.Empty, "the face hung no button at the top of the screen");

        Window screen = window;
        assert(new Wall(screen, wallPane, squares
            .Where(button => !((HomeContentTile) button.Tag!).IsShortcut).ToArray(),
            squares.Where(button => ((HomeContentTile) button.Tag!).IsShortcut).ToArray(), rail, topRow));
      }
      finally
      {
        window?.Close();
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    private static TitleViewModel NewTitle(string name)
    {
      return new TitleViewModel(new MediaTitle(Guid.NewGuid(), name, null, 2020, null, null, false));
    }

    /// <summary>
    /// The shown screen as the three things a test needs: where every square stands in the window's own
    /// coordinates, which square the arrow keys were pressed from, and where the focus ended up.
    /// </summary>
    private sealed class Wall
    {
      private readonly Window _screen;
      private readonly ListBox _rail;
      private readonly ScrollViewer _pane;

      public Wall(Window screen, ScrollViewer pane, Button[] covers, Button[] shortcuts, ListBox rail,
          Button[] topRow)
      {
        _screen = screen;
        _pane = pane;
        _rail = rail;
        Covers = covers;
        Shortcuts = shortcuts;
        TopRow = topRow;
      }

      /// <summary>
      /// How far the content pane leans toward the bottom edge right now, read off the transform it carries.
      /// Zero is the place the face gave it.
      /// </summary>
      public double ContentLean
      {
        get
        {
          // The pane's transform is a translation and the screen reports it as a matrix, whose offset along Y
          // is what a viewer sees move; anything else it might be asked to carry leans nothing.
          return _pane.RenderTransform is Avalonia.Media.ITransform shift ? shift.Value.M32 : 0;
        }
      }

      /// <summary>
      /// How far the content pane leans across the television right now. The wall's right end throws its content
      /// back into the screen, so an answer given at that edge is a negative number.
      /// </summary>
      public double ContentLeanSideways
      {
        get
        {
          // The same matrix as above, read along X: the offset a viewer sees move when the wall is thrown off its
          // side rather than off its foot.
          return _pane.RenderTransform is Avalonia.Media.ITransform shift ? shift.Value.M31 : 0;
        }
      }

      /// <summary>How far the wall has been scrolled along itself, in the screen's own numbers.</summary>
      public double OffsetX => _pane.Offset.X;

      /// <summary>The pane the wall stands in, which is the thing whose edges the viewer sees.</summary>
      public Control Pane => _pane;

      /// <summary>
      /// Where the right edge of the screen falls in the pane's own coordinates, which is what a section is
      /// measured against: the pane's place on the window plays no part in that space.
      /// </summary>
      public double PaneEdgeX => ((ScrollViewer) _pane).Viewport.Width;

      /// <summary>What dissolves the wall's left edge, if the face puts anything there at all.</summary>
      public Avalonia.Media.IBrush? PaneMask => _pane.OpacityMask;

      /// <summary>The grid the menu and the content share, which is where the line between them is drawn.</summary>
      public Grid MenuColumn => (Grid) _rail.Parent!;

      /// <summary>The section of the wall a square of it stands in, by the heading the section carries.</summary>
      public string SectionOf(Control? control)
      {
        return control is Button button && button.Tag is HomeContentTile tile && tile.Group != null
            ? tile.Group.Heading
            : "(no section)";
      }

      /// <summary>
      /// How far right the drawn squares of one section reach, in the pane's coordinates. The container a section
      /// stands in carries the face's gap as its own trailing margin, which nobody can see, so the sections are
      /// measured by what is painted on them.
      /// </summary>
      public double DrawnRightOf(string section)
      {
        return Covers.Concat<Control>(Shortcuts)
            .Where(square => SectionOf(square) == section)
            .Max(square => InPane(square).Right);
      }

      /// <summary>How far left the drawn squares of one section reach, in the pane's coordinates.</summary>
      public double DrawnLeftOf(string section)
      {
        return Covers.Concat<Control>(Shortcuts)
            .Where(square => SectionOf(square) == section)
            .Min(square => InPane(square).Left);
      }

      /// <summary>
      /// Where a square stands in the pane's own coordinates, which is the space the pane scrolls through and
      /// therefore the space the wall's edges are spoken of in.
      /// </summary>
      public Rect InPane(Control control)
      {
        Point? at = control.TranslatePoint(new Point(), _pane);
        // Both corners through the pane, because a control's own width is not the width a scaled wall stands
        // on the screen with.
        Point? end = control.TranslatePoint(
            new Point(control.Bounds.Width, control.Bounds.Height), _pane);
        Assert.That(at, Is.Not.Null, "the square lies under the pane, so it can be measured against it");
        Assert.That(end, Is.Not.Null, "and it can be measured to its far corner too");
        return new Rect(at!.Value, end!.Value);
      }

      /// <summary>The wall's sections as boxes in the pane's coordinates, container and all.</summary>
      public List<Rect> BandsInPane()
      {
        ItemsControl standing = _pane.GetVisualDescendants().OfType<ItemsControl>()
            .First(candidate => candidate.Name == "nereusWallStanding");

        var boxes = new List<Rect>();
        foreach (Control? container in standing.GetRealizedContainers())
        {
          Point? at = container?.TranslatePoint(new Point(), _pane);
          Point? end = container?.TranslatePoint(
              new Point(container.Bounds.Width, container.Bounds.Height), _pane);
          if (container != null && at != null && end != null)
            boxes.Add(new Rect(at.Value, end.Value));
        }

        return boxes;
      }

      public Button[] Covers { get; }
      public Button[] Shortcuts { get; }

      /// <summary>
      /// How far the row of corner buttons leans out of its place right now. Zero is the place the face gave it,
      /// and a row pushed off the top edge of the screen leans toward a negative amount.
      /// </summary>
      public double TopRowLean
      {
        get
        {
          // The row is the panel the four buttons stand on, and it is the whole of what leans: the screen reports
          // a translation as a matrix, whose offset along Y is what a viewer sees move.
          Control? row = TopRow.Length > 0 ? TopRow[0].Parent as Control : null;
          return row?.RenderTransform is Avalonia.Media.ITransform shift ? shift.Value.M32 : 0;
        }
      }

      /// <summary>
      /// The row of buttons the face hangs at the top of the screen - power, person, cog and search glass - in
      /// the order they stand there, which is the order an arrow walks them.
      /// </summary>
      public Button[] TopRow { get; }

      /// <summary>The entry the rail is standing on: the one whose wall the screen is showing.</summary>
      public int StandingEntry => _rail.SelectedIndex;

      /// <summary>The entries of the rail that are realized right now, as the walk sees them.</summary>
      public Control[] Entries()
      {
        return _rail.GetVisualDescendants().OfType<ListBoxItem>()
            .Where(item => item.Focusable && item.IsEffectivelyVisible && item.IsEnabled)
            .ToArray();
      }

      /// <summary>The place an entry of the rail has in the menu, or -1 for a control that is not one.</summary>
      public int IndexInRail(Control? entry)
      {
        return entry is ListBoxItem item ? _rail.IndexFromContainer(item) : -1;
      }

      /// <summary>
      /// The place in the menu of the realized entry whose middle stands nearest the middle of the given
      /// control - which is roughly what the walk answers an arrow into the rail with, before the rail is asked
      /// which entry it would rather hand back.
      /// </summary>
      public int NearestEntryIndexTo(Control from)
      {
        Rect here = Abs(from);
        Control? near = Entries()
            .OrderBy(entry => Math.Abs(Abs(entry).Center.Y - here.Center.Y))
            .FirstOrDefault();
        return near == null ? -1 : _rail.IndexFromContainer(near);
      }

      /// <summary>An entry of the menu rail, by the place it has in the menu.</summary>
      public Control? RailEntry(int index)
      {
        _rail.ScrollIntoView(index);
        Pump();
        return _rail.ContainerFromIndex(index) as Control;
      }

      /// <summary>
      /// The covers standing on the wall's left edge, top first - the only ones from which a leftward arrow has
      /// nothing between it and the shortcuts.
      /// </summary>
      public Button[] Leftmost
      {
        get
        {
          Button[] byColumn = Covers
              .OrderBy(button => Abs(button).X)
              .ThenBy(button => Abs(button).Y)
              .ToArray();
          double left = Abs(byColumn[0]).X;
          return byColumn.Where(button => Abs(button).X < left + 1).ToArray();
        }
      }

      /// <summary>A square's place on the screen, in the screen's coordinates, the wall's scale included.</summary>
      public Rect Abs(Control control)
      {
        Point? topLeft = control.TranslatePoint(new Point(), _screen);
        Point? bottomRight = control.TranslatePoint(
            new Point(control.Bounds.Width, control.Bounds.Height), _screen);
        return topLeft == null || bottomRight == null
            ? default
            : new Rect(topLeft.Value, bottomRight.Value);
      }

      public void Stand(Control control)
      {
        Assert.That(control.Focus(), Is.True, NameOf(control) + " would not take the focus");
        Pump();
      }

      /// <summary>
      /// Presses an arrow on whatever the screen is stood on and reports whether the screen answered it. A key
      /// nobody handled travels on to Avalonia's own focus navigation, which is the difference between an answer
      /// and a shrug.
      /// </summary>
      public bool Press(Key key)
      {
        Control stood = Focused ?? _rail;
        KeyEventArgs args = new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = key,
          Route = RoutingStrategies.Tunnel,
          Source = stood,
        };
        stood.RaiseEvent(args);
        Pump();
        return args.Handled;
      }

      public Control? Focused =>
          _screen.GetVisualDescendants().OfType<Control>().FirstOrDefault(control => control.IsFocused);

      public string NameOf(Control? control)
      {
        if (control == null)
          return "nothing at all";
        if (control is Button button && button.Tag is HomeContentTile tile)
          return (tile.IsShortcut ? "shortcut " : "") + tile.Label;
        if (control is Button withTag && withTag.Tag is string key)
          return "button " + key;
        if (control is ListBoxItem item && item.DataContext is MainMenuEntry entry)
          return "menu entry " + entry.Label;
        if (control is ListBoxItem)
          return "menu entry";
        return control.GetType().Name + " '" + control.Name + "'";
      }
    }

    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestNereusWallNavigation).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
  }
}
