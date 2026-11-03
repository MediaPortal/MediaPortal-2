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
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One film of the collection the reading names, standing as its poster in the collection's row.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A card of this row is a <see cref="PersonCard"/> and not an <see cref="AudioTrackCard"/>: the poster is not
  /// there when the card is made, it arrives a moment later, which is what a property that tells its bindings about
  /// a change is for.
  /// </para>
  /// <para>
  /// The card is a place the viewer stands on and presses, unlike the framed collection button it grew out of: the
  /// reference's own collection screen was a list of the collection's films whose entries opened
  /// (<c>MediaItemListMovieCollectionDetails.inc</c>), and a row of posters that could not be walked would be a
  /// picture of a collection instead of the collection's films.  Standing on one therefore reads that film, and
  /// never starts it - the rule every other card of this reading already keeps.
  /// </para>
  /// <para>
  /// The whole <see cref="MediaTitle"/> travels with the card and not only its id, because pressing the card opens
  /// the film's reading, which needs the row the shelf would have stood on: its path, its playable answer, its
  /// watch flags.  The id alone could hang the poster but could not say what the shelf knows about the film.
  /// </para>
  /// </remarks>
  public sealed partial class CollectionFilmCard : ObservableObject
  {
    public CollectionFilmCard(MediaTitle source)
    {
      Source = source;
      Id = source.Id;
      Title = (source.Title ?? "").Trim();
    }

    /// <summary>The film's own item id, which is what its poster is filed under.</summary>
    public Guid Id { get; }

    /// <summary>The shelf's row for this film, which is what pressing its poster reads.</summary>
    public MediaTitle Source { get; }

    /// <summary>The film's name, which is what a viewer hovering over its poster wants to read.</summary>
    public string Title { get; }

    /// <summary>
    /// The film's poster, or null while it is on its way and for a library that has none of this film - in which
    /// case the plate stands empty, as an empty plate stands behind a shelf tile whose title carries no poster.
    /// </summary>
    [ObservableProperty]
    private byte[]? _poster;

    /// <summary>Whether the plate has a poster to draw.</summary>
    public bool ShowsPoster => Poster != null;

    partial void OnPosterChanged(byte[]? value) => OnPropertyChanged(nameof(ShowsPoster));
  }

  /// <summary>
  /// The films a collection holds, laid into the cards of the collection's row.
  /// </summary>
  public static class MediaCollectionFilms
  {
    /// <summary>
    /// The cards for one answer about a collection, in the order the server answered them.
    /// </summary>
    /// <param name="titles">The films carrying the collection's name, as the server's shelf query handed them out.</param>
    /// <remarks>
    /// The order is the server's and is kept: the question was asked with a sort word, and a row that re-sorted the
    /// films by hand would put a different film first than the one a shelf of the same collection puts first - and
    /// would stand a different film under the caret.  A title without a name keeps its card - its poster is the
    /// answer here, and a poster of a film nobody named is still a poster of a film the collection holds.
    /// </remarks>
    public static IList<CollectionFilmCard> CardsFor(IEnumerable<MediaTitle> titles)
    {
      List<CollectionFilmCard> cards = new List<CollectionFilmCard>();
      foreach (MediaTitle title in titles)
        cards.Add(new CollectionFilmCard(title));

      return cards;
    }
  }
}
