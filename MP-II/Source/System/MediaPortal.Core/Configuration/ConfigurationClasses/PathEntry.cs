#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

namespace MediaPortal.Core.Configuration.ConfigurationClasses
{
  /// <summary>
  /// Base class for configuration setting classes for configuring a file or folder path.
  /// </summary>
  public abstract class PathEntry : ConfigSetting
  {
    #region Enums

    public enum PathSelectionType
    {
      /// <summary>
      /// Specifies to browse for files.
      /// </summary>
      File,

      /// <summary>
      /// Specifies to browse for folders.
      /// </summary>
      Folder
    }

    #endregion

    #region Variables

    protected string _path;
    protected PathSelectionType _pathSelectionType;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string Path
    {
      get { return _path; }
      set
      {
        if (_path != value)
        {
          _path = value;
          NotifyChange();
        }
      }
    }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public PathSelectionType PathType
    {
      get { return _pathSelectionType; }
      set { _pathSelectionType = value; }
    }

    #endregion
  }
}
