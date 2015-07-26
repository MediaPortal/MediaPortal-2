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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.PackageCore.Package.Content;
using MediaPortal.PackageCore.Package.Root;

namespace MediaPortal.PackageCore.Package.Action
{
  public class InstallContext : IPathManager
  {
    #region public properties

    public PackageModel Package { get; private set; }

    public InstallType InstallType { get; private set; }
    
    public ILogger Log { get; private set; }

    #endregion
    
    #region constructor

    public InstallContext(PackageModel package, InstallType installType, IDictionary<string, string> registredPaths, ILogger log)
    {
      Package = package;
      InstallType = installType;
      foreach (var registredPath in registredPaths)
      {
        SetPath(registredPath.Key, registredPath.Value);
      }
      // add the <packageData>
      if (!Exists("PackageData"))
      {
        // data path is data path of e.g. MP2 client
        // so replace the last target with "MP2-PackageManager"
        var dataPath = GetPath("data");
        dataPath = Directory.GetParent(dataPath).FullName;
        SetPath(PackageModel.PACKAGE_MANAGER_DATA_PATH_LABEL, Path.Combine(dataPath, "MP2-PackageManager"));
      }
      Log = log;
    }

    #endregion

    #region public methods

    public string GetPackagePath(ContentBaseModel content, string path)
    {
      if (path.StartsWith("%"))
      {
        int n = path.IndexOf("%", 1, StringComparison.Ordinal);
        var name = content.GetPath(path.Substring(1, n - 1));
        var replacement = name;
        if (replacement == null)
        {
          throw new ArgumentException(String.Format("Unkown package path label '{0}'", name));
        }
        path = replacement + path.Substring(n + 1);
      }
      return Path.Combine(content.Package.PackageDirectory, path);
    }


    public void LogInfo(string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Info(format, args);
      }
    }

    public void LogError(Exception ex, string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Error(format, ex, args);
      }
    }

    public void LogError(string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Error(format, args);
      }
    }

    public void LogWarn(string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Warn(format, args);
      }
    }
    
    public void LogCritical(string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Critical(format, args);
      }
    }

    public void LogCritical(Exception ex, string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Critical(format, ex, args);
      }
    }

    public void LogDebug(string format, params object[] args)
    {
      if (Log != null)
      {
        Log.Debug(format, args);
      }
    }

    #endregion

    #region Implementation of IPathManager

    private readonly object _syncObj = new object();
    private readonly Dictionary<string, string> _paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static Regex _labelRegex;

    public bool Exists(string label)
    {
      label = CheckFormat(label);
      lock (_syncObj)
      {
        return _paths.ContainsKey(label);
      }
    }

    public void SetPath(string label, string pathPattern)
    {
      label = CheckFormat(label);
      pathPattern = pathPattern.TrimEnd('\\', '/');
      lock (_syncObj)
      {
        _paths[label] = pathPattern;
      }
    }

    public string GetPath(string pathPattern)
    {
      if (_labelRegex == null)
      {
        // also allow % instead of < and > to make xml files more readable
        _labelRegex = new Regex(@"[\<%][a-zA-Z_]+[\>%]", RegexOptions.Compiled);
      }

      MatchCollection pathLabels = _labelRegex.Matches(pathPattern);

      lock (_syncObj)
      {
        while (pathLabels.Count > 0)
        {
          foreach (Match labelMatch in pathLabels)
          {
            if (!_paths.ContainsKey(labelMatch.Value))
              throw new ArgumentException(String.Format("Unkown target path label '{0}'", labelMatch.Value));

            pathPattern = pathPattern.Replace(labelMatch.Value, _paths[labelMatch.Value]);
          }

          pathLabels = _labelRegex.Matches(pathPattern);
        }}

      return pathPattern;
    }

    public void RemovePath(string label)
    {
      label = CheckFormat(label);

      lock (_syncObj)
      {
        if (_paths.ContainsKey(label))
        {
          _paths.Remove(label);
        }
      }
    }

    public bool LoadPaths(string pathsFile)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Brings the specified label in the correct form to be used as lookup value in our
    /// registration dictionary.
    /// </summary>
    /// <param name="label">The original label to be formatted.</param>
    /// <returns>Formattet label. This label will be in upper case and will have
    /// a leading <c>&lt;</c> and an ending <c>&gt;</c></returns>
    private static string CheckFormat(string label)
    {
      var labelBuilder = new StringBuilder(label, label.Length + 2);
      if (label.StartsWith("%"))
      {
        labelBuilder.Remove(0, 1);
        labelBuilder.Insert(0, '<');
      }
      else if (!label.StartsWith("<"))
      {
        labelBuilder.Insert(0, '<');
      }
      if (label.EndsWith("%"))
      {
        labelBuilder.Remove(labelBuilder.Length - 1, 1);
        labelBuilder.Append('>');
      }
      else if (!label.EndsWith(">"))
      {
        labelBuilder.Append('>');
      }
      return labelBuilder.ToString();
    }

    #endregion
  }

  /// <summary>
  /// Package install types
  /// </summary>
  public enum InstallType
  {
    /// <summary>
    /// Install package.
    /// </summary>
    Install,

    /// <summary>
    /// Update package.
    /// </summary>
    Update,

    /// <summary>
    /// Uninstall package.
    /// </summary>
    Uninstall
  }
}