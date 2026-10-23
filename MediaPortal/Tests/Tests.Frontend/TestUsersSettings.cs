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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The users screen against a server of its own making. The real store is the server's - the same one
  /// the sign-in reads - so what these tests hold is the promise the screen makes around it: the list
  /// arrives as rows, every press writes once to the server and says what came back, an empty password
  /// box means "keep it" while its own button means "take it off", and a server that refuses is quoted
  /// rather than argued with.
  /// </summary>
  [TestFixture]
  public class TestUsersSettings
  {
    /// <summary>A server that remembers nothing and answers what the test tells it to.</summary>
    private sealed class FakeUsersApi : IUsersApi
    {
      public UsersAnswer ListAnswer = new UsersAnswer();
      public UserAnswer WriteAnswer = new UserAnswer();
      public List<string> Calls { get; } = new List<string>();
      public CreateUserWish? Created;
      public UpdateUserWish? Updated;
      public Guid Deleted;
      public Exception? ThrowOnGet;

      public Task<UsersAnswer> GetUsersAsync(CancellationToken cancellation = default)
      {
        Calls.Add("get");
        if (ThrowOnGet != null)
          throw ThrowOnGet;
        return Task.FromResult(ListAnswer);
      }

      public Task<UserAnswer> CreateUserAsync(CreateUserWish wish, CancellationToken cancellation = default)
      {
        Calls.Add("create");
        Created = wish;
        return Task.FromResult(WriteAnswer);
      }

      public Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
          CancellationToken cancellation = default)
      {
        Calls.Add("update:" + profileId.ToString("N"));
        Updated = wish;
        return Task.FromResult(WriteAnswer);
      }

      public Task<UserAnswer> DeleteUserAsync(Guid profileId, CancellationToken cancellation = default)
      {
        Calls.Add("delete:" + profileId.ToString("N"));
        Deleted = profileId;
        return Task.FromResult(WriteAnswer);
      }
    }

    private static UserInfo User(string name, bool hasPassword = true, bool isYou = false, string kind = "User")
    {
      return new UserInfo { ProfileId = Guid.NewGuid(), Name = name, HasPassword = hasPassword, IsYou = isYou, Kind = kind };
    }

    [Test]
    public async Task TheListArrivesAsRowsAndSaysWhoIsWho()
    {
      FakeUsersApi api = new FakeUsersApi
      {
        ListAnswer = new UsersAnswer
        {
          Users = new[]
          {
            User("Mother", isYou: true),
            User("Kids", hasPassword: false),
            User("Living room box", kind: "Client")
          }
        }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);

      await model.LoadAsync();

      Assert.That(model.Users.Select(u => u.Name), Is.EqualTo(new[] { "Mother", "Kids", "Living room box" }));
      Assert.That(model.Users[0].Label, Does.Contain("(you)"), "the screen says which entry you are");
      Assert.That(model.Users[1].Label, Does.Contain("(no PIN)"), "and which one walks in without numbers");
      Assert.That(model.Users[2].Label, Does.Contain("(machine)"), "and which entry is no person at all");
      Assert.That(model.Status, Does.Contain("3 user(s)"));
    }

    [Test]
    public async Task WithoutAServerAddressTheScreenSaysWhereToGo()
    {
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => null);
      await model.LoadAsync();
      Assert.That(model.Status, Does.Contain("Server address"), "the refusal names the row that fixes it");
    }

    [Test]
    public async Task AnOlderServerWithoutTheEndpointIsNamedAsOne()
    {
      FakeUsersApi api = new FakeUsersApi
      {
        ThrowOnGet = new ServerApiException(new Uri("http://server/api/users"),
            System.Net.HttpStatusCode.NotFound, "")
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);

      await model.LoadAsync();
      Assert.That(model.Status, Does.Contain("older build"), "a missing endpoint is an old server, not a broken one");
    }

    [Test]
    public async Task ANameWithoutAUserIsNotSent()
    {
      FakeUsersApi api = new FakeUsersApi();
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      model.NameText = "   ";

      await model.AddAsync();
      Assert.That(model.Status, Is.EqualTo("A user needs a name."));
      Assert.That(api.Calls, Does.Not.Contain("create"), "and nothing went over the wire");
    }

    [Test]
    public async Task AddingSendsTheTypedNameAndPinAndReloads()
    {
      FakeUsersApi api = new FakeUsersApi();
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      model.NameText = "Father";
      model.PasswordText = "4913";

      await model.AddAsync();

      Assert.That(api.Created!.Name, Is.EqualTo("Father"));
      Assert.That(api.Created!.Password, Is.EqualTo("4913"), "the server hashes it, not this screen");
      Assert.That(api.Calls, Is.EqualTo(new[] { "create", "get" }), "the list is read back after the write");
      Assert.That(model.Status, Does.Contain("'Father' was added"));
    }

    [Test]
    public async Task APinIsFourDigitsOrNothingAtAll()
    {
      FakeUsersApi api = new FakeUsersApi();
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      model.NameText = "Father";
      model.PasswordText = "kitchen-table";

      await model.AddAsync();
      Assert.That(model.Status, Does.Contain("four digits"),
          "the wake screen asks four numbers in four boxes - a longer secret could never be said there");
      Assert.That(api.Calls, Does.Not.Contain("create"), "and nothing went over the wire");

      model.PasswordText = "12x4";
      await model.AddAsync();
      Assert.That(model.Status, Does.Contain("digits, 0 to 9"));
      Assert.That(api.Calls, Does.Not.Contain("create"));

      model.PasswordText = "";
      await model.AddAsync();
      Assert.That(api.Calls, Does.Contain("create"), "no PIN at all stays a whole answer");
    }

    [Test]
    public async Task TheServersOwnOpinionAboutANameIsQuotedNotInvented()
    {
      FakeUsersApi api = new FakeUsersApi
      {
        WriteAnswer = new UserAnswer { Error = "There is already a user called 'Father'." }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      model.NameText = "Father";

      await model.AddAsync();
      Assert.That(model.Status, Does.Contain("already a user called 'Father'"),
          "the refusal is the store's sentence, passed through");
    }

    [Test]
    public async Task AnEmptyPasswordBoxOnSaveKeepsWhatTheUserHas()
    {
      Guid id = Guid.NewGuid();
      FakeUsersApi api = new FakeUsersApi
      {
        ListAnswer = new UsersAnswer { Users = new[] { new UserInfo { ProfileId = id, Name = "Mother", HasPassword = true } } }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      await model.LoadAsync();
      model.Selected = model.Users[0];

      Assert.That(model.PasswordText, Is.Empty, "a stored password never comes to this side of the wire");
      model.NameText = "Mother";
      await model.SaveSelectedAsync();

      Assert.That(api.Updated!.Password, Is.Null, "an empty box means keep, not remove");
      Assert.That(api.Calls, Does.Contain("update:" + id.ToString("N")));
    }

    [Test]
    public async Task TakingThePasswordOffIsItsOwnGesture()
    {
      Guid id = Guid.NewGuid();
      FakeUsersApi api = new FakeUsersApi
      {
        ListAnswer = new UsersAnswer { Users = new[] { new UserInfo { ProfileId = id, Name = "Mother" } } }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      await model.LoadAsync();
      model.Selected = model.Users[0];

      await model.RemovePasswordAsync();
      Assert.That(api.Updated!.Password, Is.Empty, "empty here is the answer itself, not an untouched box");
      Assert.That(model.Status, Does.Contain("without a PIN now"));
    }

    [Test]
    public async Task TheSignedInUserGetsTheServersRefusalNotAScreenInventedOne()
    {
      Guid you = Guid.NewGuid();
      FakeUsersApi api = new FakeUsersApi
      {
        ListAnswer = new UsersAnswer { Users = new[] { new UserInfo { ProfileId = you, Name = "Mother", IsYou = true } } },
        WriteAnswer = new UserAnswer { Error = "You cannot delete the user you are signed in as." }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      await model.LoadAsync();
      model.Selected = model.Users[0];

      await model.RemoveSelectedAsync();

      Assert.That(api.Deleted, Is.EqualTo(you), "the screen does ask, because the rule is the server's");
      Assert.That(model.Status, Does.Contain("cannot delete the user you are signed in as"),
          "and the refusal that shows is the one the server gave");
    }

    [Test]
    public async Task PickingAUserHandsItsNameToTheBoxesAndForgetsThePassword()
    {
      FakeUsersApi api = new FakeUsersApi
      {
        ListAnswer = new UsersAnswer
        {
          Users = new[] { new UserInfo { ProfileId = Guid.NewGuid(), Name = "Mother" },
                          new UserInfo { ProfileId = Guid.NewGuid(), Name = "Father" } }
        }
      };
      UsersSettingsViewModel model = new UsersSettingsViewModel(() => api);
      await model.LoadAsync();

      model.Selected = model.Users[1];
      Assert.That(model.NameText, Is.EqualTo("Father"));
      Assert.That(model.PasswordText, Is.Empty);
      Assert.That(model.HasSelection, Is.True);
      Assert.That(model.SelectionSays, Does.Contain("Father"));
    }
  }
}
