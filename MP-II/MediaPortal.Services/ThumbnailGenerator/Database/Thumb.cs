#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Thumbnails;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  /// <summary>
  /// Encapsulates the data of a thumbnail image in a thumb database file.
  /// </summary>
  public class Thumb
  {
    public string _name;
    public ImageType _imageType = ImageType.Unknown;
    public long _offset;
    public long _size;
    public byte[] _image;

    /// <summary>
    /// Gets or sets the name of this thumbnail. The name is the unique id of a
    /// thumb entry in a thumbnail db.
    /// </summary>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Gets or sets the offset of the image data in the thumbnail db file.
    /// </summary>
    public long Offset
    {
      get { return _offset; }
      set { _offset = value; }
    }

    /// <summary>
    /// Gets or sets the size of the image data in the thumbnail db file.
    /// </summary>
    public long Size
    {
      get { return _size; }
      set { _size = value; }
    }

    /// <summary>
    /// Gets or sets the type of the image data.
    /// </summary>
    public ImageType @ImageType
    {
      get { return _imageType; }
      set { _imageType = value; }
    }

    /// <summary>
    /// Gets or sets the data array containing the image data in the format returned by
    /// <see cref="ImageType"/>, or <c>null</c>, if the image data weren't read yet.
    /// </summary>
    public byte[] Image
    {
      get { return _image; }
      set { _image = value; }
    }
  }
}
