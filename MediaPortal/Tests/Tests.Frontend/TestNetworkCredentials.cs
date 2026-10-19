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
  /// The network credentials screen against a server of its own making. The real dictionary lives in the
  /// server's neighborhood plugin, so what these tests hold is the promise the screen makes around it: the
  /// paths arrive as rows and never carry a password, storing an entry writes once and says what came back,
  /// an empty password box means "keep it" while a typed one replaces, an empty user name is refused before
  /// anything is sent, removal speaks one path or all of them, and a plugin that is not on the server is
  /// quoted rather than worked around.
  /// </summary>
  [TestFixture]
  public class TestNetworkCredentials
  {
    /// <summary>A server that remembers nothing and answers what the test tells it to.</summary>
    private sealed class FakeCredentialsApi : INetworkCredentialsApi
    {
      public NetworkCredentialsAnswer ListAnswer = new NetworkCredentialsAnswer();
      public List<string> Calls { get; } = new List<string>();
      public NetworkCredentialWish? Stored;
      public string? RemovedPath;
      public Exception? ThrowOnGet;

      public Task<NetworkCredentialsAnswer> GetNetworkCredentialsAsync(CancellationToken cancellation = default)
      {
        Calls.Add("get");
        if (ThrowOnGet != null)
          throw ThrowOnGet;
        return Task.FromResult(ListAnswer);
      }

      public Task<NetworkCredentialsAnswer> StoreNetworkCredentialAsync(NetworkCredentialWish wish,
          CancellationToken cancellation = default)
      {
        Calls.Add("store:" + wish.Path);
        Stored = wish;
        return Task.FromResult(ListAnswer);
      }

      public Task<NetworkCredentialsAnswer> RemoveNetworkCredentialAsync(string path,
          CancellationToken cancellation = default)
      {
        Calls.Add("remove:" + path);
        RemovedPath = path;
        return Task.FromResult(ListAnswer);
      }
    }

    private static NetworkCredentialInfo Entry(string path, string user, bool hasPassword = true,
        string domain = "")
    {
      return new NetworkCredentialInfo { Path = path, UserName = user, HasPassword = hasPassword, Domain = domain };
    }

    [Test]
    public async Task TheDictionaryArrivesAsRowsAndNeverCarriesAPassword()
    {
      FakeCredentialsApi api = new FakeCredentialsApi
      {
        ListAnswer = new NetworkCredentialsAnswer
        {
          RootPath = "network://",
          Credentials = new[]
          {
            Entry("network://", "shareuser"),
            Entry("network://nas/private", "brother", hasPassword: false, domain: "NAS")
          }
        }
      };
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api);

      await model.LoadAsync();

      Assert.That(model.Credentials.Select(c => c.Path),
          Is.EqualTo(new[] { "network://", "network://nas/private" }));
      Assert.That(model.Credentials[0].Label, Does.Contain("(password set)"));
      Assert.That(model.Credentials[1].Label, Does.Contain("(no password)"));
      Assert.That(model.Credentials[1].Label, Does.Contain("NAS"), "the domain rides on the row that has one");
      Assert.That(model.RootPath, Is.EqualTo("network://"), "the root travels with the answer");
      Assert.That(model.Status, Does.Contain("2 credentials stand"), "and the screen says how many it read");
    }

    [Test]
    public async Task WithoutAServerAddressTheScreenSaysWhereToGo()
    {
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => null);
      await model.LoadAsync();
      Assert.That(model.Status, Does.Contain("Server address"), "the refusal names the row that fixes it");
    }

    [Test]
    public async Task AServerWithoutThePluginIsQuotedAndNotWorkedAround()
    {
      FakeCredentialsApi api = new FakeCredentialsApi
      {
        ThrowOnGet = new ServerApiException(new Uri("http://server/api/media/network-credentials"),
            System.Net.HttpStatusCode.ServiceUnavailable, "")
      };
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api);

      await model.LoadAsync();

      Assert.That(model.Status, Does.Contain("network neighborhood"),
          "a 503 is the plugin being absent, and the screen says so instead of showing a form that lies");
    }

    [Test]
    public async Task StoringSendsOneWishAndAnEmptyPasswordMeansKeepIt()
    {
      FakeCredentialsApi api = new FakeCredentialsApi
      {
        ListAnswer = new NetworkCredentialsAnswer { RootPath = "network://" }
      };
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api)
      {
        UserText = "shareuser",
        PasswordText = "",
        DomainText = "WORKGROUP"
      };

      await model.StoreAsync();

      Assert.That(api.Calls, Is.EqualTo(new[] { "store:" }), "one press, one wish, at the root when no path was typed");
      Assert.That(api.Stored, Is.Not.Null);
      Assert.That(api.Stored!.UserName, Is.EqualTo("shareuser"));
      Assert.That(api.Stored.Password, Is.Null, "an empty password box keeps the stored password, it never clears it");
      Assert.That(api.Stored.Domain, Is.EqualTo("WORKGROUP"));
    }

    [Test]
    public async Task ANamelessCredentialIsRefusedBeforeAnythingIsSent()
    {
      FakeCredentialsApi api = new FakeCredentialsApi();
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api) { UserText = "   " };

      await model.StoreAsync();

      Assert.That(api.Calls, Is.Empty, "the screen does not ask the server to store nonsense");
      Assert.That(model.Status, Does.Contain("user name"));
    }

    [Test]
    public async Task PickingAnEntryFillsTheEditorAndKeepsThePasswordBoxEmpty()
    {
      FakeCredentialsApi api = new FakeCredentialsApi
      {
        ListAnswer = new NetworkCredentialsAnswer
        {
          Credentials = new[] { Entry("network://nas", "shareuser", domain: "NAS") }
        }
      };
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api);
      await model.LoadAsync();

      model.Selected = model.Credentials[0];

      Assert.That(model.PathText, Is.EqualTo("network://nas"));
      Assert.That(model.UserText, Is.EqualTo("shareuser"));
      Assert.That(model.DomainText, Is.EqualTo("NAS"));
      Assert.That(model.PasswordText, Is.Empty,
          "the stored password never came over the wire, so there is nothing to fill the box with");
      Assert.That(model.SelectionSays, Does.Contain("has a password"));
    }

    [Test]
    public async Task RemovalSpeaksOnePathOrAllOfThem()
    {
      FakeCredentialsApi api = new FakeCredentialsApi
      {
        ListAnswer = new NetworkCredentialsAnswer
        {
          Credentials = new[] { Entry("network://nas", "shareuser") }
        }
      };
      NetworkCredentialsViewModel model = new NetworkCredentialsViewModel(() => api);
      await model.LoadAsync();

      model.Selected = model.Credentials[0];
      await model.RemoveSelectedAsync();
      Assert.That(api.RemovedPath, Is.EqualTo("network://nas"), "the picked row's path, no more");

      await model.RemoveAllAsync();
      Assert.That(api.RemovedPath, Is.Empty,
          "the empty path is the dictionary cleared - the reference's do-not-use-credentials answer");
      Assert.That(api.Calls, Does.Contain("remove:network://nas").And.Contain("remove:"));
    }
  }
}
