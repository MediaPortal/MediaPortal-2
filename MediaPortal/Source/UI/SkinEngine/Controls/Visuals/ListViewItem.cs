#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ListViewItem : ContentControl, ISelectableItemContainer
  {
    #region Protected fields

    protected AbstractProperty _selectedProperty;
    protected AbstractProperty _itemIndexProperty;

    #endregion

    public ListViewItem()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _selectedProperty = new SProperty(typeof(bool), false);
      _itemIndexProperty = new SProperty(typeof(int), 0);
    }

    void Attach()
    {
      _selectedProperty.Attach(OnSelectedChanged);
    }

    void Detach()
    {
      _selectedProperty.Detach(OnSelectedChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ListViewItem lvi = (ListViewItem)source;
      Selected = lvi.Selected;
      ItemIndex = lvi.ItemIndex;
      Attach();
    }

    void OnSelectedChanged(AbstractProperty prop, object oldVal)
    {
      ItemsControl ic = FindParentOfType<ItemsControl>();
      if (ic != null)
        ic.UpdateSelectedItem(this);
    }

    public AbstractProperty SelectedProperty
    {
      get { return _selectedProperty; }
    }

    public bool Selected
    {
      get { return (bool)_selectedProperty.GetValue(); }
      set { _selectedProperty.SetValue(value); }
    }

    public AbstractProperty ItemIndexProperty
    {
      get { return _itemIndexProperty; }
    }

    /// <summary>
    /// Gets or sets the absolute index of this item based on the total amount of available items.
    /// This is especially used for virtualization, where only a subset of total items are allocated.
    /// </summary>
    public int ItemIndex
    {
      get { return (int)_itemIndexProperty.GetValue(); }
      set { _itemIndexProperty.SetValue(value); }
    }
  }
}
