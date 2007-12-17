#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Core.PathManager;

namespace MediaPortal.Services.PathManager
{
  /// <summary>
  /// Managers local path locations
  /// </summary>
  public class PathManager : IPathManager
  {
    #region Variables
    private Dictionary<string, string> _paths;
    #endregion

    #region Constructors/Destructors
    public PathManager()
    {
      _paths = new Dictionary<string, string>();
      SetPath("APPLICATION_ROOT", AppDomain.CurrentDomain.BaseDirectory);
      SetPath("LOCAL_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      SetPath("COMMON_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
      SetPath("MY_DOCUMENTS", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }
    #endregion

    #region IDictionary Implementations
    /// <summary>
    /// Checks if a path with the specified label exists.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <returns>true/false the label exists</returns>
    public bool Exists(string label)
    {
      label = label.ToUpper();
      if (!label.StartsWith("<"))
        label = "<" + label;
      if (!label.EndsWith(">"))
        label = label + ">";

      return _paths.ContainsKey(label);
    }

    /// <summary>
    /// Sets the path.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="pathPattern">The path pattern.</param>
    public void SetPath(string label, string pathPattern)
    {
      label = label.ToUpper();
      if (!label.StartsWith("<"))
        label = "<" + label;
      if (!label.EndsWith(">"))
        label = label + ">";

      if (pathPattern.EndsWith("\\"))
        pathPattern = pathPattern.Substring(0, pathPattern.Length - 1);

      _paths.Add(label, pathPattern);
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name="pathPattern">The path pattern.</param>
    /// <returns></returns>
    public string GetPath(string pathPattern)
    {
      Regex label = new Regex(@"\<[a-zA-Z_]+\>");

      MatchCollection pathLabels = label.Matches(pathPattern);

      while (pathLabels.Count > 0)
      {
        foreach (Match labelMatch in pathLabels)
        {
          if (!_paths.ContainsKey(labelMatch.Value))
            throw new ArgumentException("Unkown path label");

          pathPattern = pathPattern.Replace(labelMatch.Value, _paths[labelMatch.Value]);
        }

        pathLabels = label.Matches(pathPattern);
      }

      return pathPattern;
    }

    /// <summary>
    /// Gets a path as DirectoryInfo.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public DirectoryInfo GetDirectoryInfo(string path)
    {
      return new DirectoryInfo(GetPath(path));
    }
    #endregion
  }
}
