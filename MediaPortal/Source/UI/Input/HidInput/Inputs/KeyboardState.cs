#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace HidInput.Inputs
{
  public class KeyboardState
  {
    [DllImport("user32.dll")]
    static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

    protected byte[] _keyboardState = new byte[256];

    public char[] UpdateKeyboardState(Keys key, ushort makeCode, bool keyDown)
    {
      Keys virtualKey = (Keys)((ushort)key);
      UpdateKeyState(virtualKey, keyDown);
      if (virtualKey == Keys.LControlKey || virtualKey == Keys.RControlKey)
        _keyboardState[(ushort)Keys.ControlKey] = (byte)(_keyboardState[(ushort)Keys.LControlKey] | _keyboardState[(ushort)Keys.RControlKey]);
      else if (virtualKey == Keys.LShiftKey || virtualKey == Keys.RShiftKey)
        _keyboardState[(ushort)Keys.ShiftKey] = (byte)(_keyboardState[(ushort)Keys.LShiftKey] | _keyboardState[(ushort)Keys.RShiftKey]);
      else if (virtualKey == Keys.LMenu || virtualKey == Keys.RMenu)
        _keyboardState[(ushort)Keys.Menu] = (byte)(_keyboardState[(ushort)Keys.LMenu] | _keyboardState[(ushort)Keys.RMenu]);
      return GetChars(virtualKey, makeCode);
    }

    public void Reset()
    {
      _keyboardState = new byte[256];
    }

    protected void UpdateKeyState(Keys key, bool keyDown)
    {
      _keyboardState[(ushort)key] = (byte)(keyDown ? 0xff : 0);
    }

    protected char[] GetChars(Keys key, ushort makeCode)
    {
      StringBuilder stringBuilder = new StringBuilder();
      int length = ToUnicode((uint)key, (uint)makeCode, _keyboardState, stringBuilder, 16, (uint)0);
      if (length > 0)
      {
        char[] chars = new char[length];
        stringBuilder.CopyTo(0, chars, 0, length);
        return chars;
      }
      return new char[0];
    }
  }
}
