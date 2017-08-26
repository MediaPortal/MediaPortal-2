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
  /// This stub class is used to store inforation about a movie
  /// </summary>
  public class MovieStub
  {
    #region Information on internet databases

    /// <summary>
    /// ID of this movie at www.imdb.com
    /// </summary>
    /// <example>"tt0373889"</example>
    public string Id { get; set; }

    /// <summary>
    /// ID of this movie at www.imdb.com
    /// </summary>
    /// <example>"tt0373889"</example>
    public string Imdb { get; set; }

    /// <summary>
    /// ID of this movie at www.imdb.com
    /// </summary>
    /// <example>"tt0373889"</example>
    public string IdsImdbId { get; set; }

    /// <summary>
    /// ID of this movie at www.themoviedb.org
    /// </summary>
    /// <example>675</example>
    public int? TmdbId { get; set; }

    /// <summary>
    /// ID of this movie at www.themoviedb.org
    /// </summary>
    /// <example>675</example>
    public int? Thmdb { get; set; }

    /// <summary>
    /// ID of this movie at www.themoviedb.org
    /// </summary>
    /// <example>675</example>
    public int? IdsTmdbId { get; set; }

    /// <summary>
    /// ID of this movie at www.allocine.fr
    /// </summary>
    /// <example>58608</example>
    public int? Allocine { get; set; }

    /// <summary>
    /// ID of this movie at passion-xbmc.org/scraper
    /// </summary>
    /// <example>58608</example>
    public int? Cinepassion { get; set; }

    #endregion

    #region Title information

    /// <summary>
    /// Title of the movie as it should be displayed
    /// </summary>
    /// <example>"Harry Potter und der Orden des Phönix"</example>
    public string Title { get; set; }

    /// <summary>
    /// Original title of the movie
    /// </summary>
    /// <example>"Harry Potter and the Order of the Phoenix"</example>
    public string OriginalTitle { get; set; }

    /// <summary>
    /// String to be used when this movie is displayed in a sorted list of movies
    /// </summary>
    /// <example>"Harry Potter Collection05"</example>
    public string SortTitle { get; set; }

    /// <summary>
    /// Collection of Sets this movie belongs to
    /// </summary>
    public HashSet<SetStub> Sets { get; set; }

    #endregion 

    #region Making-of information

    /// <summary>
    /// Date of first theatrical release
    /// </summary>
    public DateTime? Premiered { get; set; }

    /// <summary>
    /// Date of first theatrical release
    /// </summary>
    public DateTime? Year { get; set; }

    /// <summary>
    /// Countries in which the movie was produced
    /// </summary>
    public HashSet<string> Countries { get; set; }

    /// <summary>
    /// Producing Companies
    /// </summary>
    public HashSet<string> Companies { get; set; }

    /// <summary>
    /// Producing Studios
    /// </summary>
    public HashSet<string> Studios { get; set; }

    /// <summary>
    /// Actors in this movie
    /// </summary>
    public HashSet<PersonStub> Actors { get; set; }

    /// <summary>
    /// Producers of this movie
    /// </summary>
    public HashSet<PersonStub> Producers { get; set; }

    /// <summary>
    /// Full name of the director
    /// </summary>
    public string Director { get; set; }

    /// <summary>
    /// ID of the director at www.imdb.com
    /// </summary>
    public string DirectorImdb { get; set; }
    
    /// <summary>
    /// Name(s) of the writer(s)
    /// </summary>
    public HashSet<string> Credits { get; set; }

    /// <summary>
    /// Official runtime of the movie; does not have to be the same as the runtime of the movie file
    /// </summary>
    public TimeSpan? Runtime { get; set; }

    #endregion

    #region Content information

    /// <summary>
    /// Full plot of the movie
    /// </summary>
    public string Plot { get; set; }

    /// <summary>
    /// Short outline of the plot
    /// </summary>
    public string Outline { get; set; }

    /// <summary>
    /// Description of the movie in one line
    /// </summary>
    public string Tagline { get; set; }

    /// <summary>
    /// Link to a trailer video
    /// </summary>
    public string Trailer { get; set; }

    /// <summary>
    /// Genre(s) of the movie
    /// </summary>
    public HashSet<string> Genres { get; set; }

    /// <summary>
    /// Languages spoken in the original version of the movie
    /// </summary>
    public HashSet<string> Languages { get; set; }

    #endregion

    #region Images

    public byte[] Thumb { get; set; }
    public HashSet<byte[]> FanArt { get; set; }
    public HashSet<byte[]> DiscArt { get; set; }
    public HashSet<byte[]> Logos { get; set; }
    public HashSet<byte[]> ClearArt { get; set; }
    public HashSet<byte[]> Banners { get; set; }
    public HashSet<byte[]> Landscape { get; set; }

    #endregion

    #region Certification and ratings

    /// <summary>
    /// MPAA certification for multiple countries
    /// </summary>
    /// ToDo: We need a class that encapsulates the functionality of a (Mpaa) certification
    /// <example>{"DE:FSK 12", "DE:FSK12", "DE:12", "DE:ab 12"}</example>
    /// <example>{"12"}</example>
    public HashSet<string> Certification { get; set; }

    /// <summary>
    /// MPAA certification for one or multiple countries
    /// </summary>
    /// ToDo: We need a class that encapsulates the functionality of a (Mpaa) certification
    /// <example>{"DE:FSK 12", "DE:FSK12", "DE:12", "DE:ab 12"}</example>
    /// <example>{"12"}</example>
    public HashSet<string > Mpaa { get; set; }

    /// <summary>
    /// Ratings of this movie on specific websites
    /// </summary>
    /// <example>KeyValuePair("IMDB", 8.7)</example>
    public Dictionary<string, decimal> Ratings { get; set; }

    /// <summary>
    /// General rating of this movie
    /// </summary>
    /// <example>8.7</example>
    public decimal? Rating { get; set; }

    /// <summary>
    /// Number of votes involved in Rating
    /// Only valid if there is a value for <see cref="Rating"/>
    /// </summary>
    public int? Votes { get; set; }

    /// <summary>
    /// A review of this movie
    /// </summary>
    public string Review { get; set; }

    /// <summary>
    /// Number in the Top 250 ranking from www.imdb.com
    /// </summary>
    public int? Top250 { get; set; }

    #endregion

    #region Media file information

    /// <summary>
    /// Frames per second in the movie file
    /// </summary>
    public decimal? Fps { get; set; }

    /// <summary>
    /// Information about the movie file quality
    /// </summary>
    /// <example>"Bluray"</example>
    public string Rip { get; set; }

    /// <summary>
    /// Collection of stream details in the given movie file
    /// </summary>
    public HashSet<StreamDetailsStub> FileInfo { get; set; }

    /// <summary>
    /// Position in the file where this particular movie starts
    /// </summary>
    /// <remarks>
    /// The meaning of this element is not entirely clear. Usually it is used for Series within an "episodedetails" element
    /// when there is more than one episode in one file. In the context of movies, this element is contained (as an empty
    /// element) in many nfo-files. But we have not found an example of a movie.nfo file with a meaningful epbookmark element, yet.
    /// </remarks>
    public TimeSpan? EpBookmark { get; set; }

    #endregion

    #region User information

    /// <summary>
    /// Indicates whether this movie has already been watched
    /// </summary>
    public bool? Watched { get; set; }

    /// <summary>
    /// Number of times this movie has been watched
    /// </summary>
    public int? PlayCount { get; set; }

    /// <summary>
    /// Date on which this movie has been watched last time
    /// </summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>
    /// Date on which this move has been added to the databes of the program which created the nfo-file
    /// </summary>
    public DateTime? DateAdded { get; set; }

    /// <summary>
    /// Position in the file where it is supposed to be resumed when played next time
    /// </summary>
    public TimeSpan? ResumePosition { get; set; }

    #endregion
  }
}
