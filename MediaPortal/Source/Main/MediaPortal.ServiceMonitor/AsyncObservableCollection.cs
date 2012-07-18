#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace MediaPortal.ServiceMonitor
{
  /// <summary>
  /// AsyncObservableCollection allows to modify the content of an ObservableCollection on a separate thread
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class AsyncObservableCollection<T> : ObservableCollection<T>
  {
    private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

    public AsyncObservableCollection()
    {
    }

    public AsyncObservableCollection(IEnumerable<T> list)
      : base(list)
    {
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
      if (SynchronizationContext.Current == _synchronizationContext)
      {
        // Execute the CollectionChanged event on the current thread
        RaiseCollectionChanged(e);
      }
      else
      {
        // Post the CollectionChanged event on the creator thread
        _synchronizationContext.Post(RaiseCollectionChanged, e);
      }
    }

    private void RaiseCollectionChanged(object param)
    {
      // We are in the creator thread, call the base implementation directly
      base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      if (SynchronizationContext.Current == _synchronizationContext)
      {
        // Execute the PropertyChanged event on the current thread
        RaisePropertyChanged(e);
      }
      else
      {
        // Post the PropertyChanged event on the creator thread
        _synchronizationContext.Post(RaisePropertyChanged, e);
      }
    }

    private void RaisePropertyChanged(object param)
    {
      // We are in the creator thread, call the base implementation directly
      base.OnPropertyChanged((PropertyChangedEventArgs)param);
    }

  }
}
