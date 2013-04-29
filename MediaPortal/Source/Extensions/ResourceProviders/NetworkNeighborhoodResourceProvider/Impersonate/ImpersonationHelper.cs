#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Impersonate
{
  /// <summary>
  /// Helper class to logon as a new user. This is be required to access network resources when running the main program as LocalSystem.
  /// </summary>
  public class ImpersonationHelper
  {
    #region Constants and imports

    private static readonly WellKnownSidType[] KNOWN_SID_TYPES = new[] { WellKnownSidType.NetworkServiceSid, WellKnownSidType.LocalServiceSid, WellKnownSidType.LocalSystemSid };

    // Group type enum
    public enum SecurityImpersonationLevel
    {
      SecurityAnonymous = 0,
      SecurityIdentification = 1,
      SecurityImpersonation = 2,
      SecurityDelegation = 3
    }

    public enum LogonType
    {
      /// <summary>
      /// This logon type is intended for users who will be interactively using the computer, such as a user being logged on  
      /// by a terminal server, remote shell, or similar process.
      /// This logon type has the additional expense of caching logon information for disconnected operations;
      /// therefore, it is inappropriate for some client/server applications,
      /// such as a mail server.
      /// </summary>
      LOGON32_LOGON_INTERACTIVE = 2,

      /// <summary>
      /// This logon type is intended for high performance servers to authenticate plaintext passwords.

      /// The LogonUser function does not cache credentials for this logon type.
      /// </summary>
      LOGON32_LOGON_NETWORK = 3,

      /// <summary>
      /// This logon type is intended for batch servers, where processes may be executing on behalf of a user without
      /// their direct intervention. This type is also for higher performance servers that process many plaintext
      /// authentication attempts at a time, such as mail or Web servers.
      /// The LogonUser function does not cache credentials for this logon type.
      /// </summary>
      LOGON32_LOGON_BATCH = 4,

      /// <summary>
      /// Indicates a service-type logon. The account provided must have the service privilege enabled.
      /// </summary>
      LOGON32_LOGON_SERVICE = 5,

      /// <summary>
      /// This logon type is for GINA DLLs that log on users who will be interactively using the computer.
      /// This logon type can generate a unique audit record that shows when the workstation was unlocked.
      /// </summary>
      LOGON32_LOGON_UNLOCK = 7,

      /// <summary>
      /// This logon type preserves the name and password in the authentication package, which allows the server to make
      /// connections to other network servers while impersonating the client. A server can accept plaintext credentials
      /// from a client, call LogonUser, verify that the user can access the system across the network, and still
      /// communicate with other servers.
      /// NOTE: Windows NT:  This value is not supported.
      /// </summary>
      LOGON32_LOGON_NETWORK_CLEARTEXT = 8,

      /// <summary>
      /// This logon type allows the caller to clone its current token and specify new credentials for outbound connections.
      /// The new logon session has the same local identifier but uses different credentials for other network connections.
      /// NOTE: This logon type is supported only by the LOGON32_PROVIDER_WINNT50 logon provider.
      /// NOTE: Windows NT:  This value is not supported.
      /// </summary>
      LOGON32_LOGON_NEW_CREDENTIALS = 9,
    }

    public enum LogonProvider
    {
      /// <summary>
      /// Use the standard logon provider for the system.
      /// The default security provider is negotiate, unless you pass NULL for the domain name and the user name
      /// is not in UPN format. In this case, the default provider is NTLM.
      /// NOTE: Windows 2000/NT:   The default security provider is NTLM.
      /// </summary>
      LOGON32_PROVIDER_DEFAULT = 0,
      LOGON32_PROVIDER_WINNT35 = 1,
      LOGON32_PROVIDER_WINNT40 = 2,
      LOGON32_PROVIDER_WINNT50 = 3
    }

    private static int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    private static int STANDARD_RIGHTS_READ = 0x00020000;
    private static int TOKEN_ASSIGN_PRIMARY = 0x0001;
    private static int TOKEN_DUPLICATE = 0x0002;
    private static int TOKEN_IMPERSONATE = 0x0004;
    private static int TOKEN_QUERY = 0x0008;
    private static int TOKEN_QUERY_SOURCE = 0x0010;
    private static int TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private static int TOKEN_ADJUST_GROUPS = 0x0040;
    private static int TOKEN_ADJUST_DEFAULT = 0x0080;
    private static int TOKEN_ADJUST_SESSIONID = 0x0100;
    private static int TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
    private static int TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
        TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
        TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
        TOKEN_ADJUST_SESSIONID);

    // Obtains user token
    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool LogonUser(string pszUsername, string pszDomain, string pszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, ref IntPtr phToken);

    // Closes open handles returned by LogonUser
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal extern static bool CloseHandle(IntPtr handle);

    // Creates duplicate token handle.
    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal extern static bool DuplicateToken(IntPtr existingTokenHandle, SecurityImpersonationLevel impersonationLevel, ref IntPtr duplicateTokenHandle);

    [DllImport("advapi32")]
    internal static extern bool OpenProcessToken(
        IntPtr processHandle, // handle to process
        int desiredAccess, // desired access to process
        ref IntPtr tokenHandle // handle to open access token
    );

    [DllImport("advapi32.DLL")]
    public static extern bool ImpersonateLoggedOnUser(IntPtr hToken); // handle to token for logged-on user

    #endregion

    /// <summary>
    /// Checks if the caller needs to impersonate (again).
    /// </summary>
    /// <returns><c>true</c> if impersonate is required.</returns>
    public static bool RequiresImpersonate(WindowsIdentity requestedIdentity)
    {
      if (requestedIdentity == null)
        return true;

      WindowsIdentity current = WindowsIdentity.GetCurrent();
      if (current == null || current.User == null) // Can never happen, just to avoid R# warning.
        return true;

      return 
        current.User != requestedIdentity.User || /* Current user is not the requested one. We need to compare SIDs here, instances are not equal */
        KNOWN_SID_TYPES.Any(wellKnownSidType => current.User.IsWellKnown(wellKnownSidType)) /* User is any of well known SIDs, those have no network access */;
    }

    /// <summary>
    /// Attempts to impersonate an user based on an running process. If successful, it returns a WindowsImpersonationContext of the new users identity.
    /// </summary>
    /// <param name="processName">Process name to take user account from (without .exe).</param>
    /// <param name="user">Return the user identity.</param>
    /// <returns>WindowsImpersonationContext if successful.</returns>
    public static WindowsImpersonationContext ImpersonateByProcess(string processName, out WindowsIdentity user)
    {
      // Try to find a process for given processName. There can be multiple processes, we will take the first one.
      // Attention: when working on a RemoteDesktop/Terminal session, there can be multiple user logged in. The result of finding the first process
      // might be not deterministic.
      user = null;
      Process process = Process.GetProcessesByName(processName).FirstOrDefault();
      if (process == null)
        return null;

      IntPtr pExistingTokenHandle = IntPtr.Zero;
      try
      {
        if (!OpenProcessToken(process.Handle, TOKEN_QUERY | TOKEN_IMPERSONATE | TOKEN_DUPLICATE, ref pExistingTokenHandle))
          return null;

        user = new WindowsIdentity(pExistingTokenHandle);
        return user.Impersonate();
      }
      finally
      {
        // Close handle.
        if (pExistingTokenHandle != IntPtr.Zero)
          CloseHandle(pExistingTokenHandle);
      }
    }

    /// <summary>
    /// Attempts to impersonate an user. If successful, it returns a WindowsImpersonationContext of the new users identity.
    /// </summary>
    /// <param name="sUsername">Username you want to impersonate.</param>
    /// <param name="sPassword">User's password to logon with.</param>
    /// <param name="user">Return the user identity.</param>
    /// <param name="sDomain">Logon domain, defaults to local system.</param>
    /// <returns>WindowsImpersonationContext if successful.</returns>
    public static WindowsImpersonationContext ImpersonateUser(string sUsername, string sPassword, out WindowsIdentity user, string sDomain = null)
    {
      // Initialize tokens
      IntPtr pExistingTokenHandle = IntPtr.Zero;
      IntPtr pDuplicateTokenHandle = IntPtr.Zero;

      // If domain name was blank, assume local machine
      if (string.IsNullOrWhiteSpace(sDomain))
        sDomain = Environment.MachineName;

      user = null;

      try
      {
        // Get handle to token
        bool bImpersonated = LogonUser(sUsername, sDomain, sPassword, LogonType.LOGON32_LOGON_INTERACTIVE, LogonProvider.LOGON32_PROVIDER_DEFAULT, ref pExistingTokenHandle);

        // Did impersonation fail?
        if (!bImpersonated)
        {
          int nErrorCode = Marshal.GetLastWin32Error();
          ServiceRegistration.Get<ILogger>().Warn("LogonUser() for username '{0}' failed with error code: {1} ", sUsername, nErrorCode);
          return null;
        }

        // Get identity before impersonation.
        // ServiceRegistration.Get<ILogger>().Debug("Before impersonation: {0}", WindowsIdentity.GetCurrent().Name);

        bool bRetVal = DuplicateToken(pExistingTokenHandle, SecurityImpersonationLevel.SecurityImpersonation, ref pDuplicateTokenHandle);

        // Did DuplicateToken fail?
        if (!bRetVal)
        {
          int nErrorCode = Marshal.GetLastWin32Error();
          ServiceRegistration.Get<ILogger>().Warn("DuplicateToken() failed with error code: {0} ", nErrorCode);
          return null;
        }
        else
        {
          // Create new identity using new primary token.
          user = new WindowsIdentity(pDuplicateTokenHandle);
          WindowsImpersonationContext impersonatedUser = user.Impersonate();

          // Check the identity after impersonation.
          // ServiceRegistration.Get<ILogger>().Debug("After impersonation: {0}", WindowsIdentity.GetCurrent().Name);
          return impersonatedUser;
        }
      }
      finally
      {
        // Close handle(s)
        if (pExistingTokenHandle != IntPtr.Zero)
          CloseHandle(pExistingTokenHandle);
        if (pDuplicateTokenHandle != IntPtr.Zero)
          CloseHandle(pDuplicateTokenHandle);
      }
    }
  }
}
