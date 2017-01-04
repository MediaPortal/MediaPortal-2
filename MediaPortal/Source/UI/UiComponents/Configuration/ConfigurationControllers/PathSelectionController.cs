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
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Utilities;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="PathEntry"/> configuration setting.
  /// </summary>
  public class PathSelectionController : ConfigurationController
  {
    protected PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;

    public override Type ConfigSettingType
    {
      get { return typeof(PathEntry); }
    }

    public override void ExecuteConfiguration()
    {
      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();

      var pathEntry = Setting as PathEntry;
      if (pathEntry == null)
        return;

      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(Help.Evaluate(), pathEntry.PathType == PathEntry.PathSelectionType.File, false,
        string.IsNullOrEmpty(pathEntry.Path) ? null : LocalFsResourceProviderBase.ToResourcePath(pathEntry.Path),
        path =>
        {
          string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
          return !string.IsNullOrEmpty(choosenPath);
        });

      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath =>
        {
          pathEntry.Path = LocalFsResourceProviderBase.ToDosPath(choosenPath);
          Save();
          _pathBrowserCloseWatcher.Dispose();
          _pathBrowserCloseWatcher = null;
        },
        null);
    }

    public override bool IsSettingSupported(ConfigSetting setting)
    {
      return setting != null && setting is PathEntry;
    }
  }
}
