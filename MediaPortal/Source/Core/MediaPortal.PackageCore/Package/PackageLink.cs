#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// Package link data.
  /// </summary>
  [DebuggerDisplay("Link: {Description} {Url}")]
  public struct PackageLink
  {
    #region ctor

    /// <summary>
    /// Creates a new package link item from an XML Element.
    /// </summary>
    /// <param name="xLink">Package link XML element.</param>
    public PackageLink(XElement xLink)
      : this()
    {
      foreach (var xAttribute in xLink.Attributes())
      {
        switch (xAttribute.Name.LocalName)
        {
          case "Description":
            Description = xAttribute.Value;
            break;

          default:
            throw new PackageParseException(
              String.Format("The attribute '{0}' is not supported for Image", xAttribute.Name.LocalName),
              xAttribute);
        }
      }
      if (xLink.Elements().Any())
      {
        throw new PackageParseException(
              "Link can not have any child elements", xLink);
      }
      Url = xLink.Value;
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the description of the link.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the URL of the link.
    /// </summary>
    public string Url { get; private set; }

    #endregion
  }

  /// <summary>
  /// Collection of package link data items.
  /// </summary>
  public class PackageLinkCollection : Collection<PackageLink>
  { }
}