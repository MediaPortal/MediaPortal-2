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
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Components.Control.IrInput
{
  [XmlRoot]
  public class MappedKeyCode
  {

    #region Variables

    Keys _key;
    string _code;
    
    #endregion Variables

    #region Properties

    [XmlAttribute]
    public Keys Key
    {
      get { return _key; }
      set { _key = value; }
    }
    
    [XmlAttribute]
    public string Code
    {
      get { return _code; }
      set { _code = value; }
    }

    #endregion Properties

    #region Constructors

    public MappedKeyCode() : this(Keys.None, String.Empty) { }

    public MappedKeyCode(string key, string code) : this((Keys)new KeysConverter().ConvertFrom(key), code) { }

    public MappedKeyCode(Keys key, string code)
    {
      _key  = key;
      _code = code;
    }
    
    #endregion Constructors

  }

}
