#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Button : ContentControl
  {
    #region Protected fields

    protected AbstractProperty _isDefaultProperty;
    protected AbstractProperty _isPressedProperty;

    protected AbstractProperty _commandProperty;

    #endregion

    #region Ctor

    public Button()
    {
      Init();
    }

    void Init()
    {
      _isDefaultProperty = new SProperty(typeof(bool), false);
      _isPressedProperty = new SProperty(typeof(bool), false);
      _commandProperty = new SProperty(typeof(IExecutableCommand), null);
      Focusable = true;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Button b = (Button) source;

      Command = copyManager.GetCopy(b.Command);
    }

    #endregion

    public override void OnKeyPreview(ref Key key)
    {
      base.OnKeyPreview(ref key);
      if (!HasFocus)
        return;
      // We handle "normal" button presses in the KeyPreview event, because "Default" and "Cancel" events need
      // to be handled after the focused button was able to consume the event
      if (key == Key.None) return;
      if (key == Key.Ok)
      {
        Execute();
        key = Key.None;
      }
    }

    public override void OnKeyPressed(ref Key key)
    {
      // We handle "Default" and "Cancel" events here, "normal" events will be handled in the KeyPreview event
      base.OnKeyPressed(ref key);
      if (key == Key.None) return;
      if (IsDefault && key == Key.Ok)
      {
        Execute();
        key = Key.None;
      }
    }

    protected void Execute()
    {
      HasFocus = true;
      IsPressed = true;
      if (Command != null)
        Command.Execute();
    }

    #region Public properties

    public override bool HasFocus
    {
      get { return base.HasFocus; }
      internal set
      {
        base.HasFocus = value;
        if (!value)
          IsPressed = false;
      }
    }

    public AbstractProperty IsPressedProperty
    {
      get { return _isPressedProperty; }
    }

    public bool IsPressed
    {
      get { return (bool) _isPressedProperty.GetValue(); }
      set { _isPressedProperty.SetValue(value); }
    }

    public AbstractProperty IsDefaultProperty
    {
      get { return _isDefaultProperty; }
    }

    public bool IsDefault
    {
      get { return (bool) _isDefaultProperty.GetValue(); }
      set { _isDefaultProperty.SetValue(value); }
    }

    public AbstractProperty CommandProperty
    {
      get { return _commandProperty; }
      set { _commandProperty = value; }
    }

    public IExecutableCommand Command
    {
      get { return (IExecutableCommand) _commandProperty.GetValue(); }
      set { _commandProperty.SetValue(value); }
    }

    #endregion
  }
}
