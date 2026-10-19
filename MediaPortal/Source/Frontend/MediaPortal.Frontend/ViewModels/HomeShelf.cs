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

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What one line of the home screen's content pane is: the heading of a list, a title inside one, or a line
  /// saying why a list is not there.
  /// </summary>
  public enum HomeRowKind
  {
    /// <summary>The name of one of the lists, drawn as a heading.</summary>
    Heading,

    /// <summary>A title somebody can start.</summary>
    Title,

    /// <summary>Something the pane has to say that is not a title - a refusal, or what pressing the entry does.</summary>
    Note
  }

  /// <summary>
  /// One line of the content pane, which is why it is one row type rather than a list per shelf: the pane is
  /// walked with a remote, and a viewer cannot stop on a heading that belongs to a list of its own.
  /// </summary>
  public sealed class HomeRow
  {
    private HomeRow(HomeRowKind kind, string text, TitleViewModel? title)
    {
      Kind = kind;
      Text = text;
      Title = title;
    }

    public HomeRowKind Kind { get; }

    /// <summary>The heading or the note; empty on a title row, which draws <see cref="Title"/> instead.</summary>
    public string Text { get; }

    /// <summary>The title, on a title row; null on every other kind.</summary>
    public TitleViewModel? Title { get; }

    public bool IsHeading
    {
      get { return Kind == HomeRowKind.Heading; }
    }

    public bool IsTitle
    {
      get { return Kind == HomeRowKind.Title; }
    }

    public bool IsNote
    {
      get { return Kind == HomeRowKind.Note; }
    }

    public static HomeRow Heading(string text)
    {
      return new HomeRow(HomeRowKind.Heading, text, null);
    }

    public static HomeRow Note(string text)
    {
      return new HomeRow(HomeRowKind.Note, text, null);
    }

    public static HomeRow For(TitleViewModel title)
    {
      return new HomeRow(HomeRowKind.Title, string.Empty, title);
    }
  }

  /// <summary>
  /// One of the lists MP2's home screen shows next to the main menu, for the entry that is highlighted.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The old client asked the skin to do this: <c>Nereus</c>' <c>HomeMenuModel</c>
  /// (<c>Source/UI/Skins/Nereus/Models/HomeMenuModel.cs</c>) handed the highlighted menu entry over to one of the
  /// <c>AbstractHomeContent</c> subclasses in <c>Models/HomeContent/</c>, and each of those offered the same four
  /// lists - <c>Latest*</c>, <c>ContinuePlay*</c>, <c>Favorite*</c> and <c>Unplayed*</c> - of which the skin drew at
  /// most six titles each (<c>MovieHomeContent.cs:35-73</c>). Those four names are therefore what a MediaPortal
  /// viewer expects to see, and they are the headings below: <c>Nereus.Home.LatestAdded</c>,
  /// <c>Nereus.Home.ContinuePlayed</c>, <c>Nereus.Home.Favorites</c> and <c>Nereus.Home.Unplayed</c>, in the
  /// plugin's own English words.
  /// </para>
  /// <para>
  /// What each list can be asked for over HTTP is written next to it. Three of the four can: the HTTP API sorts by
  /// title, year, the date the library first saw the file, last-played, track and episode, so "Latest added",
  /// "Continue playing" and "Unplayed" are all questions this server can answer. "Favorites" is the one that cannot,
  /// because nothing on the server keeps favourites, and it stays on the pane as one line saying so - a heading that
  /// quietly vanished would leave the viewer wondering whether the library has no favourites or the front end has
  /// lost the list.
  /// </para>
  /// <para>
  /// The four headings are therefore Nereus' four, in his order, and the dates tell them apart from each other the
  /// way MP2's own lists did: "Latest added" is a fact about this library (when did it learn of this file), while
  /// the year behind "Unplayed" is a fact about the work itself.
  /// </para>
  /// </remarks>
  public sealed class HomeContentList
  {
    /// <summary>
    /// How many titles one list contributes. This is MP2's own number: <c>AbstractHomeContent.CONTENT_LIST_LIMIT</c>
    /// (<c>Source/UI/Skins/Nereus/Models/HomeContent/AbstractHomeContent.cs</c>) is six, and the pane has to hold
    /// four lists of them above a television.
    /// </summary>
    public const int CONTENT_LIST_LIMIT = 6;

    private HomeContentList(string heading, string? sort, bool descending, string? seen, string? missing)
    {
      Heading = heading;
      Sort = sort;
      Descending = descending;
      Seen = seen;
      Missing = missing;
    }

    /// <summary>What the pane calls the list, in Nereus' words.</summary>
    public string Heading { get; }

    /// <summary>The sort to ask the server for, or null to take whatever order it gives.</summary>
    public string? Sort { get; }

    /// <summary>Whether that sort runs from the newest down.</summary>
    public bool Descending { get; }

    /// <summary>The watched-state filter to ask for, or null for titles whatever their history.</summary>
    public string? Seen { get; }

    /// <summary>Why nothing can be listed here, or null when the list can be asked for.</summary>
    public string? Missing { get; }

    /// <summary>
    /// The lists for one menu entry, in the order the pane draws them.
    /// </summary>
    /// <remarks>
    /// "Latest added" leads because that is the question MP2's own first list answered - "what arrived lately" -
    /// and since the server learned to sort by the date a title entered the library, it can be answered for real
    /// rather than by the stand-in by year that used to stand where it.
    /// </remarks>
    public static IReadOnlyList<HomeContentList> For(MainMenuEntry entry)
    {
      // Only an entry that opens a shelf of titles has lists at all. An entry that opens a screen - the folders,
      // the pictures - has its own screen to show, and the pane says what pressing it does instead.
      if (entry == null || entry.Section == null)
        return Array.Empty<HomeContentList>();

      return new List<HomeContentList>
      {
          new HomeContentList("Latest added", "added", true, null, null),
          new HomeContentList("Continue playing", "lastplayed", true, "started", null),
          new HomeContentList("Unplayed", "year", true, "unwatched", null),
          new HomeContentList("Favorites", null, false, null, "the server keeps no favourites over HTTP")
      };
    }
  }
}
