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

using System.Collections.Generic;

namespace MediaPortal.Core.ExtensionManager
{
  /// <summary>
  /// Interface for local MPI handing, like install, uninstall, enumerate 
  /// </summary>
  public interface IExtensionInstaller
  {
    
    Dictionary<string, IExtensionFileAction> GetAllFileActions();

    IExtensionFileAction GetFileAction(string action);

    /// <summary>
    /// Install a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    bool InstallPackage(IExtensionPackage package);

    /// <summary>
    /// Uninstall a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    bool UnInstallPackage(IExtensionPackage package);

    void RegisterAction(string actionName, IExtensionFileAction action);

    IExtensionPackage LoadPackageFromXML(string filename);

    IExtensionPackage LoadPackageFromMPI(string filename);

    void ExecuteQueue();

    void AddToQueue(IExtensionPackage package, string action);

    void RemoveFromQueue(IExtensionPackage package);

    void LoadQueue();

    void SaveQueue();

    bool IsUpdateWaiting(string extensionId);

    void Update(string extensionId);

  }
}
