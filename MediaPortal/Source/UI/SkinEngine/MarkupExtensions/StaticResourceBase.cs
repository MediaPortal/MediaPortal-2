#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Base class for MPF static resource lookup markup extensions
  /// </summary>
  public class StaticResourceBase
  {
    protected object FindResourceInParserContext(string resourceKey, IParserContext context)
    {
      object result = null;
      // Step up the parser's context stack to find the resource.
      // The logical tree is not yet defined at the load time of the
      // XAML file. This is the reason we have to step up the parser's context
      // stack. We will have to simulate the process of finding a resource
      // which is normally done by <see cref="FindResource(string)"/>.
      // The parser's context stack maintains a dictionary of current keyed
      // elements for each stack level because the according resource
      // dictionaries are not built yet.
      foreach (ElementContextInfo current in context.ContextStack)
      {
        if (current.ContainsKey(resourceKey))
          result = current.GetKeyedElement(resourceKey);
        else if (current.Instance is UIElement &&
            ((UIElement) current.Instance).Resources.ContainsKey(resourceKey))
          // Don't call UIElement.FindResource here, because the logical tree
          // may be not set up yet.
          result = ((UIElement) current.Instance).Resources[resourceKey];
        else if (current.Instance is ResourceDictionary)
        {
          ResourceDictionary rd = (ResourceDictionary) current.Instance;
          if (rd.ContainsKey(resourceKey))
            result = rd[resourceKey];
        }
      }
      if (result == null)
        return null;
      IEnumerable<IBinding> deferredBindings; // Don't execute bindings in copy
      // We do a copy of the result to avoid later problems when the property where the result is assigned to is copied.
      // If we don't cut the result's logical parent, a deep copy of the here assigned property would still reference
      // the static resource's logical parent, which would copy an unnecessary big tree.
      // And we cannot simply clean the logical parent of the here found resource because we must not change it.
      // So we must do a copy where we cut the logical parent.
      return MpfCopyManager.DeepCopyCutLP(result, out deferredBindings);
    }

    protected object FindResourceInTheme(string resourceKey)
    {
      object result = SkinContext.SkinResources.FindStyleResource(resourceKey);
      if (result == null)
        return null;
      IEnumerable<IBinding> deferredBindings; // Don't execute bindings in copy
      // See comment about the copying in method FindResourceInParserContext()
      return MpfCopyManager.DeepCopyCutLP(result, out deferredBindings);
    }
  }
}
