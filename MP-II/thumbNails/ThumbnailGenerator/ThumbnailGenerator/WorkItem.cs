#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.IO;

namespace SkinEngine.Thumbnails
{
  public class WorkItem
  {
    public bool IsFolder;
    public string SourceFolder;
    public string SourceFile;
    public string DestinationFolder;
    public string DestinationFile;
    public string Source;
    public string Destination;
    public int Quality;
    public int Width;
    public int Height;

    public WorkItem(string source, string dest, int width, int height, int quality)
    {
      Width = width;
      Height = height;
      Quality = quality;

      Source = source;
      Destination = dest;

      SourceFolder = Path.GetDirectoryName(source);
      SourceFile = Path.GetFileName(source);
      DestinationFolder = Path.GetDirectoryName(dest);
      DestinationFile = Path.GetFileName(dest);
      if (SourceFile.ToLower() == "folder.jpg")
      {
        DestinationFolder = SourceFolder;
        DestinationFile = SourceFile;
        IsFolder = true;
        Destination = String.Format(@"{0}\{1}", DestinationFolder, DestinationFile);
      }
    }
  } ;
}