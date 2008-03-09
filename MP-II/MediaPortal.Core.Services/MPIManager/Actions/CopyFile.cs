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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.MPIManager;
using ICSharpCode.SharpZipLib.Zip;

namespace MediaPortal.Services.MPIManager.Actions
{
  public class CopyFile:IMPIFileAction
  {
    protected Dictionary<string, string> _dirs=new Dictionary<string,string>();
    
    public CopyFile()
    {
      _dirs.Add("%BaseDir%", AppDomain.CurrentDomain.BaseDirectory);
      _dirs.Add("%PluginsDir%", AppDomain.CurrentDomain.BaseDirectory+"\\Plugins\\");
      _dirs.Add("%ModelsDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Models\\");
      _dirs.Add("%ViewsDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Views\\");
      _dirs.Add("%SkinDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Skin\\");
    }
    /// <summary>
    /// Return a zip entry for a FileItem.
    /// </summary>
    /// <param name="item">The file item.</param>
    /// <returns></returns>
    public string GetZipEntry(IMPIFileItem item)
    {
      string x_ret = "";
      if(!string.IsNullOrEmpty(item.Param1))
      {
        if (item.Param1.EndsWith("\\"))
          x_ret = item.Param1;
        else
          x_ret = item.Param1 + "\\";
      }
      return x_ret+Path.GetFileName(item.FileName);
    }

    /// <summary>
    /// Return the dir entry for fileitem.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    public string GetDirEntry(IMPIFileItem item)
    {
      string x_ret = "";
      if (!string.IsNullOrEmpty(item.Param1))
      {
        string x_path = item.Param1;
        foreach (KeyValuePair<string, string> kp in _dirs)
        {
          x_path = x_path.Replace(kp.Key,kp.Value);
        }
        if (x_path.EndsWith("\\"))
          x_ret = x_path;
        else
          x_ret = x_path + "\\";
      }
      return x_ret + Path.GetFileName(item.FileName);
    }

    public string Description()
    {
      return "Copy a file to Param1 location";
    }
    
    public List<string> Param1List()
    {
      List<string> list = new List<string>();
      foreach (KeyValuePair<string, string> kp in _dirs)
      {
        list.Add(kp.Key);
      }
      return list;
    }
    
    public List<string> Param2List()
    {
      return null;//new List<string>();
    }

    public List<string> Param3List()
    {
      return null; //new List<string>();
    }

    public override string ToString()
    {
      return "CopyFile";
    }

    /// <summary>
    /// Installs the specified holder.
    /// </summary>
    /// <param name="holder">The holder strem.</param>
    /// <param name="fileItem">The file item.</param>
    /// <returns></returns>
    public bool Install(object holder, IMPIFileItem fileItem, IMPIPackage pak)
    {
      try
      {
        string tpf = GetDirEntry(fileItem);
        Directory.CreateDirectory(Path.GetDirectoryName(tpf));
        byte[] data = new byte[2048];
        int nb = data.Length;
        FileStream fs = new FileStream(tpf, FileMode.Create);
        while ((nb = ((ZipInputStream)holder).Read(data, 0, data.Length)) > 0)
        {
          fs.Write(data, 0, nb);
        }
        fs.Close();

        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public bool UnInstall(object holder, IMPIFileItem fileItem, IMPIPackage pak)
    {
      try
      {
        string tpf = GetDirEntry(fileItem);
        if (File.Exists(tpf))
        {
          File.Delete(tpf);
        }
        string dir=Path.GetDirectoryName(tpf);
        if (Directory.GetFiles(dir).GetLength(0) == 0)
        {
          Directory.Delete(dir, false);
        }
      }
      catch (Exception)
      {
      }
      return true;
    }
  }
}
