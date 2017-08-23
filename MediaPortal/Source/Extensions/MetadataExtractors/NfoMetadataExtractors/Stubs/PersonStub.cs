#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about a person such as an actor or a director
  /// </summary>
  public class PersonStub
  {
    /// <summary>
    /// Name of the person
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Role of the person in this particular video
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Order of the person in which it should be displayed
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Picture of the person
    /// </summary>

    public byte[] Thumb { get; set; }

    /// <summary>
    /// ID of this person at www.imdb.com
    /// </summary>
    public string ImdbId { get; set; }

    /// <summary>
    /// Date of birth of this person
    /// </summary>
    public DateTime? Birthdate { get; set; }

    /// <summary>
    /// Place where this person was born
    /// </summary>
    public string Birthplace { get; set; }

    /// <summary>
    /// Date of death of this person
    /// </summary>
    public DateTime? Deathdate { get; set; }

    /// <summary>
    /// Place where this person died
    /// </summary>
    public string Deathplace { get; set; }

    /// <summary>
    /// Short biography of this person
    /// </summary>
    public string MiniBiography { get; set; }

    /// <summary>
    /// Long biography of this person
    /// </summary>
    public string Biography { get; set; }
  }
}
