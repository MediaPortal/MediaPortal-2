using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class ScreenModeModel
  {
    public static readonly Guid MODEL_ID = new Guid("0E621AA5-0733-4E88-A0F0-887F8D73A055");

    protected AbstractProperty _currentScreenModeProperty;
    protected SettingsChangeWatcher<AppSettings> _settings;

    public ScreenModeModel()
    {
      Init();
      Attach();
    }

    public AbstractProperty CurrentScreenModeProperty
    {
      get { return _currentScreenModeProperty; }
    }

    public ScreenMode CurrentScreenMode
    {
      get { return (ScreenMode)_currentScreenModeProperty.GetValue(); }
      set { _currentScreenModeProperty.SetValue(value); }
    }

    void Init()
    {
      _currentScreenModeProperty = new WProperty(typeof(ScreenMode));
      _settings = new SettingsChangeWatcher<AppSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
      CurrentScreenMode = _settings.Settings.ScreenMode;
    }

    void Attach()
    {
      _currentScreenModeProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _currentScreenModeProperty.Detach(OnPropertyChanged);
    }

    private void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      SetScreenMode(CurrentScreenMode);
    }

    void OnSettingsChanged(object sender, EventArgs e)
    {
      Detach();
      CurrentScreenMode = _settings.Settings.ScreenMode;
      Attach();
    }

    public void ToggleScreenMode()
    {
      ScreenMode currentMode = CurrentScreenMode;
      int nextMode = ((int)currentMode) + 1;
      int totalModes = Enum.GetNames(typeof(ScreenMode)).Length;
      ScreenMode newMode = (ScreenMode)(nextMode % totalModes);
      ServiceRegistration.Get<ILogger>().Info("ScreenModeModel: Switching screen mode from current '{0}' to '{1}'", currentMode, newMode);
      SetScreenMode(newMode);
    }

    protected void SetScreenMode(ScreenMode mode)
    {
      IScreenControl sc = ServiceRegistration.Get<IScreenControl>();
      sc.SwitchMode(mode);
    }
  }
}
