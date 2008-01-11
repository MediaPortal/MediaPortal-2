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

using MediaPortal.Core;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using SkinEngine.Skin;

namespace SkinEngine.Controls
{
  public class Keyboard : ListContainer
  {
    private Property _inputString; // the current input
    private string _finalString; // the String that was produced after pressing the Ok button
    private bool _shiftModifier;
    private bool _hadFocus;
    protected Style _style;

    public Keyboard(Control parent)
      : base(parent, null)
    {
      _inputString = new Property("");
      _finalString = "";
      _shiftModifier = false;
    }

    /// <summary>
    /// this switches the modifier produced by the
    /// Shift key
    /// </summary>
    public void SwitchShiftModifier()
    {
      _shiftModifier = !_shiftModifier;
    }

    /// <summary>
    /// gets/sets the current manipulating string
    /// </summary>
    public string CurrentString
    {
      get { return (string) _inputString.GetValue(); }
      set { _inputString.SetValue(value); }
    }

    public Property CurrentStringProperty
    {
      get { return _inputString; }
      set { _inputString = value; }
    }

    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus)
      {
        return;
      }
      PressKey(key);
      base.OnKeyPressed(ref key);
    }

    /// <summary>
    /// presses a key on the keyboard
    /// </summary>
    /// <param name="key"></param>
    public void PressKey(Key key)
    {
      if (key == Key.Space)
      {
        SpaceKey();
      }
      if (key == Key.BackSpace)
      {
        BackspaceKey();
      }
      else if (key.RawCode >= 'a' && key.RawCode <= 'z')
      {
        _shiftModifier = false;
        CurrentString = CurrentString + Modify(key.RawCode.ToString());
      }
      else if (key.RawCode >= 'A' && key.RawCode <= 'Z')
      {
        _shiftModifier = true;
        CurrentString = CurrentString + Modify(key.RawCode.ToString());
      }
      else if (key.RawCode >= '0' && key.RawCode <= '9')
      {
        _shiftModifier = false;
        CurrentString = CurrentString + Modify(key.RawCode.ToString());
      }
    }

    /// <summary>
    /// presses a key on the keyboard
    /// </summary>
    /// <param name="key"></param>
    public void PressAKey(string key)
    {
      if (key == " " || key.ToUpper() == "SPACE")
      {
        SpaceKey();
      }
      else if (key.ToUpper() == "RETURN")
      {
        ReturnKey();
      }
      else if (key.ToUpper() == "BACKSPACE")
      {
        BackspaceKey();
      }
      else if (key.ToLower()[0] >= 'a' && key.ToLower()[0] <= 'z')
      {
        CurrentString = CurrentString + Modify(key.ToLower());
      }
      else if (key.ToLower()[0] >= '0' && key.ToLower()[0] <= '9')
      {
        CurrentString = CurrentString + Modify(key.ToLower());
      }
    }

    /// <summary>
    /// this modifies a key dependent on the modifiers set
    /// (like Shift)
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private string Modify(string character)
    {
      if (_shiftModifier)
      {
        return character.ToUpper();
      }
      else
      {
        return character.ToLower();
      }
    }

    /// <summary>
    /// clear the current input
    /// </summary>
    public void Clear()
    {
      CurrentString = "";
    }

    #region Keys

    public void SpaceKey()
    {
      CurrentString = CurrentString + " ";
    }

    public void ReturnKey()
    {
      CurrentString = CurrentString + '\n';
    }

    public void BackspaceKey()
    {
      if (CurrentString.Length > 0)
      {
        CurrentString = CurrentString.Substring(0, CurrentString.Length - 1);
      }
    }

    public void OkKey()
    {
      _finalString = CurrentString;
    }

    #endregion

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get { return base.HasFocus; }
      set
      {
        base.HasFocus = value;
        if (value && !_hadFocus)
        {
          ServiceScope.Get<IInputManager>().NeedRawKeyData = true;
        }
        if (!value && _hadFocus)
        {
          ServiceScope.Get<IInputManager>().NeedRawKeyData = false;
        }
        _hadFocus = value;
      }
    }
  }
}
