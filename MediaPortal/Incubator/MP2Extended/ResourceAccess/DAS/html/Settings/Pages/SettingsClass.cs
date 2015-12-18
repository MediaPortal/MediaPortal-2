using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Settings.Pages
{
  partial class SettingsTemplate
  {
    private readonly Dictionary<string, SettingsDetailes> _settings = new Dictionary<string, SettingsDetailes>();
    private readonly string title = "Settings";
    private readonly string headLine = "Settings";
    private readonly string subHeadLine = "";

    public SettingsTemplate()
    {
      var properties = MP2Extended.Settings.GetType().GetProperties();
      foreach (var property in properties)
      {
        _settings.Add(property.Name, new SettingsDetailes {
          Value = property.GetValue(MP2Extended.Settings, null).ToString(),
          Type = property.PropertyType.Name
        });
      }
    }

    class SettingsDetailes
    {
      internal string Value { get; set; }
      internal string Type { get; set; }
    }
  }
}
