#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.TaskScheduler;

namespace MediaPortal.Common.Services.TaskScheduler
{
  /// <summary>
  /// <see cref="ITaskWakeUpTimerFactory"/> that hands out <see cref="WindowsTaskWaitableTimer"/>s,
  /// i.e. timers whose expiry can resume the machine. Windows hosts register this before
  /// <c>ApplicationCore.RegisterCoreServices()</c> so that scheduled TV recordings still fire on a
  /// suspended machine; without it the core's portable factory is used and recordings only run while
  /// the machine is awake.
  /// </summary>
  public sealed class WindowsTaskWakeUpTimerFactory : ITaskWakeUpTimerFactory
  {
    /// <summary>
    /// Installs this factory as <see cref="ITaskWakeUpTimerFactory"/>, replacing the portable one the
    /// core registered. Called by the Windows hosts next to their other satellite registrations.
    /// </summary>
    /// <remarks>
    /// Static, like <c>GdiPlusImageTransformer.Register()</c> and the other two satellites the hosts
    /// call. The factory itself is stateless, but it is registered as an instance because
    /// <see cref="ServiceRegistration"/> has no way to hand out a type. Order against
    /// <c>ApplicationCore.RegisterCoreServices()</c> does not matter: the task scheduler asks for the
    /// factory when it first arms a timer, not while the core is starting up.
    /// </remarks>
    public static void Register()
    {
      ServiceRegistration.Set<ITaskWakeUpTimerFactory>(new WindowsTaskWakeUpTimerFactory());
    }

    public ITaskWakeUpTimer CreateWakeUpTimer()
    {
      return new WindowsTaskWaitableTimer();
    }
  }
}
