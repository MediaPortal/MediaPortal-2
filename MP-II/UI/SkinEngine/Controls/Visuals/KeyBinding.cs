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
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class KeyBinding : FrameworkElement
  {
    #region Private fields

    Property _keyProperty;
    IExecutableCommand _command;

    #endregion

    #region Ctor

    public KeyBinding()
    {
      Init();
    }

    void Init()
    {
      _command = null;
      _keyProperty = new Property(typeof(string), "");
      Focusable = false;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      KeyBinding b = (KeyBinding) source;
      Command = copyManager.GetCopy(b.Command);
      KeyPress = copyManager.GetCopy(b.KeyPress);
    }

    #endregion

    #region Public properties

    public string KeyPress
    {
      get { return _keyProperty.GetValue() as string; }
      set { _keyProperty.SetValue(value); }
    }

    public Property KeyPressProperty
    {
      get { return _keyProperty; }
    }

    public IExecutableCommand Command
    {
      get { return _command; }
      set { _command = value; }
    }

    #endregion

    public override void DoRender()
    { }

    public override void OnKeyPressed(ref Key key)
    {
      if (key == MediaPortal.Control.InputManager.Key.None)
      {
        return;
      }
      if (key.ToString() == KeyPress)
      {
        if (Command != null)
          Command.Execute();
      }
    }
  }
}
