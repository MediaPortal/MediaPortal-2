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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.ScreenManagement;
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
      result = default(T);
      if (source is Screen)
      {
        result = source;
        return true;
      }
      object res;
      if (Registration.CopyMpfObject(source, out res))
      {
        result = (T) res;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it.
    /// This method will create a new object graph starting at the given object.
    /// </summary>
    /// <param name="o">Object to be copied. This object may implement the
    /// interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <param name="identities">Specifies a map of identities, which should
    /// not go through the deep copy process. If one of the keys in this map
    /// is requested to be copied, the mapped value will be returned rather than
    /// trying to copy the key.</param>
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
    /// The specified <paramref name="fixedObject"/> object is one object in the
    /// graph (the parent of <paramref name="o"/>, for example), which
    /// should remain the same object in the copy.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling
    /// <see cref="MpfCopyManager.DeepCopyWithIdentities{T}"/> with an identity map containing
    /// the specified <paramref name="fixedObject"/>.
    /// </remarks>
    /// <param name="o">Object to be copied. This object may implement the
    /// interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <param name="fixedObject">Object which should not be copied. This object
    /// will remain the same in the copy.</param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static T DeepCopyWithFixedObject<T>(T o, object fixedObject)
    {
      Dictionary<object, object> identities = new Dictionary<object, object>();
      identities.Add(fixedObject, fixedObject);
      return DeepCopyWithIdentities(o, identities);
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling
    /// <see cref="MpfCopyManager.DeepCopyWithIdentities{T}"/> with an empty identity map.
    /// </remarks>
    /// <param name="o">Object to be copied. This object may implement the
    /// interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static new T DeepCopy<T>(T o)
    {
      IDictionary<object, object> identities = new Dictionary<object, object>();
      return DeepCopyWithIdentities(o, identities);
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> while cutting the structure at
    /// the logical parent of the object if it is a <see cref="DependencyObject"/>.
    /// The logical parent will be <c>null</c> for the copied object.
    /// </summary>
    /// <remarks>
    /// This method is a convenience method for calling
    /// <see cref="MpfCopyManager.DeepCopyWithIdentities{T}"/> with an identity map only
    /// containing the logical parent of <paramref name="o"/>, mapped to a <c>null</c> value,
    ///  if it is a <see cref="DependencyObject"/>.
    /// </remarks>
    /// <param name="o">Object to be copied.</param>
    /// <returns>Deep copy of the specified object <paramref name="o"/>.</returns>
    public static T DeepCopyCutLP<T>(T o)
    {
      IDictionary<object, object> identities = new Dictionary<object, object>();
      DependencyObject depObj = o as DependencyObject;
      if (depObj != null)
      {
        DependencyObject lp = depObj.LogicalParent;
        if (lp != null)
          identities[lp] = null;
      }
      return DeepCopyWithIdentities(o, identities);
    }
  }
}
