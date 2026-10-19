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
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The chrome's notification slot: the reference's master header showed the importer at work and fell
  /// silent when the import was through; these are those two sentences, tested on the words themselves.
  /// </summary>
  [TestFixture]
  public class TestChromeNotice
  {
    [Test]
    public void ANoticeIsShownWhileItHolds()
    {
      ChromeInfo chrome = new ChromeInfo();

      chrome.ReportServerActivity("importing");

      Assert.That(chrome.ServerNotice, Is.EqualTo("importing"));
      Assert.That(chrome.HasServerNotice, Is.True);
    }

    [Test]
    public void SilenceTakesTheNoticeAway()
    {
      ChromeInfo chrome = new ChromeInfo();
      chrome.ReportServerActivity("importing");

      chrome.ReportServerActivity(null);

      Assert.That(chrome.HasServerNotice, Is.False, "a finished import leaves no claim in the header");
    }

    [Test]
    public void AnEmptySentenceIsNoNotice()
    {
      ChromeInfo chrome = new ChromeInfo();

      chrome.ReportServerActivity("");

      Assert.That(chrome.HasServerNotice, Is.False);
    }

    [Test]
    public void TheNoticeTravelsToTheWindowsThread()
    {
      List<System.Action> posted = new List<System.Action>();
      ChromeInfo chrome = new ChromeInfo(action => posted.Add(action));

      chrome.ReportServerActivity("importing");

      Assert.That(chrome.HasServerNotice, Is.False, "the calling thread is not the window's");
      Assert.That(posted, Has.Count.EqualTo(1));
      posted[0]();
      Assert.That(chrome.HasServerNotice, Is.True);
    }
  }
}
