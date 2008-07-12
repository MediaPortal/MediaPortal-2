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
  /// Interface for markup extensions, which are able to bind to a target property.
  /// The markup extension will be able to evaluate a source value to be assigned to its
  /// target property later.
  /// </summary>
  public interface IBinding
  {
    /// <summary>
    /// Prepares the binding. This is the last chance for the binding to have access
    /// to the parser context. Bindings will initialize their fields with all needed
    /// context variables for the later call to their <see cref="Activate"/>
    /// method.
    /// </summary>
    /// <param name="context">Current parser context.</param>
    /// <param name="dd">Descriptor specifying the target property for this binding.</param>
    void Prepare(IParserContext context, IDataDescriptor dd);

    /// <summary>
    /// Activates the binding. This will make the binding listen to changes of its source
    /// property values and maybe bind to its target property specified by the
    /// <see cref="Prepare(IParserContext,IDataDescriptor)"/> method.
    /// </summary>
    void Activate();
  }
}
