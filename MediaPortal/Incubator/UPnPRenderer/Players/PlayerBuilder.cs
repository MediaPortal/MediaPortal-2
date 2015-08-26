using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Extensions.UPnPRenderer.MediaItems;
using System;

namespace MediaPortal.Extensions.UPnPRenderer.Players
{
    /// <summary>
    /// Player builder for UPnPRenderer audio streams.
    /// </summary>
    public class UPnPRendererPlayerBuilder : IPlayerBuilder
    {
        #region IPlayerBuilder implementation

        public IPlayer GetPlayer(MediaItem mediaItem)
        {
            AudioItem audioItem = mediaItem as AudioItem;
            if (audioItem != null)
                return getAudioPlayer(audioItem);

            ImageItem imageItem = mediaItem as ImageItem;
            if (imageItem != null)
                return getImagePlayer(imageItem);

            return null;
        }

        #endregion

        IPlayer getAudioPlayer(AudioItem mediaItem)
        {
            UPnPRendererAudioPlayer player = new UPnPRendererAudioPlayer(/*mediaItem.PlayerSettings*/);
            try
            {
                player.SetMediaItem(mediaItem.GetResourceLocator(), null);
            }
            catch (Exception e)
            {
                ServiceRegistration.Get<ILogger>().Warn("UPnPRendererAudioPlayer: Unable to play audio stream", e);
                IDisposable disposablePlayer = player as IDisposable;
                if (disposablePlayer != null)
                    disposablePlayer.Dispose();
                throw;
            }
            return (IPlayer)player;
        }

        IPlayer getImagePlayer(ImageItem mediaItem)
        {
            UPnPRendererImagePlayer player = new UPnPRendererImagePlayer();
            player.NextItem(mediaItem, StartTime.AtOnce);
            return (IPlayer)player;
        }
    }
}
