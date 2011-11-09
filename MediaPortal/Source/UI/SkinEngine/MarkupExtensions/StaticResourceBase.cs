#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Base class for MPF static resource lookup markup extensions
  /// </summary>
  public class StaticResourceBase : ISkinEngineManagedResource
  {
    protected object FindResourceInTheme(string resourceKey, IParserContext context)
    {
      object result = SkinContext.SkinResources.FindStyleResource(resourceKey);
      if (result == null)
        return null;
      IEnumerable<IBinding> deferredBindings; // Don't execute bindings in copy
      // See comment about the copying in method ResourceDictionary.FindResourceInParserContext()
      result = MpfCopyManager.DeepCopyCutLP(result, out deferredBindings);
      UIElement.RememberPendingBindingsInParserContext(deferredBindings, context);
      return result;
    }
  }
}
