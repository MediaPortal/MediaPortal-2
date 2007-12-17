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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Services.MenuManager;
using MediaPortal.Core.MenuManager;

namespace MediaPortal.Services.MPIManager.Actions.Helpers
{
  class ExtMenuItem:MenuItem
  {
    private string _literalText;
    private string _packages;
    
    public ExtMenuItem(string text, string imagePath, string command, string commandParameter,string packages)
    {
      LiteralText = text;
      ImagePath = imagePath;
      Command = command;
      CommandParameter = commandParameter;
      Items = new List<IMenuItem>();
      Packages = packages;
    }


    public string LiteralText
    {
      get
      {
        return _literalText;
      }
      set
      {
        _literalText = value;
      }
    }

    public string Packages
    {
      get
      {
        return _packages;
      }
      set
      {
        _packages = value;
      }
    }
  }
}
