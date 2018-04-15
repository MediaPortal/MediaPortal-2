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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.MpfElements
{                            
  /// <summary>
  /// CopyManager class for MPF elements. This copy manager provides the
  /// functionallity of the general <see cref="CopyManager"/>, and additionally
  /// copies all bindings on the objects to be copied.
  /// </summary>
  public class MpfCopyManager: CopyManager
  {
    protected static readonly IEnumerable<IBinding> EMPTY_BINDING_ENUMERATION = new List<IBinding>();

    /// <summary>
    /// Hooks into the copying process. This method will modify the copying
    /// behavior for special types of objects in the MPF.
    /// Some object types will not be copied but reused, for other types
    /// not implementing the <see cref="IDeepCopyable"/> interface, special
    /// copying code will be added.
    /// </summary>
    /// <param name="source">Source object to be copied.</param>
    /// <param name="result">Resulting object copied from
    /// the <paramref name="source"/> object.</param>
    protected override bool CopyHook<T>(T source, out T result)
    {
      if (source is Screen)
      {
        result = source;
        return true;
      }
      UIElement element = source as UIElement;
      result = default(T);
      if (element != null && element.ElementState == ElementState.Disposing)
        return true;

      object res;
      if (MPF.CopyMpfObject(source, out res))
      {
        result = (T) res;
        return true;
      }
      return false;
    }

    public IEnumerable<IBinding> GetDeferredBindings()
    {
      return _identities.Values.OfType<IBinding>();
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it. This method will create a new object graph starting at the given object.
    /// </summary>
    /// <param name="o">Object to be copied. This object may implement the interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <param name="identities">Specifies a map of identities, which should not go through the deep copy process. If one of the keys in this map
    /// is requested to be copied, the mapped value will be returned rather than trying to copy the key.</param>
    /// <returns>Deep copy of the specified object.</returns>
    public static T DeepCopyWithIdentities<T>(T o, IDictionary<object, object> identities)
    {
      MpfCopyManager cm = new MpfCopyManager();
      foreach (KeyValuePair<object, object> kvp in identities)
        cm.AddIdentity(kvp.Key, kvp.Value);
      T result = cm.GetCopy(o);
      cm.FinishCopy();
      return result;
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it.
    /// The specified <paramref name="fixedObject"/> object is one object in the graph (the parent of <paramref name="o"/>, for example), which
    /// should remain the same object in the copy.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling  <see cref="DeepCopyWithIdentities{T}(T,IDictionary{object,object})"/>
    /// with an identity map containing the specified <paramref name="fixedObject"/>.
    /// </remarks>
    /// <param name="o">Object to be copied. This object may implement the interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <param name="fixedObject">Object which should not be copied. This object will remain the same in the copy. If this object is <c>null</c>,
    /// this method will fallback to method <see cref="DeepCopy{T}(T)"/></param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static T DeepCopyWithFixedObject<T>(T o, object fixedObject)
    {
      if (fixedObject == null)
        return DeepCopy(o);
      Dictionary<object, object> identities = new Dictionary<object, object> {{fixedObject, fixedObject}};
      return DeepCopyWithIdentities(o, identities);
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling <see cref="DeepCopyWithIdentities{T}(T,IDictionary{object,object})"/>
    /// with an empty identity map.
    /// </remarks>
    /// <param name="o">Object to be copied. This object may implement the interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static new T DeepCopy<T>(T o)
    {
      IDictionary<object, object> identities = new Dictionary<object, object>();
      return DeepCopyWithIdentities(o, identities);
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> while cutting the structure at the logical and visual parents of the object
    /// if it is a <see cref="DependencyObject"/> or <see cref="Visual"/>.
    /// The logical and visual parents will be <c>null</c> for the copied object.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling <see cref="DeepCopyWithIdentities{T}(T,IDictionary{object,object})"/>
    /// with an identity map only containing the logical and visual parents of <paramref name="o"/>, each mapped to a <c>null</c> value,
    /// if <paramref name="o"/> is a <see cref="DependencyObject"/> or <see cref="Visual"/>.
    /// </remarks>
    /// <param name="o">Object to be copied.</param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static T DeepCopyCutLVPs<T>(T o)
    {
      return DeepCopySetLVPs(o, null, null);
    }

    public static T DeepCopySetLVPs<T>(T o, DependencyObject newLP, Visual newVP)
    {
      IDictionary<object, object> identities = new Dictionary<object, object>();
      DependencyObject depObj = o as DependencyObject;
      if (depObj != null)
      {
        DependencyObject lp = depObj.LogicalParent;
        if (lp != null)
          identities[lp] = newLP;
      }
      Visual v = o as Visual;
      if (v != null)
      {
        Visual vp = v.VisualParent;
        if (vp != null)
          identities[vp] = newVP;
      }
      return DeepCopyWithIdentities(o, identities);
    }
  }
}
