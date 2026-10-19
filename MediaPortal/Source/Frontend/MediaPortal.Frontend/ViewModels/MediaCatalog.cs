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

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The tiles a viewer sees on the home screen, and which part of the server's library each one opens.
  /// </summary>
  /// <remarks>
  /// A section is a name a person understands next to an aspect name the server understands. The two are not the
  /// same thing and never were: MP2's database calls films <c>MovieItem</c> and its skins said "Filme", and a
  /// home screen that printed the aspect names would be showing the viewer the schema of a database.
  /// <para>
  /// Only sections the server actually has are offered - see <see cref="For"/>. A "Movies" tile over a server
  /// that holds no movies is a tile that leads to an empty list, which reads as a broken front end rather than
  /// as an empty library.
  /// </para>
  /// </remarks>
  public sealed class HomeSection
  {
    /// <summary>
    /// The names a viewer reads, in the order they are arranged on the screen, for the aspects MP2 stores media
    /// under. An aspect missing from this list is not offered as a tile: <c>PersonItem</c>, <c>Genre</c> and the
    /// rest are how the library is organised internally, not what somebody sits down to watch.
    /// </summary>
    private static readonly (string Aspect, string Label)[] CATALOGUE =
    {
        ("MovieItem", "Movies"),
        ("SeriesItem", "TV Series"),
        ("VideoItem", "Videos"),
        ("AudioItem", "Music"),
        ("AlbumItem", "Albums"),
        ("ImageItem", "Pictures")
    };

    public HomeSection(string label, AspectViewModel aspect, string? sort = null, bool sortDescending = false,
        string? by = null, string? value = null)
    {
      Label = label;
      Aspect = aspect;
      Sort = sort;
      SortDescending = sortDescending;
      By = by;
      Value = value;
    }

    /// <summary>What the tile says.</summary>
    public string Label { get; }

    /// <summary>
    /// The category of the browse screen's picked value - <c>genre</c>, <c>year</c>, <c>actors</c>,
    /// <c>directors</c> or <c>writers</c> - or null for a shelf opened whole.
    /// </summary>
    /// <remarks>
    /// A shelf opened through a picked genre is the same shelf read through one value, and the pair travels as
    /// two properties because the server wants both halves or neither: a category whose value got lost would read
    /// the whole shelf under a heading naming one genre, which the API itself refuses, so the section refuses it
    /// first.
    /// </remarks>
    public string? By { get; }

    /// <summary>The value the shelf was narrowed to, or null. Only means anything with <see cref="By"/>.</summary>
    public string? Value { get; }

    /// <summary>
    /// The order the server should read this shelf in, in the API's own words, or null for the name order every
    /// shelf uses by default.
    /// </summary>
    /// <remarks>
    /// Only one menu entry uses this today: "Latest media" is a shelf of videos whose entire promise is the order,
    /// and opening it alphabetically would make the tile a liar. Everything else opens by name, as it always did.
    /// </remarks>
    public string? Sort { get; }

    /// <summary>
    /// Whether <see cref="Sort"/> runs newest-first. "Latest media" is latest first by definition - the same order
    /// read oldest-first would be the opposite list - so the direction belongs to the promise and not to whoever
    /// opens the shelf.
    /// </summary>
    public bool SortDescending { get; }

    /// <summary>
    /// The single character shown large on the tile. The first letter of the label, rather than a symbol from an
    /// icon font: the front end runs on machines whose fonts nobody chose, and a missing glyph is drawn as an
    /// empty box on exactly the platforms this port exists for.
    /// </summary>
    public string Badge
    {
      get { return string.IsNullOrEmpty(Label) ? "?" : Label.Substring(0, 1); }
    }

    /// <summary>The aspect behind the tile, as the server named it.</summary>
    public AspectViewModel Aspect { get; }

    /// <summary>How many titles the server has for it, in the same words the aspect list uses.</summary>
    public string CountText
    {
      get { return Aspect.CountText; }
    }

    public string Description
    {
      get
      {
        int? count = Aspect.Count;
        if (count == null)
          return Label + " - number unknown";
        return count.Value == 1 ? Label + " - 1 title" : Label + " - " + count.Value + " titles";
      }
    }

    /// <summary>
    /// The tiles for one server: every catalogued aspect it has, in the catalogue's order.
    /// </summary>
    public static List<HomeSection> For(IEnumerable<AspectViewModel> aspects)
    {
      List<HomeSection> sections = new List<HomeSection>();
      foreach ((string aspectName, string label) in CATALOGUE)
      {
        AspectViewModel? match = aspects.FirstOrDefault(aspect =>
            string.Equals(aspect.Name, aspectName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
          sections.Add(new HomeSection(label, match));
      }
      return sections;
    }

    /// <summary>
    /// The label a section of this catalogue carries, for a screen that has only the aspect name to go on.
    /// </summary>
    public static string LabelFor(string aspectName)
    {
      foreach ((string name, string label) in CATALOGUE)
        if (string.Equals(name, aspectName, StringComparison.OrdinalIgnoreCase))
          return label;
      return aspectName;
    }

  }

  /// <summary>
  /// What the aspects of a MediaPortal library contain, in the two ways a screen has to know it: what a tile is
  /// called, and what pressing one of its titles means.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The names here are the server's own spellings - the aspect names <c>GET api/library/aspects</c> reports - and
  /// not labels invented here, because the front end asks the server by those names and a table keyed by anything
  /// else would have to translate twice and get it wrong once.
  /// </para>
  /// <para>
  /// Pressing a row means two different things and always did in MP2: a film is pressed to watch it, an album is
  /// pressed to see what is on it. Nothing in the answer for one title says which kind it is - an album has no
  /// path, and neither has a film whose disc is out of the drive - but the aspect a list was read through says it
  /// exactly, and that is the same fact MP2's own directories are built on.
  /// </para>
  /// </remarks>
  public static class MediaCatalog
  {
    /// <summary>
    /// The aspects whose titles are not things to watch but collections of them, what is filed inside each one, and
    /// the order those titles are watched or listened to in.
    /// </summary>
    /// <remarks>
    /// A series leads straight to its episodes rather than to its seasons, which is what MP2 offers too: the
    /// episodes of a whole series sorted by season and number are one list a viewer walks with the arrow keys, and a
    /// screen they have to press through twice per series is a screen in the way of the thing they came to do. A
    /// season is still accepted as a parent by the server, because which of the two an importer files episodes
    /// under depends on where the metadata came from.
    /// </remarks>
    private static readonly (string Aspect, string ChildAspect, string ChildSort)[] CONTAINERS =
    {
        ("AlbumItem", "AudioItem", "track"),
        ("SeriesItem", "EpisodeItem", "episode"),
        ("SeasonItem", "EpisodeItem", "episode"),
        ("MovieCollectionItem", "MovieItem", "title")
    };

    /// <summary>
    /// Whether pressing a title of this aspect opens something rather than playing it.
    /// </summary>
    public static bool HoldsTitles(string? aspectName)
    {
      return FindContainer(aspectName) != null;
    }

    /// <summary>
    /// The aspect of the titles filed inside this one, or the aspect itself when nothing is filed inside it.
    /// </summary>
    public static string ChildAspectFor(string? aspectName)
    {
      (string Aspect, string ChildAspect, string ChildSort)? container = FindContainer(aspectName);
      return container?.ChildAspect ?? aspectName ?? "";
    }

    /// <summary>
    /// How the titles inside this aspect are ordered: tracks by their number, episodes by season and number, films
    /// of a collection by name. <c>title</c> for anything else, which is what a whole shelf is ordered by.
    /// </summary>
    public static string ChildSortFor(string? aspectName)
    {
      (string Aspect, string ChildAspect, string ChildSort)? container = FindContainer(aspectName);
      return container?.ChildSort ?? "title";
    }

    private static (string Aspect, string ChildAspect, string ChildSort)? FindContainer(string? aspectName)
    {
      if (string.IsNullOrEmpty(aspectName))
        return null;

      foreach ((string name, string child, string sort) in CONTAINERS)
        if (string.Equals(name, aspectName, StringComparison.OrdinalIgnoreCase))
          return (name, child, sort);

      return null;
    }

    /// <summary>
    /// What the rows of a list read through this aspect are called, in the words a status line uses: "12 of 40
    /// films", "9 of 9 tracks".
    /// </summary>
    /// <remarks>
    /// Not the tile's label, because a tile is a place somebody presses and this is a count of things: the tile
    /// called <i>Music</i> holds tracks, and "12 of 12 music" is not English. Falling back to the label means a
    /// server with an aspect this list has never met still gets a word, and it is the word its own tile uses.
    /// </remarks>
    public static string RowNounFor(string? aspectName)
    {
      foreach ((string name, string noun) in ROW_NOUNS)
        if (string.Equals(name, aspectName, StringComparison.OrdinalIgnoreCase))
          return noun;

      return HomeSection.LabelFor(aspectName ?? "").ToLowerInvariant();
    }

    private static readonly (string Aspect, string Noun)[] ROW_NOUNS =
    {
        ("MovieItem", "films"),
        ("SeriesItem", "series"),
        ("SeasonItem", "seasons"),
        ("EpisodeItem", "episodes"),
        ("VideoItem", "videos"),
        ("AudioItem", "tracks"),
        ("AlbumItem", "albums"),
        ("MovieCollectionItem", "films"),
        ("ImageItem", "pictures")
    };
  }
}
