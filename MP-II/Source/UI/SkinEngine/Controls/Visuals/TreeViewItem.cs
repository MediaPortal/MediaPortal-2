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

using System.Collections;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TreeViewItem : HeaderedItemsControl, ISearchableItem, IAddChild<FrameworkElement>
  {
    #region Protected fields

    protected Property _dataStringProperty;
    protected Property _contentProperty;
    protected Property _contentTemplateProperty;

    #endregion

    #region Ctor

    public TreeViewItem()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _dataStringProperty = new Property(typeof(string), "");
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
      object oldContentTemplate = ContentTemplate;
      base.DeepCopy(source, copyManager);
      TreeViewItem twi = (TreeViewItem) source;
      DataString = copyManager.GetCopy(twi.DataString);

      Content = copyManager.GetCopy(twi.Content);
      ContentTemplate = copyManager.GetCopy(twi.ContentTemplate);
      Attach();
      OnContentChanged(_contentProperty, null);
      OnContentTemplateChanged(_contentTemplateProperty, oldContentTemplate);
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
      if (oldValue is HierarchicalDataTemplate)
        ((HierarchicalDataTemplate) oldValue).ItemsSourceProperty.Detach(OnTemplateItemsSourceChanged);
      if (!(ContentTemplate is HierarchicalDataTemplate)) return;
      HierarchicalDataTemplate hdt = (HierarchicalDataTemplate) ContentTemplate;
      hdt.ItemsSourceProperty.Attach(OnTemplateItemsSourceChanged);
      ItemsSource = hdt.ItemsSource;
      hdt.DataStringProperty.Attach(OnTemplateDataStringChanged);
      DataString = hdt.DataString;

      ContentPresenter presenter = FindContentPresenter();
      if (presenter != null)
        presenter.ContentTemplate = ContentTemplate;
    }

    #endregion

    void OnTemplateItemsSourceChanged(Property property, object oldValue)
    {
      ItemsSource = (IEnumerable) property.GetValue();
    }

    void OnTemplateDataStringChanged(Property property, object oldValue)
    {
      DataString = (string) property.GetValue();
    }

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

    public Property DataStringProperty
    {
      get { return _dataStringProperty; }
    }

    /// <summary>
    /// Returns a string representation for the current <see cref="TreeViewItem"/>. This is used
    /// by the scrolling engine to find the appropriate element when the user starts to type the first
    /// letters to move the focus to a child entry.
    /// </summary>
    /// <remarks>
    /// This value be automatically bound to the <see cref="HierarchicalDataTemplate.DataString"/> property.
    /// </remarks>
    public string DataString
    {
      get { return (string) _dataStringProperty.GetValue(); }
      set { _dataStringProperty.SetValue(value); }
    }

    #endregion

    protected override bool Prepare()
    {
      if (!IsExpanded)
        return true;
      return base.Prepare();
    }

    protected ContentPresenter FindContentPresenter()
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
