#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.Configuration
{
  /// <summary>
  /// Base class for all setting registrations which require a location and
  /// a text to be displayed.
  /// The location denotes a path in the settings tree. The path element delimiter is '/'. The
  /// location property contains the parent location with the Id of this setting registration as last
  /// path element.
  /// </summary>
  public class ConfigBaseMetadata
  {
    #region Variables

    protected string _location;
    protected string _text;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the location path of this setting registration. The location contains
    /// the parent location as well as the Id part of this setting registration.
    /// </summary>
    public string Location
    {
      get { return _location; }
    }

    /// <summary>
    /// Returns the location part of the parent setting registration element.
    /// </summary>
    public string ParentLocation
    {
      get
      {
        int i = _location.LastIndexOf('/');
        return i == -1 ? string.Empty : _location.Substring(0, i);
      }
    }

    /// <summary>
    /// Returns the Id part of the location path.
    /// </summary>
    public string Id
    {
      get
      {
        int i = _location.LastIndexOf('/');
        return i == -1 ? _location : _location.Substring(i + 1);
      }
    }

    /// <summary>
    /// Returns the text to be displayed for this setting registration element.
    /// </summary>
    public string Text
    {
      get { return _text; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new <see cref="ConfigBaseMetadata"/> instance.
    /// </summary>
    /// <param name="location">The location of the new instance. This must contain the parent location
    /// if there is a parent, and the Id of this setting registration as last location path element.</param>
    /// <param name="text">The text to be displayed for this setting registration.</param>
    public ConfigBaseMetadata(string location, string text)
    {
      _location = location;
      _text = text;
    }

    #endregion

    #region Static helper methods

    /// <summary>
    /// Concatenates two location strings. The separator char is '/'.
    /// </summary>
    /// <param name="parent">The first location string. May be <c>null</c> or <c>string.Empty</c>.</param>
    /// <param name="child">The second location string. May be <c>null</c> or <c>string.Empty</c>.</param>
    /// <returns>Concatenated location string.</returns>
    public static string ConcatLocations(string parent, string child)
    {
      if (string.IsNullOrEmpty(parent))
        return child;
      if (string.IsNullOrEmpty(child))
        return parent;
      while (true)
      {
        if (child.StartsWith("../"))
        {
          child = child.Substring("../".Length);
          parent = GetParentLocation(parent, true);
        }
        else if (child.StartsWith("./"))
          child = child.Substring("./".Length);
        else
          break;
      }
      return RemoveTrailingSlash(parent) + "/" + child;
    }

    /// <summary>
    /// Returns the parent of the specified <paramref name="location"/>. The parent location is the
    /// location expression until the last '/' character.
    /// </summary>
    /// <param name="location">Location to evaluate the parent location from.</param>
    /// <param name="throwOnNotPresent">If set to <c>true</c>, this method will throw an exception if
    /// the specified <paramref name="location"/> doesn't have a parent location (i.e. if it is
    /// empty or relative or denotes the root location '/').</param>
    /// <exception cref="ArgumentException">If the location string doesn't have a parent location
    /// and <paramref name="throwOnNotPresent"/> is set to <c>true</c>.</exception>
    /// <returns>Parent location of the specified <paramref name="location"/> or <c>null</c>, if the
    /// location doesn't have a parent and <paramref name="throwOnNotPresent"/> is set to
    /// <c>false</c>.</returns>
    public static string GetParentLocation(string location, bool throwOnNotPresent)
    {
      int i = location.LastIndexOf('/');
      if (i == -1 || location.Trim().Length == 1)
      {
        if (throwOnNotPresent)
          throw new ArgumentException("Location expression '" + location + "': Cannot ascent to parent location");
        return null;
      }
      return location.Substring(0, i);
    }

    /// <summary>
    /// Removes all trailing '/' characters from the specified location, if it contains any.
    /// </summary>
    /// <param name="location">The path expression. May be <c>null</c> or <c>string.Empty</c>.</param>
    /// <returns>Path expression without any trailing '/' characters.</returns>
    public static string RemoveTrailingSlash(string location)
    {
      if (location == null) return string.Empty;
      if (location.Length == 0) return string.Empty;
      while (location.EndsWith("/"))
        location = location.Remove(location.Length - 1);
      return location;
    }

    #endregion
  }
}