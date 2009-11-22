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

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Interface for markup extensions, which are able to bind to a target property.
  /// The markup extension will be able to evaluate a source value to be assigned to its
  /// target property later.
  /// </summary>
  public interface IBinding : IInitializable
  {
    /// <summary>
    /// Sets the target data descriptor for this binding.
    /// </summary>
    /// <param name="dd">Descriptor specifying the target property for this binding.
    /// This data descriptor will support target operations.</param>
    void SetTargetDataDescriptor(IDataDescriptor dd);

    /// <summary>
    /// Activates the binding. This will make the binding listen to changes of its source
    /// property values and bind to its target property specified by the
    /// <see cref="SetTargetDataDescriptor(IDataDescriptor)"/> method as soon as possible.
    /// </summary>
    void Activate();

    /// <summary>
    /// Will copy this binding instance and set the target of the new binding instance
    /// to the specified data descriptor.
    /// </summary>
    /// <param name="descriptor">Data descriptor to target the copied binding to.</param>
    /// <returns>New binding instance which has the same function as this binding, but will
    /// bind to the specified <paramref name="descriptor"/>.</returns>
    IBinding CopyAndRetarget(IDataDescriptor descriptor);
  }
}
