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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MediaPortal.ServiceMonitor.Collections
{
	// 100% Free but please do not remove next line. 
	// By Eric Ouellet, 2011-04-13. Multithreaded collection.
	// Using this OC is at your own risk :-). 
	// For less frustrations, perhaps better read the article that come with it (CodeProject) ?
	// Do not rely on data read from a worker thread if updates are in progress.
	/// <summary>
	/// Asynchronous MultiThread ObservableCollection. Compatible with standard OC.
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	public class ObservableCollectionMt<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable, INotifyCollectionChanged, INotifyPropertyChanged
	{
		// ******************************************************************
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;
		protected List<T> _list = new List<T>();

		private DispatcherPriority _dispatchPriorityCall = DispatcherPriority.Background;
		// ******************************************************************
		public DispatcherPriority DispatchPriorityCall
		{
			get
			{
				return _dispatchPriorityCall;
			}
			set
			{
				_dispatchPriorityCall = value;
				NotifyPropertyChanged(() => DispatchPriorityCall);
			}
		}

		// ******************************************************************
		protected void NotifyPropertyChanged(String propertyName)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// ******************************************************************
		protected void NotifyPropertyChanged<T2>(Expression<Func<T2>> propAccess)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;
			if (propertyChanged != null)
			{
				var asMember = propAccess.Body as MemberExpression;
				if (asMember == null)
					return;

				propertyChanged(this, new PropertyChangedEventArgs(asMember.Member.Name));
			}
		}

		// ******************************************************************
		protected void NotifyPropertyChanged(Object obj, PropertyChangedEventArgs e)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(obj, e);
			}
		}

		// ******************************************************************
		protected void NotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler collectionChanged = CollectionChanged;
			if (collectionChanged != null)
			{
				collectionChanged(sender, e);
			}
		}

		// ******************************************************************
		public IEnumerator<T> GetEnumeratorCopy()
		{
			lock (SyncRoot)
			{
				return (new List<T>(_list)).GetEnumerator();
			}
		}

		// ******************************************************************
		// Here, we should always return a blockingIterator but a bug from Microsoft in CollectionView prevent us from doing so.
		// CollectionView (Framework 4.0) keep iterator without call to Dispose(). But modification should be done in UI threads...
		// Then it is not a must.
		public virtual IEnumerator<T> GetEnumerator()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				return _list.GetEnumerator();
			}
			return new ObservableCollectionMtIteratorBlocking<T>(this);
		}

		// ******************************************************************
		// Call with precaution. Could get an exception in situation where add/remove/clear happen on antoher thread.
		public virtual IEnumerator<T> GetUnsafeEnum()
		{
			return _list.GetEnumerator();
		}

		// ******************************************************************
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		// ******************************************************************
		public ObservableCollectionMt()
		{
			_list = new List<T>();
		}

		// ******************************************************************
		public ObservableCollectionMt(IEnumerable<T> enumOfT)
		{
			_list = new List<T>(enumOfT);
		}

		// ******************************************************************
		public ObservableCollectionMt(List<T> listOfT)
		{
			_list = new List<T>(listOfT);
		}

		// ******************************************************************
		public int IndexOf(T item)
		{
			lock (SyncRoot)
			{
				return _list.IndexOf(item);
			}
		}

		// ******************************************************************
		public virtual void Insert(int index, T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					_list.Insert(index, item);
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => Insert(index, item)));
			}
		}

		// ******************************************************************
		public virtual void RemoveAt(int index)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				T removedItem;

				lock (SyncRoot)
				{
					removedItem = _list[index];
					_list.RemoveAt(index);
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => RemoveAt(index)));
			}
		}

		// ******************************************************************
		public virtual T this[int index]
		{
			get
			{
				lock (SyncRoot)
				{
					return _list[index];
				}
			}
			set
			{
				if (Application.Current.Dispatcher.CheckAccess())
				{
					T oldItem;
					lock (SyncRoot)
					{
						oldItem = _list[index];
						_list[index] = value;
					}

					NotifyPropertyChanged(() => "Item[]");
					NotifyCollectionChanged(this,
											new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem));
				}
				else
				{
					Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => this[index] = value));
				}
			}
		}

		// ******************************************************************
		public virtual void Add(T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				int index;

				lock (SyncRoot)
				{
					_list.Add(item);
					index = _list.Count - 1;
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => Add(item)));
			}
		}

		// ******************************************************************
		public virtual void AddRange(IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				Add(item);
			}
		}

		// ******************************************************************
		// Be carefull because the inner _list is a new object (better performance, mainly if many items)
		public virtual void ClearNotifyOnce()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				List<T> originalList;

				lock (SyncRoot)
				{
					originalList = _list;
					_list = new List<T>();
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, originalList));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => ClearNotifyOnce()));
			}
		}

		// ******************************************************************
		// Take care of the different behavior from the original OC. It notify here !!!
		public virtual void ClearNotifyForEach()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				// Remove One by One. Very penalizing but we have to live with Microsoft code.
				// This is due to CollectionView not supporting any range action.
				for (; ; )
				{
					T removedItem;
					lock (SyncRoot)
					{
						if (_list.Count == 0)
						{
							break;
						}
						removedItem = _list[0];
						_list.RemoveAt(0);
					}

					NotifyPropertyChanged(() => Count);
					NotifyPropertyChanged(() => "Item[]");
					NotifyCollectionChanged(this,
											new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem));
				}
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => ClearNotifyForEach()));
			}
		}

		// ******************************************************************
		public virtual void Clear()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					_list.Clear();
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");

			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => Clear()));
			}
		}

		// ******************************************************************
		public bool Contains(T item)
		{
			lock (SyncRoot)
			{
				return _list.Contains(item);
			}
		}

		// ******************************************************************
		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (SyncRoot)
			{
				_list.CopyTo(array, arrayIndex);
			}
		}

		// ******************************************************************
		public int Count
		{
			get
			{
				lock (SyncRoot) // Could be not needed now, but could be in the futur, who knows
				{
					return _list.Count;
				}
			}
		}

		// ******************************************************************
		public bool IsReadOnly
		{
			get { return false; }
		}

		// ******************************************************************
		public virtual bool Remove(T item)
		{
			bool isRemoved;

			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					isRemoved = _list.Remove(item);
				}

				if (isRemoved)
				{
					NotifyPropertyChanged(() => Count);
					NotifyPropertyChanged(() => "Item[]");
					NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				}
			}
			else
			{
				isRemoved = (bool)Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Func<T, bool>(Remove), item);
			}

			return isRemoved;
		}

		//// ******************************************************************
		public virtual int Add(object value)
		{
			int index;
			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					_list.Add((T)value);
					index = this.Count - 1;
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index));
			}
			else
			{
				index = (int)Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Func<object, int>(Add), value);
			}

			return index;
		}

		// ******************************************************************
		public bool Contains(object value)
		{
			lock (SyncRoot)
			{
				return _list.Contains((T)value);
			}
		}

		// ******************************************************************
		public int IndexOf(object value)
		{
			lock (SyncRoot)
			{
				return _list.IndexOf((T)value);
			}
		}

		// ******************************************************************
		public virtual void Insert(int index, object value)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					_list.Insert(index, (T)value);
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => Insert(index, value)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		public bool IsFixedSize
		{
			get { return false; }
		}

		// ******************************************************************
		public virtual void Remove(object value)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				lock (SyncRoot)
				{
					_list.Remove((T)value);
				}

				NotifyPropertyChanged(() => Count);
				NotifyPropertyChanged(() => "Item[]");
				NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(DispatchPriorityCall, new Action(() => Remove(value)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		object IList.this[int index]
		{
			get
			{
				lock (SyncRoot)
				{
					return _list[index];
				}
			}
			set
			{
				if (Application.Current.Dispatcher.CheckAccess())
				{
					T oldValue;

					lock (SyncRoot)
					{
						oldValue = _list[index];
						_list[index] = (T)value;
					}

					NotifyPropertyChanged(() => Count);
					NotifyPropertyChanged(() => "Item[]");
					NotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldValue));
				}
				else
				{
					Application.Current.Dispatcher.Invoke(new Action(() => this[index] = (T)value), DispatchPriorityCall);
				}
			}
		}

		// ******************************************************************
		public void CopyTo(Array array, int index)
		{
			lock (SyncRoot)
			{
				if (array.Rank != 1)
				{
					throw new ArgumentException("Arg_RankMultiDimNotSupported");
				}
				if (array.GetLowerBound(0) != 0)
				{
					throw new ArgumentException("Arg_NonZeroLowerBound");
				}
				if (index < 0)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				if ((array.Length - index) < this.Count)
				{
					throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
				}
				T[] localArray = array as T[];
				if (localArray != null)
				{
					_list.CopyTo(localArray, index);
				}
				else
				{
					Type elementType = array.GetType().GetElementType();
					Type c = typeof(T);
					if (!elementType.IsAssignableFrom(c) && !c.IsAssignableFrom(elementType))
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}
					object[] objArray = array as object[];
					if (objArray == null)
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}
					int count = _list.Count;
					try
					{
						for (int i = 0; i < count; i++)
						{
							objArray[index++] = _list[i];
						}
					}
					catch (ArrayTypeMismatchException)
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}
				}

			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void AddAsync(T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				Add(item);
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => Add(item)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void AddRangeAsync(IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				AddAsync(item);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void InsertAsync(int index, T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				Insert(index, item);
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => Insert(index, item)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void RemoveAsync(T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				Remove(item);
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => Remove(item)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void RemoveAtAsync(int index)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				RemoveAt(index);
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => RemoveAt(index)), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void ModifyAtAsync(int index, T item)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				this[index] = item;
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => this[index] = item), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void ClearAsync()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				Clear();
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => Clear()), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		// The name could have been have "Safe" instead of "Async" because its not really async all the time but I
		// wanted to hilight the fact that it could be async to the UI thread
		public void ClearNotifyOnceAsync()
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				ClearNotifyOnce();
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => ClearNotifyOnce()), DispatchPriorityCall);
			}
		}

		// ******************************************************************
		public bool IsSynchronized
		{
			get { return true; }
		}

		// ******************************************************************
		public object SyncRoot
		{
			get { return this; }
		}

		// ******************************************************************
		// ******************************************************************
		// ******************************************************************
		public class ObservableCollectionMtIteratorBlocking<T2> : IEnumerator<T2>, IDisposable
		{
			// ******************************************************************
			private readonly ObservableCollectionMt<T2> _obsCol;
			private readonly IEnumerator<T2> _enum;
			public ObservableCollectionMtIteratorBlocking(ObservableCollectionMt<T2> obsCol)
			{
				_obsCol = obsCol;
				Monitor.Enter(_obsCol.SyncRoot);
				_enum = _obsCol._list.GetEnumerator();
			}

			// ******************************************************************
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			// ******************************************************************
			~ObservableCollectionMtIteratorBlocking	()
			{
				Dispose(false);
			}

			// ******************************************************************
			private bool _alreadyDisposed = false;
			protected void Dispose(bool isDisposing)
			{
				if (_alreadyDisposed)
					return;

				if (isDisposing)
				{
					_enum.Dispose();
					Monitor.Exit(_obsCol.SyncRoot);
				}

				_alreadyDisposed = true;
			}
			
			// ******************************************************************
			public T2 Current
			{
				get { return _enum.Current; }
			}

			// ******************************************************************
			object System.Collections.IEnumerator.Current
			{
				get { return _enum.Current; }
			}

			// ******************************************************************
			public bool MoveNext()
			{
				return _enum.MoveNext();
			}

			// ******************************************************************
			public void Reset()
			{
				_enum.Reset();
			}

			// ******************************************************************

		}
	}
}
