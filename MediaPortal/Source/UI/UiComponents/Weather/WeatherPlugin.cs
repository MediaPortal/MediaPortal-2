using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.UiComponents.Weather.Grabbers;

namespace MediaPortal.UiComponents.Weather
{
  public class WeatherPlugin : IPluginStateTracker
  {
    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceRegistration.Set<IWeatherCatcher>(new WorldWeatherOnlineCatcher());
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