#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace MP2BootstrapperApp.ViewModels.ListItems
{
  /// <summary>
  /// Bindable wrapper for an item that can be selected.
  /// </summary>
  public class SelectableListItem<T> : BindableBase, ISelectable
  {
    protected bool _selected;

    public SelectableListItem()
    {
      PropertyChanged += ItemPropertyChanged;
    }

    private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(Selected))
        OnSelectedChanged();
    }

    public bool Selected
    {
      get { return _selected; }
      set { SetProperty(ref _selected, value); }
    }

    public T Item { get; set; }

    public event EventHandler<SelectedChangedEventArgs> SelectedChanged;

    protected virtual void OnSelectedChanged()
    {
      SelectedChanged?.Invoke(this, new SelectedChangedEventArgs(Selected));
    }
  }
}
