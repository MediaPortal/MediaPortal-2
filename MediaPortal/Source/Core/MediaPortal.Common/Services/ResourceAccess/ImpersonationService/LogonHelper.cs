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
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Security;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Helper class to encapsulate the native LogonUser API function
  /// </summary>
  public class LogonHelper
  {
    #region Enums

    /// <summary>
    /// Logon type that should be performed by a call to <see cref="LogonUser"/>
    /// </summary>
    /// <remarks>
    /// For an explanation of the different logon types see here:
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
    /// </remarks>
    internal enum LogonType
    {
      Interactive = 2,
      Network = 3,
      Batch = 4,
      Service = 5,
      Unlock = 7,
      NetworkCleartext = 8,
      NewCredentials = 9,
    }

    /// <summary>
    /// Logon provider to be used by <see cref="LogonUser"/>
    /// </summary>
    /// <remarks>
    /// For an explanation of the different logon types see here:
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
    /// </remarks>
    internal enum LogonProvider
    {
      Default = 0,
      WinNt35 = 1,
      WinNt40 = 2,
      WinNt50 = 3
    }

    #endregion

    #region Native methods

    /// <summary>
    /// Native LogonUser method
    /// </summary>
    /// <param name="lpszUsername">UserName used to log on.</param>
    /// <param name="lpszDomain">Domain used to log on.</param>
    /// <param name="lpszPassword">Pointer to a clear text password in unmanaged memory.</param>
    /// <param name="dwLogonType"><see cref="LogonType"/> used to log on.</param>
    /// <param name="dwLogonProvider"><see cref="LogonProvider"/> used to log on.</param>
    /// <param name="phToken">Access Token of the logged on user, if the call was successful.</param>
    /// <returns><c>true</c> if the call was successful; otherwise <c>false</c></returns>
    /// <remarks>
    /// For details on this function see here: https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184(v=vs.85).aspx
    /// </remarks>
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(String lpszUsername, String lpszDomain, IntPtr lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out SafeTokenHandle phToken);

    #endregion

    #region Private fields

    /// <summary>
    /// <see cref="ILogger"/> used for debug logging
    /// </summary>
    private readonly ILogger _debugLogger;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="LogonHelper"/>
    /// </summary>
    /// <param name="debugLogger"><see cref="ILogger"/> used for debug logging</param>
    public LogonHelper(ILogger debugLogger)
    {
      _debugLogger = debugLogger;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Tries to logon a user represented by <paramref name="credential"/> via the native <see cref="LogonUser"/> function
    /// </summary>
    /// <param name="credential"><see cref="NetworkCredential"/> used to log on.</param>
    /// <param name="type"><see cref="LogonType"/> used to log on.</param>
    /// <param name="provider"><see cref="LogonProvider"/> used to log on.</param>
    /// <param name="id"><see cref="WindowsIdentity"/> of the logged on user if the call was successful; otherwise <c>null</c></param>
    /// <returns><c>true</c> if the call was successful; otherwise <c>false</c></returns>
    internal bool TryLogon(NetworkCredential credential, LogonType type, LogonProvider provider, out WindowsIdentity id)
    {
      id = null;
      
      // Log parameters to debug log
      _debugLogger.Info("LogonHelper: Trying to logon:");
      _debugLogger.Info("  User:          '{0}'", credential.UserName);
      _debugLogger.Info("  Domain:        '{0}'", credential.Domain);
      _debugLogger.Info("  LogonType:     '{0}'", type);
      _debugLogger.Info("  LogonProvider: '{0}'", provider);

      // Parameter Checks
      if (!TryCheckUserNameAndDomain(credential))
        return false;
      CheckTypeAndProvider(ref type, ref provider);

      // Prepare for call to LogonUser API function
      var passwordPtr = IntPtr.Zero;
      bool success;
      SafeTokenHandle safeTokenHandle = null;
      try
      {
        // Copy password in cleartext into unmanaged memory
        passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(credential.SecurePassword);
        success = LogonUser(credential.UserName, credential.Domain, passwordPtr, type, provider, out safeTokenHandle);
      }
      catch (Exception e)
      {
        if (safeTokenHandle != null)
          safeTokenHandle.Dispose();
        _debugLogger.Error("LogonHelper: Exception while calling LogonUser:", e);
        return false;
      }
      finally
      {
        // Zero-out the cleartext password in unmanaged memory and free the memory
        Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
      }

      using (safeTokenHandle)
      {
        // Log error if LogonUser was not successful
        if (!success)
        {
          var error = Marshal.GetLastWin32Error();
          _debugLogger.Error("LogonHelper: LogonUser was not successful (ErrorCode:{0}, Message:{1})", error, new Win32Exception(error).Message);
          return false;
        }
        
        // Store Token in WindowsIdentity if LogonUser was successful
        _debugLogger.Info("LogonHelper: User logged on successfully");
        try
        {
          id = new WindowsIdentity(safeTokenHandle.DangerousGetHandle());
        }
        catch (Exception e)
        {
          _debugLogger.Error("LogonHelper: Error creating WindowsIdentity:", e);
          return false;
        }
        _debugLogger.Info("LogonHelper: WindowsIdentity successfully created");
        return true;
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Checks if <paramref name="type"/> and <paramref name="provider"/> work together and corrects not working combinations.
    /// </summary>
    /// <param name="type"><see cref="LogonType"/> to check</param>
    /// <param name="provider"><see cref="LogonProvider"/> to check</param>
    private void CheckTypeAndProvider(ref LogonType type, ref LogonProvider provider)
    {
      // LogonType.NewCredentials is only supported by LogonProvider.WinNt50
      if (type == LogonType.NewCredentials && provider != LogonProvider.WinNt50)
      {
        provider = LogonProvider.WinNt50;
        _debugLogger.Warn("LogonHelper: LogonType.NewCredentials is only supported by LogonProvider.WinNt50; corrected as follows:");
        _debugLogger.Warn("  LogonType:     '{0}'", type);
        _debugLogger.Warn("  LogonProvider: '{0}'", provider);
      }
    }

    /// <summary>
    /// Performs validity checks on <paramref name="credential"/>'s UserName and Password and adjusts them, if necessary
    /// </summary>
    /// <param name="credential"><see cref="NetworkCredential"/> object to check</param>
    /// <returns><c>false</c> if UserName and/or Domain are invalid; otherwise <c>true</c></returns>
    /// <remarks>
    /// The UserName can be provided
    ///   - as a plain UserName (<example>John</example>),
    ///   - in UPN format (<example>John@doe.com</example>), or
    ///   - in Down-Level format (<example>doe\John</example>).
    /// For details see here: https://msdn.microsoft.com/de-de/library/windows/desktop/aa380525(v=vs.85).aspx
    /// This method recognizes the UPN format and the Down-Level format and separates UserName and Domain, if possible.
    /// If no Domain was provided at all, the local MachineName is used as Domain.
    /// </remarks>
    private bool TryCheckUserNameAndDomain(NetworkCredential credential)
    {
      var modified = false;

      // Check for UserName in UPN format
      var firstIndexOfAt = credential.UserName.IndexOf('@');
      var lastIndexOfAt = credential.UserName.LastIndexOf('@');
      if (firstIndexOfAt != lastIndexOfAt)
      {
        _debugLogger.Error("LogonHelper: The UserName contains more than one '@' and is therefore invalid. Cannot logon.");
        return false;
      }
      if (firstIndexOfAt >= 0)
      {
        if (!string.IsNullOrEmpty(credential.Domain))
        {
          _debugLogger.Error("LogonHelper: Ambiguous DomainName - the UserName is in UPN format and an additional DomainName has been provided. Cannot logon.");
          return false;
        }
        var nameAndDomain = credential.UserName.Split('@');
        if (string.IsNullOrEmpty(nameAndDomain[0]))
        {
          _debugLogger.Error("LogonHelper: UserName provided in UPN format but the UserName part is missing. Cannot logon.");
          return false;
        }
        if (string.IsNullOrEmpty(nameAndDomain[1]))
        {
          _debugLogger.Error("LogonHelper: UserName provided in UPN format but the Domain part is missing. Cannot logon.");
          return false;
        }
        _debugLogger.Info("LogonHelper: UserName provided in UPN format. Values adjusted as follows:");
        credential.UserName = nameAndDomain[0];
        credential.Domain = nameAndDomain[1];
        modified = true;
      }

      // Check for Down-Level UserName
      var fistIndexOfBackSlash = credential.UserName.IndexOf('\\');
      var lastIndexOfBackSalsh = credential.UserName.LastIndexOf('\\');
      if (fistIndexOfBackSlash != lastIndexOfBackSalsh)
      {
        _debugLogger.Error("LogonHelper: The UserName contains more than one '\\' and is therefore invalid. Cannot logon.");
        return false;
      }
      if (fistIndexOfBackSlash >= 0)
      {
        if (!string.IsNullOrEmpty(credential.Domain))
        {
          _debugLogger.Error("LogonHelper: Ambiguous DomainName - the UserName is in Down-Level format and an additional DomainName has been provided. Cannot logon.");
          return false;
        }
        var nameAndDomain = credential.UserName.Split('\\');
        if (string.IsNullOrEmpty(nameAndDomain[1]))
        {
          _debugLogger.Error("LogonHelper: UserName provided in Down-Level format but the UserName part is missing. Cannot logon.");
          return false;
        }
        if (string.IsNullOrEmpty(nameAndDomain[0]))
        {
          _debugLogger.Error("LogonHelper: UserName provided in Down-Level format but the Domain part is missing. Cannot logon.");
          return false;
        }
        _debugLogger.Info("LogonHelper: UserName provided in Down-Level format. Values adjusted as follows:");
        credential.UserName = nameAndDomain[1];
        credential.Domain = nameAndDomain[0];
        modified = true;
      }

      // Check for missing Domain
      if (string.IsNullOrEmpty(credential.Domain))
      {
        _debugLogger.Info("LogonHelper: No Domain provided. Using local MachineName. Values adjusted as follows:");
        credential.Domain = Environment.MachineName;
        modified = true;
      }

      if (modified)
      {
        _debugLogger.Info("  User:          '{0}'", credential.UserName);
        _debugLogger.Info("  Domain:        '{0}'", credential.Domain);
      }
      return true;
    }

    #endregion
  }
}
