#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *	Copyright (C) 2007-2009 Team MediaPortal
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
using System.Threading;
using System.Collections.Generic;

#endregion

namespace MediaPortal.Core.Services.Threading
{
  public class ThreadPoolStartInfo
  {
    #region Public member variables

    /// <summary>
    /// Name used by the threadpool (default "ThreadPool")
    /// </summary>
    public string Name = "MediaPortal ThreadPool";

    /// <summary>
    /// Minimum number of threads in the threadpool (default 1)
    /// </summary>
    public int MinimumThreads = 1;

    /// <summary>
    /// Maximum number of threads in the threadpool (default 25)
    /// </summary>
    public int MaximumThreads = 25;

    /// <summary>
    /// Timeout (in miliseconds) for idle threads (default 20 seconds).
    /// If thread count is above MinimumThreads, threads quit when being idle this long
    /// </summary>
    public int ThreadIdleTimeout = 20000;

    /// <summary>
    /// Indicates whether the pool waits with initialization until first work is being
    /// received (true) or whether to initialize the pool upon pool creation (false).
    /// </summary>
    public bool DelayedInit = true;

    /// <summary>
    /// Default thread priority for threads in the threadpool
    /// </summary>
    public ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;

    #endregion

    #region Constructors

    public ThreadPoolStartInfo() { }
    public ThreadPoolStartInfo(string name)
    {
      this.Name = name;
    }
    public ThreadPoolStartInfo(int minThreads)
    {
      this.MinimumThreads = minThreads;
      if (MaximumThreads < MinimumThreads)
        MaximumThreads = MinimumThreads;
    }
    public ThreadPoolStartInfo(string name, int minThreads)
      : this(name)
    {
      this.MinimumThreads = minThreads;
    }
    public ThreadPoolStartInfo(int minThreads, int maxThreads)
      : this(minThreads)
    {
      this.MaximumThreads = maxThreads;
    }
    public ThreadPoolStartInfo(string name, int minThreads, int maxThreads)
      : this(name, minThreads)
    {
      this.MaximumThreads = maxThreads;
    }
    public ThreadPoolStartInfo(int minThreads, int maxThreads, int idleTimeout)
      : this(minThreads, maxThreads)
    {
      this.ThreadIdleTimeout = idleTimeout;
    }
    public ThreadPoolStartInfo(string name, int minThreads, int maxThreads, int idleTimeout)
      : this(name, minThreads, maxThreads)
    {
      this.ThreadIdleTimeout = idleTimeout;
    }
    public ThreadPoolStartInfo(int minThreads, int maxThreads, int idleTimeout, bool delayedInit)
      : this(minThreads, maxThreads, idleTimeout)
    {
      this.DelayedInit = delayedInit;
    }
    public ThreadPoolStartInfo(string name, int minThreads, int maxThreads, int idleTimeout, bool delayedInit)
      : this(name, minThreads, maxThreads, idleTimeout)
    {
      this.DelayedInit = delayedInit;
    }

    #endregion

    #region Public static Methods

    /// <summary>
    /// Validates the given ThreadPoolStartInfo
    /// Throws ArgumentOutOutrangeException for MinimumThreads, MaximumThreads and
    /// ThreadIdleTimeout if they are invalid.
    /// </summary>
    /// <param name="tpsi">ThreadPoolStartInfo to validate</param>
    public static void Validate(ThreadPoolStartInfo tpsi)
    {
      if (tpsi.MinimumThreads < 1)
        throw new ArgumentOutOfRangeException("MinimumThreads", tpsi.MinimumThreads, "cannot be less than one");
      if (tpsi.MaximumThreads < 1)
        throw new ArgumentOutOfRangeException("MaximumThreads", tpsi.MaximumThreads, "cannot be less than one");
      if (tpsi.MinimumThreads > tpsi.MaximumThreads)
        throw new ArgumentOutOfRangeException("MinimumThreads", tpsi.MinimumThreads, "must be less or equal to MaximumThreads");
      if (tpsi.ThreadIdleTimeout < 0)
        throw new ArgumentOutOfRangeException("ThreadIdleTimeout", tpsi.ThreadIdleTimeout, "cannot be less than zero");
    }

    #endregion
  }
}
