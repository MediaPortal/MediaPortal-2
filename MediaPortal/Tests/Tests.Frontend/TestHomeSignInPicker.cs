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

using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The name buttons beside the sign-in boxes. The names themselves come from the server's user store -
  /// what these tests hold is the shape around them: a picked name lands in the box that gets sent, an
  /// empty pick changes nothing, and a machine with no server to ask simply has no buttons.
  /// </summary>
  [TestFixture]
  public class TestHomeSignInPicker
  {
    [Test]
    public void APickedNameLandsInTheBoxTheSigninSends()
    {
      HomeViewModel home = new HomeViewModel();

      home.PickSignInName("Mother");
      Assert.That(home.SignInName, Is.EqualTo("Mother"));

      home.PickSignInName("  Father  ");
      Assert.That(home.SignInName, Is.EqualTo("  Father  "),
          "the box shows what was picked; the trimming happens where the name is used, like always");
    }

    [Test]
    public void AnEmptyPickKeepsWhomeverTheBoxAlreadySaid()
    {
      HomeViewModel home = new HomeViewModel();
      home.SignInName = "Mother";

      home.PickSignInName(null);
      home.PickSignInName("   ");

      Assert.That(home.SignInName, Is.EqualTo("Mother"), "nobody is a name nobody chose");
    }

    [Test]
    public void WithoutAServerToAskTheRowOfButtonsStaysEmpty()
    {
      HomeViewModel home = new HomeViewModel();
      Assert.That(home.SignInNames, Is.Empty, "no server, no names - the typed boxes carry it alone");
      Assert.That(home.SignInOffered, Is.False, "and while the server asks for nobody, the whole row hides");
    }
  }
}
