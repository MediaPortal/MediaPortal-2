#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IAddChild<FrameworkElement>
  {
    #region Protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _contentTemplateProperty;

    #endregion

    #region Ctor

    public ContentControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(object), null);
      _contentTemplateProperty = new SProperty(typeof(DataTemplate), null);
    }

    void Attach()
    {
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
      _contentProperty.Attach(OnContentChanged);
      TemplateControlProperty.Attach(OnTemplateControlChanged);
    }

    void Detach()
    {
      _contentTemplateProperty.Detach(OnContentTemplateChanged);
      _contentProperty.Detach(OnContentChanged);
      TemplateControlProperty.Detach(OnTemplateControlChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentControl c = (ContentControl) source;
      Content = copyManager.GetCopy(c.Content);
      ContentTemplate = copyManager.GetCopy(c.ContentTemplate);
      Attach();
      copyManager.CopyCompleted += OnCopyCompleted;
    }

    #endregion

    #region Eventhandlers

    void OnCopyCompleted(ICopyManager copyManager)
    {
      InitializeContentPresenter();
    }

    void OnTemplateControlChanged(AbstractProperty property, object oldValue)
    {
      InitializeContentPresenter();
    }

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      InitializeContentPresenter();
    }

    void OnContentTemplateChanged(AbstractProperty property, object oldValue)
    {
      InitializeContentPresenter();
    }

    protected void InitializeContentPresenter()
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
      {
        presenter.Content = Content;
        presenter.ContentTemplate = ContentTemplate;
      }
    }

    #endregion

    #region Public properties

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public object Content
    {
      get { return _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    public AbstractProperty ContentTemplateProperty
    {
      get { return _contentTemplateProperty; }
    }

    public DataTemplate ContentTemplate
    {
      get { return _contentTemplateProperty.GetValue() as DataTemplate; }
      set { _contentTemplateProperty.SetValue(value); }
    }

    #endregion

    protected virtual ContentPresenter FindContentPresenter()
    {
      return TemplateControl == null ? null : TemplateControl.FindElement(
          new SubTypeFinder(typeof(ContentPresenter))) as ContentPresenter;
    }

    #region IAddChild Members

    public void AddChild(FrameworkElement o)
    {
      Content = o;
    }

    #endregion
  }
}
