#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ListViewItem : ContentControl, ISearchableItem
  {
    #region Protected fields

    protected AbstractProperty _dataStringProperty;

    #endregion

    #region Ctor

    public ListViewItem()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _dataStringProperty = new SProperty(typeof(string), "");
    }

    void Attach()
    {
      ContentTemplateProperty.Attach(OnContentTemplateChanged);
    }

    void Detach()
    {
      ContentTemplateProperty.Detach(OnContentTemplateChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      DataString = copyManager.GetCopy(DataString);
      Attach();
    }

    #endregion

    void OnContentTemplateChanged(AbstractProperty property, object oldValue)
    {
      DataTemplate dt = ContentTemplate;
      dt.DataStringProperty.Attach(OnTemplateDataStringChanged);
      DataString = dt.DataString;
    }

    void OnTemplateDataStringChanged(AbstractProperty property, object oldValue)
    {
      DataString = (string) property.GetValue();
    }

    #region Public properties

    public AbstractProperty DataStringProperty
    {
      get { return _dataStringProperty; }
    }

    /// <summary>
    /// Returns a string representation for the current <see cref="TreeViewItem"/>. This is used
    /// by the scrolling engine to find the appropriate element when the user starts to type the first
    /// letters to move the focus to a child entry.
    /// </summary>
    public string DataString
    {
      get { return (string) _dataStringProperty.GetValue(); }
      set { _dataStringProperty.SetValue(value); }
    }

    #endregion

  }
}
