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

using System.Collections.Generic;
using System.ComponentModel;

namespace MediaPortal.ServiceMonitor.Collections
{
	public class ObservableCollectionMtNotifyItemChanged<T> : ObservableCollectionMt<T> where T : INotifyPropertyChanged
	{
		// ******************************************************************
		public ObservableCollectionMtNotifyItemChanged() : base()
		{
		}

		// ******************************************************************
		public ObservableCollectionMtNotifyItemChanged(IEnumerable<T> enumOfT) : base(enumOfT)
		{
		}

		// ******************************************************************
		public ObservableCollectionMtNotifyItemChanged(List<T> listOfT) : base(listOfT)
		{
		}
		
		// ******************************************************************
		void ObservableCollectionMtNotifyItemChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged(this, new PropertyChangedEventArgsWrapper("Item", sender, e));
		}

		// ******************************************************************
		public override int Add(object value)
		{
			((T)value).PropertyChanged += new PropertyChangedEventHandler(ObservableCollectionMtNotifyItemChanged_PropertyChanged);
			return base.Add(value);
		}

		// ******************************************************************
		public override void Clear()
		{
			foreach (T item in _list)
			{
				item.PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
			}
			
			base.Clear();
		}

		// ******************************************************************
		public override void Insert(int index, object value)
		{
			((T) value).PropertyChanged +=new PropertyChangedEventHandler(ObservableCollectionMtNotifyItemChanged_PropertyChanged);
			base.Insert(index, value);
		}

		// ******************************************************************
		public override void Remove(object value)
		{
			((T)value).PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
			base.Remove(value);
		}

		// ******************************************************************
		public override void RemoveAt(int index)
		{
			_list[index].PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
			base.RemoveAt(index);
		}

		// ******************************************************************
		public override T this[int index]
		{
			get
			{
				return base[index];
			}
			set
			{
				base[index].PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
				value.PropertyChanged += new PropertyChangedEventHandler(ObservableCollectionMtNotifyItemChanged_PropertyChanged);
				base[index] = value;
			}
		}

		// ******************************************************************
		public override void Add(T item)
		{
			item.PropertyChanged += new PropertyChangedEventHandler(ObservableCollectionMtNotifyItemChanged_PropertyChanged);
			base.Add(item);
		}

		// ******************************************************************
		public override void ClearNotifyOnce()
		{
			foreach (T item in _list)
			{
				item.PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
			}

			base.ClearNotifyOnce();
		}

		// ******************************************************************
		public override void Insert(int index, T item)
		{
			item.PropertyChanged += new PropertyChangedEventHandler(ObservableCollectionMtNotifyItemChanged_PropertyChanged);
			base.Insert(index, item);
		}

		// ******************************************************************
		public override bool Remove(T item)
		{
			item.PropertyChanged -= ObservableCollectionMtNotifyItemChanged_PropertyChanged;
			return base.Remove(item);
		}

		// ******************************************************************
	}
}
