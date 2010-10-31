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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  public class BindVar
  {
    protected string _name;
    protected object _value;
    protected Type _type;

    public BindVar(string name, object value, Type type)
    {
      _name = name;
      _value = value;
      _type = type;
    }

    public string Name
    {
      get { return _name; }
    }

    public object Value
    {
      get { return _value; }
    }

    public Type VariableType
    {
      get { return _type; }
    }
  }
}