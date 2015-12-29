extern alias OV;
using System;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using OV::OnlineVideos;
using OV::OnlineVideos.Helpers;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  public class UserSiteSettings
  {
    [Setting(SettingScope.User, null)]
    public SerializableDictionary<string, string> Entries { get; set; }
  }

  public class UserSiteSettingsStore : MarshalByRefObject, IUserStore
  {
    private readonly UserSiteSettings _settings;
    private bool _hasChanges;

    public UserSiteSettingsStore()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<UserSiteSettings>();
      if (_settings.Entries == null)
        _settings.Entries = new SerializableDictionary<string, string>();
    }

    public string GetValue(string key, bool decrypt = false)
    {
      string result = null;
      _settings.Entries.TryGetValue(key, out result);
      return (result != null && decrypt) ? EncryptionUtils.SymDecryptLocalPC(result) : result;
    }

    public void SetValue(string key, string value, bool encrypt = false)
    {
      _hasChanges = true;
      if (encrypt) value = EncryptionUtils.SymEncryptLocalPC(value);
      _settings.Entries[key] = value;
    }

    public void SaveAll()
    {
      if (_hasChanges)
      {
        ServiceRegistration.Get<ISettingsManager>().Save(_settings);
        _hasChanges = false;
      }
    }

    #region MarshalByRefObject overrides

    public override object InitializeLifetimeService()
    {
      // In order to have the lease across appdomains live forever, we return null.
      return null;
    }

    #endregion
  }
}