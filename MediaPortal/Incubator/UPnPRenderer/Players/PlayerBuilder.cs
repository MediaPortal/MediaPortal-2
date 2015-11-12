using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UPnPRenderer.Players
{
  /// <summary>
  /// Player builder for UPnPRenderer audio streams.
  /// </summary>
  public class UPnPRendererPlayerBuilder : IPlayerBuilder
  {
    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return null;

      if (mimeType == UPnPRendererImagePlayer.MIMETYPE)
      {
        UPnPRendererImagePlayer imagePlayer = new UPnPRendererImagePlayer();
        imagePlayer.NextItem(mediaItem, StartTime.AtOnce);
        return imagePlayer;
      }
      return null;
    }

    #endregion
  }
}
