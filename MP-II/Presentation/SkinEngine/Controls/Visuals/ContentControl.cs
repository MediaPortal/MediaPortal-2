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

using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.XamlParser.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IAddChild<FrameworkElement>
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
      Attach();
    }

    void Init()
    {
      _contentProperty = new Property(typeof(FrameworkElement), null);
      _contentTemplateProperty = new Property(typeof(DataTemplate), null);
      _contentTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);
    }

    void Attach()
    {
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
      _contentTemplateSelectorProperty.Attach(OnContentTemplateSelectorChanged);
      _contentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _contentTemplateProperty.Detach(OnContentTemplateChanged);
      _contentTemplateSelectorProperty.Detach(OnContentTemplateSelectorChanged);
      _contentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentControl c = source as ContentControl;
      Content = copyManager.GetCopy(c.Content);
      ContentTemplateSelector = copyManager.GetCopy(c.ContentTemplateSelector);
      ContentTemplate = copyManager.GetCopy(c.ContentTemplate);
      Attach();
      OnContentChanged(ContentProperty);
      OnContentTemplateChanged(ContentTemplateProperty);
      OnContentTemplateSelectorChanged(ContentTemplateSelectorProperty);
    }

    #endregion

    #region Eventhandlers

    void OnContentChanged(Property property)
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.Content = Content;
    }

    void OnContentTemplateChanged(Property property)
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.ContentTemplate = ContentTemplate;
    }

    void OnContentTemplateSelectorChanged(Property property)
    {
      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.ContentTemplateSelector = ContentTemplateSelector;
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

    public override UIElement FindElement(IFinder finder)
    {
      UIElement found = base.FindElement(finder);
      if (found != null) return found;
      if (Content != null) // Hint: Content can be set in XAML, so it is a LogicalTree property
      {
        found = Content.FindElement(finder);
        return found;
      }
      return null;
    }

    #endregion
  }
}
