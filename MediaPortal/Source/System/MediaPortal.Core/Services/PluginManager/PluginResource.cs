#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.Core.Services.PluginManager
{
  public enum PluginResourceType
  {
    Language,
    Skin
  }

  /// <summary>
  /// Provides the file access location of a plugin resource.
  /// </summary>
  public class PluginResource
  {
    protected string _path;
    protected PluginResourceType _type;

    public PluginResource(PluginResourceType type, string path)
    {
      _type = type;
      _path = path;
    }

    /// <summary>
    /// Returns the type of this resource.
    /// </summary>
    public PluginResourceType Type
    {
      get { return _type; }
    }

    /// <summary>
    /// Returns the absolute file- or directory-path of this resource.
    /// </summary>
    public string Path
    {
      get { return _path; }
    }

    public override string ToString()
    {
      return string.Format("PluginResource Path='{0}', Type='{1}'", _path, _type);
    }
  }
}
