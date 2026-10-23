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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// The household over HTTP: the role the reference derives rather than stores, the three profile
  /// templates it ships, and the floor this server puts under them - at least one administrator, always.
  /// The store is a dictionary wearing the interface the real store wears; everything between the endpoints
  /// and it is the controller's own code, so what these tests hold is the endpoints themselves.
  /// </summary>
  [TestFixture]
  public class TestUsersOverHttp
  {
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();

    private string _baseUrl;
    private InMemoryStore _store;

    [TearDown]
    public void StopEverything()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();

      ServiceRegistration.Remove<ISystemResolver>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ILogger>();
      ServiceRegistration.Remove<IMediaLibrary>();
    }

    private void StartServer()
    {
      ServiceRegistration.Set<ILogger>(new DefaultLogger(new StringWriter(), LogLevel.All, false, true));

      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>();
      resolver.SetupGet(r => r.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      ServiceRegistration.Set<IMediaLibrary>(new Mock<IMediaLibrary>().Object);

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>())
          .Returns(new ServerSettings { WebAutorizationEnabled = false, HttpServerPort = 55555 });
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);

      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(_store.Mock.Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(MediaController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;
    }

    static int FreePort()
    {
      TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    static readonly JsonSerializerOptions Json = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    static async Task<(HttpStatusCode Code, string Body)> Get(string url)
    {
      using (HttpClient client = new HttpClient())
      using (HttpResponseMessage response = await client.GetAsync(url))
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Code, string Body)> Send(string method, string url, string json)
    {
      using (HttpClient client = new HttpClient())
      using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(
          new HttpMethod(method), url)
      {
        Content = json == null ? null : new StringContent(json, Encoding.UTF8, "application/json")
      }))
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// A dictionary wearing <see cref="IUserProfileDataManagement"/>. The profiles are real
    /// <see cref="UserProfile"/> objects, so the restriction getters the controller reads are the
    /// reference's own code over the keys the controller's own writes leave behind: "0" under "allow all
    /// ages" means restricted, exactly as the reference's user editor stored it.
    /// </summary>
    private sealed class InMemoryStore
    {
      private readonly Dictionary<Guid, UserProfile> _profiles = new Dictionary<Guid, UserProfile>();

      public Mock<IUserProfileDataManagement> Mock { get; } = new Mock<IUserProfileDataManagement>();

      public InMemoryStore()
      {
        Mock.Setup(m => m.GetProfilesAsync())
            .Returns(() => Task.FromResult<ICollection<UserProfile>>(_profiles.Values.ToList()));
        Mock.Setup(m => m.GetProfileAsync(It.IsAny<Guid>()))
            .Returns((Guid id) => Task.FromResult(Find(id)));
        Mock.Setup(m => m.GetProfileByNameAsync(It.IsAny<string>()))
            .Returns((string name) => Task.FromResult(FindByName(name)));
        Mock.Setup(m => m.CreateProfileAsync(It.IsAny<string>(), It.IsAny<UserProfileType>(),
                It.IsAny<string>()))
            .Returns((string name, UserProfileType type, string password) =>
            {
              UserProfile profile = new UserProfile(Guid.NewGuid(), name, type, password, null, null);
              _profiles[profile.ProfileId] = profile;
              return Task.FromResult(profile.ProfileId);
            });
        Mock.Setup(m => m.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<UserProfileType>(), It.IsAny<string>()))
            .Returns((Guid id, string name, UserProfileType type, string password) =>
            {
              if (!_profiles.TryGetValue(id, out UserProfile current))
                return Task.FromResult(false);
              UserProfile updated = new UserProfile(id, name, type, password,
                  current.LastLogin, current.Image);
              foreach (KeyValuePair<string, IDictionary<int, string>> key in current.AdditionalData)
                updated.AdditionalData[key.Key] = key.Value;
              _profiles[id] = updated;
              return Task.FromResult(true);
            });
        Mock.Setup(m => m.DeleteProfileAsync(It.IsAny<Guid>()))
            .Returns((Guid id) => Task.FromResult(_profiles.Remove(id)));
        Mock.Setup(m => m.SetProfileImageAsync(It.IsAny<Guid>(), It.IsAny<byte[]>()))
            .Returns((Guid id, byte[] image) =>
            {
              if (!_profiles.TryGetValue(id, out UserProfile profile))
                return Task.FromResult(false);
              profile.Image = image;
              return Task.FromResult(true);
            });
        Mock.Setup(m => m.SetUserAdditionalDataAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>()))
            .Returns((Guid id, string key, string data, int dataNo) =>
            {
              if (!_profiles.TryGetValue(id, out UserProfile profile))
                return Task.FromResult(false);
              if (!profile.AdditionalData.TryGetValue(key, out IDictionary<int, string> values))
              {
                values = new Dictionary<int, string>();
                profile.AdditionalData[key] = values;
              }
              values[dataNo] = data;
              return Task.FromResult(true);
            });
        Mock.Setup(m => m.ClearUserAdditionalDataKeyAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns((Guid id, string key) =>
            {
              if (!_profiles.TryGetValue(id, out UserProfile profile))
                return Task.FromResult(false);
              profile.AdditionalData.Remove(key);
              return Task.FromResult(true);
            });
      }

      public UserProfile Add(string name, UserProfileType type = UserProfileType.UserProfile,
          string password = null, bool restrictAges = false, int? age = null,
          bool restrictShares = false, bool enableGroups = false, params string[] groups)
      {
        UserProfile profile = new UserProfile(Guid.NewGuid(), name, type, password, null, null);
        profile.AdditionalData[UserDataKeysKnown.KEY_ALLOW_ALL_AGES] =
            new Dictionary<int, string> { { 0, restrictAges ? "0" : "1" } };
        profile.AdditionalData[UserDataKeysKnown.KEY_ALLOW_ALL_SHARES] =
            new Dictionary<int, string> { { 0, restrictShares ? "0" : "1" } };
        profile.AdditionalData[UserDataKeysKnown.KEY_ENABLE_RESTRICTION_GROUPS] =
            new Dictionary<int, string> { { 0, enableGroups ? "1" : "0" } };
        if (age.HasValue)
          profile.AdditionalData[UserDataKeysKnown.KEY_ALLOWED_AGE] =
              new Dictionary<int, string> { { 0, age.Value.ToString() } };
        if (groups.Length > 0)
        {
          Dictionary<int, string> values = new Dictionary<int, string>();
          int no = 0;
          foreach (string group in groups)
            values[++no] = group;
          profile.AdditionalData[UserDataKeysKnown.KEY_RESTRICTION_GROUPS] = values;
        }
        _profiles[profile.ProfileId] = profile;
        return profile;
      }

      public UserProfile ReRead(Guid id)
      {
        return _profiles[id];
      }

      public bool Holds(Guid id)
      {
        return _profiles.ContainsKey(id);
      }

      private AsyncResult<UserProfile> Find(Guid id)
      {
        return _profiles.TryGetValue(id, out UserProfile profile)
            ? new AsyncResult<UserProfile> { Success = true, Result = profile }
            : new AsyncResult<UserProfile> { Success = false };
      }

      private AsyncResult<UserProfile> FindByName(string name)
      {
        UserProfile profile = _profiles.Values.FirstOrDefault(
            p => string.Equals(p.Name, name, StringComparison.Ordinal));
        return profile == null
            ? new AsyncResult<UserProfile> { Success = false }
            : new AsyncResult<UserProfile> { Success = true, Result = profile };
      }
    }

    // ---- the derived role ------------------------------------------------------------------------------------

    [Test]
    public async Task TheGrownUpWhoRestrictsNothingIsTheHouseholdsAdministrator()
    {
      _store = new InMemoryStore();
      _store.Add("Gerrit", password: "1234");
      _store.Add("Martina", restrictAges: true, age: 12);
      _store.Add("Wohnzimmer", type: UserProfileType.ClientProfile);
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/users");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserListResult read =
          JsonSerializer.Deserialize<UsersController.UserListResult>(body, Json);

      Assert.That(read.Users.Single(u => u.Name == "Gerrit").IsAdmin, Is.True,
          "a PIN is not a restriction - the reference counts the unrestricted person the administrator");
      Assert.That(read.Users.Single(u => u.Name == "Martina").IsAdmin, Is.False);
      Assert.That(read.Users.Single(u => u.Name == "Wohnzimmer").IsAdmin, Is.False,
          "a machine's profile is never a person, let alone the household's grown-up");
    }

    // ---- the profile templates -------------------------------------------------------------------------------

    [Test]
    public async Task TheServerStatesTheThreeKindsTheReferenceShips()
    {
      _store = new InMemoryStore();
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/users/templates");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.ProfileTemplateResult read =
          JsonSerializer.Deserialize<UsersController.ProfileTemplateResult>(body, Json);

      Assert.That(read.Templates.Select(t => t.Name), Is.EqualTo(new[] { "Admin", "Teenager", "Child" }));
      Assert.That(read.Templates[0].TemplateId,
          Is.EqualTo(new Guid("{0D5E6C2E-07DA-4427-964F-A92C497BCE04}")),
          "the reference's own ids, so a household that grew from its editor keeps its kinds");
      Assert.That(read.Templates[1].AllowedAge, Is.EqualTo(13));
      Assert.That(read.Templates[1].RestrictionGroups,
          Is.EquivalentTo(new[] { "Settings", "Settings.UserProfile.ManageOwn" }));
      Assert.That(read.Templates[2].AllowedAge, Is.EqualTo(5));
    }

    [Test]
    public async Task APersonAddedAsATeenagerCarriesTheTeenagersRulesAndName()
    {
      _store = new InMemoryStore();
      StartServer();

      (HttpStatusCode code, string body) = await Send("POST", _baseUrl + "/api/users",
          "{\"name\":\"Max\",\"password\":\"4711\",\"templateId\":\"56477003-A827-461D-92CB-E91FADD69B7D\","
          + "\"restrictions\":{\"restrictAges\":true,\"allowedAge\":13,\"enableRestrictionGroups\":true,"
          + "\"restrictionGroups\":\"Settings,Settings.UserProfile.ManageOwn\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserResult created =
          JsonSerializer.Deserialize<UsersController.UserResult>(body, Json);

      Assert.That(created.User.TemplateId, Is.EqualTo("56477003-a827-461d-92cb-e91fadd69b7d"));
      Assert.That(created.User.RestrictAges, Is.True);
      Assert.That(created.User.AllowedAge, Is.EqualTo(13));
      Assert.That(created.User.IsAdmin, Is.False, "a teenager is not the household's grown-up");

      UserProfile stored = _store.ReRead(created.User.ProfileId);
      Assert.That(stored.TemplateId, Is.EqualTo(new Guid("{56477003-A827-461D-92CB-E91FADD69B7D}")),
          "the same additional-data key the reference's own profiles carry it under");
    }

    // ---- the floor: one household, one administrator at the very least -----------------------------------------

    [Test]
    public async Task TheLastAdministratorCannotBeDeletedOutOfItsHousehold()
    {
      _store = new InMemoryStore();
      UserProfile grownUp = _store.Add("Gerrit", password: "1234");
      UserProfile kid = _store.Add("Max", restrictAges: true, age: 9);
      StartServer();

      (HttpStatusCode code, string body) = await Send("DELETE",
          _baseUrl + "/api/users/" + kid.ProfileId.ToString("N"), null);
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      (code, body) = await Send("DELETE", _baseUrl + "/api/users/" + grownUp.ProfileId.ToString("N"), null);
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("administrator"));
      Assert.That(_store.Holds(grownUp.ProfileId), Is.True,
          "the refusal is kept as well - the household still has its grown-up");
    }

    [Test]
    public async Task TheLastAdministratorCannotBeRestrictedIntoNobody()
    {
      _store = new InMemoryStore();
      UserProfile grownUp = _store.Add("Gerrit", password: "1234");
      StartServer();

      (HttpStatusCode code, string body) = await Send("PUT",
          _baseUrl + "/api/users/" + grownUp.ProfileId.ToString("N"),
          "{\"restrictions\":{\"restrictAges\":true,\"allowedAge\":16}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("administrator"));

      UserProfile stored = _store.ReRead(grownUp.ProfileId);
      Assert.That(stored.RestrictAges, Is.False, "a refused edit leaves not one key behind");
    }

    [Test]
    public async Task OnceTheHouseholdHasTwoGrownUpsOneOfThemCanBeLowered()
    {
      _store = new InMemoryStore();
      UserProfile first = _store.Add("Gerrit", password: "1234");
      UserProfile second = _store.Add("Martina");
      StartServer();

      (HttpStatusCode code, string body) = await Send("PUT",
          _baseUrl + "/api/users/" + first.ProfileId.ToString("N"),
          "{\"restrictions\":{\"restrictAges\":true,\"allowedAge\":16}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserResult changed =
          JsonSerializer.Deserialize<UsersController.UserResult>(body, Json);
      Assert.That(changed.User.IsAdmin, Is.False);

      (code, body) = await Get(_baseUrl + "/api/users");
      UsersController.UserListResult read =
          JsonSerializer.Deserialize<UsersController.UserListResult>(body, Json);
      Assert.That(read.Users.Single(u => u.Name == "Martina").IsAdmin, Is.True,
          "the one who stays unrestricted keeps the household standing");
      Assert.That(second.Name, Is.EqualTo("Martina"));
    }

    [Test]
    public async Task AHouseholdThatAlreadyLostItsGrownUpIsNotLockedOutOfFindingOne()
    {
      // The guard refuses changes that TAKE the last administrator; it never blocks the way back. In a
      // store holding only restricted people, lowering, deleting and adding unrestricted ones all stay
      // possible - otherwise a wrong tick in the users screen could never be undone from that screen.
      _store = new InMemoryStore();
      UserProfile kid = _store.Add("Max", restrictAges: true, age: 9);
      StartServer();

      (HttpStatusCode code, string body) = await Send("PUT",
          _baseUrl + "/api/users/" + kid.ProfileId.ToString("N"),
          "{\"restrictions\":{\"restrictAges\":true,\"allowedAge\":16}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      (code, body) = await Send("POST", _baseUrl + "/api/users", "{\"name\":\"GrownUp\"}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserResult added =
          JsonSerializer.Deserialize<UsersController.UserResult>(body, Json);
      Assert.That(added.User.IsAdmin, Is.True);

      (code, body) = await Send("DELETE", _baseUrl + "/api/users/" + kid.ProfileId.ToString("N"), null);
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
    }

    [Test]
    public async Task AKindIsRememberedAndCanBeTakenAway()
    {
      _store = new InMemoryStore();
      UserProfile kid = _store.Add("Max", restrictAges: true, age: 9);
      StartServer();

      (HttpStatusCode code, string body) = await Send("PUT",
          _baseUrl + "/api/users/" + kid.ProfileId.ToString("N"),
          "{\"templateId\":\"24172540-6A51-49E7-B95C-A41D3DBECCCE\"}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserResult asChild =
          JsonSerializer.Deserialize<UsersController.UserResult>(body, Json);
      Assert.That(asChild.User.TemplateId, Is.EqualTo("24172540-6a51-49e7-b95c-a41d3dbeccce"));
      Assert.That(asChild.User.RestrictAges, Is.True,
          "naming the kind does not rewrite the rules - the boxes alone say what is enforced");

      (code, body) = await Send("PUT", _baseUrl + "/api/users/" + kid.ProfileId.ToString("N"),
          "{\"templateId\":\"\"}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      UsersController.UserResult withoutKind =
          JsonSerializer.Deserialize<UsersController.UserResult>(body, Json);
      Assert.That(withoutKind.User.TemplateId, Is.Null);
    }
  }
}
