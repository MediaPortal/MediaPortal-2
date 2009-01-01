#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
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

namespace MediaPortal.Configuration.ConfigurationClasses
{
  public abstract class Path : ConfigSetting
  {
    #region Enums

    public enum PathType
    {
      /// <summary>
      /// Specifies to browse for files.
      /// </summary>
      FILE,
      /// <summary>
      /// Specifies to browse for folders.
      /// </summary>
      FOLDER
    }

    #endregion

    #region Variables

    protected string _path;
    protected PathType _pathType;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string SelectedPath
    {
      get { return this._path; }
      set
      {
        if (this._path != value)
        {
          this._path = value;
          base.NotifyChange();
        }
      }
    }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public PathType SelectedPathType
    {
      get { return this._pathType; }
      set { this._pathType = value; }
    }

    #endregion
  }
}
