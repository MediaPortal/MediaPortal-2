using MediaPortal.Core.Settings;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.Plugins.BDHandler.Settings
{
  /// <summary>
  /// Settings needed for BluRayPlayer and the BDInfo library.
  /// </summary>
  public class BDInfoSettings
  {
    public bool FilterLoopingPlaylists = false;
    public bool FilterShortPlaylists = true;
    public int FilterShortPlaylistsValue = 300;
    public bool KeepStreamOrder = false;
    public bool EnableSSIF = false;

    /// <summary>
    /// Gets or Sets the preferred AVC codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AVCCodec { get; set; }
  }
}