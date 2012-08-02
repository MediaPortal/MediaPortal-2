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
using System.ComponentModel;
using System.Windows.Controls;
using MP2_PluginWizard.ViewModel;

namespace MP2_PluginWizard.View
{
  public class WizardPage : UserControl, INotifyPropertyChanged
  {
    #region protected fields
    private bool _enableNextButton = true;

    #endregion

    #region Public properties
    public PluginDataViewModel ViewModel
    {
      get { return (PluginDataViewModel)DataContext; }
    }
    
    public bool EnableNextButton
    {
      get { return _enableNextButton; }
      set { SetProperty(ref _enableNextButton, value, "EnableNextButton"); }
    }
    

    #endregion
          
    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, string propertyName)
    {
      if (!EqualityComparer<T>.Default.Equals(field, value))
      {
        field = value;
        var handler = PropertyChanged;
        if (handler != null)
        {
          handler(this, new PropertyChangedEventArgs(propertyName));
        }
      }
    }

    #endregion

    #region Implementation of OnSwitchToEvent
    public delegate void OnSwitchedToEventHandler(object sender);

    public event OnSwitchedToEventHandler SwitchedTo;

    public void OnSwitchedTo()
    {
      var handler = SwitchedTo;
      if (handler != null) handler(this);
    }
    #endregion
  }
}
