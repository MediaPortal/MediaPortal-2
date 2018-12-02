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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  public class Autostart : YesNo
  {
    #region Constants

    protected const string AUTOSTART_REGISTER_NAME = "MediaPortal 2";

    #endregion

    #region Public properties

    public bool IsAutostart
    {
      get { return !string.IsNullOrEmpty(WindowsAPI.GetAutostartApplicationPath(AUTOSTART_REGISTER_NAME, true)); }
      set
      {
        try
        {
          string applicationPath = string.Format("\"{0}\" -a", ServiceRegistration.Get<IPathManager>().GetPath("<APPLICATION_PATH>"));
          if (value)
            WindowsAPI.AddAutostartApplication(applicationPath, AUTOSTART_REGISTER_NAME, true);
          else
            WindowsAPI.RemoveAutostartApplication(AUTOSTART_REGISTER_NAME, true);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Can't write autostart-value '{0}' to registry", ex, _yes);
        }
      }
    }

    #endregion

    #region Base overrides

    public override void Load()
    {
      _yes = IsAutostart;
    }

    public override void Save()
    {
      IsAutostart = _yes;
    }

    #endregion
  }
}
