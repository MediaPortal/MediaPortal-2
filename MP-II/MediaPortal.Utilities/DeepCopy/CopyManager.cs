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
using System.Collections.Generic;
using System.Reflection;

namespace MediaPortal.Utilities.DeepCopy
{
  /// <summary>
  /// Implementing class for interface <see cref="ICopyManager"/>. For specification
  /// see the interface documentation.
  /// </summary>
  public class CopyManager : ICopyManager
  {
    protected IDictionary<object, object> _identities = new Dictionary<object, object>();
    protected LinkedList<IDeepCopyable> _toBeCompleted = new LinkedList<IDeepCopyable>();

    /// <summary>
    /// Returns a map of already known source objects mapped to their copied
    /// counterparts. This map will grow during the copying process.
    /// </summary>
    public IDictionary<object, object> Identities
    {
      get { return _identities; }
    }

    /// <summary>
    /// Adds an object reference which should be copied to a fixed value.
    /// </summary>
    /// <typeparam name="T">Type of the object reference.</typeparam>
    /// <param name="source">Source object.</param>
    /// <param name="result">Target object.</param>
    public void AddIdentity<T>(T source, T result)
    {
      _identities.Add(source, result);
    }

    /// <summary>
    /// May be overridden by subclasses to prevent the copying process or
    /// to execute another copying process for special instances.
    /// </summary>
    /// <param name="source">Source object to be copied.</param>
    /// <param name="result">Resulting object copied from
    /// the <paramref name="source"/> object.</param>
    protected virtual bool CopyHook<T>(T source, out T result)
    {
      result = default(T);
      return false;
    }

    /// <summary>
    /// Creates a copy for the specified <paramref name="source"/> instance.
    /// Subclasses can override this method to create copied instances
    /// depending on their own needs.
    /// </summary>
    protected virtual T CreateCopyForInstance<T>(T source)
    {
      ConstructorInfo ci = source.GetType().GetConstructor(new Type[] {});
      if (ci == null)
        throw new ArgumentException(string.Format("Type '{0}' doesn't implement a standard constructor", typeof(T).Name));
      return (T) ci.Invoke(null);
    }

    /// <summary>
    /// Performs the deep copy of the specified <paramref name="source"/>
    /// object to the specified <paramref name="target"/> object.
    /// </summary>
    /// <remarks>
    /// This method can be overridden in subclasses to step in the deep copy
    /// behavior.
    /// </remarks>
    protected virtual void DoDeepCopy<T>(T source, T target) where T: IDeepCopyable
    {
      target.DeepCopy(source, this);
    }

    /// <summary>
    /// Finishes the copying process.
    /// </summary>
    public void FinishCopy()
    {
      while (_toBeCompleted.Count > 0)
      {
        IDeepCopyable source = _toBeCompleted.First.Value;
        _toBeCompleted.RemoveFirst();
        IDeepCopyable target = (IDeepCopyable) _identities[source];
        if (target != null)
          // If we wanted to avoid recursive calls for the same object, we would
          // have to mark the target object as to be currently processed. We would
          // also have to check this marking at the beginning of this method.
          DoDeepCopy(source, target);
      }
      if (CopyCompleted != null)
        CopyCompleted(this);
      CopyCompleted = null;
    }

    /// <summary>
    /// Creates a deep copy of object <paramref name="o"/> and returns it.
    /// This method will create a new object graph starting at the given object.
    /// </summary>
    /// <param name="o">Object to be copied. This object may implement the
    /// interface <see cref="IDeepCopyable"/>, or may not.</param>
    /// <returns>Deep copy of the specified object.</returns>
    public static T DeepCopy<T>(T o)
    {
      CopyManager cm = new CopyManager();
      T result = cm.GetCopy(o);
      cm.FinishCopy();
      return result;
    }

    public T GetCopy<T>(T source)
    {
      if (source == null)
        return default(T);
      if (_identities.ContainsKey(source))
        return (T)_identities[source];
      T result;
      if (CopyHook(source, out result))
        return result;
      if (source is IDeepCopyable)
      {
        result = CreateCopyForInstance(source);
        // The copy process for the new instance will be completed later
        _toBeCompleted.AddLast((IDeepCopyable)source);
      }
      else
        // No copying of instances which do not implement IDeepCopyable
        result = source;
      AddIdentity(source, result);
      return result;
    }

    public event CopyCompletedDlgt CopyCompleted;
  }
}
