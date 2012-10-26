using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common;

namespace MediaPortal.UiComponents.News.Settings.Configuration
{
  public class NewsConfigSetting : CustomConfigSetting
  {
  }

  public class RefreshInterval : LimitedNumberSelect
  {
    public override void Load()
    {
      _type = NumberType.Integer;
      _step = 1;
      _lowerLimit = 1;
      _upperLimit = 600;
      _value = SettingsManager.Load<NewsSettings>().RefreshInterval;
    }

    public override void Save()
    {
      NewsSettings settings = SettingsManager.Load<NewsSettings>();
      settings.RefreshInterval = (int)_value;
      SettingsManager.Save(settings);
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        newsCollector.ChangeRefreshInterval((int)_value);
      }
    }
  }
}