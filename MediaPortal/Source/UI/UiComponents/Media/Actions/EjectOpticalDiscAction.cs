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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.Utilities.FileSystem;
using Timer = System.Timers.Timer;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class EjectOpticalDiscAction : IWorkflowContributor
  {
    private string _opticalDrive;
    private Timer _checkTimer;

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(Consts.RES_EJECT_OPTICAL_DISC_RES); }
    }

    public void Initialize()
    {
      DetectOpticalDrive();

      _checkTimer = new Timer
      {
        AutoReset = true,
        Interval = 20000
      };
      _checkTimer.Elapsed += (sender, args) =>
      {
        DetectOpticalDrive();
      };
      _checkTimer.Enabled = true;
    }

    private void DetectOpticalDrive()
    {
      var drives = DriveInfo.GetDrives();
      if (drives.Any(d => d.Name == _opticalDrive))
        return; //Last found drive still exists

      _opticalDrive = drives.FirstOrDefault(d => d.DriveType == DriveType.CDRom)?.Name;
    }

    public void Uninitialize()
    {
      _checkTimer.Enabled = false;
      _checkTimer.Dispose();
    }

    public bool IsActionVisible(NavigationContext context)
    {
      if (string.IsNullOrEmpty(_opticalDrive))
        return false;

      //Allow action from OSD screens
      if (context?.Models?.Any(m => m.Value is BaseOSDPlayerModel) ?? false)
        return true;

      //Allow action from navigation views
      NavigationData navigationData = MediaNavigationModel.GetNavigationData(context, false);
      return navigationData != null;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return !string.IsNullOrEmpty(_opticalDrive);
    }

    public void Execute()
    {
      var path = _opticalDrive;

      //Eject the media for the currently playing video
      var currentPlayer = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext;
      var mediaItem = currentPlayer?.CurrentMediaItem;
      var mediaItemPath = GetRemovableMediaItemPath(mediaItem);
      if (!string.IsNullOrEmpty(mediaItemPath))
      {
        //Stop playback before eject
        currentPlayer?.Stop();
        path = mediaItemPath;
      }

      var error = DriveUtils.EjectDrive(path);
      string driveLetter = path.Length >= 2 ? path.Substring(0, 2) : "?";
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

    private string GetRemovableMediaItemPath(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return null;

      foreach (var pra in mediaItem.PrimaryResources)
      {
        var resPath = ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
        var dosPath = LocalFsResourceProviderBase.ToDosPath(resPath);
        if (string.IsNullOrEmpty(dosPath))
          continue;
        if (DriveUtils.IsDVD(dosPath))
          return dosPath;
      }
      return null;
    }

    #endregion
  }
}
