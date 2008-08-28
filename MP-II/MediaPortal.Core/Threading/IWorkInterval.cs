#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

#region Usings

using System;
using System.Text;
using System.Collections.Generic;

#endregion

namespace MediaPortal.Core.Threading
{
  public interface IWorkInterval
  {
    /// <summary>
    /// Work to perform at a specified interval
    /// </summary>
    IWork Work { get; }

    /// <summary>
    /// Interval to perform work in
    /// Note: Interval can never be lower than the ThreadPool's thread idle timeout
    /// </summary>
    TimeSpan WorkInterval { get; }

    /// <summary>
    /// Last time the work interval was started
    /// (used to determine when to run the schedule again; updated by the ThreadPool)
    /// </summary>
    DateTime LastRun { get; set; }

    /// <summary>
    /// Indicator whether or not the work interval is being run currently.
    /// (used to avoid running it concurrently if workload time exceeds the given interval)
    /// Make sure to set it to false again after Work has been performed.
    /// </summary>
    bool Running { get; set; }

    /// <summary>
    /// Method that gets called when the ThreadPool is stopped
    /// </summary>
    void OnThreadPoolStopped();
    void ResetWorkState();
  }
}
