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
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Bindings;

namespace SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    Property _styleProperty;
    Property _templateProperty;
    Property _commandParameter;
    Command _command;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;


    //ArrayList _items;
    //public class MyItem
    //{
    //  public string _image;
    //  public Property _label1;
    //  public string _label2;
    //  public MyItem(string img, string label1, string label2)
    //  {
    //    _image = img;
    //    _label1 = new Property(label1);
    //    _label2 = label2;
    //  }
    //  public string Label1 { get { return (string)_label1.GetValue(); } }
    //  public Property Label1Property { get { return _label1; } }
    //  public string Label2 { get { return _label2; } }
    //  public string Image { get { return _image; } }
    //}

    public ListView()
    {
      Init();
    }

    public ListView(ListView c)
      : base(c)
    {
      Init();
      if (c.Style != null)
        Style = c.Style;

      if (c.Template != null)
        Template = (UIElement)c.Template.Clone();
      Command = c.Command;
      CommandParameter = c._commandParameter;

      ContextMenuCommand = c.ContextMenuCommand;
      ContextMenuCommandParameter = c.ContextMenuCommandParameter;
    }

    public override object Clone()
    {
      return new ListView(this);
    }

    void Init()
    {
      _styleProperty = new Property(null);
      _templateProperty = new Property(null);
      _commandParameter = new Property(null);
      _command = null;
      _contextMenuCommandParameterProperty = new Property(null);
      _contextMenuCommand = null;
      _styleProperty.Attach(new PropertyChangedHandler(OnStyleChanged));

      //_items = new ArrayList();
      //_items.Add(new MyItem("defaultuser.png", "Item 1", "Item 1.1"));
      //_items.Add(new MyItem("defaultuser.png", "Item 2", "Item 1.2"));
      //_items.Add(new MyItem("defaultuser.png", "Item 3", "Item 1.3"));
      //_items.Add(new MyItem("defaultuser.png", "Item 4", "Item 1.4"));

      //ItemsSource = _items;
    }

    void OnStyleChanged(Property property)
    {
      Style.Set(this);
      this.Template.VisualParent = this;
      ItemsPanel = (Panel)this.Template.FindItemsHost();
      Invalidate();
    }

    /// <summary>
    /// Gets or sets the control template property.
    /// </summary>
    /// <value>The control template property.</value>
    public Property TemplateProperty
    {
      get
      {
        return _templateProperty;
      }
      set
      {
        _templateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control template.
    /// </summary>
    /// <value>The control template.</value>
    public UIElement Template
    {
      get
      {
        return _templateProperty.GetValue() as UIElement;
      }
      set
      {
        _templateProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the control style property.
    /// </summary>
    /// <value>The control style property.</value>
    public Property StyleProperty
    {
      get
      {
        return _styleProperty;
      }
      set
      {
        _styleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public Style Style
    {
      get
      {
        return _styleProperty.GetValue() as Style;
      }
      set
      {
        _styleProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the command.
    /// </summary>
    /// <value>The command.</value>
    public Command Command
    {
      get
      {
        return _command;
      }
      set
      {
        _command = value;
      }
    }
    /// <summary>
    /// Gets or sets the command parameter property.
    /// </summary>
    /// <value>The command parameter property.</value>
    public Property CommandParameterProperty
    {
      get
      {
        return _commandParameter;
      }
      set
      {
        _commandParameter = value;
      }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public object CommandParameter
    {
      get
      {
        return _commandParameter.GetValue();
      }
      set
      {
        _commandParameter.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the context menu command.
    /// </summary>
    /// <value>The context menu command.</value>
    public Command ContextMenuCommand
    {
      get
      {
        return _contextMenuCommand;
      }
      set
      {
        _contextMenuCommand = value;
      }
    }

    /// <summary>
    /// Gets or sets the context menu command parameter property.
    /// </summary>
    /// <value>The context menu command parameter property.</value>
    public Property ContextMenuCommandParameterProperty
    {
      get
      {
        return _contextMenuCommandParameterProperty;
      }
      set
      {
        _contextMenuCommandParameterProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the context menu command parameter.
    /// </summary>
    /// <value>The context menu command parameter.</value>
    public object ContextMenuCommandParameter
    {
      get
      {
        return _contextMenuCommandParameterProperty.GetValue();
      }
      set
      {
        _contextMenuCommandParameterProperty.SetValue(value);
      }
    }


    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      if (Width <= 0)
        _desiredSize.Width = (float)availableSize.Width - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (float)availableSize.Height - (float)(Margin.Y + Margin.Z);

      if (Template != null)
      {
        Template.Measure(_desiredSize);
        _desiredSize = Template.DesiredSize;
      }
      if (Width > 0) _desiredSize.Width = (float)Width;
      if (Height > 0) _desiredSize.Height = (float)Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;


      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (Template != null)
      {
        Template.Arrange(layoutRect);
        ActualPosition = Template.ActualPosition;
        ActualWidth = ((FrameworkElement)Template).ActualWidth;
        ActualHeight = ((FrameworkElement)Template).ActualHeight;
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
      _isLayoutInvalid = false;
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (DoUpdateItems())
      {
        Invalidate();
      }
      base.DoRender();
      if (Template != null)
      {
        Template.DoRender();
      }
    }

    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      base.Animate();
      if (Template != null)
      {
        Template.Animate();
      }
    }

    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      if (Template != null)
      {
        Template.OnMouseMove(x, y);
      }
      UpdateCurrentItem();
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      bool executeCmd = (CurrentItem != null && key == MediaPortal.Core.InputManager.Key.Enter);
      bool executeContextCmd = (CurrentItem != null && key == MediaPortal.Core.InputManager.Key.ContextMenu);
      base.OnKeyPressed(ref key);
      if (Template != null)
      {
        Template.OnKeyPressed(ref key);
      }
      UpdateCurrentItem();
      if (executeCmd)
      {
        if (Command != null)
        {
          Command.Method.Invoke(Command.Object, new object[] { CommandParameter });
        }
      }
      if (executeContextCmd)
      {
        if (ContextMenuCommand != null)
        {
          ContextMenuCommand.Method.Invoke(ContextMenuCommand.Object, new object[] { ContextMenuCommandParameter });

        }
      }
    }

    /// <summary>
    /// Updates the current item.
    /// </summary>
    void UpdateCurrentItem()
    {
      if (Template != null)
      {
        UIElement element = Template.FindFocusedItem();
        if (element == null)
        {
          CurrentItem = null;
        }
        else
        {
          CurrentItem = element.Context;
        }
      }

    }

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }


    #endregion
  }
}
