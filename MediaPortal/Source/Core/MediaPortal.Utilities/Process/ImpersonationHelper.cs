#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using MediaPortal.Utilities.SystemAPI;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Utilities.Process
{
  /// <summary>
  /// Helper class to logon as a new user. This is be required to access network resources when running the main program as LocalSystem.
  /// </summary>
  [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
  public class ImpersonationHelper
  {
    #region ImpersonationContext

    /// <summary>
    /// Helper class to store <see cref="Identity"/> and automatically impersonate.
    /// </summary>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public class ImpersonationContext : IDisposable
    {
      private WindowsIdentity _identity;

      [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
      public WindowsIdentity Identity
      {
        get { return _identity; }
        set
        {
          _identity = value;
          try
          {
            Context = value.Impersonate();
          }
          catch { }
        }
      }

      [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
      public WindowsImpersonationContext Context { get; private set; }

      [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
      public void Dispose()
      {
        WindowsImpersonationContext wic = Context;
        if (wic != null)
          wic.Dispose();
      }
    }

    #endregion

    #region Constants and imports

    private static readonly WellKnownSidType[] KNOWN_SID_TYPES = new[] { WellKnownSidType.NetworkServiceSid, WellKnownSidType.LocalServiceSid, WellKnownSidType.LocalSystemSid };

    #endregion

    /// <summary>
    /// Checks if the caller needs to impersonate (again).
    /// </summary>
    /// <returns><c>true</c> if impersonate is required.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool RequiresImpersonate(WindowsIdentity requestedIdentity)
    {
      if (requestedIdentity == null)
        return true;

      WindowsIdentity current = WindowsIdentity.GetCurrent();
      if (current == null || current.User == null) // Can never happen, just to avoid R# warning.
        return true;

      return
        current.User != requestedIdentity.User || /* Current user is not the requested one. We need to compare SIDs here, instances are not equal */
        IsWellknownIdentity(current) /* User is any of well known SIDs, those have no network access */;
    }

    /// <summary>
    /// Indicates if the <see cref="WindowsIdentity.GetCurrent()"/> represents one of the <see cref="KNOWN_SID_TYPES"/>, which do not have network access.
    /// </summary>
    /// <returns><c>true</c> for a well known identity.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool IsWellknownIdentity()
    {
      return IsWellknownIdentity(WindowsIdentity.GetCurrent());
    }

    /// <summary>
    /// Indicates if the given <paramref name="identity"/> represents one of the <see cref="KNOWN_SID_TYPES"/>, which do not have network access.
    /// </summary>
    /// <param name="identity">Identity to check.</param>
    /// <returns><c>true</c> for a well known identity.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool IsWellknownIdentity(WindowsIdentity identity)
    {
      return KNOWN_SID_TYPES.Any(wellKnownSidType => identity.User != null && identity.User.IsWellKnown(wellKnownSidType));
    }

    /// <summary>
    /// Attempts to impersonate an user based on an running process. If successful, it returns a WindowsImpersonationContext of the new users identity.
    /// </summary>
    /// <param name="processName">Process name to take user account from (without .exe).</param>
    /// <returns>WindowsImpersonationContext if successful.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static ImpersonationContext ImpersonateByProcess(string processName)
    {
      IntPtr userToken;
      if (!GetTokenByProcess(processName, out userToken))
        return null;

      try
      {
        return new ImpersonationContext { Identity = new WindowsIdentity(userToken) };
      }
      finally
      {
        // Close handle.
        SafeCloseHandle(userToken);
      }
    }

    /// <summary>
    /// Attempts to impersonate an user. If successful, it returns a WindowsImpersonationContext of the new users identity.
    /// </summary>
    /// <param name="username">Username you want to impersonate.</param>
    /// <param name="password">User's password to logon with.</param>
    /// <param name="domain">Logon domain, defaults to local system.</param>
    /// <returns>WindowsImpersonationContext if successful.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static ImpersonationContext ImpersonateUser(string username, string password, string domain = null)
    {
      // Initialize tokens
      IntPtr userToken = IntPtr.Zero;

      try
      {
        if (!GetTokenByUser(username, password, domain, out userToken))
          return null;

        // Create new identity using new primary token.
        return new ImpersonationContext { Identity = new WindowsIdentity(userToken) };
      }
      finally
      {
        // Close handle(s)
        SafeCloseHandle(userToken);
      }
    }

    /// <summary>
    /// Tries to get an existing user token running <c>explorer.exe</c>. If <paramref name="duplicate"/> is set to <c>true</c>, the caller must call <see cref="NativeMethods.CloseHandle"/> 
    /// for the returned <paramref name="existingTokenHandle"/>  when it is no longer required.
    /// </summary>
    /// <param name="existingTokenHandle">Outputs an existing token.</param>
    /// <param name="duplicate"><c>true</c> to duplicate handle.</param>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool GetTokenByProcess(out IntPtr existingTokenHandle, bool duplicate = false)
    {
      return GetTokenByProcess("explorer", out existingTokenHandle, duplicate);
    }

    /// <summary>
    /// Tries to get an existing user token from the given <paramref name="processName"/>. If <paramref name="duplicate"/> is set to <c>true</c>, the caller must call <see cref="NativeMethods.CloseHandle"/> 
    /// for the returned <paramref name="existingTokenHandle"/>  when it is no longer required.
    /// </summary>
    /// <param name="processName">Process name to take user account from (without .exe).</param>
    /// <param name="existingTokenHandle">Outputs an existing token.</param>
    /// <param name="duplicate"><c>true</c> to duplicate handle.</param>
    /// <returns><c>true</c> if successful.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool GetTokenByProcess(string processName, out IntPtr existingTokenHandle, bool duplicate = false)
    {
      // Try to find a process for given processName. There can be multiple processes, we will take the first one.
      // Attention: when working on a RemoteDesktop/Terminal session, there can be multiple user logged in. The result of finding the first process
      // might be not deterministic.
      existingTokenHandle = IntPtr.Zero;
      System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessesByName(processName).FirstOrDefault();
      if (process == null)
        return false;

      try
      {
        if (!NativeMethods.OpenProcessToken(process.Handle, NativeMethods.TokenAccess.AssignPrimary | NativeMethods.TokenAccess.Duplicate | NativeMethods.TokenAccess.Query, ref existingTokenHandle))
          return false;

        IntPtr impersonationToken = existingTokenHandle;
        return !duplicate || CreatePrimaryToken(impersonationToken, out existingTokenHandle);
      }
      catch
      { }
      return false;
    }

    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    private static bool CreatePrimaryToken(IntPtr impersonationToken, out IntPtr primaryToken)
    {
      // Convert the impersonation token into Primary token
      NativeMethods.SecurityAttributes sa = new NativeMethods.SecurityAttributes();

      bool retVal = NativeMethods.DuplicateTokenEx(
        impersonationToken,
        NativeMethods.TokenAccess.AssignPrimary | NativeMethods.TokenAccess.Duplicate| NativeMethods.TokenAccess.Query,
        sa,
        NativeMethods.SecurityImpersonationLevel.Identification,
        NativeMethods.TokenType.Primary,
        out primaryToken);

      // Close the Token that was previously opened.
      NativeMethods.CloseHandle(impersonationToken);
      return retVal;
    }

    /// <summary>
    /// Tries to create a new user token based on given user credentials. Caller must call <see cref="NativeMethods.CloseHandle"/> for the returned <paramref name="duplicateTokenHandle"/>
    /// when it is no longer required.
    /// </summary>
    /// <param name="uername">User name.</param>
    /// <param name="password">Password.</param>
    /// <param name="domain">Domain name, <c>null</c> defaults to computer name.</param>
    /// <param name="duplicateTokenHandle">Outputs a duplicated token.</param>
    /// <returns><c>true</c> if successful.</returns>
    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    public static bool GetTokenByUser(string uername, string password, string domain, out IntPtr duplicateTokenHandle)
    {
      // Initialize tokens
      duplicateTokenHandle = IntPtr.Zero;
      IntPtr existingTokenHandle = IntPtr.Zero;

      // If domain name was blank, assume local machine
      if (string.IsNullOrWhiteSpace(domain))
        domain = Environment.MachineName;

      try
      {
        // Get handle to token
        if (!NativeMethods.LogonUser(uername, domain, password, NativeMethods.LogonType.Interactive, NativeMethods.LogonProvider.Default, out existingTokenHandle))
          return false;

        return NativeMethods.DuplicateToken(existingTokenHandle, NativeMethods.SecurityImpersonationLevel.Impersonation, ref duplicateTokenHandle);
      }
      finally
      {
        // Close handle(s)
        SafeCloseHandle(existingTokenHandle);
      }
    }

    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    internal static void SafeCloseHandle(IntPtr handle)
    {
      if (handle != IntPtr.Zero)
        NativeMethods.CloseHandle(handle);
    }

    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    internal static void SafeCloseHandle(ref IntPtr handle)
    {
      SafeCloseHandle(handle);
      handle = IntPtr.Zero;
    }

    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    internal static void SafeCloseHandle(SafeFileHandle handle)
    {
      if (handle != null && !handle.IsInvalid)
        handle.Close();
    }

    [Obsolete("The impersonation functionality has been moved completely into ImpersonationService. Use this class instead.")]
    internal static void SafeCloseHandle(ref SafeFileHandle handle)
    {
      SafeCloseHandle(handle);
      handle = null;
    }
  }
}
