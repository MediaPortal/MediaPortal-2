using MediaPortal.Core.Settings;

namespace MediaPortal.UiComponents.BackgroundManager.Settings
{
  public class LayoutManagerSettings
  {    
    /// <summary>
    /// Remembers the selected layout for user.
    /// </summary>
    [Setting(SettingScope.User, 1)]
    public int SelectedLayoutIndex { get; set; }
  }
}
