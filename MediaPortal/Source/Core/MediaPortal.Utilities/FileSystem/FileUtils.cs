#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MediaPortal.Utilities.FileSystem
{
  /// <summary>
  /// Contains file and directory related utility methods.
  /// </summary>
  public class FileUtils
  {
    /// <summary>
    /// Combines two paths. This method differs from
    /// <see cref="System.IO.Path.Combine(string, string)"/> in that way that it
    /// removes relative path navigations to the parent path in the
    /// <paramref name="path2"/> path.
    /// </summary>
    /// <example>
    /// CombinePaths(@"C:\Program Files\", "MediaPortal") returns @"C:\Program Files\MediaPortal",
    /// CombinePaths(@"C:\Program Files\MediaPortal", "..\abc") returns @"C:\Program Files\abc",
    /// </example>
    /// <param name="path1">First path to combine with the second one. If this path is
    /// <c>null</c>, the second path will be returned.</param>
    /// <param name="path2">Second path to be combined with the first one. This path may contain
    /// relative path navigation elements like <c>@".."</c>. If this path is <c>null</c>,
    /// null will be returned.</param>
    /// <returns>
    /// Combined paths as string. If the <paramref name="path1"/> is null, <paramref name="path2"/>
    /// will be returned. If <paramref name="path2"/> is null, null will be returned. Otherwise,
    /// the combination of the paths will be returned, as described.
    /// </returns>
    /// <exception cref="ArgumentException">If the two paths cannot be combined (For example if
    /// the paths contain illegal characters or if the relative navigation of the second path
    /// cannot be applied to the first one.</exception>
    public static string CombinePaths(string path1, string path2)
    {
      if (path1 == null) return path2;
      if (path2 == null) return null;
      if (Path.IsPathRooted(path2))
        return path2;
      path1 = RemoveTrailingPathDelimiter(path1);
      while (path2.StartsWith(@"..\") || path2.StartsWith("../"))
      {
        path2 = path2.Substring(3);
        // Find the last / or \ character, then trim the base path
        int pos = Math.Max(path1.LastIndexOf(@"\"), path1.LastIndexOf(@"/"));
        if (pos > 0)
        {
          if (path1 == @"\\")
            throw new ArgumentException("Cannot combine paths '{0}' and '{1}'");
          path1 = path1.Remove(pos);
        }
      }
      if (path1.Length == 2 && path1[1] == ':')
        path1 += @"\";
      return Path.Combine(path1, path2);
    }

    /// <summary>
    /// Removes a trailing slash or backslash from the given path string.
    /// </summary>
    /// <param name="path">A path string with or without trailing path delimiter (@"\" or "/").</param>
    /// <returns><paramref name="path"/> with removed path delimiter(s), if there were any.</returns>
    public static string RemoveTrailingPathDelimiter(string path)
    {
      if (path == null) return string.Empty;
      if (path.Length == 0) return string.Empty;
      while (HasPathDelimiter(path))
        path = path.Remove(path.Length - 1);
      return path;
    }

    /// <summary>
    /// Checks if a trailing slash or backslash is contained in the given path string and adds one, if not present yet.
    /// </summary>
    /// <param name="path">A path string with or without trailing path delimiter (@"\" or "/").</param>
    /// <returns><paramref name="path"/> with path delimiter.</returns>
    public static string CheckTrailingPathDelimiter(string path)
    {
      if (path == null) return string.Empty;
      if (path.Length == 0) return string.Empty;
      return HasPathDelimiter(path) ? path : (path + Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Returns the information if the given path has a path delimiter at its end.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>true, if the specified <paramref name="path"/> has a path delimiter at its end,
    /// otherwise false</returns>
    public static bool HasPathDelimiter(string path)
    {
      return path.Length > 0 && (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Returns all files in the specified directory. The method traverses recursively
    /// through the directory tree.
    /// </summary>
    /// <param name="directoryPath">Directory path to start the search from.</param>
    /// <returns>List of paths of all file which exist under the specified directory
    /// in the local file system.</returns>
    public static IList<string> GetAllFilesRecursively(string directoryPath)
    {
      IList<string> result = Directory.GetFiles(directoryPath).ToList();
      foreach (string filePath in Directory.GetDirectories(directoryPath).SelectMany(GetAllFilesRecursively))
        result.Add(filePath);
      return result;
    }

    /// <summary>
    /// Copies all content of the <paramref name="sourceDirName"/> into the <paramref name="destDirName"/>. If the destination does not exists,
    /// it will be created first. By setting <paramref name="copySubDirs"/>=<c>true</c>, all subfolders will be copied as well.
    /// </summary>
    /// <param name="sourceDirName">Source directory.</param>
    /// <param name="destDirName">Target directory.</param>
    /// <param name="copySubDirs"><c>true</c> to include all sub directories.</param>
    public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
    {
      DirectoryInfo dir = new DirectoryInfo(sourceDirName);
      DirectoryInfo[] dirs = dir.GetDirectories();

      if (!dir.Exists)
        throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

      if (!Directory.Exists(destDirName))
        Directory.CreateDirectory(destDirName);

      FileInfo[] files = dir.GetFiles();
      foreach (FileInfo file in files)
      {
        string temppath = Path.Combine(destDirName, file.Name);
        file.CopyTo(temppath, false);
      }

      if (!copySubDirs)
        return;

      foreach (DirectoryInfo subdir in dirs)
      {
        string temppath = Path.Combine(destDirName, subdir.Name);
        CopyDirectory(subdir.FullName, temppath, true);
      }
    }

    /// <summary>
    /// Returns <c>true</c>, if the given complete paths <paramref name="path1"/> and <paramref name="path2"/>
    /// describe the same position in the file system, else <c>false</c>.
    /// </summary>
    /// <param name="path1">Path one.</param>
    /// <param name="path2">Path two.</param>
    /// <returns><c>true</c>, if the given paths are the same, else <c>false</c>.</returns>
    public static bool PathEquals(string path1, string path2)
    {
      if (path1 == null && path2 == null)
        return true;
      if (path1 == null || path2 == null)
        return false;
      return path1.ToLower() == path2.ToLower();
    }

    /// <summary>
    /// Returns the information if the specified <paramref name="fileOrFolderPath"/> is contained
    /// in the specified <paramref name="folderPath"/>. The evaluation is based on the file and
    /// folder names, the filesystem is not accessed for this check.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to be checked.</param>
    /// <param name="folderPath">The folder to check.</param>
    /// <returns><c>true</c>, if the <paramref name="fileOrFolderPath"/> is located in the
    /// specified <paramref name="folderPath"/>, else <c>false</c>.</returns>
    public static bool IsContainedIn(string fileOrFolderPath, string folderPath)
    {
      while (fileOrFolderPath != null)
        if (PathEquals(folderPath, fileOrFolderPath))
          return true;
        else
          fileOrFolderPath = Path.GetDirectoryName(fileOrFolderPath);
      return false;
    }

    public static string CreateHumanReadableTempFilePath(string underlayingResourcePath)
    {
      string fileName = Path.GetFileName(underlayingResourcePath);
      string directory = Path.GetTempPath() + Guid.NewGuid().ToString("D");
      Directory.CreateDirectory(directory);
      return directory + "\\" + fileName;
    }

    /// <summary>
    /// Read the complete binary content of the given <paramref name="filename"/> into a byte[].
    /// </summary>
    /// <param name="filename">Filename</param>
    /// <returns>Content</returns>
    public static byte[] ReadFile(string filename)
    {
      FileInfo thumbnail = new FileInfo(filename);
      byte[] binary = new byte[thumbnail.Length];
      using (FileStream fileStream = new FileStream(thumbnail.FullName, FileMode.Open, FileAccess.Read))
      using (BinaryReader binaryReader = new BinaryReader(fileStream))
        binaryReader.Read(binary, 0, binary.Length);
      return binary;
    }

    /// <summary>
    /// Builds a full path for a given <paramref name="fileName"/> sorted by subfolder architecture (x64 or x86) that is located in the same folder as the <see cref="Assembly.GetCallingAssembly"/>.
    /// /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Combined path</returns>
    public static string BuildAssemblyRelativePathForArchitecture(string fileName)
    {
      string executingPath = Assembly.GetCallingAssembly().Location;
      string architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
      return Path.Combine(Path.GetDirectoryName(executingPath), architecture, fileName);
    }

    /// <summary>
    /// Builds a full path for a given <paramref name="fileName"/> that is located in the same folder as the <see cref="Assembly.GetCallingAssembly"/>.
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Combined path</returns>
    public static string BuildAssemblyRelativePath(string fileName)
    {
      string executingPath = Assembly.GetCallingAssembly().Location;
      return Path.Combine(Path.GetDirectoryName(executingPath), fileName);
    }

    /// <summary>
    /// Builds a full path for a given <paramref name="fileName"/> that is located in the same folder as the <see cref="Assembly.GetExecutingAssembly"/>.
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Combined path</returns>
    public static string BuildExecutingAssemblyRelativePath(string fileName)
    {
      string executingPath = Assembly.GetExecutingAssembly().Location;
      return Path.Combine(Path.GetDirectoryName(executingPath), fileName);
    }

    /// <summary>
    /// Returns a filesystem safe name of the given <paramref name="filename"/> by replacing all invalid characters with the <paramref name="replaceChar"/>.
    /// </summary>
    /// <param name="filename">Filename to replace.</param>
    /// <param name="replaceChar">Replacement character.</param>
    /// <returns>Safe name or <c>null</c> if <paramref name="filename"/> is <seealso cref="string.IsNullOrWhiteSpace"/>.</returns>
    public static string GetSafeFilename(string filename, char replaceChar = '_')
    {
      return string.IsNullOrWhiteSpace(filename) ?
        null :
        Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c, replaceChar));
    }

    /// <summary>
    /// Returns a filesystem safe path of the given <paramref name="path"/> by replacing all invalid characters with the <paramref name="replaceChar"/>.
    /// </summary>
    /// <param name="path">Path to replace.</param>
    /// <param name="replaceChar">Replacement character.</param>
    /// <returns>Safe name or <c>null</c> if <paramref name="path"/> is <seealso cref="string.IsNullOrWhiteSpace"/>.</returns>
    public static string GetSafePath(string path, char replaceChar = '_')
    {
      return string.IsNullOrWhiteSpace(path) ?
        null :
        Path.GetInvalidPathChars().Aggregate(path, (current, c) => current.Replace(c, replaceChar));
    }

    /// <summary>
    /// Gets a temporary filename with the given <paramref name="extension"/>.
    /// </summary>
    /// <param name="extension">File extension including "."</param>
    /// <returns>Tempf file</returns>
    public static string GetTempFileName(string extension)
    {
      return Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
    }
  }
}
