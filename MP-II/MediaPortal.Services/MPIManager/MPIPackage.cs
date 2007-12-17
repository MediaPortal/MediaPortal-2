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
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using MediaPortal.Core.MPIManager;

namespace MediaPortal.Services.MPIManager
{
  /// <summary>
  /// Implementation of IMPIPackage, manage data 
  /// included in mpi package
  /// </summary>
  [Serializable]
  public class MPIPackage : IMPIPackage
  {
    public MPIPackage()
    {
      Name = string.Empty;
      FileName = string.Empty;
      PackageId = string.Empty;
      ExtensionId = string.Empty;
      Version = "0.0.0.0";
      VersionType = string.Empty;
      ExtensionType = string.Empty;
      Items = new List<MPIFileItem>();
      Dependencies = new List<MPIDependency>();
      Author = string.Empty;
      Description = string.Empty;
    }

    public MPIPackage(MPIPackage pk)
    {
      Name = pk.Name;
      FileName = pk.FileName;
      PackageId = pk.PackageId;
      ExtensionId = pk.ExtensionId;
      Version = pk.Version;
      VersionType = pk.VersionType;
      ExtensionType = pk.ExtensionType;
      Items = pk.Items;
      Dependencies = pk.Dependencies;
      Author = pk.Author;
      Description = pk.Description;
    }

    #region IMPIPackage Members
    string _name;
    /// <summary>
    /// Gets or sets the name of package.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _name;
      }
      set
      {
        _name = value;
      }
    }
    string _fileName;
    /// <summary>
    /// Gets or sets the file name of package.
    /// </summary>
    /// <value>The name of the file.</value>
    public string FileName {
      get
      {
        return _fileName;
      }
      set
      {
        _fileName = value;
      }
    }
    string _packageId;
    /// <summary>
    /// Gets or sets the GUID, unic for every package 
    /// </summary>
    /// <value>The package GUID.</value>
    public string PackageId
    {
      get
      {
        return _packageId;
      }
      set
      {
        _packageId = value;
      }
    }

    string _extensionId;
    /// <summary>
    /// Gets or sets the extension id. Unic for a Extension
    /// </summary>
    /// <value>The extension GUID.</value>
    public string ExtensionId {
      get
      {
        return _extensionId;
      }
      set
      {
        _extensionId = value;
      }
    }
    string _version;
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public string Version
    {
      get
      {
        return _version;
      }
      set
      {
        _version = value;
      }
    }
    string _versionType;
    public string VersionType
    {
      get
      {
        return _versionType;
      }
      set
      {
        _versionType = value;
      }
    }
    string _extensionType;
    public string ExtensionType
    {
      get
      {
        return _extensionType;
      }
      set
      {
        _extensionType = value;
      }
    }
    
    string _author;
    public string Author
    {
      get
      {
        return _author;
      }
      set
      {
        _author = value;
      }
    }

    string _description;
    public string Description
    {
      get
      {
        return _description;
      }
      set
      {
        _description = value;
      }
    }
    
    
    List<MPIFileItem> _items;
    /// <summary>
    /// Included file items.
    /// </summary>
    /// <value>The file list.</value>
    public List<MPIFileItem> Items
    {
      get
      {
        return _items;
      }
      set
      {
        _items = value;
      }
    }

    List<MPIDependency> _dependencies;
    /// <summary>
    /// Included file items.
    /// </summary>
    /// <value>The file list.</value>
    public List<MPIDependency> Dependencies
    {
      get
      {
        return _dependencies;
      }
      set
      {
        _dependencies = value;
      }
    }
 
    /// <summary>
    /// Loads the specified xml filename.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <returns>True if loading sucefull</returns>
    public bool Load(string filename)
    {
      return true;
    }

   #endregion
    #region methods 
    
    public int Compare(MPIPackage pak)
    {
      return CompareVersions(this.Version, pak.Version);
    }

    /// <summary>
    /// Test if the package contains a screen shoot.
    /// </summary>
    /// <returns></returns>
    public bool ContainScreenShoot()
    {
      foreach (MPIFileItem fil in Items)
      {
        if (fil.Action == "ScreenShots")
          return true;
      }
      return false;
    }
    
    public int NumScreenShoots()
    {
      int count = 0;
      foreach (MPIFileItem fil in Items)
      {
        count++;
      }
      return count;
    }
    #endregion

    #region VersionChecking
    public struct AlphanumericVersion
    {
      public Int32 intMajor;
      public Int32 intMinor;
      public Int32 intBuild;
      public Int32 intRevision;
      public String strMajor;
      public String strMinor;
      public String strBuild;
      public String strRevision;
    }

    private AlphanumericVersion ParseVersion(String version)
    {
      if (version.StartsWith("."))
      {
        version = "0" + version;
      }

      if (version.EndsWith("."))
      {
        version = version + "0";
      }

      while (version.Split('.').Length < 4)
      {
        version += ".0";
      }

      String[] splitVersion = version.Split('.');
      String strMajor, strMinor, strBuild, strRevision;
      Int32 intMajor = 0, intMinor = 0, intBuild = 0, intRevision = 0;

      strMajor = splitVersion[0];
      strMinor = splitVersion[1];
      strBuild = splitVersion[2];
      strRevision = splitVersion[3];

      Regex numerals = new Regex("^[0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

      if (numerals.IsMatch(strMajor))
      {
        intMajor = Int32.Parse(numerals.Match(strMajor).Value);
        strMajor = strMajor.Substring(intMajor.ToString().Length);
      }
      if (numerals.IsMatch(strMinor))
      {
        intMinor = Int32.Parse(numerals.Match(strMinor).Value);
        strMinor = strMinor.Substring(intMinor.ToString().Length);
      }
      if (numerals.IsMatch(strBuild))
      {
        intBuild = Int32.Parse(numerals.Match(strBuild).Value);
        strBuild = strBuild.Substring(intBuild.ToString().Length);
      }
      if (numerals.IsMatch(strMajor))
      {
        intRevision = Int32.Parse(numerals.Match(strRevision).Value);
        strRevision = strRevision.Substring(intRevision.ToString().Length);
      }

      AlphanumericVersion returnValue = new AlphanumericVersion();
      returnValue.intMajor = intMajor;
      returnValue.strMajor = strMajor;
      returnValue.intMinor = intMinor;
      returnValue.strMinor = strMinor;
      returnValue.intBuild = intBuild;
      returnValue.strBuild = strBuild;
      returnValue.intRevision = intRevision;
      returnValue.strRevision = strRevision;

      return returnValue;
    }

    private int CompareVersions(String v1, String v2)
    {
      AlphanumericVersion version1 = ParseVersion(v1);
      AlphanumericVersion version2 = ParseVersion(v2);

      Version numV1 = new Version(version1.intMajor, version1.intMinor, version1.intBuild, version1.intRevision);
      Version numV2 = new Version(version2.intMajor, version2.intMinor, version2.intBuild, version2.intRevision);
      Version alphaV1 = new Version(StringToInt32(version1.strMajor), StringToInt32(version1.strMinor), StringToInt32(version1.strBuild), StringToInt32(version1.strRevision));
      Version alphaV2 = new Version(StringToInt32(version2.strMajor), StringToInt32(version2.strMinor), StringToInt32(version2.strBuild), StringToInt32(version2.strRevision));

      if (numV1.CompareTo(numV2) == 0)
      {
        return alphaV1.CompareTo(alphaV2);
      }
      else
      {
        return numV1.CompareTo(numV2);
      }
    }

    private Int32 StringToInt32(String text)
    {
      Int32 returnValue = 0;
      foreach (Char character in text)
      {
        returnValue += (int)character;
      }
      return returnValue;
    }

    #endregion
  }
}
