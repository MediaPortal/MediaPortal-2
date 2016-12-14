#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Xml.Serialization;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UiComponents.IrInput
{
  /// <summary>
  /// Mapping of a remote button key code to a <see cref="Key"/> instance.
  /// </summary>
  [XmlRoot]
  public class MappedKeyCode
  {
    #region Protected fields

    protected Key _key;
    protected string _code;

    #endregion Variables

    #region Properties

    [XmlIgnore]
    public Key Key
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

    public MappedKeyCode() : this(Key.None, String.Empty) { }

    public MappedKeyCode(Key key, string code)
    {
      _key  = key;
      _code = code;
    }

    public override string ToString()
    {
      return _code;
    }

    #endregion Constructors

    #region Extra members for XML serialization

    [XmlAttribute]
    public string Key_Name
    {
      get { return Key.SerializeKey(_key); }
      set { _key = Key.DeserializeKey(value); }
    }

    #endregion
  }
}
