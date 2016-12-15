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

using MediaPortal.Common.Settings;
using System;

namespace MediaPortal.Common.FanArt
{
  [Serializable]
  public class FanArtSettings
  {
    #region Protected fields

    protected int _maxPosterFanArt = 1;
    protected int _maxBannerFanArt = 1;
    protected int _maxBackdropFanArt = 3;
    protected int _maxThumbFanArt = 1;
    protected int _maxClearArt = 1;
    protected int _maxDiscArt = 1;
    protected int _maxLogoFanArt = 1;

    #endregion

    #region Properties

    [Setting(SettingScope.Global)]
    public int MaxPosterFanArt
    {
      get { return _maxPosterFanArt; }
      set { _maxPosterFanArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxBannerFanArt
    {
      get { return _maxBannerFanArt; }
      set { _maxBannerFanArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxBackdropFanArt
    {
      get { return _maxBackdropFanArt; }
      set { _maxBackdropFanArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxThumbFanArt
    {
      get { return _maxThumbFanArt; }
      set { _maxThumbFanArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxClearArt
    {
      get { return _maxClearArt; }
      set { _maxClearArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxDiscArt
    {
      get { return _maxDiscArt; }
      set { _maxDiscArt = value; }
    }

    [Setting(SettingScope.Global)]
    public int MaxLogoFanArt
    {
      get { return _maxLogoFanArt; }
      set { _maxLogoFanArt = value; }
    }

    #endregion
  }
}
