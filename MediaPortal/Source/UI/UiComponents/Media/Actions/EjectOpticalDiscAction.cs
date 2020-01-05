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

using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class EjectOpticalDiscAction : IWorkflowContributor
  {
    private string _opticalDrive;

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(Consts.RES_EJECT_OPTICAL_DISC_RES); }
    }

    public void Initialize()
    {
      foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.CDRom))
      {
        _opticalDrive = drive.Name;
        break;
      }
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      NavigationData navigationData = MediaNavigationModel.GetNavigationData(context, false);
      return navigationData != null && !string.IsNullOrEmpty(_opticalDrive);
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return !string.IsNullOrEmpty(_opticalDrive);
    }

    public void Execute()
    {
      var error = DriveUtils.EjectDrive(_opticalDrive);
      string driveLetter = _opticalDrive.Length >= 2 ? _opticalDrive.Substring(0, 2) : "?";
      switch (error)
      {
        case DriveUtils.DriveEjectError.InvalidMediaError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} is invalid!", driveLetter);
          break;
        case DriveUtils.DriveEjectError.NotFoundError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} does not exist!", driveLetter);
          break;
        case DriveUtils.DriveEjectError.LockError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be locked!", driveLetter);
          break;
        case DriveUtils.DriveEjectError.DismountError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be dismounted!", driveLetter);
          break;
        case DriveUtils.DriveEjectError.PreventRemovalError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be suspended!", driveLetter);
          break;
        case DriveUtils.DriveEjectError.EjectError:
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be ejected!", driveLetter);
          break;
      }
    }

    #endregion
  }
}
