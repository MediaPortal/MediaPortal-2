#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.Services.Logging;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// Package action to copy all auto copy root directories to the target folder
  /// </summary>
  [PackageAction("CopyPackageDirectories")]
  public class CopyPackageDirectoriesPackageAction : CopyBasePackageAction
  {
    public override bool CheckValid(PackageRoot packageRoot, out string message)
    {
      int autoCopyDirCount = 0;
      foreach (var rootDirectory in packageRoot.RootDirectories)
      {
        if (rootDirectory.IsAutoCopyDirectory)
        {
          ++autoCopyDirCount;
          rootDirectory.SetUsed();
        }
      }
      if (autoCopyDirCount == 0)
      {
        message = "The package does not contain any auto copy directories";
        return false;
      }
      message = null;
      return true;
    }

    protected override void DoExecute(PackageActionContext context)
    {
      foreach (var rootDirectory in context.PackageRoot.RootDirectories)
      {
        if (rootDirectory.IsAutoCopyDirectory)
        {
          // get the real target
          var target = context.GetPath(rootDirectory.Name);
          if (String.IsNullOrEmpty(target))
          {
            throw new PackageExcecutionException(String.Format("No target path named '{0}' found", rootDirectory.Name));
          }

          // copy all files and directories recursively to target.
          // the top level target directory can exist already.
          CopyDirectory(context, rootDirectory.FullPath, target, OverwriteTarget, true, true, true);
        }
      }
    }
  }

  /// <summary>
  /// Deletes all directories which are present in the auto copy root directories from the target.
  /// </summary>
  [PackageAction("DeletePackageDirectories")]
  public class DeletePackageDirectoriesPackageAction : PackageAction
  {
    public override bool CheckValid(PackageRoot packageRoot, out string message)
    {
      int autoCopyDirCount = 0;
      foreach (var rootDirectory in packageRoot.RootDirectories)
      {
        if (rootDirectory.IsAutoCopyDirectory)
        {
          ++autoCopyDirCount;
          rootDirectory.SetUsed();
        }
      }
      if (autoCopyDirCount == 0)
      {
        message = "The package does not contain any auto copy directories";
        return false;
      }
      message = null;
      return true;
    }

    protected override void DoExecute(PackageActionContext context)
    {
      foreach (var rootDirectory in context.PackageRoot.RootDirectories)
      {
        if (rootDirectory.IsAutoCopyDirectory)
        {
          // get the real target
          var target = context.GetPath(rootDirectory.Name);
          if (String.IsNullOrEmpty(target))
          {
            throw new PackageExcecutionException(String.Format("No target path named '{0}' found", rootDirectory.Name));
          }

          // delete all files and directories on target that exists in the package
          foreach (var filePath in Directory.GetFiles(rootDirectory.FullPath))
          {
            var fileName = Path.GetFileName(filePath);
            if (String.IsNullOrEmpty(fileName))
              continue;
            var targetFilePath = Path.Combine(target, fileName);
            if (File.Exists(targetFilePath))
            {
              context.Log.Info("Deleting file {0}\\{1} at {2}", rootDirectory.RealName, fileName, targetFilePath);
              try
              {
                File.Delete(targetFilePath);
              }
              catch (IOException)
              {
                // in case of an IOException try again half a second later, sometimes this helps, if the file was locked by an ending process
                File.Delete(targetFilePath);
              }
            }
          }

          foreach (var dirPath in Directory.GetDirectories(rootDirectory.FullPath))
          {
            var dirName = Path.GetFileName(dirPath);
            if (String.IsNullOrEmpty(dirName))
              continue;
            var targetDirPath = Path.Combine(target, dirName);
            if (File.Exists(targetDirPath))
            {
              context.Log.Info("Deleting directory {0}\\{1} at {2}", rootDirectory.RealName, dirName, targetDirPath);
              try
              {
                Directory.Delete(targetDirPath, true);
              }
              catch (IOException)
              {
                // in case of an IOException try again half a second later, sometimes this helps, if the file was locked by an ending process
                Directory.Delete(targetDirPath, true);
              }
            }
          }
        }
      }
    }
  }
}
