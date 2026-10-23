#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;
using Microsoft.AspNetCore.Authorization;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The household's users, over the same HTTP API the library is read from - the endpoints behind the
  /// reference's "Settings -> Users" window.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The user store itself is not this controller's invention: it is MP2's own
  /// <see cref="IUserProfileDataManagement"/>, the same store the token endpoint asks when somebody signs in,
  /// the same store that decides whose shares and age restrictions apply to a connection. What the reference
  /// configured through a window on the server's desktop - who exists, what they must say to get in, which
  /// ones are gone - is what these endpoints carry, because a headless server has no desktop and the
  /// household reaches it from a television.
  /// </para>
  /// <para>
  /// <b>Passwords are hashed on the way in, exactly as the store expects them.</b> The store compares a login
  /// by hashing what the signer says and matching it against what is stored, so a password written down as
  /// plain text would never match again; a null password in an update means "leave it as it is", while an
  /// empty one is the answer itself - a user who needs no password, which the token endpoint already
  /// treats as a valid profile. The stored hash is never sent out; a screen only learns whether one exists.
  /// </para>
  /// <para>
  /// <b>The caller's own account is protected from their own screen.</b> Deleting the signed-in user would end
  /// the session that asked for it mid-gesture, so it is refused in words; whoever really wants to remove
  /// themselves signs in as somebody else first, which is also the reference's rule of thumb.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/users")]
  [MediaPortalAuthorize]
  public class UsersController : ControllerBase
  {
    /// <summary>One user as the store keeps them, minus everything a screen has no business seeing.</summary>
    public class UserInfo
    {
      public Guid ProfileId { get; set; }
      public string Name { get; set; }

      /// <summary><c>"User"</c> or <c>"Client"</c> - a client profile belongs to a machine, not to a person.</summary>
      public string Kind { get; set; }

      /// <summary>Whether this user must say something to sign in. The thing itself is never sent.</summary>
      public bool HasPassword { get; set; }

      /// <summary>
      /// Whether this user is the household's administrator - the reference's own derived rule, spoken the
      /// way its token endpoint speaks it: a real person (not a machine's profile) that restricts nothing
      /// at all, neither ages nor shares, and keeps nobody out of restriction groups. An administrator is
      /// never ticked anywhere; they simply are what their absence of rules makes them.
      /// </summary>
      public bool IsAdmin { get; set; }

      /// <summary>
      /// The profile template this user was created as, as the reference stores it on the profile - the
      /// Admin, Teenager or Child the room picked when adding them. Null when nothing was picked.
      /// </summary>
      public string TemplateId { get; set; }

      /// <summary>Whether this is the user the calling request is signed in as.</summary>
      public bool IsYou { get; set; }

      /// <summary>The last time this user signed in, as the store keeps it. Null if they never have.</summary>
      public DateTime? LastLogin { get; set; }

      /// <summary>The user's picture as a base64 PNG, or null when they have none. The reference caps it at 128x128.</summary>
      public string Image { get; set; }

      /// <summary>Whether this user is kept out of the groups they were not given. See <see cref="RestrictionGroups"/>.</summary>
      public bool EnableRestrictionGroups { get; set; }

      /// <summary>The groups this user may reach, comma separated. Empty when none were given.</summary>
      public string RestrictionGroups { get; set; }

      /// <summary>Whether this user only sees what a viewer of <see cref="AllowedAge"/> may see.</summary>
      public bool RestrictAges { get; set; }

      /// <summary>The oldest viewer this user is allowed to be. Null leaves them unrestricted.</summary>
      public int? AllowedAge { get; set; }

      /// <summary>Ratings a parent may allow for a younger viewer count as allowed for this user.</summary>
      public bool IncludeParentGuidedContent { get; set; }

      /// <summary>Titles without any rating at all count as allowed for this user.</summary>
      public bool IncludeUnratedContent { get; set; }

      /// <summary>Whether this user only sees the shares listed in <see cref="AllowedShares"/>.</summary>
      public bool RestrictShares { get; set; }

      /// <summary>The ids of the shares this user may reach, when their shares are restricted.</summary>
      public List<string> AllowedShares { get; set; }
    }

    /// <summary>
    /// One profile template: the reference's "profile type" a room picks when it adds a person, carrying the
    /// restrictions that kind of person starts with. The reference kept these as a Global setting its Login
    /// plugin could edit; the three it ships as defaults are what this server states here, under the
    /// reference's own template ids, because a headless server's household picks its kinds from a television.
    /// </summary>
    public class ProfileTemplate
    {
      public Guid TemplateId { get; set; }

      /// <summary><c>"Admin"</c>, <c>"Teenager"</c> or <c>"Child"</c> - what the household calls the kind.</summary>
      public string Name { get; set; }

      /// <summary>The sentence a screen puts beside the choice.</summary>
      public string Says { get; set; }

      public bool EnableRestrictionGroups { get; set; }
      public List<string> RestrictionGroups { get; set; }
      public bool RestrictAges { get; set; }
      public int? AllowedAge { get; set; }

      /// <summary>The restrictions as a write request, ready to hand to <see cref="WriteRestrictionsAsync"/>.</summary>
      public RestrictionRequest ToRestrictions()
      {
        return new RestrictionRequest
        {
          EnableRestrictionGroups = EnableRestrictionGroups,
          RestrictionGroups = string.Join(",", RestrictionGroups ?? new List<string>()),
          RestrictAges = RestrictAges,
          AllowedAge = AllowedAge,
          IncludeParentGuidedContent = false,
          IncludeUnratedContent = false
        };
      }
    }

    /// <summary>The answer of <c>GET api/users/templates</c>: the kinds a new person may be added as.</summary>
    public class ProfileTemplateResult
    {
      public List<ProfileTemplate> Templates { get; set; }
      public string Error { get; set; }
    }

    /// <summary>
    /// The three kinds the reference's Login plugin ships: the grown-up with nothing held back, the teenager
    /// allowed the settings of their own account and thirteen years of age, and the child with five years and
    /// no part of the television they were not given. Verbatim from
    /// <c>MediaPortal.UiComponents.Login.Settings.UserTemplateSettings</c>, ids included, so a household that
    /// ever grew from that reference finds its kinds already named the same.
    /// </summary>
    public static List<ProfileTemplate> DefaultProfileTemplates
    {
      get
      {
        return new List<ProfileTemplate>
        {
          new ProfileTemplate
          {
            TemplateId = new Guid("{0D5E6C2E-07DA-4427-964F-A92C497BCE04}"),
            Name = "Admin",
            Says = "A grown-up. Sees everything and may change everything.",
            EnableRestrictionGroups = false,
            RestrictionGroups = new List<string>(),
            RestrictAges = false,
            AllowedAge = null
          },
          new ProfileTemplate
          {
            TemplateId = new Guid("{56477003-A827-461D-92CB-E91FADD69B7D}"),
            Name = "Teenager",
            Says = "Thirteen years of age, and only the settings of their own account.",
            EnableRestrictionGroups = true,
            RestrictionGroups = new List<string> { "Settings", "Settings.UserProfile.ManageOwn" },
            RestrictAges = true,
            AllowedAge = 13
          },
          new ProfileTemplate
          {
            TemplateId = new Guid("{24172540-6A51-49E7-B95C-A41D3DBECCCE}"),
            Name = "Child",
            Says = "Five years of age and nothing else - no settings, no grown-up parts.",
            EnableRestrictionGroups = true,
            RestrictionGroups = new List<string>(),
            RestrictAges = true,
            AllowedAge = 5
          }
        };
      }
    }

    /// <summary>The answer of <c>GET api/users</c>.</summary>
    public class UserListResult
    {
      public List<UserInfo> Users { get; set; }
      public string Error { get; set; }
    }

    /// <summary>The answer of the write endpoints: the user they created or changed, or the refusal.</summary>
    public class UserResult
    {
      public UserInfo User { get; set; }
      public string Error { get; set; }
    }

    /// <summary>
    /// The restrictions a caller asks for. Every field is optional on its own; as soon as one of them carries
    /// a value the whole set is written, the unset ones falling back to what the reference's own user editor
    /// wrote when a box was left alone - the same "write the entire state" the reference's SaveUser performed,
    /// so a caller can never leave half a rule behind.
    /// </summary>
    public class RestrictionRequest
    {
      /// <summary>Keep this user out of groups they were not given.</summary>
      public bool? EnableRestrictionGroups { get; set; }

      /// <summary>The groups the user may reach, comma separated. Empty means none.</summary>
      public string RestrictionGroups { get; set; }

      /// <summary>Only show what a viewer of <see cref="AllowedAge"/> may see.</summary>
      public bool? RestrictAges { get; set; }

      /// <summary>The oldest viewer this user is allowed to be. Null means no ceiling.</summary>
      public int? AllowedAge { get; set; }

      /// <summary>Allow ratings that need a parent present.</summary>
      public bool? IncludeParentGuidedContent { get; set; }

      /// <summary>Allow titles that carry no rating at all.</summary>
      public bool? IncludeUnratedContent { get; set; }

      /// <summary>Only show the shares listed in <see cref="AllowedShares"/>.</summary>
      public bool? RestrictShares { get; set; }

      /// <summary>The share ids the user may reach. Null means none.</summary>
      public List<string> AllowedShares { get; set; }

      /// <summary>Whether any restriction field was sent at all.</summary>
      public bool Any()
      {
        return EnableRestrictionGroups != null || RestrictionGroups != null || RestrictAges != null
            || AllowedAge != null || IncludeParentGuidedContent != null || IncludeUnratedContent != null
            || RestrictShares != null || AllowedShares != null;
      }
    }

    /// <summary>What a caller asks for when it adds a user.</summary>
    public class CreateUserRequest
    {
      /// <summary>The name the user signs in with. Required; a name the store already has is answered honestly.</summary>
      public string Name { get; set; }

      /// <summary>The password, plain, hashed here. Null or empty means the user signs in without one.</summary>
      public string Password { get; set; }

      /// <summary>The user's picture as base64 PNG. Null or empty means no picture.</summary>
      public string Image { get; set; }

      /// <summary>The restrictions the new user starts with. Null means an unrestricted grown-up.</summary>
      public RestrictionRequest Restrictions { get; set; }

      /// <summary>
      /// The profile template the room picked when adding - Admin, Teenager or Child, see
      /// <c>GET api/users/templates</c>. The reference stored the picked template on the profile; this
      /// server stores the same answer, on the same additional-data key the reference's own profiles keep
      /// it under. Null leaves the profile without a template.
      /// </summary>
      public string TemplateId { get; set; }
    }

    /// <summary>What a caller asks for when it changes a user. Every field optional; what is null stays.</summary>
    public class UpdateUserRequest
    {
      public string Name { get; set; }

      /// <summary>New password, plain, hashed here. Null keeps the current one; empty removes the password.</summary>
      public string Password { get; set; }

      /// <summary>
      /// New picture as base64 PNG. Null keeps the current one; an empty string takes the picture away.
      /// The reference resizes to 128x128 on the way in; a client that sends a bigger picture is refused
      /// rather than silently redrawn, because the server has no business decoding images.
      /// </summary>
      public string Image { get; set; }

      /// <summary>New restrictions. Null keeps them as they are.</summary>
      public RestrictionRequest Restrictions { get; set; }

      /// <summary>
      /// The template the user is to be known as. Null keeps what the profile carries. Like the reference's
      /// editor, choosing a kind says what the person <i>is</i>; the restrictions themselves stay whatever
      /// the boxes were last saved as - a household is free to call a teenager by the teenager's name and
      /// still have raised their ceiling by hand.
      /// </summary>
      public string TemplateId { get; set; }
    }

    /// <summary>The reason a write refused, in a body a screen can show.</summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    private static IUserProfileDataManagement Store
    {
      get { return ServiceRegistration.Get<IUserProfileDataManagement>(false); }
    }

    /// <summary>The largest picture base64 the server will even look at (~4 MB), so no caller can post a movie.</summary>
    private const int MAX_IMAGE_BASE64_LENGTH = 4 * 1024 * 1024;

    /// <summary>
    /// Writes the whole restriction state of one user, exactly key for key what the reference's user editor
    /// wrote from its SaveUser button: the flags as "1"/"0" under their known keys, the age ceiling as text,
    /// the group and share lists cleared and re-laid from one, and - because the store has no partial update
    /// for these - always the entire set at once.
    /// </summary>
    private static async Task<bool> WriteRestrictionsAsync(IUserProfileDataManagement store, Guid profileId,
        RestrictionRequest request, UserProfile current)
    {
      bool enableGroups = request.EnableRestrictionGroups ?? current.EnableRestrictionGroups;
      bool restrictAges = request.RestrictAges ?? current.RestrictAges;
      int? allowedAge = request.AllowedAge ?? current.AllowedAge;
      bool parentGuided = request.IncludeParentGuidedContent ?? current.IncludeParentGuidedContent;
      bool unrated = request.IncludeUnratedContent ?? current.IncludeUnratedContent;
      bool restrictShares = request.RestrictShares ?? current.RestrictShares;

      List<string> groups = (request.RestrictionGroups ?? string.Join(",", current.RestrictionGroups))
          .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(g => g.Trim()).Where(g => g.Length > 0).ToList();
      List<string> shares = request.AllowedShares ?? current.GetAllowedShares().Select(s => s.ToString()).ToList();

      bool success = true;
      success &= await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_ALLOWED_AGE,
          allowedAge.HasValue ? allowedAge.Value.ToString() : "").ConfigureAwait(false);
      success &= await store.ClearUserAdditionalDataKeyAsync(profileId, UserDataKeysKnown.KEY_ALLOWED_SHARE)
          .ConfigureAwait(false);
      int shareNo = 0;
      foreach (string share in shares)
        success &= await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_ALLOWED_SHARE, share,
            ++shareNo).ConfigureAwait(false);
      success &= await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_ALLOW_ALL_AGES,
          restrictAges ? "0" : "1").ConfigureAwait(false);
      success &= await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_ALLOW_ALL_SHARES,
          restrictShares ? "0" : "1").ConfigureAwait(false);
      success &= await store.SetUserAdditionalDataAsync(profileId,
          UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, parentGuided ? "1" : "0").ConfigureAwait(false);
      success &= await store.SetUserAdditionalDataAsync(profileId,
          UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, unrated ? "1" : "0").ConfigureAwait(false);
      success &= await store.SetUserAdditionalDataAsync(profileId,
          UserDataKeysKnown.KEY_ENABLE_RESTRICTION_GROUPS, enableGroups ? "1" : "0").ConfigureAwait(false);
      success &= await store.ClearUserAdditionalDataKeyAsync(profileId, UserDataKeysKnown.KEY_RESTRICTION_GROUPS)
          .ConfigureAwait(false);
      int groupNo = 0;
      foreach (string group in groups)
        success &= await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_RESTRICTION_GROUPS,
            group, ++groupNo).ConfigureAwait(false);
      return success;
    }

    /// <summary>
    /// The reference's rule for who is an administrator, unchanged from the word its token endpoint speaks
    /// when it signs somebody in (<c>MediaPortalCredentials</c>): a real person rather than a machine's
    /// profile, restricting nothing at all - no shares, no ages - and keeping nobody out of restriction
    /// groups. Nothing is stored for it and nothing can be ticked to it; it is the standing consequence of
    /// a person being unrestricted.
    /// </summary>
    private static bool IsAdministrator(UserProfile profile)
    {
      return profile.ProfileType == UserProfileType.UserProfile
          && !profile.RestrictShares && !profile.RestrictAges
          && !profile.EnableRestrictionGroups;
    }

    /// <summary>
    /// Whether anybody <i>but</i> one given profile still carries the house. The reference did not guard its
    /// administrator - a household could delete every grown-up from its own editor - but a house that has
    /// lost all of them has no one left who could ever add a new one, so this server refuses that last
    /// refusal. One administrator is the household's floor.
    /// </summary>
    private static async Task<bool> HasAnotherAdministratorAsync(IUserProfileDataManagement store,
        Guid exceptProfileId)
    {
      foreach (UserProfile profile in await store.GetProfilesAsync().ConfigureAwait(false))
        if (profile.ProfileId != exceptProfileId && IsAdministrator(profile))
          return true;
      return false;
    }

    /// <summary>The words the last-administrator refusal comes in, so every endpoint says it the same way.</summary>
    private const string LAST_ADMINISTATOR_REFUSAL =
        "The household needs at least one administrator - somebody without any restrictions. "
        + "Make somebody else the administrator first, then take it away from this one.";

    /// <summary>
    /// Writes the picked profile template onto the user, under the same additional-data key the reference's
    /// own profiles keep it under - an empty id takes the template away again, the way an empty picture
    /// takes the picture away.
    /// </summary>
    private static async Task<bool> WriteTemplateAsync(IUserProfileDataManagement store, Guid profileId,
        string templateId)
    {
      if (templateId.Length == 0)
        return await store.ClearUserAdditionalDataKeyAsync(profileId, UserDataKeysKnown.KEY_TEMPLATE_ID)
            .ConfigureAwait(false);
      if (!Guid.TryParse(templateId, out Guid id) || id == Guid.Empty)
        return false;
      return await store.SetUserAdditionalDataAsync(profileId, UserDataKeysKnown.KEY_TEMPLATE_ID,
          id.ToString(), 0).ConfigureAwait(false);
    }

    /// <summary>
    /// Decodes a picture a caller sent. Null means "no picture"; a string that is not base64 at all is answered
    /// with false rather than an exception, because the caller's screen has to say what went wrong.
    /// </summary>
    private static bool TryDecodeImage(string base64, out byte[] image, out string error)
    {
      image = null;
      error = null;
      if (string.IsNullOrEmpty(base64))
        return true;
      if (base64.Length > MAX_IMAGE_BASE64_LENGTH)
      {
        error = "That picture is too big for a user.";
        return false;
      }
      try
      {
        image = Convert.FromBase64String(base64);
        return true;
      }
      catch (FormatException)
      {
        error = "The picture was not a base64 image.";
        return false;
      }
    }

    /// <summary>
    /// Every user the store keeps, in the store's own order. Anonymous on purpose: the wake screen shows the
    /// household - names and pictures - before anyone has said their PIN, which is what a login screen is for.
    /// What is still locked behind a name is every write and every judgement.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<UserListResult> GetUsers()
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return new UserListResult { Error = "This server has no user store." };

      Guid callerId = CallerProfileId();
      var users = new List<UserInfo>();
      foreach (UserProfile profile in await store.GetProfilesAsync().ConfigureAwait(false))
        users.Add(Describe(profile, callerId));

      return new UserListResult { Users = users };
    }

    /// <summary>
    /// The kinds a new person may be added as: the reference's three profile templates, verbatim. Anonymous
    /// like the user list itself - a household reads its kinds before anyone has said a PIN, and a template
    /// says nothing about anybody, only about the kinds there are.
    /// </summary>
    [HttpGet("templates")]
    [AllowAnonymous]
    public ActionResult<ProfileTemplateResult> GetProfileTemplates()
    {
      return new ProfileTemplateResult { Templates = DefaultProfileTemplates };
    }

    /// <summary>
    /// Adds a user. The name must be new; the store's own opinion about a name it already has is passed
    /// through rather than pretended away.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResult>> CreateUser([FromBody] CreateUserRequest request)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      string name = (request?.Name ?? "").Trim();
      if (name.Length == 0)
        return BadRequest(new ErrorResponse { Error = "A user needs a name." });

      AsyncResult<UserProfile> existing = await store.GetProfileByNameAsync(name).ConfigureAwait(false);
      if (existing != null && existing.Success)
        return Conflict(new ErrorResponse { Error = "There is already a user called '" + name + "'." });

      string password = string.IsNullOrEmpty(request.Password) ? "" : UserProfile.HashPassword(request.Password);
      if (!TryDecodeImage(request.Image, out byte[] image, out string imageError))
        return BadRequest(new ErrorResponse { Error = imageError });

      Guid profileId = await store.CreateProfileAsync(name, UserProfileType.UserProfile, password)
          .ConfigureAwait(false);

      AsyncResult<UserProfile> created = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (created == null || !created.Success)
        return StatusCode(500, new ErrorResponse { Error = "The store did not take the new user." });

      if (image != null && !await store.SetProfileImageAsync(profileId, image).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not take the picture." });

      RestrictionRequest restrictions = request.Restrictions ?? new RestrictionRequest();
      if (!await WriteRestrictionsAsync(store, profileId, restrictions, created.Result).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not take the restrictions." });

      if (request.TemplateId != null && !await WriteTemplateAsync(store, profileId, request.TemplateId)
          .ConfigureAwait(false))
        return BadRequest(new ErrorResponse { Error = "That is not a profile template." });

      AsyncResult<UserProfile> stored = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      UserProfile after = stored != null && stored.Success ? stored.Result : created.Result;
      return new UserResult { User = Describe(after, CallerProfileId()) };
    }

    /// <summary>
    /// Renames a user, gives them a new password, or both - the same two answers the reference's user editor
    /// held open on one row.
    /// </summary>
    [HttpPut("{profileId}")]
    public async Task<ActionResult<UserResult>> UpdateUser(Guid profileId, [FromBody] UpdateUserRequest request)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      AsyncResult<UserProfile> found = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (found == null || !found.Success)
        return NotFound(new ErrorResponse { Error = "There is no user with this id." });

      UserProfile current = found.Result;
      string name = string.IsNullOrWhiteSpace(request?.Name) ? current.Name : request.Name.Trim();
      if (name.Length == 0)
        return BadRequest(new ErrorResponse { Error = "A user needs a name." });

      if (!string.Equals(name, current.Name, StringComparison.Ordinal))
      {
        AsyncResult<UserProfile> taken = await store.GetProfileByNameAsync(name).ConfigureAwait(false);
        if (taken != null && taken.Success)
          return Conflict(new ErrorResponse { Error = "There is already a user called '" + name + "'." });
      }

      string password = request?.Password == null
          ? current.Password
          : request.Password.Length == 0 ? "" : UserProfile.HashPassword(request.Password);

      // The one rule no edit may cross: it takes the last administrator from a household. Only a change
      // that writes restrictions can do it, since the role is nothing but the absence of restrictions.
      if (IsAdministrator(current) && request?.Restrictions != null && request.Restrictions.Any())
      {
        RestrictionRequest wanted = request.Restrictions;
        bool stillAdmin = !(wanted.RestrictShares ?? current.RestrictShares)
            && !(wanted.RestrictAges ?? current.RestrictAges)
            && !(wanted.EnableRestrictionGroups ?? current.EnableRestrictionGroups);
        if (!stillAdmin && !await HasAnotherAdministratorAsync(store, profileId).ConfigureAwait(false))
          return BadRequest(new ErrorResponse { Error = LAST_ADMINISTATOR_REFUSAL });
      }

      if (!await store.UpdateProfileAsync(profileId, name, current.ProfileType, password).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not accept the change." });

      if (request?.TemplateId != null && !await WriteTemplateAsync(store, profileId, request.TemplateId)
          .ConfigureAwait(false))
        return BadRequest(new ErrorResponse { Error = "That is not a profile template." });

      if (request?.Image != null)
      {
        if (!TryDecodeImage(request.Image, out byte[] image, out string imageError))
          return BadRequest(new ErrorResponse { Error = imageError });
        // The store keeps "no picture" as a null column; an empty string is how a caller asks for that.
        if (!await store.SetProfileImageAsync(profileId, image).ConfigureAwait(false))
          return StatusCode(500, new ErrorResponse { Error = "The store did not take the picture." });
      }

      if (request?.Restrictions != null && request.Restrictions.Any())
      {
        if (!await WriteRestrictionsAsync(store, profileId, request.Restrictions, current).ConfigureAwait(false))
          return StatusCode(500, new ErrorResponse { Error = "The store did not take the restrictions." });
      }

      AsyncResult<UserProfile> updated = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      UserProfile after = updated != null && updated.Success ? updated.Result : current;
      return new UserResult { User = Describe(after, CallerProfileId()) };
    }

    /// <summary>
    /// Removes a user. The signed-in user cannot remove themselves from their own screen; the data question
    /// behind it - what happens to a deleted user's watched marks - is the store's, which is the same answer
    /// the reference's own user window gave.
    /// </summary>
    [HttpDelete("{profileId}")]
    public async Task<ActionResult<UserResult>> DeleteUser(Guid profileId)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      if (profileId == CallerProfileId())
        return BadRequest(new ErrorResponse
        {
          Error = "You cannot delete the user you are signed in as. Sign in as somebody else first."
        });

      AsyncResult<UserProfile> found = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (found == null || !found.Success)
        return NotFound(new ErrorResponse { Error = "There is no user with this id." });

      // The same floor as the edit: the last administrator cannot be deleted out of the household.
      if (IsAdministrator(found.Result) && !await HasAnotherAdministratorAsync(store, profileId)
          .ConfigureAwait(false))
        return BadRequest(new ErrorResponse { Error = LAST_ADMINISTATOR_REFUSAL });

      if (!await store.DeleteProfileAsync(profileId).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not remove the user." });

      return new UserResult { User = Describe(found.Result, CallerProfileId()) };
    }

    /// <summary>The profile id the bearer token was issued for, or empty when the call carries no name.</summary>
    private Guid CallerProfileId()
    {
      string sid = HttpContext.User?.FindFirstValue(ClaimTypes.Sid);
      return sid != null && Guid.TryParse(sid, out Guid profileId) ? profileId : Guid.Empty;
    }

    private static UserInfo Describe(UserProfile profile, Guid callerId)
    {
      return new UserInfo
      {
        ProfileId = profile.ProfileId,
        Name = profile.Name,
        Kind = profile.ProfileType == UserProfileType.ClientProfile ? "Client" : "User",
        HasPassword = !string.IsNullOrEmpty(profile.Password),
        IsAdmin = IsAdministrator(profile),
        TemplateId = profile.TemplateId == Guid.Empty ? null : profile.TemplateId.ToString("D"),
        IsYou = profile.ProfileId == callerId,
        LastLogin = profile.LastLogin,
        Image = profile.Image == null || profile.Image.Length == 0
            ? null : Convert.ToBase64String(profile.Image),
        EnableRestrictionGroups = profile.EnableRestrictionGroups,
        RestrictionGroups = string.Join(",", profile.RestrictionGroups),
        RestrictAges = profile.RestrictAges,
        AllowedAge = profile.AllowedAge,
        IncludeParentGuidedContent = profile.IncludeParentGuidedContent,
        IncludeUnratedContent = profile.IncludeUnratedContent,
        RestrictShares = profile.RestrictShares,
        AllowedShares = profile.GetAllowedShares().Select(s => s.ToString()).ToList()
      };
    }
  }
}
