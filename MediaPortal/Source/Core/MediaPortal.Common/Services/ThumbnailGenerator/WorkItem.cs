#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.Services.ThumbnailGenerator
{
  /// <summary>
  /// Holds the data of one work item for the thumbnail generator thread.
  /// </summary>
  public class WorkItem
  {
    public string _sourcePath;
    public int _width;
    public int _height;
    public CreatedDelegate _createdDelegate;

    public WorkItem(string sourcePath, int width, int height, CreatedDelegate createdDelegate)
    {
      _width = width;
      _height = height;

      _sourcePath = sourcePath;
      _createdDelegate = createdDelegate;
    }

    public string SourcePath
    {
      get { return _sourcePath; }
    }

    public int Width
    {
      get { return _width; }
    }

    public int Height
    {
      get { return _height; }
    }

    public CreatedDelegate CreatedDelegate
    {
      get { return _createdDelegate; }
    }
  }
}
