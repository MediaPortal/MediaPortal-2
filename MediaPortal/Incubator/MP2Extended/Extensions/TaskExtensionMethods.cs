﻿#region Copyright (C) 2012-2013 MPExtended

// Copyright (C) 2012-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.MP2Extended.Extensions
{
  public static class Task
  {
    public static void LogOnException(this System.Threading.Tasks.Task task)
    {
      task.ContinueWith(t => { ServiceRegistration.Get<ILogger>().Error("Exception thrown in task started with LogOnException()", t.Exception); },
        TaskContinuationOptions.OnlyOnFaulted);
    }
  }
}