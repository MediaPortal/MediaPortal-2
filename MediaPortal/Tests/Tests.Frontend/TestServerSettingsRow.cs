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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
    /// <summary>
    /// The server-settings row's model, without a server in sight. What the API itself
    /// answers is tested against the real controller in Tests.Server; what matters here
    /// is that the row never sends anything it has not read, and says the right thing
    /// when there is nothing to read from.
    /// </summary>
    [TestFixture]
    public class TestServerSettingsRow
    {
        [Test]
        public async Task ARowThatNeverLoadedRefusesToSave()
        {
            // A form of default values, saved onto a server whose settings were never
            // read, would silently rewrite them. The row has to know it knows nothing.
            ServerSettingsViewModel model = new ServerSettingsViewModel(
                () => { Assert.Fail("must not talk to a server before it loaded"); return null; });

            await model.SaveCommand.ExecuteAsync(null);

            Assert.That(model.Status, Does.Contain("Nothing loaded"),
                "an empty form refuses to be a wish");
        }

        [Test]
        public void LoadFromAppliesWhatTheServerAnswered()
        {
            ServerSettingsViewModel model = new ServerSettingsViewModel(() => null);
            model.LoadFrom(new ServerSettingsAnswer
            {
                WebAuthorizationEnabled = true,
                HttpServerPort = 4711,
                UseIPv4 = true,
                UseIPv6 = false,
                LimitIpEndpoints = true,
                IpAddressBindings = "127.0.0.1",
                ImporterEnabled = false,
                ImporterStartTime = 1.5
            });

            Assert.That(model.WebAuth, Is.True);
            Assert.That(model.Port, Is.EqualTo(4711));
            Assert.That(model.UseIPv6, Is.False);
            Assert.That(model.Bindings, Is.EqualTo("127.0.0.1"));
            Assert.That(model.Importer, Is.False);
            // The server stores fractions; the row offers whole hours and shows the
            // nearest one rather than a precision it cannot send back.
            Assert.That(model.ImporterHours, Is.EqualTo(2));
        }

        [Test]
        public async Task ReloadWithoutAServerAddressSaysSo()
        {
            // The usual state right after a first start: nobody has named a server yet,
            // and "no server" is a different sentence from "the server refused".
            ServerSettingsViewModel model = new ServerSettingsViewModel(() => null);

            await model.ReloadCommand.ExecuteAsync(null);
            Assert.That(model.Status, Does.Contain("No server address"),
                "the row points at the section that fixes it");

            await model.SaveCommand.ExecuteAsync(null);
            Assert.That(model.Status, Does.Contain("Nothing loaded"),
                "saving a row that never loaded is refused before the address is even asked for");
        }

        [Test]
        public async Task SavingAStoredAddressChangeNamesItAsRestartOnly()
        {
            // The answer carries the restart list; the row must hand it over rather than
            // claim "Saved." for a value the running server has not picked up yet.
            StubClient client = new StubClient();
            ServerSettingsViewModel model = new ServerSettingsViewModel(() => client);
            model.LoadFrom(new ServerSettingsAnswer { UseIPv6 = false });
            model.UseIPv6 = true;

            await model.SaveCommand.ExecuteAsync(null);

            Assert.That(client.PutCalled, Is.True);
            Assert.That(client.LastUseIPv6, Is.True, "what the row shows is what it sends");
            Assert.That(model.UseIPv6, Is.True, "what the server echoes is what the row shows");
            Assert.That(model.Status, Does.Contain("restart"), "a stored-but-inert change is admitted");
            Assert.That(model.Status, Does.Contain("use_ipv6"));
        }

        /// <summary>
        /// A client that answers a PUT with what it was sent plus the standing restart
        /// note for the port. Everything else the row does with a real client is the
        /// real client's own tested business.
        /// </summary>
        private sealed class StubClient : IServerSettingsApi
        {
            public bool PutCalled { get; private set; }
            public ServerSettingsUpdate? LastUpdate { get; private set; }
            public bool LastUseIPv6 { get; private set; }
 
            public Task<ServerSettingsAnswer> GetServerSettingsAsync(CancellationToken cancellation = default)
            {
                Assert.Fail("this row only ever puts"); return Task.FromResult(new ServerSettingsAnswer());
            }
 
            public Task<ServerSettingsAnswer> PutServerSettingsAsync(ServerSettingsUpdate update,
                CancellationToken cancellation = default)
            {
                PutCalled = true;
                LastUpdate = update;
                LastUseIPv6 = update.UseIPv6;
                return Task.FromResult(update.UseIPv6
                    ? new ServerSettingsAnswer { UseIPv6 = true, RestartNeededFor = new List<string> { "use_ipv6" } }
                    : new ServerSettingsAnswer { UseIPv6 = false });
            }
        }
    }
}
