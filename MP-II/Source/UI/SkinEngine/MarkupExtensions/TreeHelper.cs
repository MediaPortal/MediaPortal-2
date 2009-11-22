#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  public class TreeHelper
  {
    public static bool FindParent_VT(DependencyObject obj, out DependencyObject parent)
    {
      parent = null;
      Visual v = obj as Visual;
      if (v == null)
        return false;
      Property parentProperty = v.VisualParentProperty;
      parent = parentProperty.GetValue() as DependencyObject;
      return parent != null;
    }

    public static bool FindParent_LT(DependencyObject obj, out DependencyObject parent)
    {
      Property parentProperty = obj.LogicalParentProperty;
      parent = parentProperty.GetValue() as DependencyObject;
      return parent != null;
    }

    public static bool FindAncestorOfType(DependencyObject current, out DependencyObject ancestor, Type ancestorType)
    {
      ancestor = null;
      while (current != null)
      {
        if (ancestorType == null ||
            ancestorType.IsAssignableFrom(current.GetType()))
        {
          ancestor = current;
          return true;
        }
        DependencyObject c = current;
        if (!FindParent_VT(c, out current) && !FindParent_LT(c, out current))
          return false;
      }
      return false;
    }
  }
}