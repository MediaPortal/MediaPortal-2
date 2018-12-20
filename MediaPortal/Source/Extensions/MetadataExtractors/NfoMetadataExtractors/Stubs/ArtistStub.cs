#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store information about an artist
  /// </summary>
  public class ArtistStub
  {
    /// <summary>
    /// Name of the person
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Order of the person in which it should be displayed
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Picture of the person
    /// </summary>
    public byte[] Thumb { get; set; }

    /// <summary>
    /// ID of this person at MusicBrainz
    /// </summary>
    public string MusicBrainzArtistId { get; set; }

    /// <summary>
    /// ID of this album at TheAudioDB
    /// </summary>
    /// <example>675</example>
    public long? AudioDbId { get; set; }

    /// <summary>
    /// Date of birth of this person
    /// </summary>
    public DateTime? Birthdate { get; set; }

    /// <summary>
    /// Date of death of this person
    /// </summary>
    public DateTime? Deathdate { get; set; }

    /// <summary>
    /// Date the group was formed
    /// </summary>
    public DateTime? Formeddate { get; set; }

    /// <summary>
    /// Date the group was disbanded
    /// </summary>
    public DateTime? Disbandeddate { get; set; }

    /// <summary>
    /// Long biography of this person
    /// </summary>
    public string Biography { get; set; }

    /// <summary>
    /// Genre(s) of the artist
    /// </summary>
    public HashSet<string> Genres { get; set; }
  }
}
