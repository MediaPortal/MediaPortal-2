#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Marks a visual's element class to be content-enabled.
  /// This means instances of it do have a canonical content property which will
  /// accept child elements defined in the XAML structure.
  /// </summary>
  public interface IContentEnabled
  {
    /// <summary>
    /// Tries to find the property which is the content property for this class.
    /// Normal content enabled classes will return their content property instance,
    /// wrapped in a <see cref="DependencyPropertyDataDescriptor"/> in the
    /// <paramref name="dd"/> parameter, if they have a property instance, else they would
    /// return a <see cref="SimplePropertyDataDescriptor"/> or
    /// <see cref="FieldDataDescriptor"/> in this parameter.
    /// Anyway, it is possible to return a property on a completely different instance,
    /// which will be realized by returning a property descriptor to that other
    /// property.
    /// </summary>
    /// <param name="dd">Descriptor of the property or member used as content property for this
    /// instance.</param>
    /// <returns><c>true</c>, if the search for the class which displays the content
    /// is successful, else <c>false</c>.</returns>
    bool FindContentProperty(out IDataDescriptor dd);
  }
}
