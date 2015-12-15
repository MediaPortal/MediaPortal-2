using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Settings.Pages
{
  partial class SettingsTemplate
  {
    private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();
    private readonly string title = "Settings";
    private readonly string headLine = "Settings";
    private readonly string subHeadLine = "";

    public SettingsTemplate()
    {
      var properties = MP2Extended.Settings.GetType().GetProperties();
      foreach (var property in properties)
      {
        _settings.Add(property.Name, property.GetValue(MP2Extended.Settings, null).ToString());
      }
    }
  }
}
