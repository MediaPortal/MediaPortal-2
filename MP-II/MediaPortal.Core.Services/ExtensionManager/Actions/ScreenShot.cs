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
using System.IO;
using MediaPortal.Core.ExtensionManager;

namespace MediaPortal.Services.ExtensionManager.Actions
{
  public class ScreenShot : IExtensionFileAction
  {

    /// <summary>
    /// Return a zip entry for a FileItem.
    /// </summary>
    /// <param name="item">The file item.</param>
    /// <returns></returns>
    public string GetZipEntry(IExtensionFileItem item)
    {
      return "screenshots\\" + Path.GetFileName(item.FileName);
    }

    /// <summary>
    /// Return the dir entry for fileitem.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    public string GetDirEntry(IExtensionFileItem item)
    {
      return "screenshots\\" + Path.GetFileName(item.FileName);
    }

    public string Description()
    {
      return "Screenshots, accepted extension .jpg";
    }

    public List<string> Param1List()
    {
      return null;
    }

    public List<string> Param2List()
    {
      return null;
    }

    public List<string> Param3List()
    {
      return null;
    }

    /// <summary>
    /// Installs the specified holder.
    /// </summary>
    /// <param name="holder">The holder strem.</param>
    /// <param name="fileItem">The file item.</param>
    /// <returns></returns>
    public bool Install(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      return true;
    }

    public bool UnInstall(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      return true;
    }

  }
}
