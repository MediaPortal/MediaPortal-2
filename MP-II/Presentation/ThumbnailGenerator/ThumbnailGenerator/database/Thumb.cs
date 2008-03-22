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

using System.IO;

namespace Presentation.ThumbnailGenerator.Database
{
  public class Thumb
  {
    public string _fileName;
    public long _offset;
    public long _size;
    public byte[] _image;

    public long HeaderSize
    {
      get { return sizeof (long)*2 + _fileName.Length + 1; }
    }

    public string FileName
    {
      get { return _fileName; }
      set { _fileName = value; }
    }

    public long Offset
    {
      get { return _offset; }
      set { _offset = value; }
    }

    public long Size
    {
      get { return _size; }
      set { _size = value; }
    }

    public byte[] Image
    {
      get { return _image; }
      set { _image = value; }
    }

    public void Read(Stream stream)
    {
      _image = new byte[Size];
      stream.Seek(Offset, SeekOrigin.Begin);
      stream.Read(_image, 0, (int) Size);
    }
  }
}
