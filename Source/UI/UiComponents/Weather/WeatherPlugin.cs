using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.UiComponents.Weather.Grabbers;
using MediaPortal.UiComponents.Weather.Settings;

namespace MediaPortal.UiComponents.Weather
{
  public class WeatherPlugin : IPluginStateTracker
  {
    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      WeatherSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<WeatherSettings>();
      ServiceRegistration.Set<IWeatherCatcher>(new WeatherDotComCatcher(
          settings.TemperatureUnit, settings.WindSpeed, ServiceRegistration.Get<IPathManager>().GetPath(settings.ParsefileLocation),
          settings.SkipConnectionTest));
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      ServiceRegistration.Remove<IWeatherCatcher>();
    }

    public void Continue() { }

    void IPluginStateTracker.Shutdown() { }

    #endregion
  }
}