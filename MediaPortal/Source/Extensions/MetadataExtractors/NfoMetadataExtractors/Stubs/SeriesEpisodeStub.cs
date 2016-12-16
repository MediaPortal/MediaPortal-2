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
  /// This stub class is used to store inforation about a particular episode of a series
  /// </summary>
  public class SeriesEpisodeStub
  {
    #region Information on internet databases

    /// <summary>
    /// ID of the episode at thetvdb.com
    /// </summary>
    /// <example>"2111911"</example>
    public int? UniqueId { get; set; }

    /// <summary>
    /// Production Code Number of the episode (http://en.wikipedia.org/wiki/Production_code_number)
    /// </summary>
    /// <example>"A12301"</example>
    public string ProductionCodeNumber { get; set; }

    /// <summary>
    /// ID of the series as a whole at thetvdb.com
    /// </summary>
    /// <example>"158661"</example>
    public int? Id { get; set; }

    #endregion

    #region Title information

    /// <summary>
    /// Title of the episode
    /// </summary>
    /// <example>"Blumen für Dein Grab"</example>
    public string Title { get; set; }

    /// <summary>
    /// Title of the series as a whole
    /// </summary>
    /// <example>"Castle"</example>
    public string ShowTitle { get; set; }

    /// <summary>
    /// Number of the season the episode belongs to
    /// </summary>
    /// <example>1</example>
    public int? Season { get; set; }

    /// <summary>
    /// Number of the episode
    /// </summary>
    /// <example>1</example>
    public int? Episode { get; set; }

    /// <summary>
    /// Number of the DVD episode
    /// </summary>
    /// <example>1</example>
    public decimal? DvdEpisode { get; set; }

    /// <summary>
    /// Number of the season the episode belongs to (in DVD order)
    /// </summary>
    /// <example>1</example>
    public int? DisplaySeason { get; set; }

    /// <summary>
    /// Number of the episode (in DVD order)
    /// </summary>
    /// <example>1</example>
    public int? DisplayEpisode { get; set; }

    /// <summary>
    /// Collection of Sets this episode belongs to
    /// </summary>
    public HashSet<SetStub> Sets { get; set; }

    #endregion 

    #region Making-of information

    /// <summary>
    /// Date when the first episode of the first season of this series aired
    /// </summary>
    public DateTime? Premiered { get; set; }

    /// <summary>
    /// Date when this particular episode aired
    /// </summary>
    public DateTime? Aired { get; set; }

    /// <summary>
    /// Year in which this particular episode aired
    /// </summary>
    public DateTime? Year { get; set; }

    /// <summary>
    /// TV station on which the series was (first) broadcasted
    /// </summary>
    public string Studio { get; set; }

    /// <summary>
    /// Actors in this episode
    /// </summary>
    public HashSet<PersonStub> Actors { get; set; }

    /// <summary>
    /// Full name of the director
    /// </summary>
    public string Director { get; set; }

    /// <summary>
    /// Name(s) of the writer(s)
    /// </summary>
    public HashSet<string> Credits { get; set; }

    /// <summary>
    /// Official runtime of the episode; does not have to be the same as the runtime of the episode file
    /// </summary>
    public TimeSpan? Runtime { get; set; }

    /// <summary>
    /// Information on whether or not all episodes of all seasons of the series have already been broadcasted
    /// </summary>
    /// <example>"Continuing"</example>
    public string Status { get; set; }

    #endregion

    #region Content information

    /// <summary>
    /// Full plot of the episode
    /// </summary>
    public string Plot { get; set; }

    /// <summary>
    /// Short outline of the plot
    /// </summary>
    public string Outline { get; set; }

    /// <summary>
    /// Description of the episode in one line
    /// </summary>
    public string Tagline { get; set; }

    /// <summary>
    /// Link to a trailer video
    /// </summary>
    public string Trailer { get; set; }

    #endregion

    #region Images

    public byte[] Thumb { get; set; }

    #endregion

    #region Certification and ratings

    /// <summary>
    /// MPAA certification for one or multiple countries
    /// </summary>
    /// ToDo: We need a class that encapsulates the functionality of a (Mpaa) certification
    /// <example>{"DE:FSK 12", "DE:FSK12", "DE:12", "DE:ab 12"}</example>
    /// <example>{"12"}</example>
    public HashSet<string> Mpaa { get; set; }

    /// <summary>
    /// General rating of this episode
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

    #region Media file information

    /// <summary>
    /// Collection of stream details in the given episode file
    /// </summary>
    public HashSet<StreamDetailsStub> FileInfo { get; set; }

    /// <summary>
    /// Position in the file where this particular episode starts
    /// </summary>
    public TimeSpan? EpBookmark { get; set; }

    #endregion

    #region User information

    /// <summary>
    /// Indicates whether this episode has already been watched
    /// </summary>
    public bool? Watched { get; set; }

    /// <summary>
    /// Number of times this episode has been watched
    /// </summary>
    public int? PlayCount { get; set; }

    /// <summary>
    /// Date on which this episode has been watched last time
    /// </summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>
    /// Position in the file where it is supposed to be resumed when played next time
    /// </summary>
    public TimeSpan? ResumePosition { get; set; }

    #endregion
  }
}
