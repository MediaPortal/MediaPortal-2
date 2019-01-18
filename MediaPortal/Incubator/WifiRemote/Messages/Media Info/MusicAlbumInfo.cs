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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class MusicAlbumInfo : IAdditionalMediaInfo
  {
    public string MediaType => "album";
    public string MpExtId => ItemId.ToString();
    public int MpExtMediaType => (int)MpExtendedMediaTypes.MusicAlbum;
    public int MpExtProviderId => (int)MpExtendedProviders.MPMusic;

    public Guid ItemId { get; set; }
    public string Artist { get; set; }
    public int Disc { get; set; }
    public int DiscTotal { get; set; }
    public string Genre { get; set; }
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
    public string Title { get; set; }
    public string Summery { get; set; }
    public int TrackTotal { get; set; }
    public int Year { get; set; }
    public string ImageName { get; set; }

    public MusicAlbumInfo(MediaItem mediaItem)
    {
      try
      {
        AlbumInfo album = new AlbumInfo();
        album.FromMetadata(mediaItem.Aspects);

        ItemId = mediaItem.MediaItemId;
        Summery = album.Description.Text;
        Artist = String.Join(", ", album.Artists);
        Genre = String.Join(", ", album.Genres.Select(g => g.Name));
        Disc = album.DiscNum;
        DiscTotal = album.TotalDiscs;
        Rating = Convert.ToString(album.Rating.RatingValue ?? 0);
        RatingCount = Convert.ToString(album.Rating.VoteCount ?? 0);
        Title = album.Album;
        TrackTotal = album.TotalTracks;
        Year = album.ReleaseDate.Value.Year;
        ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Album, FanArtTypes.Cover);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting album info", e);
      }
    }
  }
}
