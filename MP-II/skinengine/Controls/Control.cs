#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Drawing;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Commands;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.Animations;

namespace SkinEngine.Controls
{
  public class Control : IControlExt
  {
    #region variables

    protected Window _window;
    protected Property _position;
    protected Property _orginalPosition;
    protected float _width;
    protected float _height;
    protected Property _hasFocus;
    protected Property _hasMouseFocus;
    protected Property _isVisible;
    protected IBooleanProperty _isDepthTestEnabled;
    protected Property _isFocusable;
    protected Control _container;
    protected ColorValue _color;
    protected Property _listItem;
    protected string _name;
    protected AnimationList _animations;
    protected ExtendedMatrix _matrix = new ExtendedMatrix();
    protected Vector4 _alphaMask = new Vector4(1, 1, 1, 1);
    protected AlphaMask _alphaGradient;
    private Control _parent;
    private Rectangle _clipPlane1;
    private bool _clipPlane1Enabled = false;
    protected Property _canFocus;

    private ICommand _command;
    private ICommandParameter _parameter;

    private ICommand _commandContextMenu;
    private ICommandParameter _parameterContextMenu;

    private ICommandResult _commandResult;

    #endregion

    #region ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="Control"/> class.
    /// </summary>
    /// <param name="Parent">The parent control.</param>
    public Control(Control Parent)
    {
      _hasFocus = new Property(false);
      _hasMouseFocus = new Property(false);
      _canFocus = new Property(false);
      _isFocusable = new Property(false);
      _parent = Parent;
      _animations = new AnimationList();
      _name = "";
      _position = new Property(new Vector3(0, 0, 0));
      _orginalPosition = new Property(new Vector3(0, 0, 0));
      _isVisible = new Property(true);
      _isDepthTestEnabled = new BooleanProperty(false);
      //_listItem = new Property(null);
    }

    #endregion

    #region properties

    /// <summary>
    /// Gets or sets a value indicating whether this control can receive & loose focus
    /// </summary>
    /// <value><c>true</c> if this control can receive & loose focus; otherwise, <c>false</c>.</value>
    public virtual bool CanFocus
    {
      get { return (bool)_canFocus.GetValue(); }
      set { _canFocus.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the canfocus property.
    /// </summary>
    /// <value>The canfocus property.</value>
    public Property CanFocusProperty
    {
      get { return _canFocus; }
      set { _canFocus = value; }
    }

    /// <summary>
    /// Gets or sets the command to execute
    /// </summary>
    /// <value>The command.</value>
    public ICommand Command
    {
      get { return _command; }
      set { _command = value; }
    }

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    /// <value>The command parameter.</value>
    public ICommandParameter CommandParameter
    {
      get { return _parameter; }
      set { _parameter = value; }
    }

    /// <summary>
    /// Gets or sets the command result.
    /// </summary>
    /// <value>The command result.</value>
    public ICommandResult CommandResult
    {
      get { return _commandResult; }
      set { _commandResult = value; }
    }

    /// <summary>
    /// Gets or sets the command to execute for the context menu 
    /// </summary>
    /// <value>The command.</value>
    public ICommand ContextMenuCommand
    {
      get { return _commandContextMenu; }
      set { _commandContextMenu = value; }
    }

    /// <summary>
    /// Gets or sets the command parameter for the context menu 
    /// </summary>
    /// <value>The command parameter.</value>
    public ICommandParameter ContextMenuCommandParameter
    {
      get { return _parameterContextMenu; }
      set { _parameterContextMenu = value; }
    }

    /// <summary>
    /// Gets a value indicating whether mouse has been used in the last couple of seconds
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is mouse used; otherwise, <c>false</c>.
    /// </value>
    public bool IsMouseUsed
    {
      get { return SkinContext.MouseUsed; }
    }

    /// <summary>
    /// Gets or sets the window.
    /// </summary>
    /// <value>The window.</value>
    public Window Window
    {
      get { return _window; }
      set { _window = value; }
    }

    /// <summary>
    /// Gets or sets the alpha gradient.
    /// </summary>
    /// <value>The alpha gradient.</value>
    public AlphaMask AlphaGradient
    {
      get { return _alphaGradient; }
      set { _alphaGradient = value; }
    }

    /// <summary>
    /// Gets or sets the alpha mask.
    /// </summary>
    /// <value>The alpha mask.</value>
    public Vector4 AlphaMask
    {
      get { return _alphaMask; }
      set { _alphaMask = value; }
    }

    /// <summary>
    /// Gets or sets the clip plane1.
    /// </summary>
    /// <value>The clip plane1.</value>
    public Rectangle ClipPlane1
    {
      get { return _clipPlane1; }
      set { _clipPlane1 = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether clip plane is enabled].
    /// </summary>
    /// <value><c>true</c> if clip plane1 is enabled; otherwise, <c>false</c>.</value>
    public bool ClipPlane1Enabled
    {
      get { return _clipPlane1Enabled; }
      set { _clipPlane1Enabled = value; }
    }

    /// <summary>
    /// Gets or sets the parent.
    /// </summary>
    /// <value>The parent.</value>
    public IControl Parent
    {
      get { return _parent; }
      set { _parent = (Control)value; }
    }

    /// <summary>
    /// Gets the animations for this control
    /// </summary>
    /// <value>The animations.</value>
    public AnimationList Animations
    {
      get { return _animations; }
    }

    /// <summary>
    /// Gets the list item associated with this control
    /// </summary>
    /// <value>The list item.</value>
    public ListItem ListItem
    {
      get
      {
        if (_listItem == null)
        {
          return null;
        }
        return (ListItem)_listItem.GetValue();
      }
    }

    /// <summary>
    /// Gets or sets the list item property associated with this control
    /// </summary>
    /// <value>The list item property.</value>
    public Property ListItemProperty
    {
      get { return _listItem; }
      set { 
        _listItem = value; 
      }
    }

    /// <summary>
    /// Gets or sets the control name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Gets or sets the associated container.
    /// </summary>
    /// <value>The container.</value>
    public virtual IControl Container
    {
      get { return _container; }
      set { _container = (Control)value; }
    }

    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    /// <value>The color.</value>
    public ColorValue Color
    {
      get { return _color; }
      set { _color = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public virtual bool HasFocus
    {
      get
      {
        if (!IsVisible)
        {
          return false;
        }
        return (bool)_hasFocus.GetValue();
      }
      set
      {
        if (value != (bool)_hasFocus.GetValue())
        {
          if (value)
          {
            Window.FocusedControl = this;
          }
          if ((bool)_hasFocus.GetValue() && Window.FocusedControl == this)
          {
            Window.FocusedControl = null;
          }
          _hasFocus.SetValue(value);
          OnChildFocusChanged(this);
        }
      }
    }
    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public virtual bool HasMouseFocus
    {
      get
      {
        if (!IsVisible)
        {
          return false;
        }
        return (bool)_hasMouseFocus.GetValue();
      }
      set
      {
        if (value != (bool)_hasMouseFocus.GetValue())
        {
          if (value)
          {
            Window.FocusedMouseControl = this;
          }
          if (!value && Window.FocusedMouseControl == this)
          {
            Window.FocusedMouseControl = null;
          }
          _hasMouseFocus.SetValue(value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the  focus property.
    /// </summary>
    /// <value>The  focus property.</value>
    public Property HasFocusProperty
    {
      get { return _hasFocus; }
      set { _hasFocus = value; }
    }
    /// <summary>
    /// Gets or sets the  focus property.
    /// </summary>
    /// <value>The  focus property.</value>
    public Property HasMouseFocusProperty
    {
      get { return _hasMouseFocus; }
      set { _hasMouseFocus = value; }
    }

    /// <summary>
    /// Gets a value indicating whether this control is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control is visible; otherwise, <c>false</c>.
    /// </value>
    public bool IsVisible
    {
      get
      {
        try
        {
          if (_isVisible == null)
          {
            HasMouseFocus = false;
            return false;
          }
          object b = _isVisible.GetValue();
          if (b == null)
          {
            HasMouseFocus = false;
            return false;
          }
          if (b.GetType() == typeof(bool))
          {
            if ((bool)b == false)
            {
              HasMouseFocus = false;
            }
            return (bool)b;
          }
        }
        catch (Exception)
        {
        }
        HasMouseFocus = false;
        return false;
      }
    }

    /// <summary>
    /// Gets or sets the visible property.
    /// </summary>
    /// <value>The visible property.</value>
    public Property IsVisibleProperty
    {
      get { return _isVisible; }
      set { _isVisible = value; }
    }

    /// <summary>
    /// Gets a value indicating wheather depth test is enabled for this control
    /// or not
    /// </summary>
    /// <value>
    ///   <c>true</c> if depth test is enabled for this control
    /// </value>
    public bool IsDepthTestEnabled
    {
      get { return _isDepthTestEnabled.Evaluate(this, Container); }
    }

    /// <summary>
    /// Gets or sets the depthtest enabled property.
    /// </summary>
    /// <value>The depth test property.</value>
    public IBooleanProperty IsDepthTestEnabledProperty
    {
      get
      {
        // check if the parent has depth-test enabled,
        // if so, we should enable it as well
        Control parent = Parent as Control;
        if (parent == null)
        {
          return _isDepthTestEnabled;
        }
        if (parent.IsDepthTestEnabled)
        {
          return parent.IsDepthTestEnabledProperty;
        }
        return _isDepthTestEnabled;
      }
      set
      {
        Control parent = Parent as Control;
        if (parent == null)
        {
          _isDepthTestEnabled = value; // true or false
        }
        else if (parent.IsDepthTestEnabled)
        {
          _isDepthTestEnabled = parent.IsDepthTestEnabledProperty; // true
        }
        else
        {
          _isDepthTestEnabled = value;
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control is currently focusable.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control is currently focusable; otherwise, <c>false</c>.
    /// </value>
    public bool IsFocusable
    {
      get { return (bool)_isFocusable.GetValue(); }
      set { _isFocusable.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the isfocusable property.
    /// </summary>
    /// <value>The isfocusable property.</value>
    public Property IsFocusableProperty
    {
      get { return _isFocusable; }
      set { _isFocusable = value; }
    }

    public virtual void DoLayout()
    {
    }

    public virtual void Invalidate()
    {
    }
    public void DoInvalidate(Control container)
    {
      if (container.Container != null)
      {
        DoInvalidate((Control)container.Container);
        return;
      }
      container.Invalidate();
    }
    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public virtual float Width
    {
      get
      {
        return _width;
      }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public virtual float Height
    {
      get { return _height; }
      set { _height = value; }
    }


    /// <summary>
    /// Gets or sets the controls position.
    /// </summary>
    /// <value>The position.</value>
    public virtual Vector3 Position
    {
      get { return (Vector3)_position.GetValue(); }
      set { _position.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the position property.
    /// </summary>
    /// <value>The position property.</value>
    public Property OriginalPositionProperty
    {
      get { return _orginalPosition; }
      set { _orginalPosition = value; }
    }
    /// <summary>
    /// Gets or sets the controls position.
    /// </summary>
    /// <value>The position.</value>
    public virtual Vector3 OriginalPosition
    {
      get { return (Vector3)_orginalPosition.GetValue(); }
      set { _orginalPosition.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the position property.
    /// </summary>
    /// <value>The position property.</value>
    public virtual Property PositionProperty
    {
      get { return _position; }
      set { _position = value; }
    }

    /// <summary>
    /// Gets a value indicating whether this control is animating.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control is animating; otherwise, <c>false</c>.
    /// </value>
    public virtual bool IsAnimating
    {
      get { return Animations.IsAnimating; }
    }

    #endregion

    #region public members
    public virtual void ExecuteContextMenu()
    {
      if (ContextMenuCommand != null)
      {
        ContextMenuCommand.Execute(ContextMenuCommandParameter);
      }
      if (_container != null)
      {
        _container.ExecuteContextMenu();
      }
    }
    /// <summary>
    /// Executes the Command
    /// If control contains a listitem, it also executes the listitem command
    /// if control is part of a container, it also executes the container control
    /// </summary>
    public virtual void Execute()
    {
      if (Command != null)
      {
        Command.Execute(CommandParameter);
        if (CommandResult != null)
        {
          CommandResult.Execute();
        }
      }
      if (ListItem != null)
      {
        if (ListItem.Command != null)
        {
          ListItem.Command.Execute(ListItem.CommandParameter);
        }
      }
      if (_container != null)
      {
        _container.Execute();
      }
    }

    /// <summary>
    /// Renders the control
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public virtual void Render(uint timePassed) { }

    /// <summary>
    /// animates and renders the control
    /// </summary>
    /// <param name="currentTime">The current time.</param>
    public virtual void DoRender(uint currentTime)
    {
      if (!SkinContext.MouseUsed)
      {
        HasMouseFocus = false;
      }
      Vector3 pos = Position;
      if (ClipPlane1Enabled)
      {
        GraphicsDevice.Device.RenderState.ScissorTestEnable = true;
        float x = _clipPlane1.X;
        float y = _clipPlane1.Y;
        float w = _clipPlane1.Width;
        float h = _clipPlane1.Height;
        x *= (((float)GraphicsDevice.Width) / ((float)SkinContext.Width));
        w *= (((float)GraphicsDevice.Width) / ((float)SkinContext.Width));

        y *= (((float)GraphicsDevice.Height) / ((float)SkinContext.Height));
        h *= (((float)GraphicsDevice.Height) / ((float)SkinContext.Height));
        if (x + w > (float)GraphicsDevice.Width)
        {
          w = (float)GraphicsDevice.Width - x;
        }
        if (y + h > (float)GraphicsDevice.Height)
        {
          h = (float)GraphicsDevice.Height - y;
        }
        GraphicsDevice.Device.ScissorRectangle = new Rectangle((int)x, (int)y, (int)w, (int)h);
      }
      if (IsDepthTestEnabled)
      {
        GraphicsDevice.Device.RenderState.ZBufferEnable = true;
      }
      else
      {
        GraphicsDevice.Device.RenderState.ZBufferEnable = false;
      }

      Animate(currentTime);
      if (_alphaGradient != null)
      {
        if (_alphaGradient.IsVisible)
        {
          SkinContext.SetAlphaGradient(this);
          _alphaGradient.Render();
        }
      }

      Render(currentTime);
      if (_alphaGradient != null)
      {
        if (_alphaGradient.IsVisible)
        {
          SkinContext.SetAlphaGradient(null);
          GraphicsDevice.Device.SetTexture(1, null);
        }
      }
      SkinContext.RemoveTransform();

      if (IsDepthTestEnabled)
      {
        GraphicsDevice.Device.RenderState.ZBufferEnable = false;
      }
      if (ClipPlane1Enabled)
      {
        GraphicsDevice.Device.RenderState.ScissorTestEnable = false;
      }
    }

    public virtual void SetState()
    {
      Animations.SetState(this);
    }

    /// <summary>
    /// Animates the control
    /// </summary>
    /// <param name="currentTime">The current time.</param>
    public virtual void Animate(uint currentTime)
    {
      _matrix.Matrix = Matrix.Identity;
      _matrix.Alpha = new Vector4(1, 1, 1, 1);

      Animations.Animate(currentTime, this, ref _matrix);


      SkinContext.AddTransform(_matrix);
    }

    /// <summary>
    /// Checks if control is positioned at coordinates (x,y) 
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public virtual bool HitTest(float x, float y)
    {
      if (x >= Position.X && x < Position.X + Width)
      {
        if (y >= Position.Y && y < Position.Y + Height)
        {
          return IsVisible;
        }
      }
      return false;
    }

    #region focus & control predicition

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsFocusable)
      {
        if (Position.Y < focusedControl.Position.Y)
        {
          if (!strict)
          {
            return this;
          }
          //           |-------------------------------|  
          //   |----------------------------------------------|
          //   |----------------------|
          //                          |-----|
          //                          |-----------------------|
          if ((Position.X >= focusedControl.Position.X && Position.X <= focusedControl.Position.X + focusedControl.Width) ||
              (Position.X <= focusedControl.Position.X &&
               Position.X + Width >= focusedControl.Position.X + focusedControl.Width) ||
              (Position.X + Width >= focusedControl.Position.X &&
               Position.X + Width <= focusedControl.Position.X + focusedControl.Width))
          {
            return this;
          }
        }
      }
      return null;
    }

    public virtual Control PredictControlUp(Control focusedControl, ref Key key)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (Position.Y < focusedControl.Position.Y)
      {
        //           |-------------------------------|  
        //   |----------------------------------------------|
        //   |----------------------|
        //                          |-----|
        //                          |-----------------------|
        if ((Position.X >= focusedControl.Position.X && Position.X <= focusedControl.Position.X + focusedControl.Width) ||
            (Position.X <= focusedControl.Position.X &&
             Position.X + Width >= focusedControl.Position.X + focusedControl.Width) ||
            (Position.X + Width >= focusedControl.Position.X &&
             Position.X + Width <= focusedControl.Position.X + focusedControl.Width))
        {
          return this;
        }
      }

      return null;
    }

    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsFocusable)
      {
        if (Position.Y > focusedControl.Position.Y)
        {
          if (!strict)
          {
            return this;
          }
          if ((Position.X >= focusedControl.Position.X && Position.X <= focusedControl.Position.X + focusedControl.Width) ||
              (Position.X <= focusedControl.Position.X &&
               Position.X + Width >= focusedControl.Position.X + focusedControl.Width) ||
              (Position.X + Width >= focusedControl.Position.X &&
               Position.X + Width <= focusedControl.Position.X + focusedControl.Width))
          {
            return this;
          }
        }
      }
      return null;
    }

    public virtual Control PredictControlDown(Control focusedControl, ref Key key)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (Position.Y > focusedControl.Position.Y)
      {
        if ((Position.X >= focusedControl.Position.X && Position.X <= focusedControl.Position.X + focusedControl.Width) ||
            (Position.X <= focusedControl.Position.X &&
             Position.X + Width >= focusedControl.Position.X + focusedControl.Width) ||
            (Position.X + Width >= focusedControl.Position.X &&
             Position.X + Width <= focusedControl.Position.X + focusedControl.Width))
        {
          return this;
        }
      }

      return null;
    }

    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsFocusable)
      {
        if (Position.X < focusedControl.Position.X)
        {
          return this;
        }
      }
      return null;
    }

    public virtual Control PredictControlLeft(Control focusedControl, ref Key key)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (Position.X < focusedControl.Position.X)
      {
        return this;
      }
      return null;
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsFocusable)
      {
        if (Position.X > focusedControl.Position.X)
        {
          return this;
        }
      }
      return null;
    }

    public virtual Control PredictControlRight(Control focusedControl, ref Key key)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (Position.X > focusedControl.Position.X)
      {
        return this;
      }
      return null;
    }

    /// <summary>
    /// Calculates the distance between 2 controls
    /// </summary>
    /// <param name="c1">The c1.</param>
    /// <param name="c2">The c2.</param>
    /// <returns></returns>
    public float Distance(Control c1, Control c2)
    {
      float y = Math.Abs(c1.Position.Y - c2.Position.Y);
      float x = Math.Abs(c1.Position.X - c2.Position.X);
      float distance = (float)Math.Sqrt(y * y + x * x);
      return distance;
    }

    #endregion

    /// <summary>
    /// Resets the control and its animations.
    /// </summary>
    public virtual void Reset()
    {
      HasFocus = false;
      Animations.Reset();
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public virtual void OnKeyPressed(ref Key key) { }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public virtual void OnMouseMove(float x, float y) { }

    /// <summary>
    /// Called when a focus change occured on one of the child controls
    /// </summary>
    /// <param name="childControl">The child control.</param>
    public virtual void OnChildFocusChanged(Control childControl)
    {
      Control c = Container as Control;
      if (c != null)
      {
        c.OnChildFocusChanged(childControl);
      }
    }

    #endregion
  }
}