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

namespace MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store information about a movie
  /// </summary>
  public class MovieStub
  {
    public bool Valid { get; set; } = true;

    /// <summary>
    /// Title of the movie as it should be displayed
    /// </summary>
    /// <example>"Harry Potter und der Orden des Phönix"</example>
    public string Title { get; set; }

    /// <summary>
    /// Disc name in windows explorer
    /// </summary>
    /// <example>"Movie"</example>
    public string DiscName { get; set; }

    /// <summary>
    /// Message to show when disc should be inserted
    /// </summary>
    /// <example>"Insert movie disc"</example>
    public string Message { get; set; }
  }
}
