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

namespace MediaPortal.Control.InputManager
{
  public class Key
  {
    #region variables

    /// <summary>
    /// gets the raw code
    /// </summary>
    public char RawCode;

    /// <summary>
    /// gets a value indicating if the SHIFT key was pressed
    /// </summary>
    public bool Shift;

    /// <summary>
    /// gets a value indicating if the CTRL key was pressed
    /// </summary>
    public bool Control;


    /// <summary>
    /// gets the modifiers flags. These flags indicate which combination of ALT, CTRL, SHIFT is pressed
    /// </summary>
    public Key Modifiers;

    /// <summary>
    /// gets the keyboard code
    /// </summary>
    public Key KeyCode;

    /// <summary>
    /// gets the key name
    /// </summary>
    public string Name;

    #endregion

    #region ctors

    public Key(char rawCode, string name)
    {
      RawCode = rawCode;
      Name = name;
      KeyCode = this;
    }

    public Key(char rawCode)
    {
      RawCode = rawCode;
      Name = RawCode.ToString();
      KeyCode = this;
    }

    public static Key None
    {
      get
      {
        Key key = new Key(' ', "None");
        return key;
      }
    }

    public static Key Up
    {
      get
      {
        Key key = new Key(' ', "Up");
        return key;
      }
    }

    public static Key Down
    {
      get
      {
        Key key = new Key(' ', "Down");
        return key;
      }
    }

    public static Key Left
    {
      get
      {
        Key key = new Key(' ', "Left");
        return key;
      }
    }

    public static Key Right
    {
      get
      {
        Key key = new Key(' ', "Right");
        return key;
      }
    }

    public static Key PageUp
    {
      get
      {
        Key key = new Key(' ', "PageUp");
        return key;
      }
    }

    public static Key PageDown
    {
      get
      {
        Key key = new Key(' ', "PageDown");
        return key;
      }
    }

    public static Key Enter
    {
      get
      {
        Key key = new Key(' ', "Enter");
        return key;
      }
    }

    public static Key Home
    {
      get
      {
        Key key = new Key(' ', "Home");
        return key;
      }
    }

    public static Key End
    {
      get
      {
        Key key = new Key(' ', "End");
        return key;
      }
    }

    public static Key ContextMenu
    {
      get
      {
        Key key = new Key(' ', "ContextMenu");
        return key;
      }
    }

    public static Key ZoomMode
    {
      get
      {
        Key key = new Key(' ', "ZoomMode");
        return key;
      }
    }

    public static Key DvdMenu
    {
      get
      {
        Key key = new Key(' ', "DvdMenu");
        return key;
      }
    }

    public static Key DvdUp
    {
      get
      {
        Key key = new Key(' ', "DvdUp");
        return key;
      }
    }

    public static Key DvdDown
    {
      get
      {
        Key key = new Key(' ', "DvdDown");
        return key;
      }
    }

    public static Key DvdLeft
    {
      get
      {
        Key key = new Key(' ', "DvdLeft");
        return key;
      }
    }

    public static Key DvdRight
    {
      get
      {
        Key key = new Key(' ', "DvdRight");
        return key;
      }
    }

    public static Key DvdSelect
    {
      get
      {
        Key key = new Key(' ', "DvdSelect");
        return key;
      }
    }

    public static Key Space
    {
      get
      {
        Key key = new Key(' ', "Space");
        return key;
      }
    }

    public static Key BackSpace
    {
      get
      {
        Key key = new Key(' ', "BackSpace");
        return key;
      }
    }

    #endregion

    #region overrides

    public override string ToString()
    {
      return Name;
    }

    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      Key key = obj as Key;
      if (key == null)
      {
        return false;
      }
      return key.Name == Name;
    }

    public static bool operator ==(Key c, Key c2)
    {
      return c.Name == c2.Name;
    }

    public static bool operator !=(Key c, Key c2)
    {
      return c.Name != c2.Name;
    }

    #endregion
  }
}
