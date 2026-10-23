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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Who may sign in on the server - the reference's "Settings -> Users" window, in the front end's own
  /// shape. The store is the server's (the very one the token endpoint reads when somebody signs in); this
  /// screen lists what it keeps and works the three answers that window had: a name, the password that
  /// belongs to it, and removal. Nothing here is local - a server without the endpoint says so in the
  /// status line rather than showing an empty form that lies.
  /// </summary>
  /// <remarks>
  /// The provider is a delegate rather than the client itself because the viewer may point at a different
  /// server between visits, exactly like the server-settings and metadata screens. The password box holds
  /// what is being typed and nothing more: a stored password never comes to this side of the wire, so the
  /// box starts empty every time a user is picked, and an empty box on Save means "keep what is there",
  /// while the "remove the password" answer is its own button - one gesture, one meaning.
  /// </remarks>
  public partial class UsersSettingsViewModel : ObservableObject
  {
    private readonly Func<IUsersApi?> _clientProvider;
    private readonly Func<IServerSettingsApi?>? _policyProvider;
    private readonly ViewerState? _viewer;

    /// <summary>The picture the room has picked but not saved yet; null while nothing new was chosen.</summary>
    private string? _pendingPicture;

    /// <summary>The group boxes were last laid out for, so a reload does not answer for the previous face.</summary>
    private Guid _rowsBelongTo = Guid.Empty;

    public UsersSettingsViewModel(Func<IUsersApi?> clientProvider,
        Func<IServerSettingsApi?>? policyProvider = null, ViewerState? viewer = null)
    {
      _clientProvider = clientProvider;
      _policyProvider = policyProvider;
      _viewer = viewer;
      foreach (RestrictionGroupInfo group in RestrictionGroups.Catalog(null))
        GroupRows.Add(new RestrictionRow(group.Name, group.Says));
    }

    public ObservableCollection<UserInfo> Users { get; } = new ObservableCollection<UserInfo>();

    public string Heading => "Users";

    public string Subtitle =>
        "Who may sign in on the server. The names, the four-number PINs and the removals are the server's "
        + "own store - the same one that answers when somebody signs in - and every change is written "
        + "there the moment it is made. The start of this machine asks the remembered person for their "
        + "PIN; a person without one walks right in.";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private UserInfo? _selected;

    [ObservableProperty]
    private string _nameText = "";

    [ObservableProperty]
    private string _passwordText = "";

    /// <summary>Whether this user only sees what a viewer of the age beside it may see.</summary>
    [ObservableProperty]
    private bool _restrictAgesChecked;

    /// <summary>The oldest viewer this user is allowed to be, as typed. Years, not a certification string.</summary>
    [ObservableProperty]
    private string _ageText = "";

    /// <summary>Ratings that need a parent present count as allowed.</summary>
    [ObservableProperty]
    private bool _parentGuidedChecked;

    /// <summary>Titles that carry no rating at all count as allowed.</summary>
    [ObservableProperty]
    private bool _unratedChecked;

    /// <summary>Whether this user is kept out of the parts of the television they were not given.</summary>
    [ObservableProperty]
    private bool _restrictGroupsChecked;

    /// <summary>Whether this user only sees the shares ticked below.</summary>
    [ObservableProperty]
    private bool _restrictSharesChecked;

    /// <summary>The picked user's face, as the server keeps it or as the room just picked it. Null shows the initial.</summary>
    [ObservableProperty]
    private Bitmap? _picturePreview;

    /// <summary>Whether the picked user wears a picture - the press that takes it away waits for this.</summary>
    [ObservableProperty]
    private bool _hasPicture;

    /// <summary>What this user may reach, one box per part of the front end the reference registered.</summary>
    public ObservableCollection<RestrictionRow> GroupRows { get; } = new ObservableCollection<RestrictionRow>();

    /// <summary>The server's folders, one box each, for a user whose shares are restricted.</summary>
    public ObservableCollection<RestrictionRow> ShareRows { get; } = new ObservableCollection<RestrictionRow>();

    /// <summary>The names the no-question picker offers: nobody, and every person the server keeps.</summary>
    public ObservableCollection<UserInfo> LoginCandidates { get; } = new ObservableCollection<UserInfo>();

    /// <summary>Whether the highlight stands on somebody. The working rows appear with a picked user.</summary>
    public bool HasSelection => Selected != null;

    /// <summary>
    /// Whether the household is asked who is watching - one of the five house rules the reference kept as
    /// Global server settings, and the reason they are all edited here under these very users. These boxes
    /// speak to the <b>server</b> (<c>GET/PUT api/media/settings</c>), not to this box's file: a second
    /// television in the house is then let in - and asked - by the same rules, which is exactly why the
    /// reference refused to keep them per-client.
    /// </summary>
    [ObservableProperty]
    private bool _askLoginChecked;

    /// <summary>
    /// Whether the screen that asks is shown when somebody is expected. Off keeps the signed-in person's
    /// rules without an interview - the reference's <c>EnableUserLoginScreen</c>, and usually paired with
    /// an auto-login name.
    /// </summary>
    [ObservableProperty]
    private bool _showLoginScreenChecked;

    /// <summary>Whether an untouched television gives its user back to the household.</summary>
    [ObservableProperty]
    private bool _autoLogoutChecked;

    /// <summary>How many minutes of nothing that takes, as typed. The reference's own default: 30.</summary>
    [ObservableProperty]
    private string _timeoutText = "30";

    /// <summary>Whether the server answered with its policy at all; the boxes wait for that before offering.</summary>
    [ObservableProperty]
    private bool _hasPolicy;

    /// <summary>The person signed in without being asked; the picker's rows are <see cref="LoginCandidates"/>.</summary>
    [ObservableProperty]
    private UserInfo? _autoLoginPick;

    /// <summary>The policy as the server last stated it, so a save can send back what was not touched.</summary>
    private LoginPolicyAnswer? _policy;

    /// <summary>The picker's row for "nobody" - a television that belongs to no person walks in together.</summary>
    public static UserInfo Nobody { get; } = new UserInfo { ProfileId = Guid.Empty, Name = "Nobody" };

    /// <summary>
    /// The kinds a person can be added as - the reference's three profile templates, kept by the server
    /// under the reference's own ids. Picking one lays its restrictions under the boxes below (which stay
    /// editable: the kind says what somebody <i>is</i>, the boxes say what the household really enforces),
    /// and the picked id travels to the server with the user, the way the reference stored it.
    /// </summary>
    public ProfileTemplateInfo[] Kinds => ProfileTemplates.Catalog;

    /// <summary>The kind picked for a new person, or worn by the picked one. Null is the household's own make.</summary>
    [ObservableProperty]
    private ProfileTemplateInfo? _kindPick = ProfileTemplates.Admin;

    /// <summary>True while the code lays a picked person's kind under the boxes, so the handler sits still.</summary>
    private bool _settingKind;

    partial void OnKindPickChanged(ProfileTemplateInfo? value)
    {
      if (_settingKind || value == null)
        return;

      // The reference's ApplyTemplate, box for box: the kind's age ceiling with its switch, its groups with
      // their switch, and nothing about shares or the two rating exceptions, which no kind prescribes.
      RestrictAgesChecked = value.RestrictAges;
      AgeText = value.AllowedAge.HasValue ? value.AllowedAge.Value.ToString() : "";
      RestrictGroupsChecked = value.EnableRestrictionGroups;
      HashSet<string> given = (value.RestrictionGroups ?? Array.Empty<string>())
          .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
      foreach (RestrictionRow row in GroupRows)
        row.IsChecked = given.Contains(row.Name);
      OnPropertyChanged(nameof(KindSays));
    }

    /// <summary>
    /// The sentence beside the kind picker: what the picked kind means, and whether it makes the person the
    /// household's administrator - which it does not make so much as reveal, since the server derives that
    /// role from nothing but the absence of restrictions.
    /// </summary>
    public string KindSays
    {
      get
      {
        ProfileTemplateInfo? kind = KindPick;
        if (kind == null)
          return "Without a kind the boxes alone say what this person may do.";
        return kind.Says + (kind == ProfileTemplates.Admin
            ? " The server counts a person with nothing restricted as the household's administrator - its last one is guarded."
            : " Not an administrator - that role falls to whoever carries no restrictions at all.");
      }
    }

    /// <summary>
    /// The house rules as they stand: what the screen does when it wakes, and what it does when nobody
    /// touches it. Said out loud because the boxes above say the rules and this says the consequence.
    /// </summary>
    public string PolicySays
    {
      get
      {
        if (!HasPolicy)
          return "The house rules live on the server; this screen has not heard from them yet.";
        string asks = !AskLoginChecked
            ? "Nobody is asked - the television opens on the household together."
            : ShowLoginScreenChecked
                ? AutoLoginPick != null && AutoLoginPick.ProfileId != Guid.Empty
                    ? "The screen wakes on its question, but " + AutoLoginPick.Name + " is expected and walks right in."
                    : "The screen wakes on its question and waits for a PIN."
                : "The screen does not ask on its own; whoever is signed in keeps their rules.";
        string idle = AutoLogoutChecked
            ? " After " + TimeoutText + " minutes of nothing it asks again."
            : "";
        return asks + idle;
      }
    }

    /// <summary>Reads the house rules from the server beside the people they govern.</summary>
    [RelayCommand]
    public async Task LoadPolicyAsync()
    {
      IServerSettingsApi? api = _policyProvider?.Invoke();
      if (api == null)
        return;

      try
      {
        ApplyPolicy(await api.GetLoginPolicyAsync());
      }
      catch (Exception)
      {
        // The people list still stands on its own; the rules row says it has not heard.
        HasPolicy = false;
        OnPropertyChanged(nameof(PolicySays));
      }
    }

    /// <summary>
    /// Writes the house rules back. Every five fields go together, because the server keeps what a message
    /// leaves out and this screen owns all five - half a policy is how televisions end up asking questions
    /// nobody set.
    /// </summary>
    [RelayCommand]
    public async Task SavePolicyAsync()
    {
      IServerSettingsApi? api = _policyProvider?.Invoke();
      if (api == null)
      {
        Status = "No server address yet - the house rules live on the server.";
        return;
      }

      if (!int.TryParse(TimeoutText.Trim(), out int minutes) || minutes < 1 || minutes > 24 * 60)
      {
        Status = "The idle timeout has to be a whole number of minutes between 1 and 1440.";
        return;
      }

      try
      {
        LoginPolicyAnswer answer = await api.PutLoginPolicyAsync(new LoginPolicyWish
        {
          EnableUserLogin = AskLoginChecked,
          EnableUserLoginScreen = ShowLoginScreenChecked,
          AutoLoginUser = (AutoLoginPick?.ProfileId ?? Guid.Empty).ToString(),
          AutoLogoutEnabled = AutoLogoutChecked,
          AutoLogoutIdleTimeoutInMin = minutes
        });
        ApplyPolicy(answer);
        Status = "The server kept the house rules - every television here obeys them now.";
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    private void ApplyPolicy(LoginPolicyAnswer answer)
    {
      _policy = answer;
      HasPolicy = true;
      AskLoginChecked = answer.EnableUserLogin;
      ShowLoginScreenChecked = answer.EnableUserLoginScreen;
      AutoLogoutChecked = answer.AutoLogoutEnabled;
      TimeoutText = answer.AutoLogoutIdleTimeoutInMin.ToString();
      RebuildLoginCandidates();
      if (Guid.TryParse(answer.AutoLoginUser, out Guid auto) && auto != Guid.Empty)
        AutoLoginPick = LoginCandidates.FirstOrDefault(c => c.ProfileId == auto) ?? Nobody;
      else
        AutoLoginPick = Nobody;
      _viewer?.Policy = answer;
      OnPropertyChanged(nameof(PolicySays));
    }

    private void RebuildLoginCandidates()
    {
      Guid picked = Guid.Empty;
      if (AutoLoginPick != null)
        picked = AutoLoginPick.ProfileId;
      else if (_policy != null && Guid.TryParse(_policy.AutoLoginUser, out Guid g) && g != Guid.Empty)
        picked = g;
      LoginCandidates.Clear();
      LoginCandidates.Add(Nobody);
      foreach (UserInfo user in Users.Where(u => u.Kind != "Client"))
        LoginCandidates.Add(user);
      AutoLoginPick = LoginCandidates.FirstOrDefault(c => c.ProfileId == picked) ?? Nobody;
    }

    partial void OnAskLoginCheckedChanged(bool value) => OnPropertyChanged(nameof(PolicySays));
    partial void OnShowLoginScreenCheckedChanged(bool value) => OnPropertyChanged(nameof(PolicySays));
    partial void OnAutoLogoutCheckedChanged(bool value) => OnPropertyChanged(nameof(PolicySays));
    partial void OnTimeoutTextChanged(string value) => OnPropertyChanged(nameof(PolicySays));
    partial void OnAutoLoginPickChanged(UserInfo? value) => OnPropertyChanged(nameof(PolicySays));

    /// <summary>The quiet sentence about today's answer: who the room watches as, or that nobody is asked.</summary>
    public string WatchingSays
    {
      get
      {
        string remembered = ClientSettings.Load(null, out _).RememberedViewer;
        return remembered.Length == 0
            ? "Nobody is remembered right now - the room watches all together."
            : string.Format("The room is watching as {0}.", remembered);
      }
    }

    /// <summary>
    /// Forgets the remembered answer and turns the asking back on, so the next start poses the question
    /// again. The reference's households got back to the question the same way - by saying so under
    /// Settings → Users, not by hunting through files.
    /// </summary>
    [RelayCommand]
    public void AskAgainNextStart()
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = "";
      settings.RememberedViewerHasPin = false;
      string says = settings.Save(null, out bool saved);
      Status = saved ? "The next start will ask who is watching." : says;
      OnPropertyChanged(nameof(WatchingSays));
    }

    partial void OnSelectedChanged(UserInfo? value)
    {
      NameText = value?.Name ?? "";
      PasswordText = "";
      FillEditor(value);
      OnPropertyChanged(nameof(HasSelection));
      OnPropertyChanged(nameof(SelectionSays));
    }

    /// <summary>
    /// Lays the picked person's own rules under the editor's boxes - the age ceiling with its two exceptions,
    /// the group boxes, the share boxes, and their picture in the round frame. The reference's user editor
    /// opened exactly like this on every row; nothing here guesses: a user who never chose anything shows
    /// everything switched off, which is what the store actually holds about them.
    /// </summary>
    private void FillEditor(UserInfo? who)
    {
      _pendingPicture = null;
      _rowsBelongTo = who?.ProfileId ?? Guid.Empty;

      // The kind the person was added as, laid under the picker without letting it re-lay the boxes -
      // those are filled from the person's own stored restrictions just below, which is the whole truth.
      _settingKind = true;
      KindPick = who == null ? ProfileTemplates.Admin : ProfileTemplates.Find(who.TemplateId);
      _settingKind = false;
      OnPropertyChanged(nameof(KindSays));

      RestrictAgesChecked = who?.RestrictAges == true;
      AgeText = who?.AllowedAge.HasValue == true ? who.AllowedAge.Value.ToString() : "";
      ParentGuidedChecked = who?.IncludeParentGuidedContent == true;
      UnratedChecked = who?.IncludeUnratedContent == true;
      RestrictGroupsChecked = who?.EnableRestrictionGroups == true;
      RestrictSharesChecked = who?.RestrictShares == true;

      HashSet<string> owned = (who?.RestrictionGroups ?? "")
          .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(g => g.Trim())
          .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
      foreach (RestrictionRow row in GroupRows)
        row.IsChecked = owned.Contains(row.Name);

      HashSet<string> allowedShares = (who?.AllowedShares ?? Array.Empty<string>()).ToHashSet();
      foreach (RestrictionRow row in ShareRows)
        row.IsChecked = allowedShares.Contains(row.Name);

      PicturePreview = UserPicture.FromBase64(who?.Image);
      HasPicture = !string.IsNullOrEmpty(who?.Image);
      OnPropertyChanged(nameof(PictureSays));
    }

    /// <summary>
    /// The file the room picked for the picked person, shrunk the reference's way before it is offered to
    /// the server. The view's "choose a picture" press asks the operating system for a file and hands the
    /// path here - the model never opens a dialog itself.
    /// </summary>
    public void PictureChosen(string path)
    {
      string? shrunk = UserPicture.LoadShrunk(path, out string? error);
      if (error != null)
      {
        Status = error;
        return;
      }
      _pendingPicture = shrunk;
      PicturePreview = UserPicture.FromBase64(shrunk);
      HasPicture = true;
      Status = "The picture is picked - press \"Save the picked user\" to give it to " +
          (Selected?.Name ?? "them") + ".";
      OnPropertyChanged(nameof(PictureSays));
    }

    /// <summary>Takes the picture away: saved as "no picture", the round frame wears the initial again.</summary>
    [RelayCommand]
    public void RemovePicture()
    {
      _pendingPicture = "";
      PicturePreview = null;
      HasPicture = false;
      OnPropertyChanged(nameof(PictureSays));
    }

    /// <summary>The line beside the round frame: whose face it wears right now, and whether it waits on Save.</summary>
    public string PictureSays
    {
      get
      {
        if (Selected == null)
          return "A picture is picked from a file and saved with the user; without one, the initial is shown.";
        if (!string.IsNullOrEmpty(_pendingPicture))
          return Selected.Name + " wears the new picture as soon as you save.";
        return HasPicture
            ? Selected.Name + " has a picture."
            : Selected.Name + " wears the initial of the name.";
      }
    }

    /// <summary>The quiet sentence under the editor: who is picked and what is already true about them.</summary>
    public string SelectionSays
    {
      get
      {
        if (Selected == null)
          return "Pick a user on the left to change them, or type a new name to add one.";
        string says = Selected.Name;
        says += Selected.IsYou ? " - this is who you are signed in as" : "";
        says += Selected.Kind == "Client" ? " - a machine's profile, not a person's" : "";
        if (Selected.TemplateName != null)
          says += " - added as a " + Selected.TemplateName;
        if (Selected.IsAdmin)
          says += " - the household's administrator";
        says += Selected.HasPassword ? " - has a PIN" : " - walks in without a PIN";
        says += " - " + Selected.AgeLabel.ToLowerInvariant();
        if (Selected.LastLogin.HasValue)
          says += ", last seen " + Selected.LastLogin.Value.ToString("g");
        return says;
      }
    }

    /// <summary>Asks the server who it keeps. Called when the section opens and by the reload button.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        UsersAnswer answer = await client.GetUsersAsync();
        Users.Clear();
        foreach (UserInfo user in answer.Users ?? Array.Empty<UserInfo>())
          Users.Add(user);
        Status = answer.Error != null
            ? answer.Error
            : string.Format("Read {0} user(s) from the server.", Users.Count);
        RaiseSelection();
        RebuildLoginCandidates();
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 404 || e.StatusCode == 405
            ? "This server does not offer its users over the API - it is probably an older build."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }

      await LoadPolicyAsync();
      await LoadSharesAsync();
    }

    /// <summary>
    /// Reads the server's folders into the share boxes once, so a child's allowlist is ticked from the
    /// folders that really exist rather than typed by hand. A server without the endpoint simply leaves the
    /// boxes empty - the switch above them stays honest whatever they hold.
    /// </summary>
    private async Task LoadSharesAsync()
    {
      IUsersApi? probe = _clientProvider();
      ServerApiClient? direct = probe as ServerApiClient;
      if (direct == null || ShareRows.Count > 0)
        return;

      try
      {
        SharesAnswer answer = await direct.GetSharesAsync();
        foreach (ShareInfo share in answer.Shares ?? Array.Empty<ShareInfo>())
          ShareRows.Add(new RestrictionRow(share.Id.ToString(),
              (string.IsNullOrWhiteSpace(share.Name) ? share.Path : share.Name) ?? share.Id.ToString("D")));
        // The picked person's ticks were read before the folders existed; lay them under the boxes now.
        if (_rowsBelongTo == (Selected?.ProfileId ?? Guid.Empty))
          FillEditor(Selected);
      }
      catch (Exception)
      {
        // No folders is the same state as no server behind the boxes; nothing to say about it here.
      }
    }

    /// <summary>Adds the typed name, with the typed password (or none). The store's name rules are the server's.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
      string name = NameText.Trim();
      if (name.Length == 0)
      {
        Status = "A user needs a name.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      string? pinProblem = PinProblem(PasswordText);
      if (pinProblem != null)
      {
        Status = pinProblem;
        return;
      }

      // The kind picked above is no decoration: its age ceiling is already laid under the boxes, and the
      // boxes - validated here like on every save - are what the server is told. The kind's id travels
      // beside them, the way the reference stored the picked template on the profile.
      string? ageProblem = AgeProblem(out int? allowedAge);
      if (ageProblem != null)
      {
        Status = ageProblem;
        return;
      }

      try
      {
        UserAnswer answer = await client.CreateUserAsync(new CreateUserWish
        {
          Name = name,
          Password = PasswordText.Length == 0 ? null : PasswordText,
          Image = string.IsNullOrEmpty(_pendingPicture) ? null : _pendingPicture,
          Restrictions = ReadRestrictions(allowedAge),
          TemplateId = KindPick?.TemplateId.ToString("D")
        });
        PasswordText = "";
        _pendingPicture = null;
        await LoadAsync();
        // The list has been read back, and what the viewer keeps reading is the server's answer to the
        // press, not the count the read-back brought.
        Status = answer.Error != null
            ? "The server would not add '" + name + "': " + answer.Error
            : string.Format("'{0}' was added.", name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Renames the picked user and, when a new password was typed, changes it too. An empty password box
    /// keeps the current password - removing one is its own answer, below, because empty-box-means-nothing
    /// and empty-box-means-remove would otherwise be the same gesture with two meanings.
    /// </summary>
    [RelayCommand]
    public async Task SaveSelectedAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      string name = NameText.Trim();
      if (name.Length == 0)
      {
        Status = "A user needs a name.";
        return;
      }

      string? pinProblem = PinProblem(PasswordText);
      if (pinProblem != null)
      {
        Status = pinProblem;
        return;
      }

      string? ageProblem = AgeProblem(out int? allowedAge);
      if (ageProblem != null)
      {
        Status = ageProblem;
        return;
      }

      try
      {
        UserAnswer answer = await client.UpdateUserAsync(who.ProfileId, new UpdateUserWish
        {
          Name = name,
          Password = PasswordText.Length == 0 ? null : PasswordText,
          Image = _pendingPicture,
          Restrictions = ReadRestrictions(allowedAge),
          TemplateId = KindPick?.TemplateId.ToString("D")
        });
        _pendingPicture = null;
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not change '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' saved.", name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Takes the typed-away PIN off the picked user. The box is not the answer here; this press is.</summary>
    [RelayCommand]
    public async Task RemovePasswordAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        UserAnswer answer = await client.UpdateUserAsync(who.ProfileId, new UpdateUserWish { Password = "" });
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not change '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' walks in without a PIN now.", who.Name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Removes the picked user. The signed-in one cannot remove themselves - the server refuses it in words,
    /// and those words land in the status line rather than being pre-empted here, so the screen shows the
    /// server's own rule rather than a second one invented on top of it.
    /// </summary>
    [RelayCommand]
    public async Task RemoveSelectedAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        UserAnswer answer = await client.DeleteUserAsync(who.ProfileId);
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not remove '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' was removed.", who.Name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Keeps the picked user's identity and lets the picked user's name stand after a reload.</summary>
    private void RaiseSelection()
    {
      UserInfo? picked = Selected;
      if (picked == null)
        return;

      foreach (UserInfo candidate in Users)
        if (candidate.ProfileId == picked.ProfileId)
        {
          Selected = candidate;
          return;
        }

      Selected = null;
    }

    /// <summary>
    /// Reads the boxes above into what the server calls the restrictions. The whole set goes every time, as
    /// the server's endpoint expects, so a tick taken off is actually taken off rather than left standing in
    /// the store from the last save.
    /// </summary>
    private RestrictionWish ReadRestrictions(int? allowedAge)
    {
      return new RestrictionWish
      {
        RestrictAges = RestrictAgesChecked,
        AllowedAge = allowedAge,
        IncludeParentGuidedContent = ParentGuidedChecked,
        IncludeUnratedContent = UnratedChecked,
        EnableRestrictionGroups = RestrictGroupsChecked,
        RestrictionGroups = string.Join(",",
            GroupRows.Where(row => row.IsChecked).Select(row => row.Name)),
        RestrictShares = RestrictSharesChecked,
        AllowedShares = ShareRows.Where(row => row.IsChecked).Select(row => row.Name).ToArray()
      };
    }

    /// <summary>
    /// What a typed age has to be: a whole number of years between 1 and 120 - or nothing at all when the
    /// switch above it is off, which is what "no ceiling" means to the server. The switch is the answer and
    /// the number is its content, so a switched-off ceiling never sends a number and a switched-on one never
    /// sends a blank.
    /// </summary>
    private string? AgeProblem(out int? allowedAge)
    {
      allowedAge = null;
      string typed = AgeText.Trim();
      if (!RestrictAgesChecked)
        return null;
      if (typed.Length == 0)
        return "An age limit needs the age in years, or switch it off.";
      if (!int.TryParse(typed, out int years) || years < 1 || years > 120)
        return "The age has to be a whole number of years between 1 and 120.";
      allowedAge = years;
      return null;
    }

    /// <summary>
    /// What a typed PIN has to be: four digits, or nothing at all for a person who needs none. The server
    /// would take any string - it is the wake screen that asks four numbers in four boxes, and a person
    /// saved beside that rule would be a person that screen can never open.
    /// </summary>
    private static string? PinProblem(string typed)
    {
      if (typed.Length == 0)
        return null;
      if (typed.Length != 4)
        return "A PIN is four digits - or nothing, for a person who needs none.";
      foreach (char c in typed)
        if (c < '0' || c > '9')
          return "A PIN is digits, 0 to 9.";
      return null;
    }
  }

  /// <summary>
  /// One ticked-or-free row of the two boxes a user can be restricted by - a part of the television, or one
  /// of the server's folders. <see cref="Name"/> is what travels to the store (the group's registered name,
  /// or the share's id); <see cref="Says"/> is what the household reads beside the box.
  /// </summary>
  public sealed partial class RestrictionRow : ObservableObject
  {
    public RestrictionRow(string name, string says)
    {
      Name = name;
      Says = says;
    }

    public string Name { get; }

    public string Says { get; }

    [ObservableProperty]
    private bool _isChecked;
  }
}
