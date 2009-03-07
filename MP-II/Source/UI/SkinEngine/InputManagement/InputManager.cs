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
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.InputManagement
{
  public class InputManager : IInputManager
  {
    #region Protected fields

    private ICollection<Key> _registeredKeys;
    private bool _needRawKeyboardData;
    protected DateTime _lastMouseUsageTime = DateTime.MinValue;
    protected DateTime _lastInputTime = DateTime.MinValue;

    #endregion

    #region Events

    public event MouseMoveHandler MouseMoved;
    public event KeyPressedHandler KeyPressed;

    #endregion

    public InputManager()
    {
      _needRawKeyboardData = false;
      _registeredKeys = new List<Key>();
      _registeredKeys.Add(Key.ContextMenu);
      _registeredKeys.Add(Key.Down);
      _registeredKeys.Add(Key.DvdDown);
      _registeredKeys.Add(Key.DvdLeft);
      _registeredKeys.Add(Key.DvdMenu);
      _registeredKeys.Add(Key.DvdRight);
      _registeredKeys.Add(Key.DvdSelect);
      _registeredKeys.Add(Key.DvdUp);
      _registeredKeys.Add(Key.End);
      _registeredKeys.Add(Key.Enter);
      _registeredKeys.Add(Key.Home);
      _registeredKeys.Add(Key.Left);
      _registeredKeys.Add(Key.None);
      _registeredKeys.Add(Key.PageDown);
      _registeredKeys.Add(Key.PageUp);
      _registeredKeys.Add(Key.Right);
      _registeredKeys.Add(Key.Up);
      _registeredKeys.Add(Key.ZoomMode);
      _registeredKeys.Add(Key.Space);
    }

    public ICollection<Key> Keys
    {
      get { return _registeredKeys; }
    }

    public DateTime LastMouseUsageTime
    {
      get { return _lastMouseUsageTime; }
      internal set { _lastMouseUsageTime = value; }
    }

    public DateTime LastInputTime
    {
      get { return _lastInputTime; }
      internal set { _lastInputTime = value; }
    }

    public void MouseMove(float x, float y)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      if (MouseMoved != null)
        MouseMoved(x, y);
    }

    public void KeyPress(Key key)
    {
      _lastInputTime = DateTime.Now;
      if (KeyPressed != null)
        KeyPressed(ref key);
    }

    public void KeyPress(string keyName)
    {
      _lastInputTime = DateTime.Now;
      foreach (Key key in Keys)
        if (String.Compare(keyName, key.Name, true) == 0)
          KeyPress(key);
    }

    public bool NeedRawKeyData
    {
      get { return _needRawKeyboardData; }
      set { _needRawKeyboardData = value; }
    }
  }
}