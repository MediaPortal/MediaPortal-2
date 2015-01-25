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
using System.IO;
using System.Xml.Linq;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.PackageManager.Core.Package
{
  /// <summary>
  /// Plugin related package meta data.
  /// </summary>
  public class PackagePluginMetaData
  {
    #region ctor

    /// <summary>
    /// Creates a new plugin meta data item from an XML element.
    /// </summary>
    /// <param name="xRoot">Plugin meta data XML element.</param>
    public PackagePluginMetaData(XElement xRoot)
    {
      Images = new PackageImageCollection();
      Links = new PackageLinkCollection();

      // set defaults
      Target = PackageTarget.Invalid;
      Copyright = String.Empty;
      Author = String.Empty;
      Description = String.Empty;

      foreach (var xAttribute in xRoot.Attributes())
      {
        switch (xAttribute.Name.LocalName)
        {
          case "Name":
            Name = xAttribute.Value;
            break;

          case "FriendlyName":
            FriendlyName = xAttribute.Value;
            break;

          case "Copyright":
            Copyright = xAttribute.Value;
            break;

          case "Author":
            Author = xAttribute.Value;
            break;

          case "Description":
            Description = xAttribute.Value;
            break;

          case "Target":
            PackageTarget target;
            if (!Enum.TryParse(xAttribute.Value, out target))
            {
              target = PackageTarget.Invalid;
            }
            Target = target;
            break;

          case "Channel":
            PackageChannel channel;
            if (!Enum.TryParse(xAttribute.Value, out channel))
            {
              channel = PackageChannel.Invalid;
            }
            Channel = channel;
            break;

          default:
            throw new PackageParseException(
              String.Format("The attribute '{0}' is not supported for PluginInfo", xAttribute.Name.LocalName), 
              xAttribute);
        }
      }
      FriendlyName = FriendlyName ?? Name;

      foreach (var xElement in xRoot.Elements())
      {
        switch (xElement.Name.LocalName)
        {
          case "Image":
            Images.Add(new PackageImage(xElement));
            break;

          case "Link":
            Links.Add(new PackageLink(xElement));
            break;

          default:
            throw new PackageParseException(
              String.Format("The element '{0}' is not supported for PluginInfo", xElement.Name.LocalName),
              xElement);
        }
      }
    }

    #endregion

    #region public methods

    /// <summary>
    /// Checks if the meta data is valid
    /// </summary>
    /// <param name="package">Package containing the meta data.</param>
    /// <param name="message">Is set to the error message if the data is not valid.</param>
    /// <returns>Returns <c>true</c> if the data is valid; else <c>false</c>.</returns>
    public bool CheckValid(PackageRoot package, out string message)
    {
      if (Target == PackageTarget.Invalid)
      {
        message = "Target is invalid";
        return false;
      }
      if (Channel == PackageChannel.Invalid)
      {
        message = "Channel is invalid";
        return false;
      }

      foreach (var image in Images)
      {
        if (!File.Exists(image.GetFullImagePath(package)))
        {
          message = String.Format("The image {0} is missing", image.ImagePath);
          return false;
        }
        package.SetRootDirectoryUsed(image.ImagePath);
      }

      foreach (var link in Links)
      {
        //TODO: check if URL is valid
      }

      message = null;
      return true;
    }

    /// <summary>
    /// Fills missing properties from the actual plugin meta data
    /// </summary>
    /// <param name="pluginMetadata">Plugin meta data.</param>
    public void FillMissingMetadata(PluginMetadata pluginMetadata)
    {
      if (String.IsNullOrEmpty(Name))
        Name = Path.GetFileName(pluginMetadata.SourceInfo.PluginPath);
      if (String.IsNullOrEmpty(FriendlyName))
        FriendlyName = pluginMetadata.Name;
      if (String.IsNullOrEmpty(Copyright))
        Copyright = pluginMetadata.Copyright;
      if (String.IsNullOrEmpty(Author))
        Author = pluginMetadata.Author;
      if (String.IsNullOrEmpty(Description))
        Description = pluginMetadata.Description;
    }

    /// <summary>
    /// Checks if the property values matches the data in an plugin meta data.
    /// </summary>
    /// <param name="pluginMetadata">Plugin meta data.</param>
    public void CheckMetadataMismatch(PluginMetadata pluginMetadata)
    {
      if (!String.Equals(Name, Path.GetFileName(pluginMetadata.SourceInfo.PluginPath), StringComparison.OrdinalIgnoreCase))
        throw new PackageParseException("The folder name of the main plugin and the Name in PluginInfo.xml file does not match");
      if (!String.Equals(Author, pluginMetadata.Author))
        throw new PackageParseException("The Author of the main plugin and the PluginInfo.xml file does not match");
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the unique name of the plugin.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the friendly (user readable) name of the plugin.
    /// </summary>
    public string FriendlyName { get; private set; }

    /// <summary>
    /// Gets the copyright notice of the plugin.
    /// </summary>
    public string Copyright { get; private set; }

    /// <summary>
    /// Gets the author name of the plugin.
    /// </summary>
    public string Author { get; private set; }

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the target system of the plugin.
    /// </summary>
    public PackageTarget Target { get; private set; }

    /// <summary>
    /// Gets the publishing channel of the plugin.
    /// </summary>
    public PackageChannel Channel { get; private set; }

    /// <summary>
    /// Gets a collection with all images of the plugin.
    /// </summary>
    public PackageImageCollection Images { get; private set; }

    /// <summary>
    /// Gets a collection with all links of the plugin.
    /// </summary>
    public PackageLinkCollection Links { get; private set; }

    #endregion
  }

  /// <summary>
  /// Package target types
  /// </summary>
  public enum PackageTarget
  {
    /// <summary>
    /// Invalid target. Mainly for internal usage.
    /// </summary>
    Invalid,

    /// <summary>
    /// MP2 server.
    /// </summary>
    Server,

    /// <summary>
    /// MP2 client.
    /// </summary>
    Client,

    /// <summary>
    /// Shared, MP2 server or client.
    /// </summary>
    Shared
  }

  /// <summary>
  /// Publishing channel of the plugin.
  /// </summary>
  public enum PackageChannel
  {
    /// <summary>
    /// Invalid channel. Mainly for internal usage. 
    /// </summary>
    Invalid,

    /// <summary>
    /// Internal channel.
    /// </summary>
    Internal,

    /// <summary>
    /// Developers channel.
    /// </summary>
    Developers,

    /// <summary>
    /// Alpha release channel.
    /// </summary>
    Alpha,

    /// <summary>
    /// Beta release channel.
    /// </summary>
    Beta,

    /// <summary>
    /// Release candidate channel.
    /// </summary>
    ReleaseCadidate,

    /// <summary>
    /// Official release channel.
    /// </summary>
    Release
  }
}