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
  internal class MusicInfo : IAdditionalMediaInfo
  {
    public string MediaType => "music";
    public string MpExtId => ItemId.ToString();
    public int MpExtMediaType => (int)MpExtendedMediaTypes.MusicTrack;
    public int MpExtProviderId => (int)MpExtendedProviders.MPMusic;

    public Guid ItemId { get; set; }
    public string Album { get; set; }
    public string AlbumArtist { get; set; }
    public string Artist { get; set; }
    public int BitRate { get; set; }
    public string BitRateMode { get; set; }
    public int BPM { get; set; }
    public int Channels { get; set; }
    public string Codec { get; set; }
    public string Composer { get; set; }
    public string Conductor { get; set; }
    public DateTime DateTimePlayed { get; set; }
    public int DiscId { get; set; }
    public int DiscTotal { get; set; }
    public long Duration { get; set; }
    public string Genre { get; set; }
    public string Lyrics { get; set; }
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
    public int SampleRate { get; set; }
    public int TimesPlayed { get; set; }
    public string Title { get; set; }
    public int Track { get; set; }
    public int TrackTotal { get; set; }
    public int Year { get; set; }
    public string ImageName { get; set; }

    public MusicInfo(MediaItem song)
    {
      try
      {
        TrackInfo track = new TrackInfo();
        track.FromMetadata(song.Aspects);

        var mediaAspect = MediaItemAspect.GetAspect(song.Aspects, MediaAspect.Metadata);

        ItemId = song.MediaItemId;
        Album = track.Album;
        AlbumArtist = String.Join(", ", track.AlbumArtists);
        Artist = String.Join(", ", track.Artists);
        Composer = String.Join(", ", track.Composers);
        Conductor = String.Join(", ", track.Conductors);
        Genre = String.Join(", ", track.Genres.Select(g => g.Name));
        BitRate = track.BitRate;
        BitRateMode = string.Empty;
        BPM = 0;
        Channels = track.Channels;
        Codec = track.Encoding;
        DateTimePlayed = DateTime.Now;
        DiscId = track.DiscNum;
        DiscTotal = track.TotalDiscs;
        Duration = track.Duration;
        Lyrics = track.TrackLyrics;
        Rating = Convert.ToString(track.Rating.RatingValue ?? 0);
        RatingCount = Convert.ToString(track.Rating.VoteCount ?? 0);
        SampleRate = Convert.ToInt32(track.SampleRate);
        TimesPlayed = mediaAspect.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT);
        Title = track.TrackName;
        Track = track.TrackNum;
        TrackTotal = track.TotalTracks;
        Year = track.ReleaseDate.Value.Year;
        ImageName = Helper.GetImageBaseURL(song, FanArtMediaTypes.Audio, FanArtTypes.Cover);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting music info", e);
      }
    }
  }
}
