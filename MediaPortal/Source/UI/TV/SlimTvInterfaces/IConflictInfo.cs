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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{

  public interface IConflictInfoAsync
  {
    /// <summary>
    /// Gets a list of currently known conflicts.
    /// </summary>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one conflict could be found.
    /// <see cref="AsyncResult{T}.Result"/> conflicts.
    /// </returns>
    Task<AsyncResult<IList<IConflict>>> GetConflictsAsync();
  }
}
