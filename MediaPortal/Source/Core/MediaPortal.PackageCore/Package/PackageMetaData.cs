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
using System.Xml.Linq;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// Package related package meta data.
  /// </summary>
  public class PackageMetaData
  {
    #region constants

    public const int CURRENT_PACKAGE_VERSION = 1;

    #endregion

    #region public static fields

    /// <summary>
    /// Gets the default package meta data for new packages.
    /// </summary>
    public static PackageMetaData DefaultInfo = new PackageMetaData
    {
      PackageVersion = CURRENT_PACKAGE_VERSION
    };

    #endregion

    #region ctor

    private PackageMetaData()
    { }

    /// <summary>
    /// Creates a new package meta data item from an XML element.
    /// </summary>
    /// <param name="xRoot">Package meta data XML element.</param>
    public PackageMetaData(XElement xRoot)
    {
      int v;
      if (!Int32.TryParse((string) xRoot.Attribute("PackageVersion"), out v))
        v = -1;
      PackageVersion = v;
    }

    #endregion

    #region public methods

    /// <summary>
    /// Saves the file.
    /// </summary>
    /// <param name="path">Target file path.</param>
    public void Save(string path)
    {
      var xDoc = new XDocument(
        new XElement("PackageInfo",
          new XAttribute("PackageVersion", PackageVersion)));
      xDoc.Save(path);
    }

    /// <summary>
    /// Checks if the meta data is valid
    /// </summary>
    /// <param name="packageRoot">Package containing the meta data.</param>
    /// <param name="message">Is set to the error message if the data is not valid.</param>
    /// <returns>Returns <c>true</c> if the data is valid; else <c>false</c>.</returns>
    public bool CheckValid(PackageRoot packageRoot, out string message)
    {
      if (PackageVersion < 0)
      {
        message = "PackageVersion is invalid or missing";
        return false;
      }
      if (PackageVersion > CURRENT_PACKAGE_VERSION)
      {
        message = String.Format("Package version {0} is not supported. Maximum version is {1}",
          PackageVersion, CURRENT_PACKAGE_VERSION);
        return false;
      }
      message = null;
      return true;
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the package version index.
    /// </summary>
    public int PackageVersion { get; private set; }

    #endregion
  }
}