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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about a set a movie belongs to
  /// </summary>
  public class SetStub
  {
    /// <summary>
    /// Name of the set this movie belongs to
    /// </summary>
    /// <example>"Harry Potter Collection"</example>
    public string Name { get; set; }

    /// <summary>
    /// Descpription of the set this movie belongs to
    /// </summary>
    /// <example>
    /// "The Harry Potter films are a fantasy series based on the series
    /// of seven Harry Potter novels by British writer J. K. Rowling."
    /// </example>
    public string Description { get; set; }

    /// <summary>
    /// An expression that was used to link this movie to the respective Set
    /// </summary>
    /// <remarks>
    /// This expression e.g. links all movies containing the term "Harry Potter" to the "Harry Potter Collection"
    /// </remarks>
    public string Rule { get; set; }

    /// <summary>
    /// Image describing the set
    /// </summary>

    public byte[] Image { get; set; }

    /// <summary>
    /// Order of the movie in the set
    /// </summary>
    /// <example>5</example>
    public int? Order { get; set; }  
  }
}
