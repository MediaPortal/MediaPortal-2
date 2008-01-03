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
using System.Diagnostics;
using MediaPortal.Core.Properties;


namespace SkinEngine.Controls.Visuals
{
  public class ContentPresenter : FrameworkElement
  {
    private Property _contentProperty;
    private Property _contentTemplateProperty;
    private Property _contentTemplateSelectorProperty;

    public ContentPresenter()
    {
      Init();
    }

    public ContentPresenter(ContentPresenter c)
      : base(c)
    {
      Init();
      if (c.Content != null)
      {
        Content = (FrameworkElement)c.Content.Clone();
        Content.VisualParent = this;
      }
      if (c.ContentTemplate != null)
        ContentTemplate = (DataTemplate)c.ContentTemplate.Clone();
      if (c.ContentTemplateSelector != null)
        ContentTemplateSelector = c.ContentTemplateSelector;
    }

    public override object Clone()
    {
      return new ContentPresenter(this);
    }

    void Init()
    {
      _contentProperty = new Property(null);
      _contentTemplateProperty = new Property(null);
      _contentTemplateSelectorProperty = new Property(null);
      _contentProperty.Attach(new PropertyChangedHandler(OnContentChanged));
    }
    void OnContentChanged(Property property)
    {
      Content.VisualParent = this;
    }

    public Property ContentProperty
    {
      get
      {
        return _contentProperty;
      }
      set
      {
        _contentProperty = value;
      }
    }

    public FrameworkElement Content
    {
      get
      {
        return _contentProperty.GetValue() as FrameworkElement;
      }
      set
      {
        _contentProperty.SetValue(value);
      }
    }

    public Property ContentTemplateProperty
    {
      get
      {
        return _contentTemplateProperty;
      }
      set
      {
        _contentTemplateProperty = value;
      }
    }

    public DataTemplate ContentTemplate
    {
      get
      {
        return _contentTemplateProperty.GetValue() as DataTemplate;
      }
      set
      {
        _contentTemplateProperty.SetValue(value);
      }
    }


    public Property ContentTemplateSelectorProperty
    {
      get
      {
        return _contentTemplateSelectorProperty;
      }
      set
      {
        _contentTemplateSelectorProperty = value;
      }
    }

    public DataTemplateSelector ContentTemplateSelector
    {
      get
      {
        return _contentTemplateSelectorProperty.GetValue() as DataTemplateSelector;
      }
      set
      {
        _contentTemplateSelectorProperty.SetValue(value);
      }
    }

    public override void Measure(Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      if (Content != null)
      {
        Content.Measure(_desiredSize);
        _desiredSize = Content.DesiredSize;
      }
      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;
      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);

      _availableSize = new Size(availableSize.Width, availableSize.Height);
    }

    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      _availablePoint = new System.Drawing.Point(finalRect.Location.X, finalRect.Location.Y);
      System.Drawing.Rectangle layoutRect = new System.Drawing.Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (Content != null)
      {
        Content.Arrange(layoutRect);
        ActualPosition = Content.ActualPosition;
        ActualWidth = ((FrameworkElement)Content).ActualWidth;
        ActualHeight = ((FrameworkElement)Content).ActualHeight;
      }


      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
    }
    public override void DoRender()
    {
      base.DoRender();
      if (Content != null)
      {
        Content.DoRender();
      }
    }

    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (!IsFocusScope) return;
      if (Content != null)
      {
        Content.OnMouseMove(x, y);
      }
      base.OnMouseMove(x, y);
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref MediaPortal.Core.InputManager.Key key)
    {
      if (!HasFocus) return;
      if (!IsFocusScope) return;

      UIElement cntl = FocusManager.PredictFocus(this, ref key);
      if (cntl != null)
      {
        HasFocus = false;
        cntl.HasFocus = true;
        key = MediaPortal.Core.InputManager.Key.None;
      }
    }
  }
}
