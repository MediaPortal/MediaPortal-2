using MediaPortal.Common.General;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UiComponents.WMCSkin.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class SkinSettingsModel
  {
    public static readonly Guid MODEL_ID = new Guid("B074D623-D161-4F15-BE18-D73DD580B1BB");

    protected AbstractProperty _enableFanartProperty;
    protected SettingsChangeWatcher<WMCSkinSettings> _settingsWatcher;

    public SkinSettingsModel()
    {
      _enableFanartProperty = new WProperty(typeof(bool), false);
      _settingsWatcher = new SettingsChangeWatcher<WMCSkinSettings>();
      _settingsWatcher.SettingsChanged += OnSettingsChanged;
      UpdateProperties();
    }

    void OnSettingsChanged(object sender, EventArgs e)
    {
      UpdateProperties();
    }

    public AbstractProperty EnableFanartProperty
    {
      get { return _enableFanartProperty; }
    }

    public bool EnableFanart
    {
      get { return (bool)_enableFanartProperty.GetValue(); }
      set { _enableFanartProperty.SetValue(value); }
    }

    protected void UpdateProperties()
    {
      var settings = _settingsWatcher.Settings;
      EnableFanart = settings.EnableFanart;
    }
  }
}
