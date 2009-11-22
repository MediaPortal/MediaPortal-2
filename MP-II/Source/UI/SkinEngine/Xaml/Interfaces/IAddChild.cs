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
  /// Marks a visual element to be able to have children and to not expose
  /// a content property to add them to.
  /// </summary>
  /// <remarks>
  /// The standard way for visual elements to expose their ability to have a content
  /// is, they implement the interface <see cref="IContentEnabled"/>, and the parser
  /// automatically assigns the content to their so defined content property.
  /// In the case that the children collection should not be exposed to the outside world
  /// (via a content property), or that the class of the children collection does not
  /// expose a default constructor (which is necessary to type-convert to it), the
  /// visual element can implement this interface.
  /// </remarks>
  public interface IAddChild<T>
  {
    /// <summary>
    /// Adds a child element to this visual element.
    /// </summary>
    /// <param name="o">Child instanciated by the parser. The type of <paramref name="o"/>
    /// is the type the parser created for the corresponding XAML element in the XAML file.</param>
    void AddChild(T o);
  }
}
