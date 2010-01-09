#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Weak event delegate implementation for arbitrary events which prevents memory leaks.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides another weak event delegate implementation. But most other weak event delegate
  /// solutions use a technique where a given method delegate is assigned to a given "normal" .net event using
  /// a decorator instance which is added to that event and which itself references the target method delegate weakly.
  /// That solution allows the referenced object to be garbage collected but still leaks the memory which is needed for
  /// the weak event delegate decorator itself.
  /// </para>
  /// <para>
  /// This implementation produces absolutely no memory leaks. To achieve that, it is necessary to implement an own
  /// little garbage collector method which periodically checks for orphaned weak event delegate instances and which
  /// removes them if found.
  /// </para>
  /// </remarks>
  public class WeakEventMulticastDelegate
  {
    protected const int NUM_PASS = 20;
    protected const int GC_INTERVAL = 5000;

    /// <summary>
    /// Represents a single delegate which weakly references a method in a target object.
    /// </summary>
    protected struct WeakEventDelegateData
    {
      public WeakReference TargetRef;
      public MethodInfo Method;
    }

    protected IList<WeakEventDelegateData> _eventHandlers = null;
    protected static object _syncObj = new object();
    protected static IList<WeakReference> _delegates = new List<WeakReference>();
    protected static Thread _garbageCollectorThread;

    static WeakEventMulticastDelegate()
    {
      _garbageCollectorThread = new Thread(DoBackgroundWork)
        {
            Name = typeof(WeakEventMulticastDelegate).Name + " garbage collector thread",
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };
      _garbageCollectorThread.Start();
    }

    public WeakEventMulticastDelegate()
    {
      lock (_syncObj)
        _delegates.Add(new WeakReference(this));
    }

    public void Fire(object[] parameters)
    {
      IList<WeakEventDelegateData> ehs;
      lock (_syncObj)
      {
        if (_eventHandlers == null || _eventHandlers.Count == 0)
          return;
        ehs = new List<WeakEventDelegateData>(_eventHandlers);
      }
      foreach (WeakEventDelegateData wehd in ehs)
      {
        object target = wehd.TargetRef.Target;
        if (target != null)
          wehd.Method.Invoke(target, parameters);
      }
    }

    /// <summary>
    /// Attaches an event handler.
    /// </summary>
    /// <remarks>
    /// The given <paramref name="handler"/> will be referenced weakly by this class, i.e. attaching an event handler
    /// will not prevent the target object of the handler from being garbage collected.
    /// </remarks>
    /// <param name="handler">The handler.</param>
    public void Attach(Delegate handler)
    {
      lock (_syncObj)
      {
        if (_eventHandlers == null)
          _eventHandlers = new List<WeakEventDelegateData>();
        _eventHandlers.Add(new WeakEventDelegateData {TargetRef=new WeakReference(handler.Target), Method=handler.Method});
      }
    }

    /// <summary>
    /// Detaches the specified event handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public void Detach(Delegate handler)
    {
      lock (_syncObj)
      {
        if (_eventHandlers == null)
          return;
        foreach (WeakEventDelegateData wehd in _eventHandlers)
          if (wehd.TargetRef.Target == handler.Target && wehd.Method == handler.Method)
          {
            _eventHandlers.Remove(wehd);
            break;
          }
      }
    }

    public void ClearAttachedEvents()
    {
      lock (_syncObj)
        _eventHandlers = null;
    }

    /// <summary>
    /// Cleans up all event handler registrations. Will be executed automatically by method <see cref="GarbageCollect"/> for
    /// each weak delegate.
    /// </summary>
    protected void GarbageCollectHandlers()
    {
      if (_eventHandlers == null)
        return;
      lock (_syncObj)
      {
        if (_eventHandlers == null) // Must be checked again while lock is held
          return;
        bool needCleanup = false;
        foreach (WeakEventDelegateData wehd in _eventHandlers)
          if (wehd.TargetRef.Target == null)
          {
            needCleanup = true;
            break;
          }
        if (needCleanup)
        {
          IList<WeakEventDelegateData> oldHandlers = _eventHandlers;
          _eventHandlers = new List<WeakEventDelegateData>(oldHandlers.Count);
          foreach (WeakEventDelegateData wehd in oldHandlers)
            if (wehd.TargetRef.Target != null)
              _eventHandlers.Add(wehd);
        }
      }
    }

    /// <summary>
    /// Cleans up all weak event delegates. Must be done on a regular basis.
    /// </summary>
    protected static void GarbageCollect()
    {
      int idx = 0;
      lock (_syncObj)
      {
        int max;
        bool needCleanup = false;
        while (idx < (max = _delegates.Count))
        {
          max = Math.Min(max, idx + NUM_PASS);
          for (; idx < max; idx++)
          {
            WeakReference wp = _delegates[idx];
            WeakEventMulticastDelegate wemd = (WeakEventMulticastDelegate) wp.Target;
            if (wemd == null)
            {
              needCleanup = true;
              continue;
            }
            wemd.GarbageCollectHandlers();
          }
          Monitor.PulseAll(_syncObj); // Interrupt the work to let other threads work
        }
        if (needCleanup)
        {
          ICollection<WeakReference> oldDelegates = _delegates;
          _delegates = new List<WeakReference>(oldDelegates.Count);
          foreach (WeakReference p in oldDelegates)
            if (p.Target != null)
              _delegates.Add(p);
        }
      }
    }

    protected static void DoBackgroundWork()
    {
      while (true)
      {
        GarbageCollect();
        Thread.Sleep(GC_INTERVAL);
      }
    }
  }
}
