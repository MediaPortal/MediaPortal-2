#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.Controls
{
  /// <summary>
  /// Provides a way to apply styles based on custom logic.
  /// </summary>
  public class StyleSelector
  {
    /// <summary>
    /// When overridden in a derived class, returns a Style based on custom logic.
    /// </summary>
    /// <param name="item">The content.</param>
    /// <param name="container">The element to which the style will be applied.</param>
    /// <returns>
    /// Returns an application-specific style to apply; otherwise, <see langword="null" />.
    /// </returns>
    public virtual Style SelectStyle(object item, DependencyObject container)
    {
      return (Style)null;
    }
  }

  public class SeparatorStyleSelector : StyleSelector, ISkinEngineManagedObject, IDisposable
  {
    protected Style _itemContainerStyle = null;
    protected Style _separatorStyle = null;

    public Style ItemContainerStyle
    {
      get { return _itemContainerStyle; }
      set
      {
        MPF.TryCleanupAndDispose(_itemContainerStyle);
        // No need to set the LogicalParent at styles or data templates because they don't bind bindings
        _itemContainerStyle = MpfCopyManager.DeepCopyCutLVPs(value);
      }
    }

    public Style SeparatorStyle
    {
      get { return _separatorStyle; }
      set
      {
        MPF.TryCleanupAndDispose(_separatorStyle);
        // No need to set the LogicalParent at styles or data templates because they don't bind bindings
        _separatorStyle = MpfCopyManager.DeepCopyCutLVPs(value);
      }
    }

    public override Style SelectStyle(object item, DependencyObject container)
    {
      // Return a copy of the selected style
      return MpfCopyManager.DeepCopyCutLVPs(item is SeparatorListItem ?
         SeparatorStyle :
         ItemContainerStyle
        );
    }

    public void Dispose()
    {
      MPF.TryCleanupAndDispose(_itemContainerStyle);
      _itemContainerStyle = null;
      MPF.TryCleanupAndDispose(_separatorStyle);
      _separatorStyle = null;
    }
  }
}
