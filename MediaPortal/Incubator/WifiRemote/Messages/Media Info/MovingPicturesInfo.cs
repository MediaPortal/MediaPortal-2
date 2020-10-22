#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class MovingPicturesInfo : IAdditionalMediaInfo
  {
    public string MediaType => "movie";
    public string Id => ItemId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Movie;
    public int MpProviderId => (int)MpProviders.MPMovie;

    /// <summary>
    /// Movie ID in moving pictures database table "movie_info"
    /// </summary>
    public Guid ItemId { get; set; }
    /// <summary>
    /// Plot summary
    /// </summary>
    public string Summary { get; set; }
    /// <summary>
    /// Movie title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Alternate titles of this movie
    /// </summary>
    public string AlternateTitles { get; set; }
    /// <summary>
    /// Tagline of the movie
    /// </summary>
    public string Tagline { get; set; }
    /// <summary>
    /// Director of this movie
    /// </summary>
    public string Directors { get; set; }
    /// <summary>
    /// Writer of this movie
    /// </summary>
    public string Writers { get; set; }
    /// <summary>
    /// Actors in this movie
    /// </summary>
    public string Actors { get; set; }
    /// <summary>
    /// Online rating
    /// </summary>
    private string rating;
    public string Rating
    {
      get { return rating; }
      set
      {
        // Shorten to 3 chars, ie
        // 5.67676767 to 5.6
        if (value.Length > 3)
        {
          value = value.Remove(3);
        }
        rating = value;
      }
    }
    /// <summary>
    /// Number of online votes
    /// </summary>
    public string RatingCount { get; set; }
    /// <summary>
    /// Movie air date
    /// </summary>
    public int Year { get; set; }
    /// <summary>
    /// Genres of the movie
    /// </summary>
    public string Genres { get; set; }
    /// <summary>
    /// Certification of the movie
    /// </summary>
    public string Certification { get; set; }
    /// <summary>
    /// Movie poster filepath
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public MovingPicturesInfo(MediaItem mediaItem)
    {
      try
      {
        MovieInfo movie = new MovieInfo();
        movie.FromMetadata(mediaItem.Aspects);

        ItemId = mediaItem.MediaItemId;
        Title = movie.MovieName.Text;
        AlternateTitles = movie.OriginalName;
        Directors = String.Join(", ", movie.Directors);
        Writers = String.Join(", ", movie.Writers);
        Actors = String.Join(", ", movie.Actors);
        Genres = String.Join(", ", movie.Genres.Select(g => g.Name));
        Rating = Convert.ToString(movie.Rating.RatingValue ?? 0);
        RatingCount = Convert.ToString(movie.Rating.VoteCount ?? 0);
        Year = movie.ReleaseDate.Value.Year;
        Certification = movie.Certification;
        Tagline = movie.Tagline;
        Summary = movie.Summary.Text;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Movie, FanArtTypes.Cover);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting movie info", e);
      }
    }
  }
}
