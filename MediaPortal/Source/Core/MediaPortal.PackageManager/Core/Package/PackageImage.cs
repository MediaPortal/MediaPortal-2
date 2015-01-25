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
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MediaPortal.PackageManager.Core.Package
{
  /// <summary>
  /// Package image data.
  /// </summary>
  [DebuggerDisplay("Image: {ImageType} {ImagePath}")]
  public struct PackageImage
  {
    #region ctor

    /// <summary>
    /// Creates a new package image data item.
    /// </summary>
    /// <param name="xImage">XML element of the image data.</param>
    public PackageImage(XElement xImage) 
      : this()
    {
      Description = String.Empty;

      foreach (var xAttribute in xImage.Attributes())
      {
        switch (xAttribute.Name.LocalName)
        {
          case "Type":
            PackageImageType type;
            if (!Enum.TryParse(xAttribute.Value, out type))
            {
              throw new PackageParseException(
              String.Format("The value '{0}' is not a valid image type", xAttribute.Value),
              xAttribute);
            }
            ImageType = type;
            break;

          case "Description":
            Description = xAttribute.Value;
            break;

          default:
            throw new PackageParseException(
              String.Format("The attribute '{0}' is not supported for Image", xAttribute.Name.LocalName),
              xAttribute);
        }
      }
      if (xImage.Elements().Any())
      {
        throw new PackageParseException(
              "Image can not have any child elements", xImage);
      }
      ImagePath = xImage.Value;
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the type of the image.
    /// </summary>
    public PackageImageType ImageType { get; private set; }

    /// <summary>
    /// Gets the description of the image.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the relative path of the image file in the package.
    /// </summary>
    public string ImagePath { get; private set; }

    #endregion

    #region public methods

    /// <summary>
    /// Gets the full path of the image file inside the given package.
    /// </summary>
    /// <param name="packageRoot">Package root that contains the image.</param>
    /// <returns></returns>
    public string GetFullImagePath(PackageRoot packageRoot)
    {
      return Path.Combine(packageRoot.PackagePath, ImagePath);
    }

    #endregion
  }

  /// <summary>
  /// Collection of package image data items.
  /// </summary>
  public class PackageImageCollection : Collection<PackageImage>
  { }
  
  /// <summary>
  /// Enumeration of possible package image types.
  /// </summary>
  public enum PackageImageType
  {
    /// <summary>
    /// Package logo
    /// </summary>
    Logo,

    /// <summary>
    /// Screen shot.
    /// </summary>
    Screenshot
  }
}