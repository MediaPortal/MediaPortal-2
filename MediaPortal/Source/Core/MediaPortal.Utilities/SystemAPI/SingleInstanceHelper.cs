using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace MediaPortal.Utilities.SystemAPI
{
  public class SingleInstanceHelper
  {
    /// <summary>
    /// Windows message id to bring MP2-Client to front.
    /// </summary>
    public static readonly uint SHOW_MP2_CLIENT_MESSAGE = WindowsAPI.RegisterWindowMessage("SHOW_MP2_CLIENT_MESSAGE");

    /// <summary>
    /// Check if an application is running or not
    /// </summary>
    /// <returns>returns true if already running</returns>
    public static bool IsAlreadyRunning(string MUTEX_ID, out Mutex mutex)
    {
      // Allow only one instance
      mutex = new Mutex(false, MUTEX_ID);

      var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                  MutexRights.FullControl, AccessControlType.Allow);
      var securitySettings = new MutexSecurity();
      securitySettings.AddAccessRule(allowEveryoneRule);
      mutex.SetAccessControl(securitySettings);
      
      bool hasHandle = false;
      try
      {
        // Check if we can start the application
        hasHandle = mutex.WaitOne(500, false);
      }
      catch (AbandonedMutexException)
      {
        // The mutex was abandoned in another process, it will still get aquired
        hasHandle = true;
      }
      return !hasHandle;
    }
  }
}
