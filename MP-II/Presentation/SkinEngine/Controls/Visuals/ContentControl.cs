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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IAddChild
  {
    private Property _contentProperty;
    private Property _contentTemplateProperty;
    private Property _contentTemplateSelectorProperty;

    #region ctor
    public ContentControl()
    {
      Init();
    }

    public ContentControl(ContentControl c)
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
      ContentControl result = new ContentControl(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _contentProperty = new Property(typeof(FrameworkElement), null);
      _contentTemplateProperty = new Property(typeof(DataTemplate), null);
      _contentTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
      _contentTemplateSelectorProperty.Attach(OnContentTemplateSelectorChanged);
      _contentProperty.Attach(OnContentChanged);
    }
    #endregion

    #region eventhandlers
    void OnContentChanged(Property property)
    {
      ContentPresenter presenter = FindElementType(typeof(ContentPresenter)) as ContentPresenter;
      if (presenter == null)
        presenter = FindElementType(typeof(ScrollContentPresenter)) as ContentPresenter;
      if (presenter != null)
      {
        presenter.Content = Content;
      }
    }
    void OnContentTemplateChanged(Property property)
    {
      ContentPresenter presenter = FindElementType(typeof(ContentPresenter)) as ContentPresenter;
      if (presenter == null)
        presenter = FindElementType(typeof(ScrollContentPresenter)) as ContentPresenter;
      if (presenter != null)
      {
        presenter.ContentTemplate = ContentTemplate;
      }
    }
    void OnContentTemplateSelectorChanged(Property property)
    {
      ContentPresenter presenter = FindElementType(typeof(ContentPresenter)) as ContentPresenter;
      if (presenter == null)
        presenter = FindElementType(typeof(ScrollContentPresenter)) as ContentPresenter;
      if (presenter != null)
      {
        presenter.ContentTemplateSelector = ContentTemplateSelector;
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the content property.
    /// </summary>
    /// <value>The content property.</value>
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

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    /// <value>The content.</value>
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

    /// <summary>
    /// Gets or sets the content template property.
    /// </summary>
    /// <value>The content template property.</value>
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

    /// <summary>
    /// Gets or sets the content template.
    /// </summary>
    /// <value>The content template.</value>
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

    /// <summary>
    /// Gets or sets the content template selector property.
    /// </summary>
    /// <value>The content template selector property.</value>
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

    /// <summary>
    /// Gets or sets the content template selector.
    /// </summary>
    /// <value>The content template selector.</value>
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
    #endregion


    #region IAddChild Members

    public void AddChild(object o)
    {
      Content = (o as FrameworkElement);
    }

    #region findXXX methods
    public override UIElement FindElementType(Type t)
    {
      if (Content != null)
      {
        UIElement o = Content.FindElementType(t);
        if (o != null) return o;
        if (Content.GetType() == t) return Content;
      }
      return base.FindElementType(t);
    }
    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (Content != null)
      {
        UIElement o = Content.FindElement(name);
        if (o != null) return o;
      }
      return base.FindElement(name);
    }

    #endregion
    #endregion
  }
}
