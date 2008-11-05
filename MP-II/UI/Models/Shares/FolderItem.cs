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

using MediaPortal.Presentation.DataObjects;

namespace Models.Shares
{
  public class FolderItem : TreeItem
  {
    string _folder;
    string _name;
    FolderItem _parentfolder;

    public FolderItem(string name, string folder, FolderItem parentfolder)
    {
      if (name == "..")
      {
        Add("CoverArt", "DefaultFolderBackBig.png");
      }
      else
      {
        Add("CoverArt", "DefaultFolderBig.png");
      }

      Add("Name", name);
      Add("Size", "");
      Add("Date", "");
      Add("Path", folder);
      _folder = folder;
      _name = name;
      _parentfolder = parentfolder;
    }

    public FolderItem ParentFolder
    {
      get
      {
        return _parentfolder;
      }
    }

    public string Folder
    {
      get
      {
        return _folder;
      }
    }
    public string Name
    {
      get
      {
        return _name;
      }
    }
  }
}
