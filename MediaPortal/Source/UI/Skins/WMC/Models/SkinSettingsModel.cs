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
    protected AbstractProperty _enableListWatchedFlagsProperty;
    protected AbstractProperty _enableGridWatchedFlagsProperty;
    protected AbstractProperty _enableCoverWatchedFlagsProperty;
    protected SettingsChangeWatcher<WMCSkinSettings> _settingsWatcher;

    public SkinSettingsModel()
    {
      _enableFanartProperty = new WProperty(typeof(bool), false);
      _enableListWatchedFlagsProperty = new WProperty(typeof(bool), false);
      _enableGridWatchedFlagsProperty = new WProperty(typeof(bool), false);
      _enableCoverWatchedFlagsProperty = new WProperty(typeof(bool), false);
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

    public AbstractProperty EnableListWatchedFlagsProperty
    {
      get { return _enableListWatchedFlagsProperty; }
    }

    public bool EnableListWatchedFlags
    {
      get { return (bool)_enableListWatchedFlagsProperty.GetValue(); }
      set { _enableListWatchedFlagsProperty.SetValue(value); }
    }

    public AbstractProperty EnableGridWatchedFlagsProperty
    {
      get { return _enableGridWatchedFlagsProperty; }
    }

    public bool EnableGridWatchedFlags
    {
      get { return (bool)_enableGridWatchedFlagsProperty.GetValue(); }
      set { _enableGridWatchedFlagsProperty.SetValue(value); }
    }

    public AbstractProperty EnableCoverWatchedFlagsProperty
    {
      get { return _enableCoverWatchedFlagsProperty; }
    }

    public bool EnableCoverWatchedFlags
    {
      get { return (bool)_enableCoverWatchedFlagsProperty.GetValue(); }
      set { _enableCoverWatchedFlagsProperty.SetValue(value); }
    }

    protected void UpdateProperties()
    {
      var settings = _settingsWatcher.Settings;
      EnableFanart = settings.EnableFanart;
      EnableListWatchedFlags = settings.EnableListWatchedFlags;
      EnableGridWatchedFlags = settings.EnableGridWatchedFlags;
      EnableCoverWatchedFlags = settings.EnableCoverWatchedFlags;
    }
  }
}
