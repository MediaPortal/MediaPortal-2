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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MP2_PluginWizard.Model;
using MP2_PluginWizard.ViewModel;

namespace MP2_PluginWizard.View
{
  /// <summary>
  /// Interaction logic for WizardWindow.xaml
  /// </summary>
  public partial class PluginWindow : INotifyPropertyChanged
  {
    #region Private/protected fields
   
    #endregion

    #region Ctor/Dtor
    public PluginWindow()
    {
      InitializeComponent();
    }

    #endregion

    #region Public properties
    
    #endregion

    #region Public Methods
    
    #endregion

    #region Private Methods
        
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
  }
}
