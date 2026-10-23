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

        Task<LoginPolicyAnswer> GetLoginPolicyAsync(CancellationToken cancellation = default);

        Task<LoginPolicyAnswer> PutLoginPolicyAsync(LoginPolicyWish wish,
            CancellationToken cancellation = default);
    }

    /// <summary>
    /// The household's login policy as the server keeps it: whether anybody is asked who is watching, who is
    /// signed in without being asked, and whether an untouched television gives its user back. The five fields
    /// ride on the server's own settings answer - the reference kept them Global for exactly this reason, so
    /// every television in the house is governed by the same rules.
    /// </summary>
    public sealed class LoginPolicyAnswer
    {
        /// <summary>On, like the server's own default: a house with names asks who is watching.</summary>
        public bool EnableUserLogin { get; init; } = true;
        public bool EnableUserLoginScreen { get; init; } = true;

        /// <summary>The person signed in without being asked. Empty for nobody.</summary>
        public string AutoLoginUser { get; init; } = "";

        public bool AutoLogoutEnabled { get; init; }
        public int AutoLogoutIdleTimeoutInMin { get; init; } = 30;

        /// <summary>The server's reason when it refused, in words a screen can show. Null when nothing refused.</summary>
        public string? Error { get; init; }
    }

    /// <summary>
    /// The change to send with <see cref="IServerSettingsApi.PutLoginPolicyAsync"/>. Every field is optional;
    /// what is null stays as the server has it, so a screen that owns only the login rows cannot undo the
    /// importer's schedule by forgetting it.
    /// </summary>
    public sealed class LoginPolicyWish
    {
        public bool? EnableUserLogin { get; init; }
        public bool? EnableUserLoginScreen { get; init; }

        /// <summary>The person to sign in without asking. The empty string means nobody.</summary>
        public string? AutoLoginUser { get; init; }

        public bool? AutoLogoutEnabled { get; init; }
        public int? AutoLogoutIdleTimeoutInMin { get; init; }
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

        /// <summary>Whether the household has to say who is watching. Off means nobody is asked.</summary>
        public bool EnableUserLogin { get; init; }

        /// <summary>Whether the screen that asks is shown when a person is expected.</summary>
        public bool EnableUserLoginScreen { get; init; }

        /// <summary>The person signed in without being asked. Empty for nobody.</summary>
        public string AutoLoginUser { get; init; } = "";

        /// <summary>Whether an untouched television gives its user back to the household.</summary>
        public bool AutoLogoutEnabled { get; init; }

        /// <summary>How many minutes of nothing it takes.</summary>
        public int AutoLogoutIdleTimeoutInMin { get; init; } = 30;

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

    /// <summary>
    /// The metadata extractors' settings call pair, on its own interface for the same reason
    /// <see cref="IServerSettingsApi"/> exists: a screen model can be handed a stub, and a server
    /// build without the endpoint is discovered by this pair failing, not the older one's.
    /// </summary>
    public interface IMetadataSettingsApi
    {
        Task<MetadataSettingsAnswer> GetMetadataSettingsAsync(CancellationToken cancellation = default);

        Task<MetadataSettingsAnswer> PutMetadataSettingsAsync(MetadataSettingsWish wish,
            CancellationToken cancellation = default);
    }

    /// <summary>
    /// The switches of one extractor, and of the online libraries. Every field is nullable: a null in
    /// the answer means the server has no such switch to speak of - the extractor is not installed, or
    /// this kind of media has no such flag - and a screen shows no row for it. A null in a wish means
    /// "leave this one as it is".
    /// </summary>
    public sealed record MetadataFlags
    {
        public bool? SkipOnlineSearches { get; init; }
        public bool? SkipFanArtDownload { get; init; }
        public bool? OnlyLocalMedia { get; init; }
        public bool? CacheOfflineFanArt { get; init; }
        public bool? CacheLocalFanArt { get; init; }
        public bool? CacheOfflineMovieFanArt { get; init; }
        public bool? CacheLocalMovieFanArt { get; init; }
        public bool? CacheOfflineSeriesFanArt { get; init; }
        public bool? CacheLocalSeriesFanArt { get; init; }

        // Whose details the extractor is asked for, and how many people of a kind it keeps. Null everywhere the
        // server's extractor has no such property, which is why the music block has no counts and the movie
        // block no television network. The range of a count is the endpoint's own - zero to a hundred - and the
        // screen repeats it on the row, because a number refused by the server is a number typed twice.
        public bool? IncludeActorDetails { get; init; }
        public bool? IncludeCharacterDetails { get; init; }
        public bool? IncludeDirectorDetails { get; init; }
        public bool? IncludeWriterDetails { get; init; }
        public bool? IncludeTVNetworkDetails { get; init; }
        public bool? IncludeProductionCompanyDetails { get; init; }
        public bool? IncludeArtistDetails { get; init; }
        public bool? IncludeComposerDetails { get; init; }
        public bool? IncludeMusicLabelDetails { get; init; }
        public bool? IncludeConductorDetails { get; init; }
        public int? MaximumActorCount { get; init; }
        public int? MaximumCharacterCount { get; init; }

        // The online libraries' own fields; null everywhere else.
        public bool? OnlyBasicFanArt { get; init; }
        public bool? UseSecureWebCommunication { get; init; }
        public bool? UseMovieAudioLanguageIfUnmatched { get; init; }
        public bool? UseSeriesAudioLanguageIfUnmatched { get; init; }
        public bool? UseMusicAudioLanguageIfUnmatched { get; init; }
        public string? MovieLanguageCulture { get; init; }
        public string? SeriesLanguageCulture { get; init; }
        public string? MusicLanguageCulture { get; init; }
        public string? SubtitleLanguageCulture { get; init; }
    }

    /// <summary>
    /// What the extractors are configured to do, per kind of media. A block is null when the server
    /// does not have that extractor at all.
    /// </summary>
    public sealed record MetadataSettingsAnswer
    {
        public MetadataFlags? Movie { get; init; }
        public MetadataFlags? Series { get; init; }
        public MetadataFlags? Audio { get; init; }
        public MetadataFlags? Video { get; init; }
        public MetadataFlags? Subtitle { get; init; }
        public MetadataFlags? OnlineLibraries { get; init; }

        /// <summary>
        /// Always true: these settings are read while the extractors work, so a change speaks to the
        /// next import pass rather than to the library as it stands.
        /// </summary>
        public bool NextImportPass { get; init; }
    }

    /// <summary>The change to send; every field left null stays as it is on the server.</summary>
    public sealed record MetadataSettingsWish
    {
        public MetadataFlags? Movie { get; init; }
        public MetadataFlags? Series { get; init; }
        public MetadataFlags? Audio { get; init; }
        public MetadataFlags? Video { get; init; }
        public MetadataFlags? Subtitle { get; init; }
        public MetadataFlags? OnlineLibraries { get; init; }
    }
}
