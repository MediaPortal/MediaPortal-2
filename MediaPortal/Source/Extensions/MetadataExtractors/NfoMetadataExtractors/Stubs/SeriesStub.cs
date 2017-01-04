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
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about a series as a whole
  /// </summary>
  public class SeriesStub
  {
    #region Information on internet databases

    /// <summary>
    /// ID of the series as a whole at thetvdb.com
    /// </summary>
    /// <example>"158661"</example>
    public int? Id { get; set; }

    /// <summary>
    /// Production Code Number of the series as a whole (http://en.wikipedia.org/wiki/Production_code_number)
    /// </summary>
    /// <example>"A"</example>
    public string ProductionCodeNumber { get; set; }
    
    /// <summary>
    /// Link to a ZIP file containing further information on the series as a whole
    /// </summary>
    /// <example>"http://thetvdb.com/api/1D62F2F90030C444/series/83462/all/de.zip"</example>
    public string EpisodeGuide { get; set; }

    #endregion

    #region Title information

    /// <summary>
    /// Title of the series as a whole
    /// </summary>
    /// <example>"Castle"</example>
    public string Title { get; set; }

    /// <summary>
    /// Title of the series as a whole (same as <see cref="Title"/>)
    /// </summary>
    /// <example>"Castle"</example>
    public string ShowTitle { get; set; }

    /// <summary>
    /// String to be used when this series is displayed in a sorted list of series
    /// </summary>
    /// <example>"Star Trek02"</example>
    public string SortTitle { get; set; }

    /// <summary>
    /// Collection of Sets this series belongs to
    /// </summary>
    public HashSet<SetStub> Sets { get; set; }

    #endregion 

    #region Making-of information

    /// <summary>
    /// Date when the first episode of the first season of this series aired
    /// </summary>
    public DateTime? Premiered { get; set; }

    /// <summary>
    /// Year in which the first episode of the first season of this series aired
    /// </summary>
    public DateTime? Year { get; set; }

    /// <summary>
    /// TV station on which the series was (first) broadcasted
    /// </summary>
    public string Studio { get; set; }

    /// <summary>
    /// Actors in this series
    /// </summary>
    public HashSet<PersonStub> Actors { get; set; }

    /// <summary>
    /// Information on whether or not all episodes of all seasons of the series have already been broadcasted
    /// </summary>
    /// <example>"Continuing"</example>
    public string Status { get; set; }

    #endregion

    #region Content information

    /// <summary>
    /// Full plot of the series
    /// </summary>
    public string Plot { get; set; }

    /// <summary>
    /// Short outline of the plot
    /// </summary>
    public string Outline { get; set; }

    /// <summary>
    /// Description of the series in one line
    /// </summary>
    public string Tagline { get; set; }

    /// <summary>
    /// Link to a trailer video
    /// </summary>
    public string Trailer { get; set; }

    /// <summary>
    /// Genre(s) of the series
    /// </summary>
    public HashSet<string> Genres { get; set; }

    #endregion

    #region Images

    /// <summary>
    /// Various thumbs for the series or particular seasons
    /// </summary>
    public HashSet<SeriesThumbStub> Thumbs { get; set; }

    #endregion

    #region Certification and ratings

    /// <summary>
    /// MPAA certification for one or multiple countries
    /// </summary>
    /// ToDo: We need a class that encapsulates the functionality of a (Mpaa) certification
    /// <example>{"DE:FSK 12", "DE:FSK12", "DE:12", "DE:ab 12"}</example>
    /// <example>{"12"}</example>
    public HashSet<string > Mpaa { get; set; }

    /// <summary>
    /// General rating of this series
    /// </summary>
    /// <example>8.7</example>
    public decimal? Rating { get; set; }

    /// <summary>
    /// Number of votes involved in Rating
    /// Only valid if there is a value for <see cref="Rating"/>
    /// </summary>
    public int? Votes { get; set; }

    /// <summary>
    /// Number in the Top 250 ranking from www.imdb.com
    /// </summary>
    public int? Top250 { get; set; }

    #endregion
  }
}
