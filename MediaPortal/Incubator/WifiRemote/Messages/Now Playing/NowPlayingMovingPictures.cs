#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class NowPlayingMovingPictures : IAdditionalNowPlayingInfo
  {
    private bool movieFound = false;

    private string mediaType = "movie";

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return ItemId.ToString(); }
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.Movie; }
    }

    public int MpExtProviderId
    {
      get { return (int)MpExtendedProviders.MovingPictures; }
    }

    /// <summary>
    /// Movie ID in moving pictures database table "movie_info"
    /// </summary>
    public Guid ItemId { get; set; }

    private string summary;

    /// <summary>
    /// Plot summary
    /// </summary>
    public string Summary
    {
      get { return summary; }
      set { summary = value; }
    }

    private string title;

    /// <summary>
    /// Movie title
    /// </summary>
    public string Title
    {
      get { return title; }
      set { title = value; }
    }

    private string alternateTitles;

    /// <summary>
    /// Alternate titles of this movie
    /// </summary>
    public string AlternateTitles
    {
      get { return alternateTitles; }
      set { alternateTitles = value; }
    }

    private string tagline;

    /// <summary>
    /// Tagline of the movie
    /// </summary>
    public string Tagline
    {
      get { return tagline; }
      set { tagline = value; }
    }

    private string directors;

    /// <summary>
    /// Director of this movie
    /// </summary>
    public string Directors
    {
      get { return directors; }
      set { directors = value; }
    }

    private string writers;

    /// <summary>
    /// Writer of this movie
    /// </summary>
    public string Writers
    {
      get { return writers; }
      set { writers = value; }
    }

    private string actors;

    /// <summary>
    /// Actors in this movie
    /// </summary>
    public string Actors
    {
      get { return actors; }
      set { actors = value; }
    }


    private string rating;

    /// <summary>
    /// Online rating
    /// </summary>
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

    private int year;

    /// <summary>
    /// Movie air date
    /// </summary>
    public int Year
    {
      get { return year; }
      set { year = value; }
    }

    private string genres;

    /// <summary>
    /// Genres of the movie
    /// </summary>
    public string Genres
    {
      get { return genres; }
      set { genres = value; }
    }

    private string certification;

    /// <summary>
    /// Certification of the movie
    /// </summary>
    public string Certification
    {
      get { return certification; }
      set { certification = value; }
    }

    private string detailsUrl;

    /// <summary>
    /// Get more info about the movie at this URL
    /// </summary>
    public string DetailsUrl
    {
      get { return detailsUrl; }
      set { detailsUrl = value; }
    }

    private string imageName;

    /// <summary>
    /// Movie poster filepath
    /// </summary>
    public string ImageName
    {
      get { return imageName; }
      set { imageName = value; }
    }


    /// <summary>
    /// Constructor.
    /// </summary>
    public NowPlayingMovingPictures(MediaItem mediaItem)
    {
      try
      {
        movieFound = true;

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
        Year = movie.ReleaseDate.Value.Year;
        Certification = movie.Certification;
        Tagline = movie.Tagline;
        Summary = movie.Summary.Text;
        //DetailsUrl = match.DetailsURL;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Movie, FanArtTypes.Cover);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error getting now playing moving pictures: " + e.Message);
      }
    }

    /// <summary>
    /// Checks if the supplied filename is a moving pictures movie
    /// </summary>
    /// <returns></returns>
    public bool IsMovingPicturesMovie()
    {
      return movieFound;
    }
  }
}
