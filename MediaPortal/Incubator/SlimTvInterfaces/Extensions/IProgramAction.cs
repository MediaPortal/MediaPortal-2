#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Extensions
{
  public delegate bool ProgramActionDelegate(IProgram program);

  /// <summary>
  /// Extension interface to add actions for <see cref="IProgram"/>s. Plugins can implement this interface and register the class in
  /// <c>plugin.xml</c> <see cref="SlimTvExtensionBuilder.SLIMTVEXTENSIONPATH"/> path.
  /// </summary>
  public interface IProgramAction
  {
    /// <summary>
    /// Checks if this action is available for the given <paramref name="program"/>.
    /// </summary>
    /// <param name="program">Program</param>
    /// <returns><c>true</c> if available</returns>
    bool IsAvailable(IProgram program);

    /// <summary>
    /// Gets the action to be executed for the selected program (<seealso cref="ProgramActionDelegate"/>).
    /// </summary>
    ProgramActionDelegate ProgramAction { get; }
  }
}
