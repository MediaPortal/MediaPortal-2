namespace MediaPortal.Frontend.Caching;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

/// <summary>
/// The channel pictures, fetched once and kept for as long as the front end runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a screen does not ask the backend for a picture itself.</b> A guide shows a dozen channels and redraws
/// whenever anything moves; asked naively, that is a dozen pictures fetched again and again, from the same machine,
/// for a picture that never changes while somebody is watching. Everything in this front end that fetches a picture
/// from a machine does it through the same two pieces - a shelf on disk, so that the second run of the front end
/// does not ask the backend for anything it already has, and a small queue, so that a screen full of pictures does
/// not arrive as a hundred simultaneous requests - and a channel icon is such a picture. This class is those two
/// pieces pointed at a channel, with one addition of its own: the shelf remembers pictures by a number, and only
/// here is known which channel that number belongs to.
/// </para>
/// <para>
/// <b>Which machine the pictures come from.</b> Only the TV backend, at the address the backend itself gave
/// (<see cref="TvChannel.LogoPath"/>). MP1 did the same thing differently and did not mean it the same way: it kept
/// a picture collection of its own, downloaded a theme of them from a logo site, and matched a channel to a picture
/// by name. That is a second source of pictures to trust, to keep up to date, and to be connected to while watching
/// television, for an answer TVHeadend already gives. What that costs in pictures of better provenance is the price
/// of leaving it behind.
/// </para>
/// <para>
/// <b>What "no picture" means.</b> A channel the backend has no icon for is answered with <c>null</c> and the guide
/// draws that row without a column of white. A backend that is unreachable says so rather than being answered with
/// <c>null</c>, and the next guide asks again - because filing "the network was down" as "this channel has no icon"
/// would leave a guide without icons until the front end was restarted.
/// </para>
/// </remarks>
public sealed class ChannelLogos
{
  private readonly Func<TvBackendSettings> _settings;
  private readonly Func<TvBackendSettings, TvHeadendApiClient> _build;
  private readonly PictureCache? _cache;

  /// <summary>
  /// The machine the pictures come from and the holder of the pictures it has answered, both good for as long as
  /// the address they were built from is the address the settings name.
  /// </summary>
  private TvHeadendApiClient? _client;
  private Artwork? _pictures;
  private string _askedOf = "";

  /// <summary>Which channel one shelf key was asked for. Filled before the picture is asked for.</summary>
  private readonly Dictionary<Guid, TvChannel> _askedFor = new Dictionary<Guid, TvChannel>();

  private readonly Dictionary<string, Guid> _keys = new Dictionary<string, Guid>();

  /// <param name="settingsProvider">
  /// Where the TV machine's address comes from, read again whenever a picture is asked for - the same answer the
  /// guide itself is built from, so that a household writing down a new address in the settings gets its pictures
  /// from the new machine without a restart.
  /// </param>
  /// <param name="clientFactory">How to build the machine to ask. A test hands in a scripted one.</param>
  /// <param name="cache">
  /// The picture shelf, or null - every test, and a front end running without one - which leaves the pictures in
  /// memory for this run only, exactly as every other screen of this front end behaves without a shelf.
  /// </param>
  public ChannelLogos(Func<TvBackendSettings>? settingsProvider = null,
      Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null, PictureCache? cache = null)
  {
    _settings = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
    _build = clientFactory ?? delegate (TvBackendSettings settings)
    {
      return DemoTvBackend.Open(settings);
    };
    _cache = cache;
  }

  /// <summary>
  /// The machine to ask now, with the holder of its pictures beside it. Rebuilt when the address changed, because
  /// a shelf of pictures belonging to one TV machine is not a shelf of pictures belonging to another.
  /// </summary>
  private Artwork Current()
  {
    TvBackendSettings settings;
    try
    {
      settings = _settings();
    }
    catch (Exception)
    {
      settings = new TvBackendSettings();
    }

    string said = settings.BaseUrl?.ToString() ?? "";
    if (_pictures != null && string.Equals(said, _askedOf, StringComparison.OrdinalIgnoreCase))
      return _pictures;

    _client = _build(settings);
    Uri? server = _client.BaseUrl;
    _pictures = _cache != null && server != null
        ? _cache.ArtworkFor(KIND, server, Fetch)
        : new Artwork(Fetch, null, Artwork.MAX_REMEMBERED, Artwork.MAX_AT_ONCE);
    _askedOf = said;
    return _pictures;
  }

  /// <summary>What the pictures of this kind are called on the shelf.</summary>
  public const string KIND = "ChannelLogo";

  /// <summary>Whether there is any picture to be had for this channel at all.</summary>
  public bool Has(TvChannel? channel)
  {
    return channel != null && channel.HasLogo;
  }

  /// <summary>
  /// The key one channel's picture is kept under. Two channels whose backend gives the same picture share it,
  /// which is what a bouquet of regional stations with one shared icon looks like on the wire.
  /// </summary>
  public Guid KeyOf(TvChannel channel)
  {
    if (channel == null)
      throw new ArgumentNullException(nameof(channel));

    string path = channel.LogoPath ?? channel.Id;
    if (_keys.TryGetValue(path, out Guid known))
      return known;

    // The shelf keys everything by a number and has no idea what a channel is, so the picture's own address is
    // hashed into one. Not the channel's name: two channels can be called the same thing.
    byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(path));
    Guid key = new Guid(new ReadOnlySpan<byte>(digest, 0, 16));
    _keys[path] = key;
    return key;
  }

  /// <summary>
  /// The picture, if it is already here: memory first, and nothing is fetched. A screen redrawing a guide it drew a
  /// moment ago wants this, because waiting for a picture that is in the next room would be silly.
  /// </summary>
  public bool TryPeek(TvChannel? channel, out byte[]? picture)
  {
    picture = null;

    // Peeking never builds a machine to ask - if nothing has been fetched yet there is nothing here to peek at -
    // and it never answers from a machine the settings no longer name, because a picture of a channel belonging
    // to the old address is somebody else's picture.
    if (channel == null || !channel.HasLogo || _pictures == null)
      return false;

    if (!string.Equals(_askedOf, AddressNow(), StringComparison.OrdinalIgnoreCase))
      return false;

    return _pictures.TryPeek(KeyOf(channel), out picture);
  }

  /// <summary>The address the settings name now, or an empty string when they cannot be read.</summary>
  private string AddressNow()
  {
    try
    {
      return _settings().BaseUrl?.ToString() ?? "";
    }
    catch (Exception)
    {
      return "";
    }
  }

  /// <summary>
  /// The channel's picture: from memory or from the shelf when it is already known, otherwise fetched now.
  /// <c>null</c> when the channel has no picture or the backend has none for it.
  /// </summary>
  public async Task<byte[]?> PictureOfAsync(TvChannel? channel)
  {
    if (!Has(channel))
      return null;

    TvChannel one = channel!;
    Guid key = KeyOf(one);
    _askedFor[key] = one;
    return await Current().LoadAsync(key).ConfigureAwait(false);
  }

  /// <summary>
  /// Asks for one picture by its key, for a screen that got the key from <see cref="KeyOf"/> and would rather not
  /// be handed the channel again. A key never asked about is answered with no picture.
  /// </summary>
  public Task<byte[]?> LoadAsync(Guid key)
  {
    return Current().LoadAsync(key);
  }

  /// <summary>How many pictures are on the wire right now.</summary>
  public int Running
  {
    get { return _pictures == null ? 0 : _pictures.Running; }
  }

  private async Task<byte[]?> Fetch(Guid key)
  {
    TvHeadendApiClient? client = _client;
    if (client == null || !_askedFor.TryGetValue(key, out TvChannel? channel))
      return null;

    return await client.GetChannelLogoAsync(channel).ConfigureAwait(false);
  }
}
