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
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MP2BootstrapperApp.MarkupExtensions
{
  /// <summary>
  /// Base class for a MarkupExtension that can update its provided value.
  /// </summary>
  public abstract class UpdatableMarkupExtension : MarkupExtension, INotifyPropertyChanged
  {
    public sealed override object ProvideValue(IServiceProvider serviceProvider)
    {
      IProvideValueTarget provdeValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
      // If this MarkupExtension is used in a template then the target object will initially be null.
      // Return the MarkupExtension so it will be reevaluated when its included in the visual tree.
      if (provdeValueTarget == null || !(provdeValueTarget.TargetObject is DependencyObject target) || !(provdeValueTarget.TargetProperty is DependencyProperty targetProperty))
        return this;
      // Binding handles updating the target property
      BindingBase binding = ProvideValueOverride(target);
      return binding.ProvideValue(serviceProvider);
    }

    /// <summary>
    /// Should be overriden in derived classes to return a Binding that will be applied to the target object and property.
    /// </summary>
    /// <param name="target">The target DependencyObject that will be bound to</param>
    /// <returns>A binding to be applied to the target property.</returns>
    protected abstract BindingBase ProvideValueOverride(DependencyObject target);

    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
