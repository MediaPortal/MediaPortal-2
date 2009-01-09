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

using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IAddChild<FrameworkElement>
  {
    #region Private fields

    private Property _contentProperty;
    private Property _contentTemplateProperty;

    #endregion

    #region Ctor

    public ContentControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new Property(typeof(object), null);
      _contentTemplateProperty = new Property(typeof(DataTemplate), null);
    }

    void Attach()
    {
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
      _contentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _contentTemplateProperty.Detach(OnContentTemplateChanged);
      _contentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentControl c = (ContentControl) source;
      Content = copyManager.GetCopy(c.Content);
      ContentTemplate = copyManager.GetCopy(c.ContentTemplate);
      Attach();
      OnContentChanged(ContentProperty, null);
      OnContentTemplateChanged(ContentTemplateProperty, null);
    }

    #endregion

    #region Eventhandlers

    void OnContentChanged(Property property, object oldValue)
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.Content = Content;
    }

    void OnContentTemplateChanged(Property property, object oldValue)
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.ContentTemplate = ContentTemplate;
    }

    #endregion

    #region Public properties

    public Property ContentProperty
    {
      get { return _contentProperty; }
    }

    public object Content
    {
      get { return _contentProperty.GetValue(); }
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

    #endregion

    #region IAddChild Members

    public void AddChild(FrameworkElement o)
    {
      Content = o;
    }

    #endregion

    protected ContentPresenter FindContentPresenter()
    {
      return TemplateControl == null ? null : TemplateControl.FindElement(
          new SubTypeFinder(typeof(ContentPresenter))) as ContentPresenter;
    }

    #region Base overrides

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null && presenter.TemplateControl != null)
        childrenOut.Add(presenter.TemplateControl);
    }

    #endregion
  }
}
