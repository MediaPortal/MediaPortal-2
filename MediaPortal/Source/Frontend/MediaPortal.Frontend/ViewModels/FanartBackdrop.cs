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
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The room's own picture: the fanart of whoever the caret stands on, laid behind a whole screen, dimmed by the
  /// worn skin's overlay and thrown out of focus by however far that skin asks.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the law the shelf of titles already lives by (<c>MediaBrowserViewModel</c>'s fanart), written down in a
  /// file of its own so that a second screen wearing the shelf's chrome wears its room too.  The law, in short: the
  /// picture follows the caret one name at a time; it is asked for only once the caret has stood still for the
  /// reference's half second (<c>ImageSourceWrapper Delay="0.5"</c> on that skin's background image), because a
  /// viewer walking a wall covers a hundred names in the time one backdrop takes to travel and at most the last one
  /// is ever worn - what is worn while they walk is the picture they came from, not a blank room; a picture the
  /// machine remembers costs no second question, so walking back asks nothing; and both the reading of the bytes and
  /// the blur happen away from the thread that draws, so that the room changes its mood between two frames rather
  /// than inside one.
  /// </para>
  /// <para>
  /// Who the picture is asked for is the caller's business: a shelf asks a title (and a season of one), a wall of
  /// names asks a person.  That is all the two screens differ in.
  /// </para>
  /// </remarks>
  public sealed class FanartBackdrop : INotifyPropertyChanged
  {
    /// <summary>
    /// The reference's <c>Delay</c>: how long a name has to be stood on before its picture is asked for at all.
    /// </summary>
    internal static readonly TimeSpan SETTLING = TimeSpan.FromMilliseconds(500);

    /// <summary>The holder of these pictures, or null with no server to ask.</summary>
    private readonly Artwork? _pictures;

    /// <summary>The name whose picture the room is meant to wear, or none.</summary>
    private Guid _for = Guid.Empty;

    /// <summary>Counters: a walk puts question after question on the same road, and only the last one may land.</summary>
    private int _stillTurn;

    private int _wearTurn;

    /// <summary>The picture as it was fetched, so that a dial turned live can re-wear it without asking again.</summary>
    private byte[]? _raw;

    /// <summary>Whether what the room wears is a blurred COPY, which is what decides the second dial.</summary>
    private bool _baked;

    private double _blur;

    private SkinSettingsBase? _skin;

    public FanartBackdrop(Artwork? pictures)
    {
      _pictures = pictures;
      ReadSkinSettings();

      // A setting the viewer flips on the skin-settings screen while this room stands takes its effect on the
      // television at once - the screen is a remote control, not an application form.
      SkinSettingsBase.Saved += OnSkinSettingsSaved;
    }

    /// <summary>Whether the worn skin lets a room wear a picture at all - the reference's <c>EnableFanart</c>.</summary>
    public bool FanartShown { get; private set; }

    /// <summary>
    /// How far the skin dims the picture: the overlay is the picture's OWN opacity, so the setting dims the fanart
    /// towards the ground rather than dropping a veil over everything.
    /// </summary>
    public double FanartOverlay { get; private set; }

    /// <summary>
    /// How far the picture still has to be thrown out of focus where it stands.  A copy that arrives already
    /// blurred is worn at nought, since asking such a copy for the blur again would blur it twice.
    /// </summary>
    public double FanartBlurRadius
    {
      get { return _baked ? 0 : _blur; }
    }

    /// <summary>What the room wears: the bytes of the stood name's picture, or null for the plain ground.</summary>
    public byte[]? BackgroundArtwork { get; private set; }

    /// <summary>Whether there is a picture to draw - the layer asks this rather than testing the bytes.</summary>
    public bool HasBackgroundArtwork
    {
      get { return BackgroundArtwork != null; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The room is asked to wear the picture of one name: at once when the machine already has it, and after the
    /// standing-still half second when it has to be asked for.  An id nobody stands on any more takes the picture
    /// away, because a room going on about a name the caret left is a screen lying about what is stood on.
    /// </summary>
    public void WearFor(Guid id)
    {
      Artwork? pictures = _pictures;
      if (!FanartShown || pictures == null || id == Guid.Empty)
      {
        _for = Guid.Empty;
        _ = WearAsync(null);
        return;
      }

      _for = id;
      if (pictures.TryPeek(id, out byte[]? held))
      {
        // Remembered, so wearing it costs nothing and the room may change its mood in this very breath.
        _ = WearAsync(held);
        return;
      }

      _ = AskWhenStillAsync(pictures, id);
    }

    /// <summary>Takes the room's picture away, which is what a caret walking off the wall asks for.</summary>
    public void Quiet()
    {
      _for = Guid.Empty;
      _stillTurn++;
      _ = WearAsync(null);
    }

    /// <summary>The name this room is wearing its picture for, which a test and a trace both ask about.</summary>
    internal Guid Wears => _for;

    /// <summary>
    /// Asks the server for one name's picture once the caret has stood on it for <see cref="SETTLING"/>, and asks
    /// for nothing at all when the caret walked on.
    /// </summary>
    private async Task AskWhenStillAsync(Artwork pictures, Guid id)
    {
      int turn = ++_stillTurn;

      //  Waited without ConfigureAwait(false) on purpose: what follows touches what the screen is bound to, and
      //  that belongs to the thread that drew the first frame.
      await Task.Delay(SETTLING);

      //  Walked on - and the name behind this one already put its own question on the same counter.
      if (turn != Volatile.Read(ref _stillTurn) || _for != id)
        return;

      try
      {
        byte[]? picture = await pictures.LoadAsync(id);
        if (_for == id)
          _ = WearAsync(picture);
      }
      catch (Api.ServerApiException)
      {
        //  A server without this picture, or without the fanart service at all, is answered with no picture and no
        //  word: the plain screen behind is the right drawing for that.
        if (_for == id)
          _ = WearAsync(null);
      }
    }

    /// <summary>
    /// Putting a fetched picture on the room, thrown out of focus by however far the worn face asks.  That
    /// arithmetic takes about a sixth of a second on a full-size picture, so it happens away from the thread that
    /// draws, and the room keeps wearing what it wore until the copy is ready.
    /// </summary>
    private async Task WearAsync(byte[]? raw)
    {
      int turn = ++_wearTurn;
      _raw = raw;

      if (raw == null || !FanartShown)
      {
        Show(null, false);
        return;
      }

      // A backdrop is the size of the television, and reading it costs about a tenth of a second.  Asked of the
      // binding, that tenth is paid in front of the frame the viewer is waiting for, once for every step of a walk
      // over the wall - which is what a juddering wall is made of.  So the bytes are read here, and the room is told
      // about the picture afterwards.
      byte[]? worn = raw;
      double radius = _blur;
      if (radius > 0)
      {
        Stopwatch blurred = Stopwatch.StartNew();
        worn = (await BackgroundBlur.BlurredAsync(raw, radius)) ?? raw;
        blurred.Stop();
        Services.HomeTrace.Log("fanartblur", "ms=" + (long)blurred.Elapsed.TotalMilliseconds + " bytes="
            + raw.Length + " radius=" + radius.ToString("0.#"));
      }

      // The caret walked on, or the face - and with it the question - changed in the meantime.
      if (turn != Volatile.Read(ref _wearTurn))
        return;

      byte[]? toWear = worn;

      // But only when it is not already lying there: a wall walked back onto a name it has just worn has the answer
      // in hand, and the room is expected to wear it in this very breath.
      if (!Views.SkinFanartConverter.Instance.StandsReady(toWear, 0))
      {
        await Task.Run(delegate { Views.SkinFanartConverter.Instance.Warm(toWear, 0); });

        if (turn != Volatile.Read(ref _wearTurn))
          return;
      }

      Show(toWear, !ReferenceEquals(toWear, raw));
    }

    /// <summary>
    /// What stands behind the wall, said together with how far it still has to be thrown out of focus - the two are
    /// one answer, since a copy that comes already blurred is worn at nought.
    /// </summary>
    private void Show(byte[]? bytes, bool baked)
    {
      _baked = baked;
      BackgroundArtwork = bytes;
      Raise(nameof(BackgroundArtwork));
      Raise(nameof(HasBackgroundArtwork));
      Raise(nameof(FanartBlurRadius));

      // A backdrop is the size of the television, so the frames this change costs are worth their line in the
      // ledger: a wall that stutters half a second after every stopped name is stuttering here, not on the wall.
      Services.HomeTrace.NextFrames("background", 3);
    }

    /// <summary>The worn skin's answers, or none when the machine wears its own face (or a test runs without one).</summary>
    private void ReadSkinSettings()
    {
      Application? app = Application.Current;
      _skin = app == null ? null : SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _);
      ApplySkinSettings();
    }

    private void OnSkinSettingsSaved(object? sender, EventArgs args)
    {
      ReadSkinSettings();

      // The dial was turned while the room stood: the picture it wears is re-worn by the new answer, out of the
      // bytes already fetched rather than out of a second question.
      _ = WearAsync(_raw);
    }

    private void ApplySkinSettings()
    {
      FanartShown = _skin?.EnableFanart ?? false;
      FanartOverlay = _skin?.FanartOverlayOpacity ?? 0.85;

      // Nereus' own dial; every other face speaks no blur, which leaves the picture as the library drew it.
      _blur = _skin is NereusSkinSettings nereus ? nereus.FanartBlurRadius : 0;

      Raise(nameof(FanartShown));
      Raise(nameof(FanartOverlay));
      Raise(nameof(FanartBlurRadius));
    }

    private void Raise(string name)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
