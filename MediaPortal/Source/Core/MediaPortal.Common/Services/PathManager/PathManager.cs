#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.PathManager
{
  /// <summary>
  /// Registration for local file path locations, like specified in <see cref="IPathManager"/>.
  /// Some general path registrations will be initialized in the constructor, other
  /// path registrations will be added from a file Paths.xml, located in the applications
  /// DEFAULTS directory.
  /// </summary>
  public class PathManager : IPathManager, IStatus
  {
    #region Variables

    protected object _syncObj = new object();
    protected readonly Dictionary<string, string> _paths;

    #endregion

    #region Constructors/Destructor

    public PathManager()
    {
      _paths = new Dictionary<string, string>();
    }

    #endregion

    #region Public methods

    public void InitializeDefaults()
    {
      string applicationPath = Environment.GetCommandLineArgs()[0];
      InitializeDefaults(applicationPath);
    }

    public void InitializeDefaults(string applicationPath)
    {
      SetPath("APPLICATION_PATH", applicationPath);
      SetPath("APPLICATION_ROOT", Path.GetDirectoryName(applicationPath));
      SetPath("LOCAL_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      SetPath("COMMON_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
      SetPath("MY_DOCUMENTS", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
      SetPath("DEFAULTS", @"<APPLICATION_ROOT>\Defaults");
      LoadPaths(GetPath(@"<DEFAULTS>\Paths.xml"));
    }

    #endregion

    #region IPathManager Implementation

    public bool Exists(string label)
    {
      label = CheckFormat(label);
      lock (_syncObj)
        return _paths.ContainsKey(label);
    }

    public void SetPath(string label, string pathPattern)
    {
      label = CheckFormat(label);
      pathPattern = StringUtils.RemoveSuffixIfPresent(pathPattern, "\\");
      lock (_syncObj)
        _paths[label] = pathPattern;
    }

    public string GetPath(string pathPattern)
    {
      Regex label = new Regex(@"\<[a-zA-Z_]+\>");

      MatchCollection pathLabels = label.Matches(pathPattern);

      lock (_syncObj)
        while (pathLabels.Count > 0)
        {
          foreach (Match labelMatch in pathLabels)
          {
            if (!_paths.ContainsKey(labelMatch.Value))
              throw new ArgumentException("Unkown path label '" + labelMatch.Value + "'");

            pathPattern = pathPattern.Replace(labelMatch.Value, _paths[labelMatch.Value]);
          }

          pathLabels = label.Matches(pathPattern);
        }

      return pathPattern;
    }

    public void RemovePath(string label)
    {
      label = CheckFormat(label);

      lock (_syncObj)
        if (_paths.ContainsKey(label))
          _paths.Remove(label);
    }

    public void LoadPaths(string pathsFile)
    {
      try
      {
        XmlSerializer s = new XmlSerializer(typeof(PathListFile));
        using (TextReader r = new StreamReader(pathsFile))
        {
          PathListFile defaults = (PathListFile) s.Deserialize(r);

          foreach (PathDefinition path in defaults.Paths)
            SetPath(path.Name, path.Value);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error reading default paths file", e);
      }
    }

    #endregion

		#region IStatus implementation

		public IList<string> GetStatus()
		{
			List<string> status = new List<string> {"=== PathManager"};
		  lock (_syncObj)
		    status.AddRange(_paths.Select(pair => String.Format("     {0} => {1}", pair.Key, pair.Value)));
		  return status;
		}

		#endregion

    #region Private methods

    /// <summary>
    /// Brings the specified label in the correct form to be used as lookup value in our
    /// registration dictionary.
    /// </summary>
    /// <param name="label">The original label to be formatted.</param>
    /// <returns>Formattet label. This label will be in upper case and will have
    /// a leading <c>&lt;</c> and an ending <c>&gt;</c></returns>
    private static string CheckFormat(string label)
    {
      label = label.ToUpper();
      if (!label.StartsWith("<"))
        label = "<" + label;
      if (!label.EndsWith(">"))
        label = label + ">";
      return label;
    }

    #endregion
  }
}
