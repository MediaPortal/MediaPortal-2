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
using System.Windows.Markup;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// <see cref="MPFExtensionBase"/> provides the MPF specific base class for MarkupExtensions. It provides a "bridge" between WPF and MPF based classes and interfaces.
  /// </summary>
  public abstract class MPFExtensionBase : MarkupExtension
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      object result = null;
// ReSharper disable SuspiciousTypeConversion.Global
      var eme = this as IEvaluableMarkupExtension;
// ReSharper restore SuspiciousTypeConversion.Global
      if (eme != null)
        eme.Evaluate(out result);
      return result;
    }
  }
}
