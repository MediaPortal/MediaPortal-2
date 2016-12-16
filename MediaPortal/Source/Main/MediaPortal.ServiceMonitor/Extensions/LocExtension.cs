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
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Data;

namespace MediaPortal.ServiceMonitor.Extensions
{
  /// <summary>
  /// Enables localization of properties.
  /// </summary> 
  public class LocExtension : MarkupExtension
  {
    private string _key;

    public LocExtension(string key)
    {
      _key = key;
    }

    [ConstructorArgument("Key")]
    public string Key
    {
      get { return _key; }
      set { _key = value; }
    }

    public static bool IsDesignMode
    {
      get
      {
        return (bool)
          DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement))
              .Metadata.DefaultValue;
      }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      if (IsDesignMode)
        return Key;

      var binding = new Binding("Value") {Source = new LocalizationData(_key)};
      return binding.ProvideValue(serviceProvider);
    }

  }

}
