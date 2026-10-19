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
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The home menu checked against the files the previous code actually assembled it from.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="MainMenu"/> is a table of fifteen entries that claims to be MP2's own menu - the contributions every
  /// plugin makes to the home state, sorted the way <c>SkinBase</c>'s <c>MenuModel</c> sorted them - and a claim a
  /// person typed by hand is a claim that goes stale the moment a plugin is renamed, added to, or corrected in this
  /// tree. So these tests re-read the tree on every run: the workflow XML of every plugin (not of the skins, whose
  /// extra promotions onto the home screen this front end deliberately does not copy), and the <c>strings_en.xml</c>
  /// files the labels come from. A plugin that starts contributing to the home state without the menu knowing, or
  /// one whose label changes under the table, fails here with the file named - rather than turning the home screen
  /// into a menu that quietly stopped matching the product it replaced.
  /// </para>
  /// <para>
  /// The workflow files are read with the regular expression rather than an XML parser because they are read the way
  /// a person reads them: one element per contribution, name, category and sort order, and nothing in between worth
  /// resolving - namespaces, includes and all the rest of the grammar are the old client's problem, not this test's.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestHomeMenuContributions
  {
    /// <summary>
    /// The state the old client's home screen was, and the only source state that makes a contribution a main-menu
    /// entry. Written down in <c>SkinBase</c>'s <c>Consts.CONST_HOME_STATE</c>.
    /// </summary>
    private const string HOME_STATE = "7F702D9C-F2DD-42da-9ED8-0BA92F07787F";

    /// <summary>One contributed element of a workflow file: its name, its sort key, and the file it came from.</summary>
    private sealed record Contribution(string Name, string Category, string SortOrder, string File, int DocumentOrder);

    /// <summary>A workflow element that carries a <c>Home-&gt;</c> name, attributes and all.</summary>
    private static readonly Regex CONTRIBUTION =
        new Regex(@"<(?<element>\w+)[^>]*Name=""(?<name>Home->[^""]*)""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex CATEGORY = new Regex(@"DisplayCategory=""(?<v>[^""]*)""", RegexOptions.Compiled);
    private static readonly Regex SORT_ORDER = new Regex(@"SortOrder=""(?<v>[^""]*)""", RegexOptions.Compiled);
    private static readonly Regex SOURCE_STATES = new Regex(@"SourceStates=""(?<v>[^""]*)""", RegexOptions.Compiled);

    /// <summary>A language string of a plugin, in the file that defines it.</summary>
    private static readonly Regex LANGUAGE_STRING =
        new Regex(@"<string name=""(?<key>[^""]*)"">(?<value>[^<]*)</string>", RegexOptions.Compiled);

    [Test]
    public void TheMenuIsExactlyWhatThePluginsContributeToTheHomeState()
    {
      List<Contribution> contributed = HomeContributions();
      List<MainMenuEntry> menu = MainMenu.Build(Array.Empty<AspectViewModel>(), false);

      string[] fromTheTree = contributed.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
      string[] onTheMenu = menu.Where(entry => entry.Mp2Action != null)
          .Select(entry => entry.Mp2Action!).OrderBy(n => n, StringComparer.Ordinal).ToArray();

      Assert.That(onTheMenu, Is.EquivalentTo(fromTheTree),
          "the menu and the plugins' workflow files contribute the same set of home entries; a name in only one of "
          + "the two is a menu that gained or lost an entry nobody decided on (files: "
          + string.Join(", ", contributed.Select(c => c.File).Distinct().OrderBy(f => f)) + ")");

      Assert.That(menu.Where(entry => entry.Mp2Action == null), Is.Empty,
          "every entry of this menu claims to be one MP2 contributed - an entry with no workflow action behind it "
          + "is this front end inventing a menu item, which the table's own documentation rules out");
    }

    [Test]
    public void TheMenuOrderIsWhatThePluginsSortInto()
    {
      // MenuModel sorted by DisplayCategory and then SortOrder. It used string.CompareTo, whose answer depends on
      // the culture of the machine it ran on; comparing case-insensitively here is what that sort produced on the
      // machines MP2 ran on for these files' categories - "a-Media" before "A-SlimTvClient" before "a-News" - and
      // it is the answer that does not change when the test does.
      List<Contribution> expected = HomeContributions()
          .OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
          .ThenBy(c => c.SortOrder, StringComparer.Ordinal)
          .ThenBy(c => c.DocumentOrder)
          .ToList();

      // Home->Movies and Home->Series share sort order "e" in media-actions.xml, so MP2's order between them was
      // whatever its unstable list sort produced that day. This front end puts films first, on purpose and
      // documented in MainMenu's remarks; everything else has to be the tree's own order.
      int movies = expected.FindIndex(c => c.Name == "Home->Movies");
      int series = expected.FindIndex(c => c.Name == "Home->Series");
      Assert.That(movies, Is.GreaterThanOrEqualTo(0) & Is.LessThan(expected.Count), "the media plugin offers films");
      Assert.That(series, Is.GreaterThanOrEqualTo(0) & Is.LessThan(expected.Count), "and it offers series");
      Assert.That(expected[movies].SortOrder, Is.EqualTo(expected[series].SortOrder),
          "the swap is only allowed because the two really do tie in the plugin's own file");
      (expected[movies], expected[series]) = (expected[series], expected[movies]);

      List<MainMenuEntry> menu = MainMenu.Build(Array.Empty<AspectViewModel>(), false);
      Assert.That(menu.Select(entry => entry.Mp2Action), Is.EqualTo(expected.Select(c => c.Name)),
          "the order on the television is the order the plugins' DisplayCategory and SortOrder sort into, plus the "
          + "one documented tie between films and series");
    }

    [Test]
    public void EveryLabelIsThePluginsOwnString()
    {
      Dictionary<string, SortedSet<string>> strings = PluginLanguageStrings();
      List<MainMenuEntry> menu = MainMenu.Build(Array.Empty<AspectViewModel>(), false);

      var wrong = new List<string>();
      foreach (MainMenuEntry entry in menu)
      {
        Assert.That(entry.LabelKey, Is.Not.Null.And.Not.Empty,
            entry.Label + " is on a menu whose every other entry is MP2's own word for itself, so it has to name "
            + "the language key it is claiming to be");

        string key = entry.LabelKey!.Trim('[', ']');
        if (!strings.TryGetValue(key, out SortedSet<string>? found))
          wrong.Add(entry.Mp2Action + " claims '" + entry.LabelKey + "', which no plugin's strings_en.xml defines");
        else if (found.Count > 1)
          wrong.Add("the key '" + key + "' is defined with " + found.Count + " different words ("
              + string.Join(" / ", found) + "), which leaves 'the plugin's own string' without an answer");
        else if (found.First() != entry.Label)
          wrong.Add(entry.Mp2Action + " says '" + entry.Label + "' but the plugin's own file says '"
              + found.First() + "'");
      }

      Assert.That(wrong, Is.Empty, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>
    /// Every element of a plugin's workflow XML that contributes a <c>Home-&gt;</c> state to the navigation, with
    /// the file it lives in and its position there - in document order, which is what a stable sort needs.
    /// </summary>
    /// <remarks>
    /// The skins' own workflow files are walked past on purpose: the rescued WMC skin promoted the television
    /// plugin's insides onto its home screen, and <see cref="MainMenu"/> says that this front end does not copy
    /// that. Checking the plugins and not the skins is what keeps the claim checkable rather than circular.
    /// </remarks>
    private static List<Contribution> HomeContributions()
    {
      string root = MetadataPath("UiSourceRoot");
      var found = new List<Contribution>();
      int order = 0;

      foreach (string file in Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
          .Where(f => f.Contains("\\workflow\\") || f.Contains("/workflow/"))
          .Where(f => !f.Contains("\\Skins\\") && !f.Contains("/Skins/"))
          .OrderBy(f => f, StringComparer.Ordinal))
      {
        foreach (Match element in CONTRIBUTION.Matches(File.ReadAllText(file)))
        {
          Match states = SOURCE_STATES.Match(element.Value);
          if (!states.Success || !states.Groups["v"].Value
                  .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                  .Any(state => string.Equals(state.Trim(), HOME_STATE, StringComparison.OrdinalIgnoreCase)))
            continue; // somebody else's source state - not a main-menu contribution.

          found.Add(new Contribution(
              element.Groups["name"].Value,
              CATEGORY.Match(element.Value).Groups["v"].Value,
              SORT_ORDER.Match(element.Value).Groups["v"].Value,
              Path.GetFileName(file),
              order++));
        }
      }

      Assert.That(found, Is.Not.Empty,
          "not one plugin in this tree contributes to the home state, which cannot be true of a tree that still "
          + "has the media plugin in it - the test is looking in the wrong place");
      return found;
    }

    /// <summary>
    /// Every language string every plugin of this tree defines, keyed without the brackets the old code writes the
    /// references in, and mapped to every word some file has used for it. Generic keys are written more than once
    /// in MP2's tree; the menu's test only demands that the keys it stands on have one answer.
    /// </summary>
    private static Dictionary<string, SortedSet<string>> PluginLanguageStrings()
    {
      string root = MetadataPath("UiSourceRoot");
      var strings = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

      foreach (string file in Directory.EnumerateFiles(root, "strings_en.xml", SearchOption.AllDirectories))
      {
        foreach (Match text in LANGUAGE_STRING.Matches(File.ReadAllText(file)))
        {
          if (!strings.TryGetValue(text.Groups["key"].Value, out SortedSet<string>? values))
            strings[text.Groups["key"].Value] = values = new SortedSet<string>(StringComparer.Ordinal);
          values.Add(text.Groups["value"].Value);
        }
      }

      Assert.That(strings, Is.Not.Empty,
          "no plugin in this tree has a language file, which the media plugin proves wrong - the test is looking "
          + "in the wrong place");
      return strings;
    }

    /// <summary>
    /// A directory the test project was told about at build time, in the way <c>TestTheme</c> established: from the
    /// assembly rather than the working directory, because a test run's working directory is wherever the runner
    /// happened to be started.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestHomeMenuContributions).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
