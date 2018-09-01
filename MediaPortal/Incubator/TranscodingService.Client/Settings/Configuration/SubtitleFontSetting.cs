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

using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.ServerSettings.Settings;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.TranscodingService.Client.Settings.Configuration
{
  public class SubtitleFontSetting : SingleSelectionList, IDisposable
  {
    private readonly List<string> _fonts = new List<string>
    {
      "Arial",
      "Calibri",
      "SimSun",
      "Tahoma",
      "MS Gothic",
      "Malgun Gothic",
    };
    private const string RES_DEFAULT = "[Settings.Transcode.Default]";

    public SubtitleFontSetting()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
    }

    public override void Load()
    {
      if (!Enabled)
        return;

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();
      int selected = 0;
      _items.Clear();
      _items.Add(LocalizationHelper.CreateResourceString(RES_DEFAULT));
      foreach (var font in _fonts)
      {
        if (!string.IsNullOrEmpty(settings.SubtitleFont) && settings.SubtitleFont.ToUpperInvariant() == font.ToUpperInvariant())
          selected = _items.Count;
        _items.Add(LocalizationHelper.CreateStaticString(font));
      }
      Selected = selected;
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();
      string fontName = _items[Selected].Evaluate();
      string defaultFont = LocalizationHelper.Translate(RES_DEFAULT);
      settings.SubtitleFont = "";
      if (defaultFont != fontName)
      {
        settings.SubtitleFont = fontName;
      }
      serverSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
