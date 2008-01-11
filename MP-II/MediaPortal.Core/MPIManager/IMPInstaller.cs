#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Core.MPIManager
{
  /// <summary>
  /// Interface for local MPI handing, like install, uninstall, enumerate 
  /// </summary>
  public interface IMPInstaller
  {
    
    Dictionary<string, IMPIFileAction> GetAllFileActions();

    IMPIFileAction GetFileAction(string action);

    /// <summary>
    /// Install a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    bool InstallPackage(IMPIPackage package);

    /// <summary>
    /// Uninstall a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    bool UnInstallPackage(IMPIPackage package);

    void RegisterAction(string actionName, IMPIFileAction action);

    IMPIPackage LoadPackageFromXML(string filename);

    IMPIPackage LoadPackageFromMPI(string filename);

    void ExecuteQueue();

    void AddToQueue(IMPIPackage package, string action);

    void RemoveFromQueue(IMPIPackage package);

    void LoadQueue();

    void SaveQueue();
  }
}
