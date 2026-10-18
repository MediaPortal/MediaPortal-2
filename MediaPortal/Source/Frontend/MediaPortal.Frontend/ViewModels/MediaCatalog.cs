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
        ("EpisodeItem", "Episodes"),
        ("VideoItem", "Videos"),
        ("AudioItem", "Music"),
        ("AlbumItem", "Albums"),
        ("ImageItem", "Pictures")
    };

    public HomeSection(string label, AspectViewModel aspect)
    {
      Label = label;
      Aspect = aspect;
    }

    /// <summary>What the tile says.</summary>
    public string Label { get; }

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
}
