/*
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The credentials the server offers to the network's shares - the neighborhood resource provider's own
  /// settings, reached across the plugin wall the way the metadata endpoint reaches the extractors'.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The settings behind this endpoint belong to a plugin: the network neighborhood provider keeps a dictionary
  /// of resource paths and credentials as its settings class, and only it is named by the code that reads them
  /// at access time. This assembly does not reference that plugin and does not start to - it looks the settings
  /// class up among the loaded assemblies, exactly like <see cref="MetadataSettingsController"/> looks the
  /// extractors' classes up. The consequence is the same honest one: <b>a server without the provider answers
  /// "not installed"</b> rather than storing credentials nothing will ever offer to a share.
  /// </para>
  /// <para>
  /// <b>The password goes one way.</b> The provider stores it encrypted in its settings file, and this endpoint
  /// never reads it back - the answer names the user and the domain of each entry and says only that a password
  /// is on it. The reference's server-settings window showed the stored password in a plain text box on a PC
  /// and flagged even that; a television is a worse place for a secret than a configuration dialog was.
  /// </para>
  /// <para>
  /// The reference's own setup screen stored one credential, for the provider's root path, and carried a
  /// standing "to do: support several paths" note; the dictionary underneath always held more. This endpoint
  /// speaks that dictionary as what it is - a list keyed by path - and when the client sends no path it uses
  /// the provider's own root, which is what the reference always stored.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class NetworkCredentialsController : ControllerBase
  {
    private const string PROVIDER_TYPE =
        "MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NetworkNeighborhoodResourceProvider";
    private const string SETTINGS_TYPE =
        "MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings.NetworkNeighborhoodResourceProviderSettings";
    private const string CREDENTIAL_TYPE = "MediaPortal.Utilities.Xml.SerializableNetworkCredential";

    /// <summary>
    /// The dictionary as it was answered: whether the plugin that would use these credentials is even there,
    /// the path a pathless wish lands on, and the stored entries with everything about them except the password.
    /// </summary>
    public class NetworkCredentialsAnswer
    {
      public bool ProviderInstalled { get; set; }
      public string RootPath { get; set; }
      public List<NetworkCredentialEntry> Credentials { get; set; } = new List<NetworkCredentialEntry>();
    }

    /// <summary>One stored credential. A password is never a field of an answer; HasPassword is.</summary>
    public class NetworkCredentialEntry
    {
      public string Path { get; set; }
      public string UserName { get; set; }
      public string Domain { get; set; }
      public bool HasPassword { get; set; }
    }

    /// <summary>
    /// One credential to store. The path may stay empty, which means the provider's root - the reference's one
    /// and only slot. An empty username is refused rather than stored; a null password leaves an existing
    /// entry's password alone when it edits itself, and means "no password" when it creates one.
    /// </summary>
    public class NetworkCredentialWish
    {
      public string Path { get; set; }
      public string UserName { get; set; }
      public string Password { get; set; }
      public string Domain { get; set; }
    }

    /// <summary>Which entries to remove: one path, or all of them when the path is empty.</summary>
    public class NetworkCredentialRemoval
    {
      public string Path { get; set; }
    }

    /// <summary>Every credential the provider has, and the root a pathless wish lands on.</summary>
    [HttpGet("network-credentials")]
    public ActionResult<NetworkCredentialsAnswer> GetNetworkCredentials()
    {
      try
      {
        DictionaryState state = OpenCredentials();
        if (state == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Ok(state.Answer);
      }
      catch (Exception ex)
      {
        return StatusCode(StatusCodes.Status500InternalServerError,
            new ErrorResponse { Error = "the stored network credentials could not be read: " + ex.Message });
      }
    }

    /// <summary>Stores one credential, replacing the entry for the same path.</summary>
    [HttpPut("network-credentials")]
    public ActionResult<NetworkCredentialsAnswer> PutNetworkCredential([FromBody] NetworkCredentialWish wish)
    {
      if (wish == null)
        return BadRequest(new ErrorResponse { Error = "no credential was sent" });

      try
      {
        DictionaryState state = OpenCredentials();
        if (state == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        string path = string.IsNullOrWhiteSpace(wish.Path) ? state.RootPath : wish.Path.Trim();
        if (string.IsNullOrEmpty(path))
          return BadRequest(new ErrorResponse { Error = "no path was given, and the provider names no root" });

        string user = (wish.UserName ?? "").Trim();
        if (user.Length == 0)
          return BadRequest(new ErrorResponse
          {
            Error = "a credential without a user name is no credential - remove the entry instead"
          });

        // An existing entry keeps its password across an edit that does not retype it; a password the client
        // does send is handed to the credential type plain, which is what encrypts it on the way to the file.
        object credential = state.Entries.Contains(path) ? state.Entries[path] : null;
        if (credential == null)
          credential = Activator.CreateInstance(state.CredentialType);

        SetIfWrites(credential, "UserName", user);
        SetIfWrites(credential, "Domain", (wish.Domain ?? "").Trim());
        if (wish.Password != null)
          SetIfWrites(credential, "Password", wish.Password);

        state.Entries[path] = credential;
        state.SettingsManager.Save(state.Settings);
        return Ok(state.Answer);
      }
      catch (Exception ex)
      {
        return StatusCode(StatusCodes.Status500InternalServerError,
            new ErrorResponse { Error = "the network credential could not be stored: " + ex.Message });
      }
    }

    /// <summary>Removes one path's credential, or every one of them when the path is empty.</summary>
    [HttpDelete("network-credentials")]
    public ActionResult<NetworkCredentialsAnswer> DeleteNetworkCredential([FromBody] NetworkCredentialRemoval removal)
    {
      try
      {
        DictionaryState state = OpenCredentials();
        if (state == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        string path = (removal?.Path ?? "").Trim();
        if (path.Length == 0)
          state.Entries.Clear();
        else
          state.Entries.Remove(path);
        state.SettingsManager.Save(state.Settings);

        return Ok(state.Answer);
      }
      catch (Exception ex)
      {
        return StatusCode(StatusCodes.Status500InternalServerError,
            new ErrorResponse { Error = "the network credential could not be removed: " + ex.Message });
      }
    }

    #region The settings behind the endpoint, held through reflection

    /// <summary>
    /// The provider's settings object, its dictionary seen as the IDictionary it is, and the credential type
    /// new entries are made of - everything the three endpoints do their work through, none of it named at
    /// compile time.
    /// </summary>
    private sealed class DictionaryState
    {
      internal ISettingsManager SettingsManager;
      internal object Settings;
      internal IDictionary Entries;
      internal Type CredentialType;
      internal string RootPath;

      /// <summary>
      /// The dictionary spelled out for the client. Reading each value through NetworkCredential is the whole
      /// read: the password the class also carries is deliberately never asked.
      /// </summary>
      internal NetworkCredentialsAnswer Answer
      {
        get
        {
          NetworkCredentialsAnswer answer = new NetworkCredentialsAnswer
          {
            ProviderInstalled = true,
            RootPath = RootPath
          };
          foreach (DictionaryEntry entry in Entries)
          {
            if (entry.Value is not NetworkCredential credential)
              continue;
            answer.Credentials.Add(new NetworkCredentialEntry
            {
              Path = entry.Key as string ?? "",
              UserName = credential.UserName,
              Domain = credential.Domain,
              HasPassword = !string.IsNullOrEmpty(credential.Password)
            });
          }
          return answer;
        }
      }
    }

    /// <summary>
    /// The provider's credential dictionary opened for reading and writing, or null when this server has
    /// neither a settings manager nor the network neighborhood plugin - the absences the endpoint answers with
    /// a 503 instead of a file written for a plugin that is not there.
    /// </summary>
    private static DictionaryState OpenCredentials()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      Type settingsType = FindLoadedType(SETTINGS_TYPE);
      if (settingsManager == null || settingsType == null)
        return null;

      Type credentialType = FindLoadedType(CREDENTIAL_TYPE);
      if (credentialType == null)
        return null;

      object settings = settingsManager.Load(settingsType);
      if (settings == null)
        return null;

      PropertyInfo dictionaryProperty = settingsType.GetProperty("NetworkCredentials");
      IDictionary entries = dictionaryProperty?.GetValue(settings) as IDictionary;
      if (entries == null)
        return null;

      return new DictionaryState
      {
        SettingsManager = settingsManager,
        Settings = settings,
        Entries = entries,
        CredentialType = credentialType,
        RootPath = ReadRootPath()
      };
    }

    /// <summary>
    /// The provider's own root path, read off the provider class - the reference's setup screen stored its one
    /// credential under exactly this string, and a pathless wish lands on the same place rather than on a
    /// spelling this assembly invented.
    /// </summary>
    private static string ReadRootPath()
    {
      Type providerType = FindLoadedType(PROVIDER_TYPE);
      object root = providerType?.GetProperty("RootPath", BindingFlags.Public | BindingFlags.Static)
          ?.GetValue(null);
      return root?.ToString() ?? "";
    }

    /// <summary>
    /// Writes one property of the credential, if it takes writes. A credential class that stopped having a
    /// settable property answers by refusing the edit, not by the endpoint crashing on a null reference.
    /// </summary>
    private static void SetIfWrites(object target, string propertyName, string value)
    {
      PropertyInfo property = target.GetType().GetProperty(propertyName);
      if (property == null || !property.CanWrite)
        throw new InvalidOperationException("the credential type has no writable property '" + propertyName + "'");
      property.SetValue(target, value, null);
    }

    /// <summary>
    /// Finds a class among the assemblies this process has loaded, by full name - the metadata endpoint's
    /// lookup, for the same reason: the plugin is there or it is not, and "not" is a legal answer.
    /// </summary>
    private static Type FindLoadedType(string fullName)
    {
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type found = assembly.GetType(fullName, false);
        if (found != null)
          return found;
      }

      return null;
    }

    #endregion
  }
}
