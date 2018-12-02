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

using HomeEditor.Extensions;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;

namespace HomeEditor.Settings.Configuration
{
  public class HomeEditorConfig : CustomConfigSetting, IDisposable
  {
    public HomeEditorConfig()
    {
      //Only show this configuration if the current skin supports the home menu editor
      SkinChangeMonitor.Instance.RegisterConfiguration(this);
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
