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

using System.Collections.Generic;
using System.Windows.Forms;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Controls
{
  public enum PositionCalculationMode
  {
    Relative,
    Absolute,
  }

  public class UserInputCapture : FrameworkElement
  {
    #region Protected fields

    protected ICommandStencil _keyPressedCommand = null;
    protected ICommandStencil _mouseMovedCommand = null;
    protected ICommandStencil _mouseClickedCommand = null;

    protected MouseButtons _mouseButtons = MouseButtons.Left;
    protected PositionCalculationMode _mousePositionMode = PositionCalculationMode.Relative;
    protected bool _isActive = true;
    protected float _lastMouseX = 0;
    protected float _lastMouseY = 0;

    #endregion

    #region Ctor & Maintainance

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);

      UserInputCapture uic = (UserInputCapture) source;
      KeyPressedCommand = uic.KeyPressedCommand;
      MouseMovedCommand = uic.MouseMovedCommand;
      MouseClickedCommand = uic.MouseClickedCommand;
      IsActive = uic.IsActive;
      _lastMouseX = uic._lastMouseX;
      _lastMouseY = uic._lastMouseY;
    }

    #endregion

    #region Public properties

    public bool IsActive
    {
      get { return _isActive; }
      set { _isActive = value; }
    }

    public MouseButtons Buttons
    {
      get { return _mouseButtons; }
      set { _mouseButtons = value; }
    }

    public PositionCalculationMode MousePositionMode
    {
      get { return _mousePositionMode; }
      set { _mousePositionMode = value; }
    }

    public ICommandStencil KeyPressedCommand
    {
      get { return _keyPressedCommand; }
      set { _keyPressedCommand = value; }
    }

    public ICommandStencil MouseMovedCommand
    {
      get { return _mouseMovedCommand; }
      set { _mouseMovedCommand = value; }
    }

    public ICommandStencil MouseClickedCommand
    {
      get { return _mouseClickedCommand; }
      set { _mouseClickedCommand = value; }
    }

    #endregion

    #region Base overrides

    internal override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      if (!IsActive || key == Key.None)
        return;
      ICommandStencil command = KeyPressedCommand;
      if (command == null)
        return;
      command.Execute(new object[] {key});
    }

    internal override void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      //TODO: check if this could also be done in routed event OnMouseMove or OnPreviewMouseMove
      // The only difference should be that routed events get not called if mouse is not over this or a child element
      base.OnMouseMove(x, y, focusCandidates);
      float xTrans = x;
      float yTrans = y;
      if (!TransformMouseCoordinates(ref xTrans, ref yTrans))
        return;

      _lastMouseX = _mousePositionMode == PositionCalculationMode.Relative ? xTrans / (float) ActualWidth : x;
      _lastMouseY = _mousePositionMode == PositionCalculationMode.Relative ? yTrans / (float) ActualHeight : y;
      if (!IsActive)
        return;
      ICommandStencil command = MouseMovedCommand;
      if (command == null)
        return;
      command.Execute(new object[] {_lastMouseX, _lastMouseY});
    }

    //TODO: check if we can use routed OnMouseClick handler. This is the only usage of old OnMouseClick!
    internal override void OnMouseClick(MouseButtons buttons, ref bool handled)
    {
      if (IsActive && buttons == Buttons)
      {
        ICommandStencil command = MouseClickedCommand;
        if (command == null)
          return;
        command.Execute(new object[] {buttons, _lastMouseX, _lastMouseY});
        handled = true;
      }
      base.OnMouseClick(buttons, ref handled);
    }

    #endregion
  }
}