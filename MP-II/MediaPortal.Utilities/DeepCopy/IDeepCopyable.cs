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

namespace MediaPortal.Utilities.DeepCopy
{
  /// <summary>
  /// Marks an object to be able to deep-copy itself as a part of an object
  /// graph. The to-be-copied object graph may contain cycles.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every class implementing this interface must define a (no parameter) standard
  /// constructor.
  /// </para>
  /// <para>
  /// The copy procedure will proceed in two steps for every object <c>o</c> to
  /// be cloned:
  /// <list type="bullet">
  /// <item>The standard constructor of the type of o will be called.</item>
  /// <item>Later, (<see cref="IDeepCopyable.DeepCopy(IDeepCopyable,ICopyManager"/>)
  /// will be called on the new object (=target) with the source object.</item>
  /// </list>
  /// Between the execution of those two steps for an object, other objects of the
  /// to-be-copied object graph may be processed.
  /// </para>
  /// </remarks>
  public interface IDeepCopyable
  {
    /// <summary>
    /// Will copy all values and references of the specified <paramref name="source"/>
    /// object to this instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Purpose</b>
    /// After the standard constructor of the object to-be-copied was called,
    /// this method executes the second step of the two-step copying process.
    /// </para>
    /// <para>
    /// <b>Referenced objects</b>
    /// As we will perform a deep copy, referenced objects also have to be copied.
    /// This should be done by calling
    /// <see cref="ICopyManager.GetCopy(object)"/> for every referenced object.
    /// The <paramref name="copyManager"/> keeps track of every object already
    /// copied, so that the deep copied result object graph connects the objects
    /// corresponding to those connected in the source graph.
    /// </para>
    /// <para>
    /// <b>Where to implement/inheritance</b>
    /// In the inheritance hierarchy, for a given type implementing
    /// <see cref="IDeepCopyable"/> this method should be implemented at least
    /// in every supertype implementing non-transient fields.
    /// Implementations will look like this:
    /// <example>
    /// <code>
    /// void IDeepCopyable.DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    /// {
    ///   base.DeepCopy(source, copyManager);
    ///   _field1 = source._field1;
    ///   [...]
    ///   _reference1 = (Reference1Type) copyManager.GetCopy(source._reference1);
    ///   [...]
    /// }
    /// </code>
    /// </example>
    /// </para>
    /// </remarks>
    void DeepCopy(IDeepCopyable source, ICopyManager copyManager);
  }
}
