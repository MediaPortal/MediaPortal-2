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
using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Integration
{
  class PathManagerWrapper : IPathManager
  {
    private readonly Common.PathManager.IPathManager _pathManager;

    public PathManagerWrapper(bool isTVE3)
    {
      // Set custom folder for TVE3 or TVE3.5.
      string versionFolder = isTVE3 ? "v3.0" : "v3.5";

      _pathManager = ServiceRegistration.Get<Common.PathManager.IPathManager>();
      _pathManager.SetPath("TVCORE", "<DATA>\\SlimTVCore\\" + versionFolder);
      string path = GetPath("<TVCORE>");
      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);
    }

    public bool Exists(string label)
    {
      return _pathManager.Exists(label);
    }

    public void SetPath(string label, string pathPattern)
    {
      _pathManager.SetPath(label, pathPattern);
    }

    public string GetPath(string pathPattern)
    {
      return _pathManager.GetPath(pathPattern);
    }

    public void RemovePath(string label)
    {
      _pathManager.RemovePath(label);
    }

    public bool LoadPaths(string pathsFile)
    {
      return _pathManager.LoadPaths(pathsFile);
    }
  }
}
