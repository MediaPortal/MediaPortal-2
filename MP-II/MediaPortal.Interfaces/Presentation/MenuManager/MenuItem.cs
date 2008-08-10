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

using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Presentation.MenuManager;

namespace MediaPortal.Presentation.MenuManager
{
  public class MenuItem : IMenuItem
  {
    StringId _text;
    string _imagePath;
    string _command;
    string _commandParameter;
    List<IMenuItem> _subItems;

    public MenuItem()
    {
    }

    public MenuItem(StringId text, string imagePath)
    {
      _text = text;
      _imagePath = imagePath;
      _subItems = new List<IMenuItem>();
    }

    public MenuItem(StringId text, string imagePath,string command)
    {
      _text = text;
      _imagePath = imagePath;
      _command = command;
      _subItems = new List<IMenuItem>();
    }

    public MenuItem(StringId text, string imagePath, string command, string commandParameter)
    {
      _text = text;
      _imagePath = imagePath;
      _command = command;
      _commandParameter = commandParameter;
      _subItems = new List<IMenuItem>();
    }

    #region IMenuItem Members

    public StringId Text
    {
      get 
      {
        return _text;
      }
    }

    public string ImagePath
    {
      get 
      {
        return _imagePath;
      }
      set
      {
        _imagePath = value;
      }
    }

    public string Command
    {
      get 
      {
        return _command;
      }
      set
      {
        _command = value;
      }
    }

    public string CommandParameter
    {
      get 
      {
        return _commandParameter;
      }
      set
      {
        _commandParameter = value;
      }
    }

    public List<IMenuItem> Items
    {
      get 
      {
        return _subItems;
      }
      set
      {
        _subItems = value;
      }
    }

    #endregion
  }
}
