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
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The skin files one application is currently wearing.
  /// </summary>
  public sealed class WornSkin
  {
    public WornSkin(SkinManifest manifest, SkinTheme theme, ResourceDictionary resources, Styles styles,
        System.IDisposable? background = null, SkinImages? images = null)
    {
      Manifest = manifest;
      Theme = theme;
      Resources = resources;
      Styles = styles;
      Background = background;
      Images = images ?? new SkinImages();
    }

    public SkinManifest Manifest { get; }

    /// <summary>Which of the skin's themes it is wearing - 'default', 'dark', ...</summary>
    public SkinTheme Theme { get; }

    /// <summary>Colours, measurements, master templates and overlays, as one dictionary tree.</summary>
    public ResourceDictionary Resources { get; }

    /// <summary>The skin's own control styles, read from its XAML.</summary>
    public Styles Styles { get; }

    /// <summary>
    /// The picture the worn theme stands on, or null while it stands on a colour. The engine owns it and
    /// disposes it when the skin comes off - a skin put on and taken off a hundred times must not leave a
    /// hundred pictures behind.
    /// </summary>
    public System.IDisposable? Background { get; }

    /// <summary>
    /// Every picture the skin folder carries under its images directories, by name, loaded off disk the
    /// first time a screen asks for it. A skin is hundreds of pictures as much as it is colours; this is
    /// the directory of the skin's pictures, the way the reference screens pulled
    /// <c>ImageSource="Square_small.png"</c> out of the skin they were wearing.
    /// </summary>
    public SkinImages Images { get; }
  }

  /// <summary>
  /// The pictures one worn skin carries: its own images directory and its theme's images directory, listed
  /// by name and read off disk only when a screen asks - a skin ships hundreds of pictures and a screen
  /// shows a handful of them at a time.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A name is the file's path under an images directory with slashes and without the extension -
  /// 'Square_small', or 'HomeContentIcons/album' for a file in a subdirectory. A name that is unique among
  /// the subdirectories answers as its bare file too, because that is how the reference screens spelled
  /// them. When the skin's own theme directory carries a file the skin level also has, the theme's copy
  /// wins: the theme is the more specific voice, as it is for colours.
  /// </para>
  /// <para>
  /// Loaded pictures belong to the catalog and are disposed when the skin comes off, the same bargain the
  /// background picture made.
  /// </para>
  /// </remarks>
  public sealed class SkinImages : System.IDisposable
  {
    private readonly Dictionary<string, string> _locations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap> _loaded =
        new Dictionary<string, Avalonia.Media.Imaging.Bitmap>(StringComparer.OrdinalIgnoreCase);

    internal SkinImages()
    {
    }

    internal void Add(string key, string location, bool overwriteBare = false)
    {
      _locations[key] = location;
      int slash = key.LastIndexOf('/');
      if (slash >= 0)
      {
        string bare = key.Substring(slash + 1);
        if (overwriteBare || !_locations.ContainsKey(bare))
          _locations[bare] = location;
      }
    }

    /// <summary>How many pictures the skin carries.</summary>
    public int Count => _locations.Count;

    /// <summary>Every name a screen may ask for.</summary>
    public IEnumerable<string> Names => _locations.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalogued form of an asked-for name: the name without its trailing picture extension. The catalog
    /// keeps every picture extension-free, but the reference screens - and this front end's own shortcut model -
    /// sometimes spell a name as it stands on disk, 'genre.png' where the catalog says 'genre'. Both spellings
    /// must answer, because a shortcut tile whose icon simply does not appear is the failure a viewer sees.
    /// </summary>
    private static string LookupName(string name)
    {
      int dot = name.LastIndexOf('.');
      if (dot < 0 || dot < name.LastIndexOf('/'))
        return name;

      string extension = name.Substring(dot).ToLowerInvariant();
      return extension switch
      {
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => name.Substring(0, dot),
        _ => name
      };
    }

    public bool Has(string name) => _locations.ContainsKey(LookupName(name));

    /// <summary>Where a picture lives - a disk path, or the avares URI of a shipped skin's file.</summary>
    public bool TryGetLocation(string name, out string location) =>
        _locations.TryGetValue(LookupName(name), out location!);

    /// <summary>
    /// The picture a screen asked for, read the first time and kept until the skin comes off. Null when the
    /// skin carries no such picture or the file cannot be read - a screen with no picture in hand draws
    /// without it rather than crashing the television.
    /// </summary>
    public Avalonia.Media.Imaging.Bitmap? Load(string name)
    {
      string key = LookupName(name);
      if (_loaded.TryGetValue(key, out Avalonia.Media.Imaging.Bitmap? known))
        return known;
      if (!_locations.TryGetValue(key, out string? location))
        return null;

      try
      {
        Avalonia.Media.Imaging.Bitmap bitmap =
            (Avalonia.Media.Imaging.Bitmap) SkinEngine.ReadImage(location);
        _loaded[key] = bitmap;
        return bitmap;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Lets go of the loaded pictures when the skin comes off. The pictures are not disposed: a screen that
    /// was shown a picture holds the very bitmap this store handed it, and disposing what a live visual is
    /// painting brings the front end down (ObjectDisposedException on Ref&lt;IBitmapImpl&gt; inside
    /// Image.Render). Avalonia's bitmap carries a finalizer, so a picture whose last visual has moved on
    /// returns its memory to the garbage collector by itself; clearing here bounds the references this
    /// store keeps, which is its part, and no more.
    /// </summary>
    public void Dispose()
    {
      _loaded.Clear();
    }
  }

  /// <summary>
  /// The skins this front end reads from disk - installed by being copied, worn without recompiling.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is what the reference SkinEngine was for, in miniature. The reference front end read its skins - the
  /// Nereus and WMC directories under <c>Source\UI\Skins</c> - as data: a manifest and directories of XAML,
  /// with colours, measurements, styles, master screens and overlays all written in markup and read at run
  /// time. A screen pulled its layout out of the skin's resources by name, and switching a skin meant changing
  /// which folder was read. Nothing here is a colour chart: the skin folders hold the same kinds of files, for
  /// the same reasons, read the same way - Avalonia's own <see cref="ResourceInclude"/> and
  /// <see cref="StyleInclude"/> parse the loose XAML at run time, which is Avalonia's word for what the
  /// reference's SkinEngine was.
  /// </para>
  /// <para>
  /// <b>How a name becomes a skin and a theme.</b> A theme name may spell itself as a palette does - 'wmc' or
  /// 'wmc-dark' - which reads as skin 'wmc' wearing its theme 'dark', because that is exactly how the reference
  /// spelled it: one WMC skin shipping a default, a Dark-WMC and a Turquoise-WMC scheme. A remainder that names
  /// no theme ('nereus-orange', an accent family rather than a theme) wears the skin's default theme; the
  /// accent family itself is painted by the palette layer, which merges over the skin's colours.
  /// </para>
  /// <para>
  /// <b>Layering.</b> The skin's dictionary goes in at the front of the application's merged dictionaries, so
  /// the palette <see cref="FrontendTheme"/> merges afterwards still wins where both speak - the skin files and
  /// the palette table were both transcribed from the same reference colour files and a test keeps them honest,
  /// so on a real screen the layering is invisible; what only the skin speaks (the reference's own colour
  /// names, the measurements, the master templates, the overlays) is answered by the skin alone. The skin's
  /// styles go on at the end of the application's styles, behind nothing and after the palette's authored
  /// chrome: where the XAML and the C# say the same thing the XAML simply repeats, and where a person edits the
  /// skin's styles.xml, it is the XAML that changes what the television shows.
  /// </para>
  /// <para>
  /// <b>A skin that cannot be read.</b> A broken file is said as a sentence and leaves the previous skin on.
  /// The reference threw exceptions out of its skin loading; a television that stops because a hand-edited
  /// style file has a typo is a television nobody can watch until they find a text editor.
  /// </para>
  /// </remarks>
  public static class SkinEngine
  {
    /// <summary>
    /// Where installed skins live: the folder the viewer's machine keeps them in, one directory per skin, each
    /// with a skin.xml at its top. Settable so a test can point at its own folder. It answers null when the
    /// folder is not there - and then the front end falls back to <see cref="AssetsRoot"/>, the copy that
    /// shipped inside the assembly, so a machine without the folder still wears its skins.
    /// </summary>
    /// <remarks>
    /// The reference read its skin XML off disk exactly like this: a skin was installed by its folder existing,
    /// edited by opening a file, and read again the next time it was put on - no recompiling of the front end
    /// anywhere near it. The default is that folder; the MP2_SKINS_DIR environment variable names another one.
    /// </remarks>
    public static string? SkinsFolder
    {
      get
      {
        if (!skinsFolderResolved)
        {
          skinsFolderResolved = true;
          skinsFolder = ResolveSkinsFolder();
        }
        return skinsFolder;
      }
      set
      {
        skinsFolder = value;
        skinsFolderResolved = true;
      }
    }

    private static string? skinsFolder;
    private static bool skinsFolderResolved;

    /// <summary>
    /// Where the skins folder is: MP2_SKINS_DIR when said, the viewer's own folder otherwise, and null when
    /// neither exists - which is the only way the built-in copy below ever gets used.
    /// </summary>
    private static string? ResolveSkinsFolder()
    {
      string? fromEnv = Environment.GetEnvironmentVariable("MP2_SKINS_DIR");
      string folder = string.IsNullOrWhiteSpace(fromEnv) ? @"D:\Mediaportal\TIF\skins" : fromEnv;
      return Directory.Exists(folder) ? folder : null;
    }

    /// <summary>
    /// Where the skins this front end shipped with live: the Skins folder of this assembly's loose XAML assets.
    /// Settable so a test can point at its own embedded skins. The files ride with the assembly as
    /// AvaloniaResource because that is Avalonia's way of shipping loose markup; the disk folder above is what
    /// a skin is read from while it exists, and this is the copy that answers when it does not.
    /// </summary>
    public static string AssetsRoot { get; set; } =
        "avares://" + typeof(SkinEngine).Assembly.GetName().Name + "/Skins/";

    private static readonly ConditionalWeakTable<Application, WornSkin> WORN =
        new ConditionalWeakTable<Application, WornSkin>();

    /// <summary>
    /// Every skin the viewer's machine has - the folders under <see cref="SkinsFolder"/> that carry a
    /// skin.xml - or, while there is no such folder, every skin the index inside the assembly lists. A folder
    /// whose manifest cannot be read names nothing; a test says out loud when the listing and the folders
    /// drift apart.
    /// </summary>
    public static IReadOnlyList<SkinManifest> Installed()
    {
      string? folder = SkinsFolder;
      if (folder != null)
        return InstalledFromFolder(folder);

      List<SkinManifest> skins = new List<SkinManifest>();

      foreach (string name in ListedSkins())
      {
        string manifestUri = AssetsRoot + name + "/skin.xml";
        try
        {
          using Stream stream = AssetLoader.Open(new Uri(manifestUri));
          SkinManifest? manifest = SkinManifest.Load(stream, manifestUri, AssetsRoot + name + "/", out _);
          if (manifest != null)
            skins.Add(manifest);
        }
        catch (Exception)
        {
          // A line in the index whose folder holds no readable manifest names nothing.
        }
      }

      return skins;
    }

    /// <summary>
    /// The skins of one folder on disk: every subfolder with a skin.xml, read the way the reference read its
    /// skin XML - the file path itself is where the skin's other files hang under, so editing one and putting
    /// the skin on again is all a skin needs to change.
    /// </summary>
    private static IReadOnlyList<SkinManifest> InstalledFromFolder(string root)
    {
      List<SkinManifest> skins = new List<SkinManifest>();

      foreach (string dir in Directory.EnumerateDirectories(root)
          .OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
      {
        string manifestFile = Path.Combine(dir, "skin.xml");
        if (!File.Exists(manifestFile))
          continue;

        try
        {
          using Stream stream = File.OpenRead(manifestFile);
          SkinManifest? manifest = SkinManifest.Load(stream, manifestFile, dir + "/", out _);
          if (manifest != null)
            skins.Add(manifest);
        }
        catch (Exception)
        {
          // A folder whose skin.xml cannot be read names nothing, exactly as in the shipped copy.
        }
      }

      return skins;
    }

    private static IEnumerable<string> ListedSkins()
    {
      try
      {
        using Stream index = AssetLoader.Open(new Uri(AssetsRoot + "index.txt"));
        using StreamReader reader = new StreamReader(index);

        List<string> folders = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
          line = line.Trim();
          if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            folders.Add(line);
        }

        return folders;
      }
      catch (Exception)
      {
        return Array.Empty<string>();
      }
    }

    /// <summary>The installed skin this name asks for - the part before any theme remainder - or null.</summary>
    public static SkinManifest? Find(string? name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return null;

      Split(name, out string skinName, out _);
      foreach (SkinManifest skin in Installed())
        if (string.Equals(skin.Name, skinName, StringComparison.OrdinalIgnoreCase))
          return skin;

      return null;
    }

    /// <summary>Whether this name is worn from a skin folder rather than from the palette table alone.</summary>
    public static bool IsSkin(string? name)
    {
      return Find(name) != null;
    }

    /// <summary>The skin the application is wearing right now, or null while it wears only the palette.</summary>
    public static WornSkin? Worn(Application app)
    {
      return WORN.TryGetValue(app, out WornSkin? worn) ? worn : null;
    }

    /// <summary>
    /// A control template the worn skin carries under <paramref name="key"/> - what a screen hangs a host on so
    /// the face decides how that part of the screen looks.
    /// </summary>
    /// <remarks>
    /// This is how a home screen lets the skin own its content: the view writes bare hosts named
    /// <c>HomeBackgroundTemplate</c>, <c>HomeMenuFaceTemplate</c> and <c>HomeOverlaysTemplate</c>, and the worn
    /// skin's <c>home.xaml</c> answers any of those keys it cares about. A skin that answers none leaves the
    /// general arrangement standing, and a skin that answers one overrides only that part.
    /// </remarks>
    /// <returns>The template, or null while no skin is worn or this skin is silent about the key.</returns>
    public static Avalonia.Controls.Templates.IControlTemplate? WornTemplate(string key)
    {
      Application? app = Application.Current;
      if (app == null || !WORN.TryGetValue(app, out WornSkin? worn))
        return null;

      return worn.Resources.TryGetResource(key, null, out object? value)
          ? value as Avalonia.Controls.Templates.IControlTemplate
          : null;
    }

    /// <summary>
    /// A number the worn skin's <c>metrics</c> file carries under <paramref name="key"/>, or null while no skin
    /// is worn and the skin says nothing about it.
    /// </summary>
    /// <remarks>
    /// A screen that scales something to a share of the panel asks for the share here rather than keeping a
    /// second one of its own: the worn skin's measurements are its <c>metrics.xaml</c> (the role the reference's
    /// <c>Consts.xaml</c> played), so the number travels with the skin and a face that wants the share
    /// differently needs no code. A skin silent about the key leaves the asking screen's own default standing.
    /// </remarks>
    public static double? WornNumber(string key)
    {
      Application? app = Application.Current;
      if (app == null || !WORN.TryGetValue(app, out WornSkin? worn))
        return null;

      return worn.Resources.TryGetResource(key, null, out object? value) ? value as double? : null;
    }

    /// <summary>
    /// A tile or row template the worn skin offers for a list to draw its items with instead of the view's own.
    /// </summary>
    /// <remarks>
    /// This is the reference's arrangement, kept: a screen's lists were always there, bound and wired, but which
    /// tree every item drew came from the skin's XAML - Nereus spelled a menu entry as a quiet text row with a
    /// grey subtitle, WMC as a broad coloured band, and no C# anywhere decided the difference. The template comes
    /// from the skin file and is built by Avalonia's runtime loader, so a person editing it sees the result the
    /// next time the skin is put on, without a recompile.
    /// <para>
    /// Answers null when no skin is worn or this skin says nothing about this list - and the view keeps the
    /// template it was written with. A skin is never required to speak every key; it overrides what it cares
    /// about and stays quiet about the rest.
    /// </para>
    /// </remarks>
    /// <param name="key">
    /// The resource name the view asks about, by convention '&lt;Screen&gt;&lt;Thing&gt;Template' -
    /// <c>HomeMenuTileTemplate</c>, <c>TitleRowTemplate</c>.
    /// </param>
    public static IDataTemplate? TryTileTemplate(string key)
    {
      Application? app = Application.Current;
      if (app == null || !WORN.TryGetValue(app, out WornSkin? worn))
        return null;

      return worn.Resources.TryGetResource(key, null, out object? value) ? value as IDataTemplate : null;
    }

    /// <summary>
    /// Raised whenever the application's skin goes on, changes, or comes off - the moment its chrome templates
    /// become answerable or stop being. The window listens and swaps the header, footer, and overlay it draws
    /// from the skin, so no theme switch has to remember to knock on the window's door.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// Put the skin this name asks for on the application, or take the skin off when the name asks for none.
    /// Answers whether a skin is on afterwards; a broken skin file is the sentence, and the old skin stays.
    /// </summary>
    public static bool Apply(Application app, string name, out string? problem)
    {
      SkinManifest? manifest = Find(name);
      if (manifest == null)
      {
        Clear(app);
        problem = null;
        return false;
      }

      Split(name, out _, out string? themeName);
      SkinTheme theme = manifest.ThemeByName(themeName) ?? manifest.DefaultTheme;

      ResourceDictionary resources = new ResourceDictionary();
      Styles styles = new Styles();
      System.IDisposable? background = null;
      SkinImages? images = null;
      try
      {
        Dress(manifest, manifest.DefaultTheme, resources, styles);
        if (!ReferenceEquals(theme, manifest.DefaultTheme))
          Dress(manifest, theme, resources, styles);

        if (!string.IsNullOrEmpty(manifest.SuperlayersFile))
          resources.MergedDictionaries.Add(Read(JoinAsset(manifest.Directory, manifest.SuperlayersFile!), "overlays"));

        // And the skin's own home: the same mechanism one level further in. Every face carries its home in
        // <c>home.xaml</c> under its own roof and nowhere else - the general home screen only hosts what these
        // templates answer, so a face that wants nothing of the general arrangement simply writes its own.
        if (!string.IsNullOrEmpty(manifest.HomeFile))
          resources.MergedDictionaries.Add(Read(JoinAsset(manifest.Directory, manifest.HomeFile!), "home"));

        // The picture this theme stands on, if it named one. It goes in under its own key rather than as a
        // brush, because a window painting a picture behind the screens wants the picture, and a flat colour
        // stays the answer for every theme too plain to carry one.
        if (!string.IsNullOrEmpty(theme.BackgroundFile))
        {
          background = ReadImage(theme.AssetUri(theme.BackgroundFile));
          resources["SkinBackgroundImage"] = background;
        }

        // The hundreds of pictures the skin folder carries. They are listed, not loaded - a screen asks one
        // for 'Square_small' by name and gets the picture then, because loading four hundred and forty-five
        // bitmaps to show four of them is how a television runs out of memory before the first menu.
        images = ReadImages(manifest, theme);

        FlattenTopLevel(resources);
      }
      catch (Exception e)
      {
        background?.Dispose();
        images?.Dispose();
        problem = string.Format("The skin '{0}' could not be read: {1}", manifest.Title, e.Message);
        return false;
      }

      Wear(app, new WornSkin(manifest, theme, resources, styles, background, images));
      problem = null;
      return true;
    }

    /// <summary>Take the skin off: the application draws with its palette and its control theme again.</summary>
    public static void Clear(Application app)
    {
      if (!WORN.TryGetValue(app, out WornSkin? worn))
        return;

      app.Resources.MergedDictionaries.Remove(worn.Resources);
      app.Styles.Remove(worn.Styles);
      // The background and the pictures are let go of rather than disposed - see SkinImageStore.Dispose:
      // visuals on screen may still be painting with exactly these bitmaps until the new skin's templates
      // replace them, and the garbage collector knows the honest moment better than this call does.
      worn.Images.Dispose();
      WORN.Remove(app);
      Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Read one skin into resources and styles: its colours and measurements and masters merged in order, its
    /// style files included so the XAML - not this code - says how a button looks.
    /// </summary>
    private static void Dress(SkinManifest manifest, SkinTheme theme, ResourceDictionary resources, Styles styles)
    {
      foreach (SkinThemeFile file in theme.Files)
      {
        string uri = theme.AssetUri(file.Name);
        if (file.Role == "styles")
          styles.Add(ReadStyles(uri));
        else
          resources.MergedDictionaries.Add(Read(uri, file.Role));
      }
    }

    private static string JoinAsset(string prefix, string relative)
    {
      relative = relative.Replace('\\', '/').TrimStart('/');
      return prefix.EndsWith("/", StringComparison.Ordinal) ? prefix + relative : prefix + "/" + relative;
    }

    private static void Wear(Application app, WornSkin worn)
    {
      Clear(app);

      // Front, not last: the palette merges after the skin's own colours and keeps the final word on the
      // fourteen keys both speak, so an accent family chosen as a palette name still paints the skin.
      app.Resources.MergedDictionaries.Insert(0, worn.Resources);
      app.Styles.Add(worn.Styles);
      WORN.Add(app, worn);
      Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Copy every key the skin files answer to the top level of the worn dictionary, highest priority first.
    /// A merged dictionary's own entries are what anyone can plainly ask it about - the settings screen, a
    /// test, a debugger - and a skin whose keys all hide inside includes could not say what it answers.
    /// </summary>
    private static void FlattenTopLevel(ResourceDictionary resources)
    {
      HashSet<object> seen = new HashSet<object>();

      // The last merged dictionary wins a key, so walk backwards: the first copy made is the value a lookup
      // would have answered, and the copy then sits at the same top of the lookup order the search obeys.
      for (int i = resources.MergedDictionaries.Count - 1; i >= 0; i--)
      {
        IResourceDictionary? loaded = resources.MergedDictionaries[i] switch
        {
          ResourceInclude include => include.Loaded,
          IResourceDictionary direct => direct,
          _ => null
        };
        if (loaded == null)
          continue;

        foreach (object key in loaded.Keys)
        {
          if (!seen.Add(key) || resources.ContainsKey(key))
            continue;
          if (loaded.TryGetResource(key, null, out object? value) && value != null)
            resources.Add(key, value);
        }
      }
    }

    /// <summary>
    /// Read one of a skin's dictionary files - colours, measurements, chrome templates, overlays. A skin that
    /// lives on disk is read as text and compiled right here, the way the reference read its skin XML; a skin
    /// that rides inside the assembly is read through Avalonia's own resource include. Either way an unreadable
    /// file complains here, at the line that puts the skin on, rather than at the first brush lookup during a
    /// paint - which is where the reference's skin engine used to crash a television.
    /// </summary>
    private static IResourceProvider Read(string uriOrPath, string role)
    {
      if (!uriOrPath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
      {
        object parsed = ReadDiskXaml(uriOrPath);
        if (parsed is not IResourceProvider node)
          throw new InvalidDataException(string.Format("the {0} file '{1}' is not a resource dictionary", role,
              uriOrPath));
        return node;
      }

      Uri assetUri = new Uri(uriOrPath);
      ResourceInclude include = new ResourceInclude(assetUri) { Source = assetUri };

      // Asking for the loaded dictionary makes the promise explicit.
      if (include.Loaded == null)
        throw new InvalidDataException(string.Format("the {0} file '{1}' could not be read as XAML", role,
            uriOrPath));

      return include;
    }

    /// <summary>
    /// Read one of a skin's style files, from disk or from the assembly, as in <see cref="Read"/>.
    /// </summary>
    private static IStyle ReadStyles(string uriOrPath)
    {
      if (!uriOrPath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
      {
        object parsed = ReadDiskXaml(uriOrPath);
        if (parsed is not IStyle style)
          throw new InvalidDataException(string.Format("the styles file '{0}' is not a style", uriOrPath));
        return style;
      }

      Uri assetUri = new Uri(uriOrPath);
      StyleInclude include = new StyleInclude(assetUri) { Source = assetUri };

      if (include.Loaded == null)
        throw new InvalidDataException(string.Format("the styles file '{0}' could not be read as XAML",
            uriOrPath));

      return include;
    }

    /// <summary>
    /// List the pictures a worn skin carries: the skin's own images directory, then its theme's, so the
    /// theme's copy wins a name the way it wins a colour. Only a folder on disk can be listed - embedded
    /// shipped skins carry their pictures' directory nowhere, and a screen that asks one answers without.
    /// </summary>
    private static SkinImages ReadImages(SkinManifest manifest, SkinTheme theme)
    {
      SkinImages images = new SkinImages();
      AddImageDirectory(images, JoinAsset(manifest.Directory, "images"), false);
      AddImageDirectory(images, JoinAsset(theme.Directory, "images"), true);
      return images;
    }

    private static void AddImageDirectory(SkinImages images, string directory, bool themeLevel)
    {
      if (directory.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(directory))
        return;

      // Shortest path first: a picture sitting at the top of the images directory is the one the reference
      // screens spelled by its bare name, so it must win that name against a same-named file in a subfolder.
      foreach (string file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
          .OrderBy(f => f.Length))
      {
        string extension = Path.GetExtension(file).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp"
            && extension != ".gif")
          continue;

        string key = file.Substring(directory.Length).TrimStart('\\', '/').Replace('\\', '/');
        images.Add(key.Substring(0, key.Length - extension.Length), file, themeLevel);
      }
    }

    /// <summary>
    /// Read one of a theme's picture files - the background the theme stands on - from disk or from the
    /// assembly. A file that is not a picture complains in the same sentence the rest of the skin is reported
    /// in, and the skin stays off rather than half-worn.
    /// </summary>
    internal static System.IDisposable ReadImage(string uriOrPath)
    {
      try
      {
        if (!uriOrPath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
        {
          if (!File.Exists(uriOrPath))
            throw new FileNotFoundException("the file is not there");
          return new Avalonia.Media.Imaging.Bitmap(uriOrPath);
        }

        if (!AssetLoader.Exists(new Uri(uriOrPath)))
          throw new FileNotFoundException("the file is not in the skin");

        using Stream stream = AssetLoader.Open(new Uri(uriOrPath));
        return new Avalonia.Media.Imaging.Bitmap(stream);
      }
      catch (InvalidDataException)
      {
        throw;
      }
      catch (Exception e)
      {
        throw new InvalidDataException(string.Format("the background file '{0}' could not be read as a picture: {1}",
            uriOrPath, e.Message));
      }
    }

    /// <summary>
    /// Compile one XAML file off disk into its object - a resource dictionary or a style set. This is Avalonia's
    /// own XAML compiler doing here at run time what it otherwise does to a front end at build time, which is
    /// the whole point: a hand-edited skin file takes effect the next time the skin is put on, with no rebuild
    /// of anything. A file that does not parse answers with the sentence the wearing reports, and the skin
    /// that was on before stays on.
    /// </summary>
    private static object ReadDiskXaml(string path)
    {
      string text;
      try
      {
        text = File.ReadAllText(path);
      }
      catch (Exception e)
      {
        throw new InvalidDataException(string.Format("the skin file '{0}' could not be read: {1}", path,
            e.Message));
      }

      try
      {
        return Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(text, typeof(SkinEngine).Assembly, null,
            new Uri(path), false);
      }
      catch (InvalidDataException)
      {
        throw;
      }
      catch (Exception e)
      {
        throw new InvalidDataException(string.Format("the skin file '{0}' is not valid XAML: {1}", path,
            e.Message));
      }
    }

    /// <summary>
    /// 'wmc-dark' is the theme 'dark' of the skin 'wmc'; 'nereus' is the skin 'nereus' with no theme said;
    /// 'nereus-orange' is the skin 'nereus' with a remainder that is not a theme, which the caller lets the
    /// default theme answer.
    /// </summary>
    private static void Split(string name, out string skinName, out string? themeName)
    {
      name = name.Trim();
      int dash = name.IndexOf('-');
      if (dash <= 0)
      {
        skinName = name;
        themeName = null;
        return;
      }

      skinName = name.Substring(0, dash);
      themeName = name.Substring(dash + 1);
    }
  }
}
