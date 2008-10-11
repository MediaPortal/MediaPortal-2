#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.IO;
using System.Security;
using MediaPortal.Configuration.Settings.System;
using Microsoft.Win32;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Configuration.Settings;

namespace Components.Configuration.Settings.System
{
  public class Balloontips : YesNo
  {
    #region Public properties

    public override Type SettingsObjectType
    {
      get { return typeof(SystemSettings); }
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      base._yes = !((SystemSettings)settingsObject).Balloontips;
    }

    public override void Save(object settingsObject)
    {
      ((SystemSettings)settingsObject).Balloontips = !base._yes;
      Commit();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Commits the balloontip setting to the registry.
    /// </summary>
    private void Commit()
    {
      try
      {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"software\microsoft\windows\currentversion\explorer\advanced"))
        {
          if (base._yes)
            key.SetValue("EnableBalloonTips", 0, RegistryValueKind.DWord);
          else
            key.DeleteValue("EnableBalloonTips", false);
        }
      }
      catch (SecurityException ex)
      {
        ServiceScope.Get<ILogger>().Error("SecurityException: {0} -> Can't write balloontip-value \"{1}\" to registry.", ex.Message, base._yes);
      }
      catch (UnauthorizedAccessException ex)
      {
        ServiceScope.Get<ILogger>().Error("UnauthorizedAccessException: {0} -> Can't write balloontip-value \"{1}\" to registry.", ex.Message, base._yes);
      }
      catch (IOException ex)
      {
        ServiceScope.Get<ILogger>().Error("IOException: {0} -> Can't write balloontip-value \"{1}\" to registry.", ex.Message, base._yes);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Can't write balloontip-value \"{0}\" to registry.", ex, base._yes);
      }
    }

    #endregion

  }
}