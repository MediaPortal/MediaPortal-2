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
using System.IO;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;

namespace Tests.Frontend
{
  /// <summary>
  /// The skin's answers, per person. The reference stored them per MediaPortal user, so two viewers of one
  /// household wear different colours of one skin; the layer above the per-skin file does exactly that, and
  /// these tests hold it to the promise from every side: the signed-in name names the file, a new person
  /// inherits what the machine was told, the first save leaves a file of their own behind, and a machine
  /// where nobody signs in keeps answering from the very file it always used.
  /// </summary>
  [TestFixture]
  public class TestSkinSettingsPerUser
  {
    private string _folder = "";
    private string? _ownerBefore;

    [SetUp]
    public void AFolderOfItsOwn()
    {
      _ownerBefore = SkinSettingsBase.FileOwner;
      _folder = Path.Combine(Path.GetTempPath(), "mp2-skinusers-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_folder);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _folder);
      SkinSettingsBase.FileOwner = null;
    }

    [TearDown]
    public void PutTheWorldBack()
    {
      SkinSettingsBase.FileOwner = _ownerBefore;
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", null);
      try { Directory.Delete(_folder, true); } catch (IOException) { }
    }

    [Test]
    public void WhileNobodyIsSignedInTheSharedFileStandsExactlyWhereItAlwaysStood()
    {
      string shared = Path.Combine(_folder, "skin-nereus.json");
      Assert.That(SkinSettingsBase.FileFor("nereus"), Is.EqualTo(shared));
      Assert.That(SkinSettingsBase.FileForUser("nereus"), Is.EqualTo(shared),
          "no owner, no other answer - the old file is the file");
    }

    [Test]
    public void TheSignedInNameNamesTheFile()
    {
      SkinSettingsBase.FileOwner = "Mother";
      string own = SkinSettingsBase.FileForUser("nereus");
      Assert.That(own, Is.EqualTo(Path.Combine(_folder, "skin-nereus-mother.json")));
      Assert.That(SkinSettingsBase.FileFor("nereus"), Does.Not.Contain("mother"),
          "and the shared file stays the shared file");
    }

    [Test]
    public void ANameIsTidiedForTheFilesystemNotRenamedForTheScreen()
    {
      SkinSettingsBase.FileOwner = "The/Kids:Room";
      string own = Path.GetFileName(SkinSettingsBase.FileForUser("wmc"));
      Assert.That(own, Does.Not.Contain("/").And.Not.Contain(":"), "a name and a filename are two questions");
    }

    [Test]
    public void ANewPersonInheritsWhatTheMachineWasToldAndThenSpeaksForItself()
    {
      // The household answered once, before anybody ever signed in.
      NereusSkinSettings shared = new NereusSkinSettings { FanartOverlayOpacity = 0.33 };
      shared.Save(null, out bool saved);
      Assert.That(saved, Is.True);
      Assert.That(File.Exists(SkinSettingsBase.FileFor("nereus")), Is.True);

      // Father signs in: the machine's answers stand until he changes one.
      SkinSettingsBase.FileOwner = "Father";
      SkinSettingsBase? inherited = SkinSettingsBase.LoadFor("nereus", null, out _);
      Assert.That(inherited, Is.InstanceOf<NereusSkinSettings>());
      Assert.That(inherited!.FanartOverlayOpacity, Is.EqualTo(0.33), "he inherits what the machine was told");

      // He changes his own answer; it lands in his own file and leaves the shared one alone.
      inherited.FanartOverlayOpacity = 0.9;
      inherited.Save(null, out saved);
      Assert.That(saved, Is.True);
      Assert.That(File.Exists(SkinSettingsBase.FileForUser("nereus")), Is.True, "his own file now exists");

      SkinSettingsBase? father = SkinSettingsBase.LoadFor("nereus", null, out _);
      Assert.That(father!.FanartOverlayOpacity, Is.EqualTo(0.9));

      // Mother has never touched anything: she still wears the machine's answer, not his.
      SkinSettingsBase.FileOwner = "Mother";
      SkinSettingsBase? mother = SkinSettingsBase.LoadFor("nereus", null, out _);
      Assert.That(mother!.FanartOverlayOpacity, Is.EqualTo(0.33), "one person's change is not everybody's");
    }

    [Test]
    public void NobodyEverAnsweringStillMeansTheShippedDefaultsNotAComplaint()
    {
      SkinSettingsBase.FileOwner = "Stranger";
      SkinSettingsBase? settings = SkinSettingsBase.LoadFor("nereus", null, out string? problem);

      Assert.That(settings, Is.InstanceOf<NereusSkinSettings>());
      Assert.That(problem, Is.Null, "a person who never touched the settings is not a broken file");
      Assert.That(settings!.EnableFanart, Is.True, "and the shipped answer stands");
    }
  }
}
