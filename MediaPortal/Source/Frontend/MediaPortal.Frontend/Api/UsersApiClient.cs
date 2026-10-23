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

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// The household's users, as the server's <c>api/users</c> answers them. One entry per user the store
  /// keeps - the password itself never travels to this side, only the word of whether there is one.
  /// </summary>
  public sealed class UserInfo
  {
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = "";

    /// <summary><c>"User"</c> or <c>"Client"</c> - a client profile belongs to a machine, not to a person.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Whether this user must say something to sign in.</summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// Whether the server counts this user as the household's administrator - which the reference derives,
    /// never stores: a person that restricts nothing at all. See <see cref="RoleLabel"/>.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>The id of the profile template this user was added as, or null when nothing was picked.</summary>
    public string? TemplateId { get; set; }

    /// <summary>Whether this is the user the current session is signed in as.</summary>
    public bool IsYou { get; set; }

    /// <summary>The last time this user signed in, as the server's store keeps it. Null when they never have.</summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>The user's picture as base64 PNG, or null when they have none. The reference caps it at 128x128.</summary>
    public string? Image { get; set; }

    /// <summary>Whether this user is kept out of the parts of the television they were not given.</summary>
    public bool EnableRestrictionGroups { get; set; }

    /// <summary>The groups this user may reach, comma separated. Empty when none were given.</summary>
    public string RestrictionGroups { get; set; } = "";

    /// <summary>Whether this user only sees what a viewer of <see cref="AllowedAge"/> may see.</summary>
    public bool RestrictAges { get; set; }

    /// <summary>The oldest viewer this user is allowed to be. Null leaves them unrestricted.</summary>
    public int? AllowedAge { get; set; }

    /// <summary>Ratings that need a parent present count as allowed for this user.</summary>
    public bool IncludeParentGuidedContent { get; set; }

    /// <summary>Titles without any rating count as allowed for this user.</summary>
    public bool IncludeUnratedContent { get; set; }

    /// <summary>Whether this user only sees the shares named in <see cref="AllowedShares"/>.</summary>
    public bool RestrictShares { get; set; }

    /// <summary>The share ids this user may reach, when their shares are restricted.</summary>
    public string[]? AllowedShares { get; set; }

    /// <summary>What a list row shows: the name, and beside it only what sets the entry apart.</summary>
    public string Label
    {
      get
      {
        string line = Name;
        if (IsYou)
          line += "  (you)";
        if (Kind == "Client")
          line += "  (machine)";
        else
        {
          if (IsAdmin)
            line += "  (admin)";
          if (!HasPassword)
            line += "  (no PIN)";
        }
        return line;
      }
    }

    /// <summary>
    /// The kind the reference would say this user is: the profile template they were added as, when one was
    /// picked. Null when the profile carries no template - the household's own hand made them what they are.
    /// </summary>
    public string? TemplateName => ProfileTemplates.Find(TemplateId)?.Name;

    /// <summary>
    /// The one line the row carries about what this user may watch: the reference said it as "All" against a
    /// grown-up and as the ceiling in years against a child, and the ceiling means nothing at all unless it is
    /// switched on, so a switched-off age is an unrestricted one whatever number is written beside it.
    /// </summary>
    public string AgeLabel
    {
      get
      {
        if (!RestrictAges || !AllowedAge.HasValue)
          return "All ages";
        string ages = IncludeParentGuidedContent ? ", with a parent" : "";
        return IncludeUnratedContent
            ? "Up to " + AllowedAge.Value + ages + ", unrated too"
            : "Up to " + AllowedAge.Value + ages;
      }
    }
  }

  /// <summary>The answer of <c>GET api/users</c>: the whole list, or the reason there is none.</summary>
  public sealed class UsersAnswer
  {
    public UserInfo[]? Users { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>
  /// One profile template: the kind of person the room picks when it adds somebody - Admin, Teenager, Child -
  /// carrying the restrictions that kind starts with. The reference kept these as a Global setting its Login
  /// plugin edited; the three its plugin ships as defaults are what <see cref="ProfileTemplates"/> knows.
  /// </summary>
  public sealed class ProfileTemplateInfo
  {
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = "";
    public string Says { get; set; } = "";
    public bool EnableRestrictionGroups { get; set; }
    public string[]? RestrictionGroups { get; set; }
    public bool RestrictAges { get; set; }
    public int? AllowedAge { get; set; }
  }

  /// <summary>The answer of <c>GET api/users/templates</c>.</summary>
  public sealed class ProfileTemplatesAnswer
  {
    public ProfileTemplateInfo[]? Templates { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>
  /// The three kinds the reference's Login plugin ships as a household's profile templates, verbatim and
  /// under the reference's own template ids. The users screen knows them without asking the server - and a
  /// server that carries them states the very same ones over <c>GET api/users/templates</c>.
  /// </summary>
  public static class ProfileTemplates
  {
    public static readonly ProfileTemplateInfo Admin = new ProfileTemplateInfo
    {
      TemplateId = new Guid("{0D5E6C2E-07DA-4427-964F-A92C497BCE04}"),
      Name = "Admin",
      Says = "A grown-up. Sees everything and may change everything.",
      EnableRestrictionGroups = false,
      RestrictionGroups = Array.Empty<string>(),
      RestrictAges = false,
      AllowedAge = null
    };

    public static readonly ProfileTemplateInfo Teenager = new ProfileTemplateInfo
    {
      TemplateId = new Guid("{56477003-A827-461D-92CB-E91FADD69B7D}"),
      Name = "Teenager",
      Says = "Thirteen years of age, and only the settings of their own account.",
      EnableRestrictionGroups = true,
      RestrictionGroups = new[] { "Settings", "Settings.UserProfile.ManageOwn" },
      RestrictAges = true,
      AllowedAge = 13
    };

    public static readonly ProfileTemplateInfo Child = new ProfileTemplateInfo
    {
      TemplateId = new Guid("{24172540-6A51-49E7-B95C-A41D3DBECCCE}"),
      Name = "Child",
      Says = "Five years of age and nothing else - no settings, no grown-up parts.",
      EnableRestrictionGroups = true,
      RestrictionGroups = Array.Empty<string>(),
      RestrictAges = true,
      AllowedAge = 5
    };

    public static ProfileTemplateInfo[] Catalog { get; } = { Admin, Teenager, Child };

    /// <summary>The template behind a stored id, or null when it names no kind that is known here.</summary>
    public static ProfileTemplateInfo? Find(string? templateId)
    {
      if (string.IsNullOrEmpty(templateId))
        return null;
      foreach (ProfileTemplateInfo template in Catalog)
        if (template.TemplateId.ToString("D").Equals(templateId, StringComparison.OrdinalIgnoreCase))
          return template;
      return null;
    }
  }

  /// <summary>The answer of the write endpoints: the user they touched, or the refusal.</summary>
  public sealed class UserAnswer
  {
    public UserInfo? User { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>
  /// The restrictions the caller asks a user to live under. Null fields keep what the user has; as soon as one
  /// of them carries a value the server writes the whole set, so a screen cannot leave half a rule behind.
  /// </summary>
  public sealed class RestrictionWish
  {
    public bool? EnableRestrictionGroups { get; set; }

    /// <summary>The groups the user may reach, comma separated. The empty string means none.</summary>
    public string? RestrictionGroups { get; set; }

    public bool? RestrictAges { get; set; }

    /// <summary>The oldest viewer allowed. Null means no ceiling.</summary>
    public int? AllowedAge { get; set; }

    public bool? IncludeParentGuidedContent { get; set; }

    public bool? IncludeUnratedContent { get; set; }

    public bool? RestrictShares { get; set; }

    public string[]? AllowedShares { get; set; }
  }

  /// <summary>What <c>POST api/users</c> asks for.</summary>
  public sealed class CreateUserWish
  {
    public string Name { get; set; } = "";

    /// <summary>Plain, as the caller spells it; the server hashes it on the way into its store.</summary>
    public string? Password { get; set; }

    /// <summary>The user's picture as base64 PNG. Null or empty means no picture.</summary>
    public string? Image { get; set; }

    /// <summary>What the new user is allowed to watch and reach. Null means a grown-up with no rules.</summary>
    public RestrictionWish? Restrictions { get; set; }

    /// <summary>
    /// The profile template the new person is added as - see <see cref="ProfileTemplates"/>. Null leaves the
    /// user without a kind.
    /// </summary>
    public string? TemplateId { get; set; }
  }

  /// <summary>What <c>PUT api/users/{id}</c> asks for. Null fields stay as they are.</summary>
  public sealed class UpdateUserWish
  {
    public string? Name { get; set; }

    /// <summary>Null keeps the current password; the empty string removes it.</summary>
    public string? Password { get; set; }

    /// <summary>Null keeps the current picture; the empty string takes it away.</summary>
    public string? Image { get; set; }

    /// <summary>Null keeps the rules as they are.</summary>
    public RestrictionWish? Restrictions { get; set; }

    /// <summary>Null keeps the user's kind; the empty string takes it away.</summary>
    public string? TemplateId { get; set; }
  }

  /// <summary>
  /// What the front end asks of a server that keeps users. ServerApiClient answers it; a test wears any
  /// other answer, so the settings screen can be walked through without a server standing behind it.
  /// </summary>
  public interface IUsersApi
  {
    System.Threading.Tasks.Task<UsersAnswer> GetUsersAsync(
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> CreateUserAsync(CreateUserWish wish,
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> DeleteUserAsync(Guid profileId,
        System.Threading.CancellationToken cancellation = default);
  }
}
