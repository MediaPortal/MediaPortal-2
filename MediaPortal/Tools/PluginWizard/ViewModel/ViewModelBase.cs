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

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MP2_PluginWizard.ViewModel
{
  public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
  {
    #region DisplayName

    /// <summary>
    /// Returns the user-friendly name of this object.
    /// Child classes can set this property to a new value,
    /// or override it to determine the value on-demand.
    /// </summary>
    public virtual string DisplayName { get; protected set; }

    #endregion // DisplayName

    #region INotifyPropertyChanged
    /// <summary>
    /// Raised when a public property of this object is set.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, string propertyName)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return;

      field = value;
      OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(propertyName));
      }
    }


    #endregion

    #region IDisposable

    /// <summary>
    /// Invoked when this object is being removed from the application
    /// and will be subject to garbage collection.
    /// </summary>
    public void Dispose()
    {
      OnDispose();
    }

    /// <summary>
    /// Child classes can override this method to perform 
    /// clean-up logic, such as removing event handlers.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

#if DEBUG
    /// <summary>
    /// Useful for ensuring that ViewModel objects are properly garbage collected.
    /// </summary>
    ~ViewModelBase()
    {
      var msg = string.Format("{0} ({1}) ({2}) Finalized", GetType().Name, DisplayName, GetHashCode());
      System.Diagnostics.Debug.WriteLine(msg);
    }
#endif
    #endregion

  }
}
