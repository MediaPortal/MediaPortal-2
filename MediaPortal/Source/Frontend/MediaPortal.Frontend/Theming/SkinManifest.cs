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
using System.Xml;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// One theme of an installed skin: its name, its directory, and the files the manifest says to read.
  /// </summary>
  public sealed class SkinThemeFile
  {
    public SkinThemeFile(string role, string name)
    {
      Role = role;
      Name = name;
    }

    /// <summary>'colors', 'resources' or 'styles' - what kind of XAML the file is.</summary>
    public string Role { get; }

    /// <summary>The file's name, relative to the theme's directory.</summary>
    public string Name { get; }
  }

  /// <summary>
  /// One theme inside a skin, as the manifest's Theme element described it.
  /// </summary>
  public sealed class SkinTheme
  {
    public SkinTheme(string name, string directory, IReadOnlyList<SkinThemeFile> files, bool isDefault,
        string backgroundFile = "")
    {
      Name = name;
      Directory = directory;
      Files = files;
      IsDefault = isDefault;
      BackgroundFile = backgroundFile;
    }

    public string Name { get; }

    /// <summary>Asset prefix holding this theme's files, ending with a slash.</summary>
    public string Directory { get; }

    public IReadOnlyList<SkinThemeFile> Files { get; }

    public bool IsDefault { get; }

    /// <summary>
    /// The picture file this theme stands on, named by the manifest's Background attribute, or empty when the
    /// theme stands on a flat colour like it always did. Relative to the theme's directory, read from the same
    /// place as the XAML files - off disk for a skin folder, out of the assembly for a shipped one.
    /// </summary>
    public string BackgroundFile { get; }

    /// <summary>The asset URI of one of this theme's files.</summary>
    public string AssetUri(string file)
    {
      if (!Directory.EndsWith("/", StringComparison.Ordinal))
        return Directory + "/" + file;
      return Directory + file;
    }
  }

  /// <summary>
  /// What one skin's manifest said, read off disk.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference skin engine was given skins the same way it gives everything else: a folder of XAML with a
  /// manifest at its top - the reference skins' <c>theme.xml</c> named the skin, its author, its version, and the
  /// style files every screen should read. A skin was installed by the folder existing and nothing else. This is
  /// the same bargain, and this class is only the reading of that manifest: the wearing of it is
  /// <see cref="SkinEngine"/>.
  /// </para>
  /// <para>
  /// A manifest that cannot be read is not an exception on the way to the settings screen; the sentence of what
  /// is wrong is what <see cref="Load"/> hands back, because a person hand-editing a skin is exactly the person
  /// who needs to be told what they mistyped.
  /// </para>
  /// </remarks>
  public sealed class SkinManifest
  {
    /// <summary>The name themes and settings use for this skin, lowercase, as 'nereus' or 'wmc'.</summary>
    public string Name { get; private set; } = "";

    /// <summary>What the skin wants to be called on screen.</summary>
    public string Title { get; private set; } = "";

    public string Author { get; private set; } = "";

    public string Version { get; private set; } = "";

    public string Description { get; private set; } = "";

    /// <summary>Asset prefix of this skin's files, ending with a slash.</summary>
    public string Directory { get; private set; } = "";

    /// <summary>The manifest's own asset URI, so an error message can point at it.</summary>
    public string ManifestPath { get; private set; } = "";

    public IReadOnlyList<SkinTheme> Themes { get; private set; } = Array.Empty<SkinTheme>();

    /// <summary>The overlays that float above every screen, or null when the skin has none.</summary>
    public string? SuperlayersFile { get; private set; }

    public SkinTheme DefaultTheme
    {
      get
      {
        foreach (SkinTheme theme in Themes)
          if (theme.IsDefault)
            return theme;
        return Themes[0];
      }
    }

    public SkinTheme? ThemeByName(string? name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return null;

      foreach (SkinTheme theme in Themes)
        if (string.Equals(theme.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
          return theme;

      return null;
    }

    /// <summary>
    /// Reads one skin manifest from an already-open stream. The out sentence says what is wrong with a manifest
    /// that cannot be read; a manifest that can be read is answered with the manifest and a null sentence.
    /// <paramref name="sourceUri"/> is only there so the sentence can name the file, and
    /// <paramref name="assetDirectory"/> is the asset prefix every file of this skin hangs under.
    /// </summary>
    public static SkinManifest? Load(Stream stream, string sourceUri, string assetDirectory, out string? problem)
    {
      problem = null;
      SkinManifest manifest = new SkinManifest { ManifestPath = sourceUri };

      XmlDocument doc = new XmlDocument();
      try
      {
        doc.Load(stream);
      }
      catch (Exception e)
      {
        problem = string.Format("The skin manifest '{0}' cannot be read: {1}", sourceUri, e.Message);
        return null;
      }

      XmlElement? root = doc.DocumentElement;
      if (root == null || root.Name != "Skin")
      {
        problem = string.Format("The skin manifest '{0}' does not start with a Skin element.", sourceUri);
        return null;
      }

      manifest.Name = Attribute(root, "Name");
      manifest.Title = Attribute(root, "Title");
      manifest.Author = Attribute(root, "Author");
      manifest.Version = Attribute(root, "Version");
      manifest.Directory = assetDirectory;

      foreach (XmlNode node in root.ChildNodes)
      {
        if (!(node is XmlElement element))
          continue;

        switch (element.Name)
        {
          case "Description":
            manifest.Description = element.InnerText.Trim();
            break;

          case "Theme":
            try
            {
              manifest.Themes = Append(manifest.Themes, ReadTheme(manifest.Directory, element, sourceUri));
            }
            catch (InvalidDataException e)
            {
              problem = e.Message;
              return null;
            }
            break;

          case "Superlayers":
            manifest.SuperlayersFile = Attribute(element, "File");
            break;

          default:
            problem = string.Format("The skin manifest '{0}' has a {1} element, which skins do not have.",
                sourceUri, element.Name);
            return null;
        }
      }

      if (manifest.Name.Length == 0 || manifest.Themes.Count == 0)
      {
        problem = string.Format("The skin manifest '{0}' names no skin or lists no Theme.", sourceUri);
        return null;
      }

      if (manifest.Title.Length == 0)
        manifest.Title = manifest.Name;

      return manifest;
    }

    private static SkinTheme ReadTheme(string skinDirectory, XmlElement element, string manifestPath)
    {
      string name = Attribute(element, "Name");
      string directory = Attribute(element, "Directory");
      if (name.Length == 0 || directory.Length == 0)
        throw new InvalidDataException(string.Format(
            "A Theme in the manifest '{0}' names neither itself nor its directory.", manifestPath));

      List<SkinThemeFile> files = new List<SkinThemeFile>();
      foreach (XmlNode child in element.ChildNodes)
      {
        if (child is not XmlElement file || file.Name != "File")
          continue;

        string role = Attribute(file, "Role").ToLowerInvariant();
        string fileName = Attribute(file, "Name");
        // "templates" is the file where the skin says how a screen's items look - DataTemplates under names
        // the screens ask about. It is read exactly like a resources file; it gets its own role because what
        // it answers is not a colour somebody can point at, and a person reading the manifest should see the
        // difference.
        if (role != "colors" && role != "resources" && role != "styles" && role != "templates"
            || fileName.Length == 0)
          throw new InvalidDataException(string.Format(
              "A File in theme '{0}' of the manifest '{1}' is not a colors, resources, styles or templates file.",
              name, manifestPath));

        files.Add(new SkinThemeFile(role, fileName));
      }

      bool isDefault = string.Equals(Attribute(element, "Default"), "true", StringComparison.OrdinalIgnoreCase);

      // An optional picture the theme stands on. The reference skins carried their own backgrounds - Nereus a
      // dark fanart, WMC its blue wash - and a skin that may not carry a picture is only half a skin.
      string background = Attribute(element, "Background");

      return new SkinTheme(name, CombineAsset(skinDirectory, directory), files, isDefault, background);
    }

    /// <summary>Joins an asset prefix and a manifest-relative directory the way asset URIs are spelled.</summary>
    private static string CombineAsset(string prefix, string relative)
    {
      relative = relative.Replace('\\', '/').TrimStart('/');
      if (!prefix.EndsWith("/", StringComparison.Ordinal))
        prefix += "/";
      return prefix + relative;
    }

    private static IReadOnlyList<SkinTheme> Append(IReadOnlyList<SkinTheme> themes, SkinTheme theme)
    {
      List<SkinTheme> all = new List<SkinTheme>(themes) { theme };
      return all;
    }

    private static string Attribute(XmlElement element, string name)
    {
      XmlAttribute? attribute = element.GetAttributeNode(name);
      return attribute == null ? "" : attribute.Value.Trim();
    }
  }
}
