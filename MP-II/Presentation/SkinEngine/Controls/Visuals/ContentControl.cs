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
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IAddChild
  {
    #region Private fields

    private Property _contentProperty;
    private Property _contentTemplateProperty;
    private Property _contentTemplateSelectorProperty;

    #endregion

    #region Ctor

    public ContentControl()
    {
      Init();
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

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ContentControl c = source as ContentControl;
      Content = copyManager.GetCopy(c.Content);
      ContentTemplateSelector = copyManager.GetCopy(c.ContentTemplateSelector);

      // Don't take part in the outer copying process for the ContentTemplate property here -
      // we need a finished copied content template here. As the template has no references to its
      // containing instance, it is safe to do a self-contained deep copy of it.
      ContentTemplate = MpfCopyManager.DeepCopy(c.ContentTemplate);
    }

    #endregion

    #region Eventhandlers

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

    #region Public properties

    public Property ContentProperty
    {
      get { return _contentProperty; }
    }

    public FrameworkElement Content
    {
      get { return _contentProperty.GetValue() as FrameworkElement; }
      set { _contentProperty.SetValue(value); }
    }

    public Property ContentTemplateProperty
    {
      get { return _contentTemplateProperty; }
    }

    public DataTemplate ContentTemplate
    {
      get { return _contentTemplateProperty.GetValue() as DataTemplate; }
      set { _contentTemplateProperty.SetValue(value); }
    }

    public Property ContentTemplateSelectorProperty
    {
      get { return _contentTemplateSelectorProperty; }
    }

    public DataTemplateSelector ContentTemplateSelector
    {
      get { return _contentTemplateSelectorProperty.GetValue() as DataTemplateSelector; }
      set { _contentTemplateSelectorProperty.SetValue(value); }
    }

    #endregion

    #region IAddChild Members

    public void AddChild(object o)
    {
      Content = (o as FrameworkElement);
    }

    #endregion

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
  }
}
