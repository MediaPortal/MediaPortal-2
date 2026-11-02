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
  /// One card of a people band: a name, and the portrait that may arrive under it a moment later.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A card of the people bands is not a record like the track cards beside it, because a portrait is not there
  /// when the card is made. The reference drew the same two states: the card showed the library's portrait of the
  /// person and, where the library had none, the standing figure of <c>Person.png</c> in its place
  /// (<c>themes/default/styles/CurrentlyPlayingStyles.xaml:216-222</c>, the fallback made visible by
  /// <c>Path=!HasImage</c>). So the portrait goes onto a card that is already on the screen, which is what a
  /// property that tells its bindings about a change is for.
  /// </para>
  /// <para>
  /// The id travels with the card because that is the id the library files the portrait under - the reference
  /// asked for a person's picture by exactly this id and by no other means
  /// (<c>FanArtName="{Binding MediaItem.MediaItemId}"</c> at the same place).
  /// </para>
  /// </remarks>
  public sealed partial class PersonCard : ObservableObject
  {
    public PersonCard(Guid id, string name)
    {
      Id = id;
      Name = name;
    }

    /// <summary>The person's own item id, which is what their portrait is filed under.</summary>
    public Guid Id { get; }

    /// <summary>The name the library holds for the person, as it holds it.</summary>
    public string Name { get; }

    /// <summary>
    /// The person's portrait, or null while it is on its way and for a library that has none - in which case the
    /// card draws the skin's standing figure, as the reference did.
    /// </summary>
    [ObservableProperty]
    private byte[]? _portrait;

    /// <summary>Whether the portrait is what the card is drawing.</summary>
    public bool ShowsPortrait => Portrait != null;

    partial void OnPortraitChanged(byte[]? value) => OnPropertyChanged(nameof(ShowsPortrait));
  }

  /// <summary>
  /// The people a title names, laid into the cards of the two bands.
  /// </summary>
  public static class MediaPersons
  {
    /// <summary>
    /// The cards for one part of a title, in the order the library answered them.
    /// </summary>
    /// <param name="people">The answer for the part, as <see cref="MediaPersonsAnswer.PeopleOf"/> hands it out.</param>
    /// <remarks>
    /// A person the library never named is left out. A card of a nameless person is a card a viewer cannot read,
    /// cannot tell one from the next, and cannot walk past - and the band's height is paid for every card in it.
    /// The name is otherwise taken exactly as it came: nobody here shortens "Josefwynne" to a first name or puts a
    /// surname first because that would read better here.
    /// </remarks>
    public static IList<PersonCard> CardsFor(IEnumerable<PersonInfo> people)
    {
      List<PersonCard> cards = new List<PersonCard>();
      foreach (PersonInfo person in people)
      {
        string name = (person.Name ?? "").Trim();
        if (name.Length == 0)
          continue;

        cards.Add(new PersonCard(person.Id, name));
      }
      return cards;
    }
  }
}
