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
using MediaPortal.Core.ExtensionManager;

namespace Components.ExtensionUpdater.ExtensionManager.Actions
{
  class CopyRegisterFile:CopyFile,IExtensionFileAction
  {
    public new bool Install(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      if (base.Install(holder, fileItem, pak))
      {
        try
        {
          System.Diagnostics.Process.Start(string.Format("regsvr32 /s {0}", GetDirEntry(fileItem)));
          return true;
        }
        catch (Exception)
        {
          return false;
        }
      }
      return false;
    }

    public new bool UnInstall(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      try
      {
        System.Diagnostics.Process.Start(string.Format("regsvr32 /u {0}", GetDirEntry(fileItem)));
      }
      catch (Exception)
      {
        return false;
      }
      return base.UnInstall(holder, fileItem, pak);
    }

    public new string Description()
    {
      return "Copy a file to Param1 location and \n register it with regsvr32";
    }
  }
}
