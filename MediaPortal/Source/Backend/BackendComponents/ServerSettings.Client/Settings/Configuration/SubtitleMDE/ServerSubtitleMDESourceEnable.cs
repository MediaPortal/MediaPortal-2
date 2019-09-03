#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using System.Collections.Generic;

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerSubtitleMDESourceEnable : MultipleSelectionList, IDisposable
  {
    private Dictionary<string, int> _dictionary = new Dictionary<string, int>();

    public ServerSubtitleMDESourceEnable()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
    }

    public override void Load()
    {
      if (!Enabled)
        return;

      _items.Clear();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      OnlineLibrarySettings settings = serverSettings.Load<OnlineLibrarySettings>();
      foreach(MatcherSetting setting in settings.SubtitleMatchers)
      {
        if (setting.Id.Equals("OpenSubtitlesMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("OpenSubtitles.org"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("MovieSubtitlesMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("MovieSubtitles.org"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("TvSubtitlesMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("TvSubtitles.net"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("SubsceneMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Subscene.com"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("PodnapisiMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Podnapisi.net"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("SublightMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Sublight.si"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("SubDbMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Thesubdb.com"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("SousTitresMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Sous-titres.eu"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("TitloviMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Titlovi.com"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        else if (setting.Id.Equals("TitulkyMatcher", StringComparison.InvariantCultureIgnoreCase))
        {
          _items.Add(LocalizationHelper.CreateStaticString("Titulky.com"));
          if (setting.Enabled)
            _selected.Add(_items.Count - 1);
        }
        _dictionary[setting.Id] = _items.Count - 1;
      }
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      OnlineLibrarySettings settings = serverSettings.Load<OnlineLibrarySettings>();
      foreach (MatcherSetting setting in settings.SubtitleMatchers)
      {
        setting.Enabled = _selected.Contains(_dictionary[setting.Id]);
      }
      serverSettings.Save(settings);
      localSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
