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

namespace MediaPortal.UI.SkinEngine.Commands
{
  /// <summary>
  /// Describes a single command which can be executed to fulfill a special short-time job.
  /// Instances of this interface need to implement the <see cref="Execute()"/> method
  /// to be able to run their command code.
  /// </summary>
  /// <remarks>
  /// This interface describes a very general command, which is not further specified here.
  /// The concrete application, which uses this interface, may specify constraints
  /// to the implemented class more in detail.
  /// In contrast to <see cref="ICommandStencil"/>, instances of this interface have to
  /// be self-contained. This means, the command described by this class should be able
  /// to be called without setting any more parameters.
  /// </remarks>
  public interface IExecutableCommand
  {
    /// <summary>
    /// Executes this command.
    /// </summary>
    void Execute();
  }
}
