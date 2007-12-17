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

#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MediaPortal.Services.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  /// <summary>
  /// Represents a versioned reference to an Plugin. Used by <see cref="PluginManifest"/>.
  /// </summary>
  public class PluginReference : ICloneable
  {
    #region Variables
    string name;
    Version minimumVersion;
    Version maximumVersion;

    static Version entryVersion;
    #endregion

    #region Constructors/Destructors
    public PluginReference(string name)
      : this(name, new Version(0, 0, 0, 0), new Version(int.MaxValue, int.MaxValue))
    {
    }

    public PluginReference(string name, Version specificVersion)
      : this(name, specificVersion, specificVersion)
    {
    }

    public PluginReference(string name, Version minimumVersion, Version maximumVersion)
    {
      this.Name = name;
      if (minimumVersion == null) throw new ArgumentNullException("minimumVersion");
      if (maximumVersion == null) throw new ArgumentNullException("maximumVersion");

      this.minimumVersion = minimumVersion;
      this.maximumVersion = maximumVersion;
    }
    #endregion

    #region Properties
    public Version MinimumVersion
    {
      get
      {
        return minimumVersion;
      }
    }

    public Version MaximumVersion
    {
      get
      {
        return maximumVersion;
      }
    }

    public string Name
    {
      get
      {
        return name;
      }
      set
      {
        if (value == null) throw new ArgumentNullException("name");
        if (value.Length == 0) throw new ArgumentException("name cannot be an empty string", "name");
        name = value;
      }
    }
    #endregion

    #region Public Methods
    /// <returns>Returns true when the reference is valid.</returns>
    public bool Check(Dictionary<string, Version> Plugins, out Version versionFound)
    {
      if (Plugins.TryGetValue(name, out versionFound))
      {
        return CompareVersion(versionFound, minimumVersion) >= 0
          && CompareVersion(versionFound, maximumVersion) <= 0;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Compares two versions and ignores unspecified fields (unlike Version.CompareTo)
    /// </summary>
    /// <returns>-1 if a &lt; b, 0 if a == b, 1 if a &gt; b</returns>
    private int CompareVersion(Version a, Version b)
    {
      if (a.Major != b.Major)
      {
        return a.Major > b.Major ? 1 : -1;
      }
      if (a.Minor != b.Minor)
      {
        return a.Minor > b.Minor ? 1 : -1;
      }
      if (a.Build < 0 || b.Build < 0)
        return 0;
      if (a.Build != b.Build)
      {
        return a.Build > b.Build ? 1 : -1;
      }
      if (a.Revision < 0 || b.Revision < 0)
        return 0;
      if (a.Revision != b.Revision)
      {
        return a.Revision > b.Revision ? 1 : -1;
      }
      return 0;
    }
    #endregion

    #region Public static Methods
    public static PluginReference Create(PluginProperties properties, string hintPath)
    {
      PluginReference reference = new PluginReference(properties["Plugin"]);
      string version = properties["version"];
      if (version != null && version.Length > 0)
      {
        int pos = version.IndexOf('-');
        if (pos > 0)
        {
          reference.minimumVersion = ParseVersion(version.Substring(0, pos), hintPath);
          reference.maximumVersion = ParseVersion(version.Substring(pos + 1), hintPath);
        }
        else
        {
          reference.maximumVersion = reference.minimumVersion = ParseVersion(version, hintPath);
        }
      }
      return reference;
    }
    #endregion

    #region internal static Methods
    internal static Version ParseVersion(string version, string hintPath)
    {
      if (version == null || version.Length == 0)
        return new Version(0, 0, 0, 0);
      if (version.StartsWith("@"))
      {
        if (version == "@Infinity")
        {
          if (entryVersion == null)
            entryVersion = new Version(0, 1, 0, 0);
          return entryVersion;
        }
        if (hintPath != null)
        {
          string fileName = Path.Combine(hintPath, version.Substring(1));
          try
          {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(fileName);
            return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
          }
          catch (FileNotFoundException ex)
          {
            throw new PluginLoadException("Cannot get version '" + version + "': " + ex.Message);
          }
        }
        return new Version(0, 0, 0, 0);
      }
      else
      {
        return new Version(version);
      }
    }
    #endregion

    #region <Base class> Overloads
    public override bool Equals(object obj)
    {
      if (!(obj is PluginReference)) return false;
      PluginReference b = (PluginReference)obj;
      return name == b.name && minimumVersion == b.minimumVersion && maximumVersion == b.maximumVersion;
    }

    public override int GetHashCode()
    {
      return name.GetHashCode() ^ minimumVersion.GetHashCode() ^ maximumVersion.GetHashCode();
    }

    public override string ToString()
    {
      if (minimumVersion.ToString() == "0.0.0.0")
      {
        if (maximumVersion.Major == int.MaxValue)
        {
          return name;
        }
        else
        {
          return name + ", version <" + maximumVersion.ToString();
        }
      }
      else
      {
        if (maximumVersion.Major == int.MaxValue)
        {
          return name + ", version >" + minimumVersion.ToString();
        }
        else if (minimumVersion == maximumVersion)
        {
          return name + ", version " + minimumVersion.ToString();
        }
        else
        {
          return name + ", version " + minimumVersion.ToString() + "-" + maximumVersion.ToString();
        }
      }
    }
    #endregion

    #region <ICloneable> Implementations
    public PluginReference Clone()
    {
      return new PluginReference(name, minimumVersion, maximumVersion);
    }

    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion
  }
}
