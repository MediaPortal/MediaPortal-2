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
using System.IO;

namespace CustomActions
{
  public class CustomActionRunner
  {
    private const string LAV_FILTERS_FILE_NAME = "LAVFilters.exe";
    private readonly IRunnerHelper _runnerHelper;

    public CustomActionRunner(IRunnerHelper runnerHelper)
    {
      _runnerHelper = runnerHelper;
    }

    public bool IsLavFiltersAlreadyInstalled()
    {
      string lavSplitterRegistryPath = @"CLSID\{171252A0-8820-4AFE-9DF8-5C92B2D66B04}\InprocServer32"; // LAV Splitter
      Version onlineVersion = new Version(0, 69, 0, 0); // major, minor, build, private

      string splitterPath = _runnerHelper.GetPathForRegistryKey(lavSplitterRegistryPath);
      if (string.IsNullOrEmpty(splitterPath) && !_runnerHelper.Exists(splitterPath))
      {
        return false;
      }

      return IsEqualOrHigherVersion(splitterPath, onlineVersion);
    }

    public bool IsLavFiltersDownloaded()
    {
      string downloadedUrl = "http://install.team-mediaportal.com/MP2/install/LAVFilters.exe";
      string tempLAVFileName = Path.Combine(Path.GetTempPath(), LAV_FILTERS_FILE_NAME);
      _runnerHelper.DownloadFileAndReleaseResources(downloadedUrl, tempLAVFileName);

      return _runnerHelper.Exists(tempLAVFileName);
    }

    public bool InstallLavFilters()
    {
      string arg = "/SILENT /SP-";
      int waitToComplete = 60000; // 1 minute
      string tempLAVFileName = Path.Combine(Path.GetTempPath(), LAV_FILTERS_FILE_NAME);

      return _runnerHelper.Start(tempLAVFileName, arg, waitToComplete);
    }

    private bool IsEqualOrHigherVersion(string pathToFile, Version onlineVersion)
    {
      int majorPart = _runnerHelper.GetFileMajorVersion(pathToFile);
      int minorPart = _runnerHelper.GetFileMinorVersion(pathToFile);
      int buildPart = _runnerHelper.GetFileBuildVersion(pathToFile);
      int privatePart = _runnerHelper.GetFilePrivateVersion(pathToFile);
      Version localSplitterVersion = new Version(majorPart, minorPart, buildPart, privatePart);

      return localSplitterVersion >= onlineVersion;
    }
  }
}
