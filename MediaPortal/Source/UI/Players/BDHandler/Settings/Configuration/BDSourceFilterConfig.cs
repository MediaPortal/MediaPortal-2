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
using System.Collections.Generic;
using MediaPortal.UI.Players.Video.Settings.Configuration;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.Plugins.BDHandler.Settings.Configuration
{
  public class BDSourceFilterConfig : GenericCodecSelection
  {
    #region Constants

    // One of these Source Filters has to be installed for BluRays being watchable
    public static CodecInfo[] SupportedSourceFilters =
    { 
      new CodecInfo
        {
        CLSID = "{083863F1-70DE-11d0-BD40-00A0C911CE86}",
        Name = "LAV Splitter Source"
      },
      new CodecInfo
        {
        CLSID = "{1365BE7A-C86A-473C-9A41-C0A6E82C9FA3}",
        Name = "MPC - Mpeg Source (Gabest)"
      }
    };

    #endregion

    #region Constructor

    public BDSourceFilterConfig()
      : base(
        new Guid[] { Guid.Empty },
        new Guid[] { Guid.Empty } // Currently a dynamic detection of Bluray Source Filters is not possible
        )
    {
      _codecList = new List<CodecInfo>();
    }

    #endregion

    #region GenericCodecSelection overrides

    protected override void GetAvailableFilters()
    {
      // Check whether one or more of the SupportedSourceFilters are installed and if so, add them to the codec list
      _codecList.Clear();
      foreach (CodecInfo codecInfo in SupportedSourceFilters)
        if (FilterGraphTools.IsThisComObjectInstalled(new Guid(codecInfo.CLSID)))
          _codecList.Add(codecInfo);
    }

    public override void Load()
    {
      // Load settings from the SettingsManager
      BDPlayerSettings settings = SettingsManager.Load<BDPlayerSettings>();
      if (settings != null && settings.BDSourceFilter != null)
        _currentSelection = settings.BDSourceFilter.GetCLSID();
      base.Load();
    }

    public override void Save()
    {
      // Save settings via the SettingsManager
      BDPlayerSettings settings = SettingsManager.Load<BDPlayerSettings>();
      settings.BDSourceFilter = _codecList[Selected];
      SettingsManager.Save(settings);
    }
  }

    #endregion
}
