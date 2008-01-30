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
  public class TreeView : ItemsControl
  {
    Property _templateProperty;
    Property _commandParameter;
    Command _command;
    Property _commands;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;
    Command _selectionChanged;

    public TreeView()
    {
      Init();
    }

    public TreeView(TreeView c)
      : base(c)
    {
      Init();

      if (c.Template != null)
        Template = (UIElement)c.Template.Clone();
      Command = c.Command;
      CommandParameter = c._commandParameter;
      SelectionChanged = c.SelectionChanged;

      ContextMenuCommand = c.ContextMenuCommand;
      ContextMenuCommandParameter = c.ContextMenuCommandParameter;
      Commands = (CommandGroup)c.Commands.Clone();
      if (c.Style != null)
      {
        Style = c.Style;
        OnStyleChanged(StyleProperty);
      }
    }

    public override object Clone()
    {
      return new TreeView(this);
    }

    void Init()
    {
      _templateProperty = new Property(null);
      _commandParameter = new Property(null);
      _commands = new Property(new CommandGroup());
      _command = null;
      _contextMenuCommandParameterProperty = new Property(null);
      _contextMenuCommand = null;

    }

    protected override void OnStyleChanged(Property property)
    {
      if (_templateProperty == null)
        return;
      Style.Set(this);
      this.Template.VisualParent = this;
      ArrayList l = new ArrayList();
      l.Add(new TreeViewItem());
      l.Add(new TreeViewItem());
      l.Add(new TreeViewItem());
      l.Add(new TreeViewItem());
      l.Add(new TreeViewItem());
      ItemsSource = l;
      Invalidate();
    }

    public Command SelectionChanged
    {
      get
      {
        return _selectionChanged;
      }
      set
      {
        _selectionChanged = value;
      }
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

    public Property CommandsProperty
    {
      get
      {
        return _commands;
      }
      set
      {
        _commands = value;
      }
    }
    /// <summary>
    /// Gets or sets the command.s
    /// </summary>
    /// <value>The command.</value>
    public CommandGroup Commands
    {
      get
      {
        return _commands.GetValue() as CommandGroup;
      }
      set
      {
        _commands.SetValue(value);
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
      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = (float)(availableSize.Width - marginWidth);
      if (Height <= 0)
        _desiredSize.Height = (float)(availableSize.Height - marginHeight);

      if (_desiredSize.Width == 0) _desiredSize.Width = 200;
      if (_desiredSize.Height == 0) _desiredSize.Height = 200;
      if (Width > 0) _desiredSize.Width = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) _desiredSize.Height = (float)Height * SkinContext.Zoom.Height;
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
      if (Template != null)
      {
        Template.Measure(_desiredSize);
      }
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
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

      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);

      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      if (Template != null)
      {
        layoutRect = new System.Drawing.RectangleF((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualWidth, (float)ActualHeight);

        layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
        layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
        layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
        layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);


        System.Drawing.PointF p = layoutRect.Location;
        ArrangeChild((FrameworkElement)Template, ref p, layoutRect.Width, layoutRect.Height);
        Template.Arrange(new System.Drawing.RectangleF(p, Template.DesiredSize));
        Template.Arrange(layoutRect);
      }

      _isLayoutInvalid = false;
    }
    protected void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p, double widthPerCell, double heightPerCell)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {

        p.X += (float)((widthPerCell - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (float)(widthPerCell - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += (float)((heightPerCell - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (float)(heightPerCell - child.DesiredSize.Height);
      }
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
        ExtendedMatrix m = new ExtendedMatrix(this.Opacity);
        SkinContext.AddTransform(m);
        Template.DoRender();
        SkinContext.RemoveTransform();
      }
    }
    public override void Reset()
    {
      base.Reset();
      if (Template != null)
        Template.Reset();
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
          Command.Execute(CommandParameter, false);
        }
        Commands.Execute(this);
      }
      if (executeContextCmd)
      {
        if (ContextMenuCommand != null)
        {
          ContextMenuCommand.Execute(ContextMenuCommandParameter, false);

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
          while (element.Context == null && element.VisualParent != null)
            element = element.VisualParent;
          CurrentItem = element.Context;
        }
        if (SelectionChanged != null)
        {
          SelectionChanged.Execute(CurrentItem, true);
        }
      }
    }

    public override bool HasFocus
    {
      get
      {
        if (Template != null)
        {
          UIElement element = Template.FindFocusedItem();
          return (element != null);
        }
        return base.HasFocus;
      }
      set
      {
        /*
        if (ItemsPanel.Children.Count > 0)
        {
          ItemsPanel.Children[0].OnMouseMove((float)ItemsPanel.Children[0].ActualPosition.X, (float)ItemsPanel.Children[0].ActualPosition.Y);
          return;
        }*/
        base.HasFocus = value;
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
