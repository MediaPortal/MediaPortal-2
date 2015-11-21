using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.SkinEngine.Players;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class Helper
  {
    internal static bool IsNowPlaying()
    {
      bool isPlaying = false;
      if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
        isPlaying = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer != null;
        //isPlaying = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.PlaybackState == PlaybackState.Playing;
      return isPlaying;
    }

    internal static MediaItem GetMediaItemById(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);


      IFilter searchFilter = new MediaItemIdFilter(id);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = 1 };
      IList<MediaItem> items = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.Search(searchQuery, false);

      return items[0];
    }

    internal static void PlayMediaItem(Guid mediaItemGuid, int startPos)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);


      IFilter searchFilter = new MediaItemIdFilter(mediaItemGuid);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = 1 };
      IList<MediaItem> items = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.Search(searchQuery, false);

      if (items.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Info("PlayFile: No media item found");
        return;
      }

      PlayItemsModel.PlayItem(items[0]);

      SetPosition(startPos, true);
    }

    /// <summary>
    /// Set the player position to the given absolute percentage 
    /// </summary>
    /// <param name="position">position in %</param>
    /// <param name="absolute">absolute or relative to current position</param>
    internal static void SetPositionPercent(int position, bool absolute)
    {
      IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
      IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
      if (mediaPlaybackControl != null)
      {
        if (absolute)
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.Duration.TotalSeconds * ((float)position / 100));
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
        else
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.CurrentTime.TotalSeconds + (mediaPlaybackControl.Duration.TotalSeconds * ((float)position / 100)));
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
      }
    }

    /// <summary>
    /// Set the player position to the given absolute time (in s)
    /// </summary>
    /// <param name="position">position in s</param>
    /// <param name="absolute">absolute or relative to current position</param>
    internal static void SetPosition(double position, bool absolute)
    {
      IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
      IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
      if (mediaPlaybackControl != null)
      {
        if (absolute)
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(position);
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
        else
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.CurrentTime.TotalSeconds + position);
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
      }
    }
  }
}
