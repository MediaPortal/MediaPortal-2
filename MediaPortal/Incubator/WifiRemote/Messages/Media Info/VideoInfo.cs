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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  public class VideoInfo : IAdditionalMediaInfo
  {
    public string MediaType => "video";
    public string MpExtId => ItemId.ToString();
    public int MpExtMediaType => (int)MpExtendedMediaTypes.Movie; 
    public int MpExtProviderId => (int)MpExtendedProviders.MPVideo; 

    /// <summary>
    /// ID of the video
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
    /// Director of this video
    /// </summary>
    public string Directors { get; set; }
    /// <summary>
    /// Writer of this video
    /// </summary>
    public string Writers { get; set; }
    /// <summary>
    /// Actors in this video
    /// </summary>
    public string Actors { get; set; }
    /// <summary>
    /// Video poster
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public VideoInfo(MediaItem mediaItem)
    {
      MovieInfo movie = new MovieInfo();
      movie.FromMetadata(mediaItem.Aspects);

      ItemId = mediaItem.MediaItemId;
      Title = movie.MovieName.Text;
      Directors = String.Join(", ", movie.Directors);
      Writers = String.Join(", ", movie.Writers);
      Actors = String.Join(", ", movie.Actors);
      Summary = movie.Summary.Text;
      ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Undefined, FanArtTypes.Thumbnail);
    }
  }
}
