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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using MediaPortal.Core.ExtensionManager;

namespace MediaPortal.Plugins.ExtensionUpdater.ExtensionManager.Actions
{
  class CopyExecuteFile : CopyFile, IExtensionFileAction
  {
    public new bool Install(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      if (base.Install(holder, fileItem, pak))
      {
        Process p = null;
        try
        {
          p = new Process();
          string x_path = fileItem.Param2;
          foreach (KeyValuePair<string, string> kp in base._dirs)
          {
            x_path = x_path.Replace(kp.Key, kp.Value);
          }
          x_path = x_path.Replace(@"\\",@"\");
          p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GetDirEntry(fileItem));
          p.StartInfo.FileName = Path.GetFileName(GetDirEntry(fileItem));

          p.StartInfo.Arguments = x_path;
          p.Start();
          p.WaitForExit();
          return true;
        }
        catch (Exception ex)
        {
          System.Windows.Forms.MessageBox.Show(ex.Message);
          return false;
        }
      }
      return false;
    }

    public new bool UnInstall(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      return base.UnInstall(holder, fileItem, pak);
    }

    public new string Description()
    {
      return "Copy a file to Param1 location and \n execute it whit Param2 parameters";
    }

    public new List<string> Param2List()
    {
      return Param1List();
    }
  }
}
