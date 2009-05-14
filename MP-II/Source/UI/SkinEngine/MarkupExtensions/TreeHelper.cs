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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.MpfElements;

namespace MediaPortal.SkinEngine.MarkupExtensions
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

    public static bool FindAncestorOfType(DependencyObject obj, out DependencyObject parent, Type ancestorType)
    {
      parent = null;
      DependencyObject next = obj;
      while (next != null)
      {
        parent = next;
        if (ancestorType == null ||
            ancestorType.IsAssignableFrom(parent.GetType()))
          return true;
        if (!FindParent_VT(parent, out next) && !FindParent_LT(parent, out next))
          return false;
      }
      return false;
    }
  }
}