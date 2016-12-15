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

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Marker interface for objects that typically are used as resources in <see cref="ResourceDictionary"/> instances.
  /// Objects of this interface are unmodifiable and copying/disposing of them will be optimized in that way that if they
  /// have an owner, they are not copied at all and they are only disposed by their owner, for example an enclosing
  /// <see cref="ResourceDictionary"/>. If they don't have an owner, they will be copied and disposed normally as any other object.
  /// </summary>
  /// <remarks>
  /// This interface provides a similar function as the WPF <c>Freezable</c> class, but a bit more lightweight. We can only
  /// differentiate between modifiable (=normal) and unmodifiable (corresponds to a WPF "frozen" object) objects.
  /// </remarks>
  public interface IUnmodifiableResource : ISkinEngineManagedObject, IBindingContainer
  {
    /// <summary>
    /// Gets the owner object of this resource. Implementors should simply provide a trivial implementation with
    /// a variable to set and get the owner.
    /// </summary>
    object Owner { get; set; }
  }
}