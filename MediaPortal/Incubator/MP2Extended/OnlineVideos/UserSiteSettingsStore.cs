#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using OnlineVideos;
using OnlineVideos.Helpers;

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
