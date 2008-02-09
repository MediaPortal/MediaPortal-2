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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
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
      SetPath("DEFAULTS", @"<APPLICATION_ROOT>\Defaults");
      LoadDefaults();
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
    /// Replaces an existing path.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="pathPattern">The path pattern.</param>
    public void ReplacePath(string label, string pathPattern)
    {
      RemovePath(label);
      SetPath(label, pathPattern);
    }

    /// <summary>
    /// Removes a path.
    /// </summary>
    /// <param name="label">The path label.</param>
    public void RemovePath(string label)
    {
      label = label.ToUpper();
      if (!label.StartsWith("<"))
        label = "<" + label;
      if (!label.EndsWith(">"))
        label = label + ">";

      if (_paths.ContainsKey(label))
        _paths.Remove(label);
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

		#region IStatus Implementation
		public List<string> GetStatus()
		{
			List<string> status = new List<string>();
			status.Add("=== PathManager");
			foreach (KeyValuePair<string, string> pair in _paths)
			{
				status.Add(String.Format("     {0} => {1}", pair.Key, pair.Value));
			}
			return status;
		}
		#endregion

    #region private
    private void LoadDefaults()
    {
      try
      {
        XmlSerializer s = new XmlSerializer(typeof(PathListFile));
        TextReader r = new StreamReader(GetPath(@"<DEFAULTS>\Paths.xml"));
        PathListFile defaults = (PathListFile)s.Deserialize(r);

        foreach (PathDefinition path in defaults.Paths)
        {
          SetPath(path.Name, path.Value);
        }
      }
      catch (Exception)
      {
        // If something is wrong with the defaults file use the following defaults
        if (!Exists("USER_DATA"))
          SetPath("USER_DATA", @"<COMMON_APPLICATION_DATA>\MediaPortal");
        if (!Exists("CONFIG"))
          SetPath("CONFIG", @"<USER_DATA>\Config");
        if (!Exists("LOG"))
          SetPath("LOG", @"<USER_DATA>\Log");
      }
    }
    #endregion
  }
}
