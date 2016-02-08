using System;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;
using MediaPortal.UI.Presentation.Players;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal class MessageNowPlaying : MessageNowPlayingBase, IMessage
  {
    public String Type
    {
      get { return "nowplaying"; }
    }
    

    public IAdditionalNowPlayingInfo MediaInfo
    {
      get
      {
        if (Helper.IsNowPlaying())
        {
          MediaItem mediaItem = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentMediaItem;

          if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
            return new NowPlayingMusic(mediaItem);
          // TODO Add Radio
          // TODO Add Dvd
          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
            return new NowPlayingMovingPictures(mediaItem);

          if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
            return new NowPlayingSeries(mediaItem);

            if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          return new NowPlayingVideo(mediaItem);
          // TODO: Add Tv

        }

        return null;
      }
    }

    /// <summary>
    /// Constructor. 
    /// </summary>
    public MessageNowPlaying()
    {

    }
  }
}