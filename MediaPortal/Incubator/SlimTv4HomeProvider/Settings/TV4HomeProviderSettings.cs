using MediaPortal.Core.Settings;

namespace MediaPortal.Plugins.SlimTvClient.Providers.Settings
{
  class TV4HomeProviderSettings
  {
    /// <summary>
    /// Holds the host name or IP adress of the TV4home service (running on same machine as TvServer).
    /// </summary>
    [Setting(SettingScope.User, "localhost")]
    public string TvServerHost { get; set; }
  }
}
