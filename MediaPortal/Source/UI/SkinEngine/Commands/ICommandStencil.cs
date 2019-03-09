#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.UI.SkinEngine.Commands
{
  /// <summary>
  /// Describes a "stencil" for a command to be executed with actual parameters, to fulfill a
  /// special short-time job.
  /// The actual parameters to execute this command stencil have to be provided at the time this
  /// command should be executed.
  /// </summary>
  /// <remarks>
  /// Like <see cref="IExecutableCommand"/>, this interface describes a very general, partial command,
  /// which is not further specified here.
  /// The concrete application, which uses this interface, may specify constraints to the implemented
  /// class more in detail.
  /// </remarks>
  public interface ICommandStencil
  {
    /// <summary>
    /// Executes this command with the specified <paramref name="parameters"/>.
    /// </summary>
    void Execute(IEnumerable<object> parameters);
  }
}
