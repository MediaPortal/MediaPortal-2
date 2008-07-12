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

using Presentation.SkinEngine.General;

namespace Presentation.SkinEngine.XamlParser.Interfaces
{
  /// <summary>
  /// This interfaces will be implemented by binding markup extensions.
  /// </summary>
  public interface IBinding
  {
    /// <summary>
    /// Prepares the binding. This is the last chance for the binding to have access
    /// to the parser context. Bindings will initialize their fields with all needed
    /// context variables for the later call to their <see cref="Bind(IPropertyDescriptor)"/>
    /// method.
    /// </summary>
    /// <param name="context">Current parser context.</param>
    /// <param name="dd">Descriptor specifying the property this binding should bind to.</param>
    void Prepare(IParserContext context, IDataDescriptor dd);

    /// <summary>
    /// Binds this instance to the property specified in the
    /// <see cref="Prepare(IParserContext,IDataDescriptor)"/> method.
    /// </summary>
    /// <returns>The return value will be <c>true</c>, if the binding could be
    /// completed. In this case, there is no need to call this method again;
    /// every change in all involved objects is tracked automatically by this class.
    /// The return value will be <c>false</c>, if this binding could not bind yet, maybe
    /// because some structures are not set up yet. In this case, this method
    /// should be called again later, when all needed structures are set up.</returns>
    bool Bind();
  }
}
