using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace WifiRemote
{
  internal class NowPlayingMusic : IAdditionalNowPlayingInfo
  {
    private string mediaType = "music";

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
      get { return (int)MpExtendedMediaTypes.MusicTrack; }
    }

    public int MpExtProviderId
    {
      get { return (int)MpExtendedProviders.MPMusic; }
    }

    public Guid ItemId { get; set; }
    public string Album { get; set; }
    public string AlbumArtist { get; set; }
    public string Artist { get; set; }
    public int BitRate { get; set; }
    public string BitRateMode { get; set; }
    public int BPM { get; set; }
    public int Channels { get; set; }
    public string Codec { get; set; }
    public string Comment { get; set; }
    public string Composer { get; set; }
    public string Conductor { get; set; }
    public DateTime DateTimeModified { get; set; }
    public DateTime DateTimePlayed { get; set; }
    public int DiscId { get; set; }
    public int DiscTotal { get; set; }
    public long Duration { get; set; }
    public string Genre { get; set; }
    public string Lyrics { get; set; }
    public int Rating { get; set; }
    public int SampleRate { get; set; }
    public int TimesPlayed { get; set; }
    public string Title { get; set; }
    public int Track { get; set; }
    public int TrackTotal { get; set; }
    public string URL { get; set; }
    public string WebImage { get; set; }
    public int Year { get; set; }
    public string ImageName { get; set; }

    public NowPlayingMusic(MediaItem song)
    {
      try
      {
        var audioAspect = song.Aspects[AudioAspect.ASPECT_ID];

        ItemId = song.MediaItemId;
        Album = (string)audioAspect[AudioAspect.ATTR_ALBUM];

        var audioAlbumArtists = (List<string>)audioAspect[AudioAspect.ATTR_ALBUMARTISTS];
        if (audioAlbumArtists != null)
          AlbumArtist = String.Join(", ", audioAlbumArtists.Cast<string>().ToArray());

        var audioArtists = (List<string>)audioAspect[AudioAspect.ATTR_ARTISTS];
        if (audioArtists != null)
          Artist = String.Join(", ", audioArtists.Cast<string>().ToArray());

        var audioComposers = (List<string>)audioAspect[AudioAspect.ATTR_COMPOSERS];
        if (audioComposers != null)
          Composer = String.Join(", ", audioComposers.Cast<string>().ToArray());

        BitRate = (int)audioAspect[AudioAspect.ATTR_BITRATE];
        BitRateMode =string.Empty;
        BPM = 0;
        Channels = 0;
        Codec = (string)audioAspect[AudioAspect.ATTR_ENCODING];
        Comment = string.Empty;
        Conductor = string.Empty;
        DateTimeModified = DateTime.Now;
        DateTimePlayed = DateTime.Now;
        DiscId = (int)(audioAspect[AudioAspect.ATTR_DISCID] ?? 0);
        DiscTotal = (int)(audioAspect[AudioAspect.ATTR_NUMDISCS] ?? 0);
        Duration = (long)audioAspect[AudioAspect.ATTR_DURATION];

        var audioGenres = (List<string>)audioAspect[AudioAspect.ATTR_GENRES];
        if (audioGenres != null)
          Genre = String.Join(", ", audioGenres.Cast<string>().ToArray());

        Lyrics = String.Empty;
        Rating = 0;
        SampleRate = 0;
        TimesPlayed = (int)song[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT];
        Title = (string)song[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
        Track = (int)audioAspect[AudioAspect.ATTR_TRACK];
        TrackTotal = (int)audioAspect[AudioAspect.ATTR_NUMTRACKS];
        URL = String.Empty;
        WebImage = String.Empty;
        Year = 0;

        //ImageName = MediaPortal.Util.Utils.GetAlbumThumbName(song.Artist, song.Album);
        //ImageName = MediaPortal.Util.Utils.ConvertToLargeCoverArt(ImageName);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error getting now playing music: " + e.Message);
      }
    }
  }
}