using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;

namespace MediaPortal.UiComponents.Trakt.Settings
{
  public class TraktSettings
  {
    [Setting(SettingScope.User)]
    public bool EnableTrakt { get; set; }

    [Setting(SettingScope.User)]
    public TraktAuthentication Authentication { get; set; }
  }
}
