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
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Collections;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Commands;
using SkinEngine.Commands;
using SkinEngine.Scripts;
using SkinEngine.Skin;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls
{
  public class TreeContainer : Control
  {
    #region variables

    private StylesCollection _styles;
    private readonly string _itemsProperty;
    private readonly Property _listItems;
    private bool _isSubTree = false;
    private TreeContainer _rootContainer;
    float _offset;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeContainer"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="itemsProperty">The items property.</param>
    public TreeContainer(Control parent, string itemsProperty)
      : base(parent)
    {
      _itemsProperty = itemsProperty;
      _styles = new StylesCollection(parent);
      IsFocusable = false;
      _listItems = new Property("ListItemsProperty", null);
      _rootContainer = this;
      _offset = 0.0f;
    }
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    /// <value>The items.</value>
    public ItemsCollection Items
    {
      get { return (ItemsCollection)_listItems.GetValue(); }
      set
      {
        if (Items != null)
        {
          //Items.Changed -= _handler;
        }
        _listItems.SetValue(value);
        if (Items != null)
        {
          //Items.Changed += _handler;
        }
      }
    }

    /// <summary>
    /// Gets or sets the root tree container.
    /// </summary>
    /// <value>The root container.</value>
    public TreeContainer RootContainer
    {
      get
      {
        return _rootContainer;
      }
      set
      {
        _rootContainer = value;
      }
    }

    /// <summary>
    /// Gets or sets the items property.
    /// </summary>
    /// <value>The items property.</value>
    public Property ItemsProperty
    {
      get { return _listItems; }
      set { _listItems.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public override float Width
    {
      get
      {
        if (_width != 0) return _width;
        return _styles.SelectedStyle.Width;
      }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public override float Height
    {
      get
      {
        if (_height != 0) return _height;
        return _styles.SelectedStyle.Height;
      }
      set { _height = value; }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (!IsSubTree)
      {
        if (HasFocus || _styles.SelectedStyle.HasFocus)
        {
          if (key == Key.Home)
          {
            Position = new Microsoft.DirectX.Vector3(OriginalPosition.X, OriginalPosition.Y, OriginalPosition.Z);
            _offset = 0.0f;
            if (Window.FocusedControl != this && Window.FocusedControl != null)
              Window.FocusedControl.HasFocus = false;
            _styles.SelectedStyle.SetFocus(0);
            return;
          }
          if (key == Key.End)
          {
            if (_styles.SelectedStyle.Height > Height)
            {
              _offset = -(_styles.SelectedStyle.Height - Height);
              Position = new Microsoft.DirectX.Vector3(OriginalPosition.X, _offset + OriginalPosition.Y, OriginalPosition.Z);
            }
            if (Window.FocusedControl != this && Window.FocusedControl != null)
              Window.FocusedControl.HasFocus = false;
            _styles.SelectedStyle.SetFocus(_styles.SelectedStyle.FocusableControlCount - 1);
            return;
          }
          if (key == Key.PageDown)
          {
            PageDown();
            return;

          }

          if (key == Key.PageUp)
          {
            PageUp();
            return;
          }
        }
      }
      _styles.SelectedStyle.OnKeyPressed(ref key);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get
      {
        return (bool)_hasFocus.GetValue();
      }
      set
      {
        if (value != (bool)_hasFocus.GetValue())
        {
          _hasFocus.SetValue(value);
          base.OnChildFocusChanged(this);
        }
      }
    }

    /// <summary>
    /// Called when a focus change occured on one of the child controls
    /// </summary>
    /// <param name="childControl">The child control.</param>
    public override void OnChildFocusChanged(Control childControl)
    {
      HasFocus = childControl.HasFocus;

      if (IsSubTree)
        RootContainer.OnChildFocusChanged(childControl);
    }
    /// <summary>
    /// Gets or sets the styles.
    /// </summary>
    /// <value>The styles.</value>
    public StylesCollection Styles
    {
      get { return _styles; }
      set { _styles = value; }
    }
    /// <summary>
    /// Resets the control and its animations.
    /// </summary>
    public override void Reset()
    {
      HasFocus = false;
      base.Reset();
      _styles.Reset();
      Position = new Microsoft.DirectX.Vector3(OriginalPosition.X, OriginalPosition.Y, OriginalPosition.Z);
      _offset = 0.0f;

      Items = new ItemsCollection();
      Items = UpdateItems();
      Invalidate();
    }
    /// <summary>
    /// Updates the items by refreshing the databinding from listcontainer<->model
    /// </summary>
    /// <returns></returns>
    private ItemsCollection UpdateItems()
    {
      try
      {
        if (_itemsProperty != null)
        {
          string[] parts = _itemsProperty.Split(new char[] { '.' });
          object model = ObjectFactory.GetObject(Window, parts[0]);
          if (model != null)
          {
            if (parts[1].StartsWith("#script"))
            {
              string scriptName = parts[1].Substring("#script:".Length);
              if (ScriptManager.Instance.Contains(scriptName))
              {
                IProperty property = (IProperty)ScriptManager.Instance.GetScript(scriptName);
                return (ItemsCollection)property.Evaluate(model);
              }
              return new ItemsCollection();
            }

            int partNr = 1;
            object obj = null;
            while (partNr < parts.Length)
            {
              Type classType = model.GetType();
              PropertyInfo property = classType.GetProperty(parts[partNr],
                                                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                                            BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
              if (property == null)
              {
                ServiceScope.Get<ILogger>().Error("Property {0} is not found", _itemsProperty);
                return new ItemsCollection();
              }
              obj = property.GetValue(model, null);
              //obj = classType.InvokeMember(parts[partNr], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty, System.Type.DefaultBinder, model, null);
              partNr++;
              if (partNr < parts.Length)
              {
                model = obj;
                if (obj == null)
                {
                  return null;
                }
              }
            }
            return (ItemsCollection)obj;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ListContainer error updating items from model:{0}", _itemsProperty);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return new ItemsCollection();
    }


    /// <summary>
    /// Checks if control is positioned at coordinates (x,y)
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      return _styles.HitTest(x, y);
    }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      _styles.SelectedStyle.OnMouseMove(x, y);

    }

    /// <summary>
    /// Gets the focused control.
    /// </summary>
    /// <value>The focused control.</value>
    public Control FocusedControl
    {
      get
      {
        if (_styles.SelectedStyle == null)
        {
          return null;
        }
        return _styles.SelectedStyle.FocusedControl;
      }
    }

    /// <summary>
    /// Switches to the next available style.
    /// </summary>
    public void SwitchStyle()
    {
      int style = _styles.SelectedStyleIndex + 1;
      if (style >= _styles.Styles.Count)
      {
        style = 0;
      }
      _styles.SelectedStyleIndex = style;
    }
    /// <summary>
    /// Renders the control
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Render(uint timePassed)
    {

      lock (_listItems)
      {
        if (!IsVisible)
        {
          if (false == Animations.IsAnimating && false == _styles.SelectedStyle.IsAnimating)
          {
            return;
          }
        }
        base.Render(timePassed);
        _styles.SelectedStyle.DoRender(timePassed);
      }
    }

    /// <summary>
    /// Does the layout.
    /// </summary>
    public override void DoLayout()
    {
    }
    /// <summary>
    /// Invalidates this instance.
    /// </summary>
    public override void Invalidate()
    {
      Trace.WriteLine("tree.invalidate");
      _styles.SelectedStyle.Invalidate();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this tree is sub tree or not.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is sub tree; otherwise, <c>false</c>.
    /// </value>
    public bool IsSubTree
    {
      get
      {
        return _isSubTree;
      }
      set
      {
        _isSubTree = value;
      }
    }

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      Control childControl = _styles.SelectedStyle.PredictFocusUp(focusedControl, ref key, strict);
      if (!IsSubTree && childControl != null)
      {
        if (childControl.Position.Y < OriginalPosition.Y)
        {
          _offset = (OriginalPosition.Y - childControl.Position.Y);

          Position = new Microsoft.DirectX.Vector3(OriginalPosition.X, Position.Y + _offset, OriginalPosition.Z);
        }
      }
      return childControl;
    }

    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      Control childControl = _styles.SelectedStyle.PredictFocusDown(focusedControl, ref key, strict);
      if (!IsSubTree && childControl != null)
      {
        if (childControl.Position.Y >= OriginalPosition.Y + Height)
        {
          Trace.WriteLine(String.Format(">pos:{0} offset:{1} {2}", Position.Y, _offset, childControl.Position.Y));

          Key keyt = Key.Down;
          Control c = _styles.SelectedStyle.PredictFocusDown(childControl, ref keyt, true);
          if (c != null)
          {
            _offset = -(c.Position.Y - childControl.Position.Y);
            Trace.WriteLine(String.Format("  next:{0} cur:{1}->off={2}", c.Position.Y, childControl.Position.Y, _offset));


            Position = new Microsoft.DirectX.Vector3(OriginalPosition.X, Position.Y + _offset, OriginalPosition.Z);
            Trace.WriteLine(String.Format("  pos:{0} offset:{1} {2}", Position.Y, _offset, childControl.Position.Y));
          }
        }

      }
      return childControl;
    }
    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      return _styles.SelectedStyle.PredictFocusLeft(focusedControl, ref key, strict);
    }
    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      return _styles.SelectedStyle.PredictFocusRight(focusedControl, ref key, strict);
    }


    float GetTreeItemHeight(Group g)
    {
      foreach (Control c in g.Controls)
      {
        TreeItem item = c as TreeItem;
        if (item != null && !item.IsExpanded)
        {
          return item.Height;
        }
        Group gsub = c as Group;
        if (gsub != null)
        {
          float h = GetTreeItemHeight(gsub);
          if (h > 0) return h;
        }
      }
      return 0.0f;
    }

    void PageDown()
    {
      int count = 0;
      Control control = Window.FocusedControl;
      while (true)
      {
        Key key = Key.Down;
        Control c = _styles.SelectedStyle.PredictFocusDown(control, ref key, true);
        if (c == null) return;
        if (c.Position.Y >= OriginalPosition.Y + Height)
        {
          break;
        }
        control = c;
        count++;
      }
      int items = (int)(Height / GetTreeItemHeight((Group)_styles.SelectedStyle));
      for (int i = 0; i < items; ++i)
      {
        Key key = Key.Down;
        control = PredictFocusDown(control, ref  key, true);
      }
      if (control != null)
      {

        for (int i = 0; i < count; ++i)
        {
          Key key = Key.Up;
          control = PredictFocusUp(control, ref  key, true);
        }
        Window.FocusedControl.HasFocus = false;
        control.HasFocus = true;
      }

    }
    void PageUp()
    {
      int count = 0;
      Control control = Window.FocusedControl;
      while (true)
      {
        Key key = Key.Up;
        Control c = _styles.SelectedStyle.PredictFocusUp(control, ref key, true);
        if (c == null) return;
        if (c.Position.Y < OriginalPosition.Y)
        {
          break;
        }
        control = c;
        count++;
      }
      int items = (int)(Height / GetTreeItemHeight((Group)_styles.SelectedStyle));
      for (int i = 0; i < items; ++i)
      {
        Key key = Key.Up;
        control = PredictFocusUp(control, ref  key, true);
      }
      if (control != null)
      {

        for (int i = 0; i < count; ++i)
        {
          Key key = Key.Down;
          control = PredictFocusDown(control, ref  key, true);
        }
        Window.FocusedControl.HasFocus = false;
        control.HasFocus = true;
      }

    }
  }
}
