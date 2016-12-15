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
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// <see cref="PriorityBindingConverter"/> will use the first valid binding that can be evaluated. 
  /// It requires <see cref="MultiBindingExtension.AllowEmptyBinding"/> set to <c>true</c>, otherwise the markup extension
  /// won't evaluate other bindings.
  /// </summary>
  public class PriorityBindingConverter : IMultiValueConverter
  {
    public bool Convert (IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      foreach (IDataDescriptor dataDescriptor in values)
      {
        if (dataDescriptor !=  null)
        {
          result = dataDescriptor.Value;
          return true;
        }
      }
      result = false;
      return false;
    }
  }
}
