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
using System.Globalization;
using System.IO;
using System.Xml;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The words a skin's language files carry, read by this front end itself.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference asked its service host for these: <c>ServiceRegistration.Get&lt;ILocalization&gt;()</c> answered
  /// the label every control carried in the form <c>[Section.Name]</c> - the keyboard's own file writes
  /// <c>Content="[Keys.Shift]"</c> and the like
  /// (<c>Source/UI/Skins/Nereus/Skin/Nereus/themes/default/styles/VirtualKeyboardStyle_fr.xaml:44-45</c>), and
  /// <c>VirtualKeyboardControl.cs:731</c> is where the reference went asking. This front end owns no service host
  /// and may not reach into one (its project file says so out loud, <c>MediaPortal.Frontend.csproj:3-16</c>), so it
  /// reads the same files the reference's localization module read, from disk, and answers the same question.
  /// </para>
  /// <para>
  /// The rules are the reference's: the files are named <c>strings_&lt;culture&gt;.xml</c> with either the two
  /// letters alone or the two letters and a region, a culture is loaded by reading its parent first and its own
  /// file over it so the more specific wording wins
  /// (<c>Source/Core/MediaPortal.Common/Services/Localization/LocalizationStrings.cs:160-198</c>), each entry is a
  /// <c>&lt;string name="Section.Name"&gt;</c> under <c>&lt;resources&gt;</c>, and an apostrophe written as
  /// <c>\'</c> in the file is an apostrophe on the screen (same unescaping as
  /// <c>LocalizationStrings.cs:200-208</c>).
  /// </para>
  /// <para>
  /// One difference, said on purpose: when the reference found nothing it drew the label itself, brackets and all,
  /// so a missing translation put <c>[Keys.Shift]</c> on a key. Here the caller always hands over the word the
  /// markup already carries - the English one - so a viewer is shown a word they can read rather than a piece of
  /// the file. That is also why the shipped English file is read under every language: a translation that stays
  /// silent about one string is answered with the English of that one string, not with a hole.
  /// </para>
  /// <para>
  /// The files beside the executable are the ones this build was made from, which is the same rule the skins
  /// follow (<c>Theming/SkinEngine.cs:373-400</c>): <c>Language\strings_en.xml</c> beside it, and the
  /// <c>MP2_LANGUAGE_DIR</c> environment over that, for a machine whose words live elsewhere.
  /// </para>
  /// </remarks>
  public static class SkinWords
  {
    /// <summary>The language every other language is read on top of.</summary>
    public const string FALLBACK = "en";

    private static readonly Dictionary<string, string> _strings =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private static string? _spoken;
    private static bool _asked;

    /// <summary>How many words the last read brought in, so a probe can tell a full file from an empty one.</summary>
    public static int Count => _strings.Count;

    /// <summary>
    /// Where the words are looked for: the folder named in <c>MP2_LANGUAGE_DIR</c> when it is one, the
    /// <c>Language</c> folder this build was made from beside its executable when nobody said anything, and
    /// nothing when the name was set blank or neither folder is one. Setting it is how a test - or a machine
    /// whose words live elsewhere - puts its own words in front, exactly like <see cref="SkinEngine.SkinsFolder"/>.
    /// </summary>
    public static string? WordsFolder
    {
      get
      {
        if (!_folderResolved)
        {
          _folderResolved = true;
          _folder = ResolveWordsFolder(Environment.GetEnvironmentVariable("MP2_LANGUAGE_DIR"), BuiltInFolder);
        }
        return _folder;
      }
      set
      {
        _folder = value;
        _folderResolved = true;
        WordsFolderProblem = null;
        Forget();
      }
    }

    private static string? _folder;
    private static bool _folderResolved;

    /// <summary>The words folder this build writes next to its own executable.</summary>
    internal static string BuiltInFolder => Path.Combine(AppContext.BaseDirectory, "Language");

    /// <summary>
    /// The same decision on a name handed to it, so the rule can be stood in front of without editing the
    /// environment of a whole process - the shape <see cref="SkinEngine.ResolveSkinsFolder"/> uses. A name that
    /// leads nowhere is <see cref="WordsFolderProblem"/> rather than a silent fallback.
    /// </summary>
    internal static string? ResolveWordsFolder(string? fromEnv, string? besideThisBuild)
    {
      string? builtIn = Directory.Exists(besideThisBuild) ? besideThisBuild : null;

      if (fromEnv != null && fromEnv.Trim().Length == 0)
      {
        WordsFolderProblem = null;
        return null;
      }

      if (!string.IsNullOrWhiteSpace(fromEnv))
      {
        if (Directory.Exists(fromEnv))
        {
          WordsFolderProblem = null;
          return fromEnv;
        }

        WordsFolderProblem = "MP2_LANGUAGE_DIR names \"" + fromEnv + "\", which is not a folder on this machine - "
            + (builtIn is null
                ? "and this build carries no words folder either, so every word is the one the markup carries"
                : "the words this build put beside its own executable are read instead");
        return builtIn;
      }

      WordsFolderProblem = builtIn is null
          ? "no words folder beside this build (" + besideThisBuild + "), so every word is the one the markup carries"
          : null;
      return builtIn;
    }

    /// <summary>
    /// What the machine asked for and could not be had, as a sentence, or <c>null</c> when nothing went wrong.
    /// A front end whose language files are missing still speaks - the markup's own English - and says once why.
    /// </summary>
    public static string? WordsFolderProblem { get; private set; }

    /// <summary>Reads the words of the language the viewer's settings name, if they are not read already.</summary>
    public static void UseCurrentLanguage()
    {
      Use(ClientSettings.UiCulture());
    }

    /// <summary>
    /// Reads the words of one language: the English file first, then that language's own file over it. Reading the
    /// language that is already spoken costs nothing.
    /// </summary>
    public static void Use(CultureInfo language)
    {
      string wanted = language is null ? FALLBACK : language.Name;
      if (_asked && string.Equals(_spoken, wanted, StringComparison.OrdinalIgnoreCase))
        return;

      _asked = true;
      _spoken = wanted;
      _strings.Clear();

      string? folder = WordsFolder;
      if (folder is null)
        return;

      // The parent first, the specific over it, because a file for "de-AT" is meant to be read as German with the
      // pieces that differ in Austria written down - which is the order the reference's loader used.
      if (!string.Equals(language?.TwoLetterISOLanguageName, FALLBACK, StringComparison.OrdinalIgnoreCase))
        Read(folder, FALLBACK);

      if (language is null)
        return;

      if (!string.IsNullOrEmpty(language.TwoLetterISOLanguageName))
        Read(folder, language.TwoLetterISOLanguageName);
      if (!string.Equals(language.Name, language.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) &&
          language.Name.Length > language.TwoLetterISOLanguageName.Length + 1)
      {
        Read(folder, language.Name);

        // A translation cache writes a region with an underscore rather than a dash: the folder this repository
        // keeps translations in (Tools/Transifex/Cache_MP2/SkinBase) holds strings_pt_BR.xml and nothing spelled
        // strings_pt-BR.xml, so a Brazilian reading the front end would otherwise get European Portuguese.
        Read(folder, language.TwoLetterISOLanguageName + "_" +
             language.Name.Substring(language.TwoLetterISOLanguageName.Length + 1).ToUpperInvariant());
      }
    }

    /// <summary>
    /// The word for <paramref name="section"/> and <paramref name="name"/> in the language being spoken, or
    /// <paramref name="whenMissing"/> when the files say nothing about it.
    /// </summary>
    public static string Say(string section, string name, string whenMissing)
    {
      if (!_asked)
        UseCurrentLanguage();

      return _strings.TryGetValue(section + "." + name, out string? word) && !string.IsNullOrWhiteSpace(word)
          ? word
          : whenMissing;
    }

    /// <summary>
    /// The word a label of the reference's <c>[Section.Name]</c> form asks for, or <paramref name="whenMissing"/>.
    /// A label that is not in that form was never a question, and is answered with <paramref name="whenMissing"/>.
    /// </summary>
    public static string Say(string label, string whenMissing)
    {
      if (label.Length < 4 || label[0] != '[' || label[label.Length - 1] != ']')
        return whenMissing;

      string inside = label.Substring(1, label.Length - 2);
      int dot = inside.IndexOf('.');
      if (dot <= 0 || dot == inside.Length - 1)
        return whenMissing;

      return Say(inside.Substring(0, dot), inside.Substring(dot + 1), whenMissing);
    }

    /// <summary>Forgets what was read, so the next question reads the files again. For tests and for a skin switch.</summary>
    internal static void Forget()
    {
      _asked = false;
      _spoken = null;
      _strings.Clear();
    }

    /// <summary>Reads one <c>strings_&lt;culture&gt;.xml</c> over the words already standing.</summary>
    private static void Read(string folder, string cultureName)
    {
      string file = Path.Combine(folder, "strings_" + cultureName + ".xml");
      if (!File.Exists(file))
        return;

      XmlDocument doc = new XmlDocument();
      try
      {
        doc.Load(file);
      }
      catch (Exception e)
      {
        WordsFolderProblem = file + " could not be read: " + e.Message;
        return;
      }

      foreach (XmlNode entry in doc.SelectNodes("/resources/string")!)
      {
        XmlAttribute? named = entry.Attributes?["name"];
        if (named is null || string.IsNullOrEmpty(named.Value))
          continue;

        // The reference's loader did this unescaping too: an Android string file writes \' for an apostrophe.
        _strings[named.Value] = (entry.InnerText ?? string.Empty).Trim().Replace(@"\'", "'").Replace("\\\"", "\"");
      }
    }
  }
}
