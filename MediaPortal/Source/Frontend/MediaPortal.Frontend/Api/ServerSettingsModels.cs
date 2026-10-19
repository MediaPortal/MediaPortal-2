// Server settings over HTTP. Mirrors the answer of the server's SettingsController
// (GET/PUT api/media/settings) field for field; the names are what System.Text.Json
// camelCases them to, so a rename on either side without the other shows up as a
// field that silently stops arriving.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Api
{
    /// <summary>
    /// The two settings calls of <see cref="ServerApiClient"/> on their own, so a screen
    /// model can be handed a stub. <see cref="ServerApiClient"/> is sealed - it owns an
    /// HttpClient and its disposal - which is a reason to cut them out, not to work
    /// around it.
    /// </summary>
    public interface IServerSettingsApi
    {
        Task<ServerSettingsAnswer> GetServerSettingsAsync(CancellationToken cancellation = default);

        Task<ServerSettingsAnswer> PutServerSettingsAsync(ServerSettingsUpdate update,
            CancellationToken cancellation = default);
    }

    /// <summary>
    /// What the server has stored for its own behaviour: who may talk to it, which
    /// address it listens on, and whether the importer keeps the library fresh by
    /// itself. Read with <see cref="ServerApiClient.GetServerSettingsAsync"/>, changed
    /// with <see cref="ServerApiClient.PutServerSettingsAsync"/>.
    /// </summary>
    public sealed record ServerSettingsAnswer
    {
        /// <summary>Whether every API call but sign-in needs a login first.</summary>
        public bool WebAuthorizationEnabled { get; init; }

        /// <summary>
        /// The port stored on the server. Read-only in practice: this build listens on the
        /// shared port 55555 whatever is stored here, and says so in the screen's hint.
        /// </summary>
        public int HttpServerPort { get; init; }

        /// <summary>Whether the server answers on IPv4. Restart.</summary>
        public bool UseIPv4 { get; init; }

        /// <summary>Whether the server answers on IPv6. Restart.</summary>
        public bool UseIPv6 { get; init; }

        /// <summary>Whether only the listed addresses are bound. Restart.</summary>
        public bool LimitIpEndpoints { get; init; }

        /// <summary>The addresses bound when limited. Restart.</summary>
        public string IpAddressBindings { get; init; } = "";

        /// <summary>Whether the importer re-scans shares by itself.</summary>
        public bool ImporterEnabled { get; init; }

        /// <summary>
        /// How many hours the automatic import waits before its next pass. The server
        /// stores a fraction (0.5 is half an hour); a screen rounds to whatever it can ask for.
        /// </summary>
        public double ImporterStartTime { get; init; }

        /// <summary>
        /// Which of the changed fields the server said it needs a restart for. Only ever
        /// filled in the answer to a PUT; an empty list means the changes take effect as
        /// they are.
        /// </summary>
        public IList<string> RestartNeededFor { get; init; } = new List<string>();
    }

    /// <summary>
    /// The change to send with a PUT. Every field carries its full new value - the
    /// server overwrites the settings object it has with what arrives, so a field left
    /// out of the JSON would arrive as its default and be stored as that. Screens read
    /// first, change, and send the whole thing back.
    /// </summary>
    public sealed record ServerSettingsUpdate
    {
        public bool WebAuthorizationEnabled { get; init; }
        public int HttpServerPort { get; init; }
        public bool UseIPv4 { get; init; }
        public bool UseIPv6 { get; init; }
        public bool LimitIpEndpoints { get; init; }
        public string IpAddressBindings { get; init; } = "";
        public bool ImporterEnabled { get; init; }
        public double ImporterStartTime { get; init; }
    }
}
