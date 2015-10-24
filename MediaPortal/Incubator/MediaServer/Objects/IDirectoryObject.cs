#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// This is the root class of the entire content directory class hierarchy. It can not be instantiated, in the sense that no XML fragment returned by a Browse() or Search() action can be of class object. The object class defines properties that are common to both atomic media items, as well as logical collections of these items.
  /// </summary>
  public interface IDirectoryObject
  {
    /// <summary>
    /// An identifier for the object. The value of each object id property must be unique with respect to the Content Directory.
    /// </summary>
    [DirectoryProperty("@id")]
    string Id { get; set; }

    /// <summary>
    /// id property of object’s parent. The parentID of the Content Directory ‘root’ container must be set to the reserved value of “-1”. No other parentID attribute of any other Content Directory object may take this value.
    /// </summary>
    [DirectoryProperty("@parentID")]
    string ParentId { get; set; }

    /// <summary>
    /// Name of the object
    /// </summary>
    [DirectoryProperty("dc:title")]
    string Title { get; set; }

    /// <summary>
    /// Primary content creator or owner of the object
    /// </summary>
    [DirectoryProperty("dc:creator", Required = false)]
    string Creator { get; set; }

    /// <summary>
    /// Resource, typically a media file, associated with the object. Values must be properly escaped URIs as described in [RFC 2396].
    /// </summary>
    [DirectoryProperty("res", Required = false)]
    IList<IDirectoryResource> Resources { get; set; }

    /// <summary>
    /// Class of the object.
    /// </summary>
    [DirectoryProperty("upnp:class")]
    string Class { get; set; }

    /// <summary>
    /// When true, ability to modify a given object is confined to the Content Directory Service. Control point metadata write access is disabled.
    /// </summary>
    [DirectoryProperty("@restricted")]
    bool Restricted { get; set; }

    /// <summary>
    /// When present, controls the modifiability of the resources of a given object. Ability of a Control Point to change writeStatus of a given resource(s) is implementation dependent.
    /// </summary>
    [DirectoryProperty("upnp:writeStatus", Required = false)]
    string WriteStatus { get; set; }
  }

  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
  public class DirectoryPropertyAttribute : Attribute
  {
    public string XmlPart { get; set; }
    public bool Required { get; set; }

    public DirectoryPropertyAttribute(string xmlPart)
    {
      XmlPart = xmlPart;
      Required = true;
    }
  }
}
