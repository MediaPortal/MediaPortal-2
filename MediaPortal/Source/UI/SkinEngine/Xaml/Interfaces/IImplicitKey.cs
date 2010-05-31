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

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Marks a visual's element class to have an implicit key. A resource
  /// with an implicit key may not have an explicit <c>x:Key</c> attribute
  /// when used in a
  /// <see cref="MediaPortal.UI.SkinEngine.MpfElements.Resources.ResourceDictionary"/>.
  /// </summary>
  public interface IImplicitKey
  {
    /// <summary>
    /// Returns the implicit key for this element.
    /// </summary>
    /// <returns>Implicit key for this instance.</returns>
    object GetImplicitKey();
  }
}
