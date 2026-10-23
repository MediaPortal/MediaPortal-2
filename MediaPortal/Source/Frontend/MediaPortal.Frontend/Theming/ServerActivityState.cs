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

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// What the front end's deal with the server is at this moment, said as one thing rather than as a sentence, so
  /// that the head line can put a glyph in front of it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference never needed this word, because its front end and its television and library services ran in the
  /// same process: there was no connection to have lost, and what its notification bar carried was the importer at
  /// work and the tuner recording (<c>Skin/Nereus/screens/NotificationsBar.inc:19-40</c>), both as icons - a turning
  /// circle with the importer's picture and a percentage, and a blinking recording picture - with no sentence at all.
  /// This front end asks a machine across the network for everything, so it has a fourth thing to say, and until now
  /// the only way of saying it was a sentence ("Listening to the server", "Lost the server. Trying again in 5
  /// seconds."). This is the same news as a state, which lets the glyph carry the state and the words carry only what
  /// a glyph cannot: how long until the next try, and what to do about it.
  /// </para>
  /// <para>
  /// The glyph names are the ones the worn skin carries under <c>images/Notification</c>, asked through the same
  /// catalog every other icon in the skin is asked through, so a skin that would rather draw its own server states
  /// has only to carry its own files of these names.
  /// </para>
  /// </remarks>
  public enum ServerActivityState
  {
    /// <summary>Nothing is being listened to, so the head line says nothing either.</summary>
    NotListening,

    /// <summary>Asked, and nothing has answered yet.</summary>
    Connecting,

    /// <summary>
    /// The event stream is open, so the library on screen changes while nobody presses anything. The head line draws
    /// this with the connected glyph.
    /// </summary>
    Connected,

    /// <summary>
    /// The event stream is not open, whatever the reason - it dropped, it was refused at the door, or it stopped
    /// after an error. The head line draws this with the disconnected glyph, and the sentence handed along with it
    /// says which of those it was, because those three want different things from whoever reads it.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The server is at work in the library. The reference drew this as its turning circle with the importer's
    /// picture inside it and the percentage standing under it; a percentage comes along with this state when the
    /// server said one, and is left out when it only said that it had started.
    /// </summary>
    Filing
  }

  /// <summary>
  /// The glyph and the short words a server state is drawn with.
  /// </summary>
  public static class ServerActivity
  {
    /// <summary>
    /// The worn skin's glyph for a state, or the empty string for "nothing is being listened to", which draws
    /// nothing at all.
    /// </summary>
    public static string Glyph(ServerActivityState state)
    {
      switch (state)
      {
        case ServerActivityState.Connected:
          return "Notification/connected";
        case ServerActivityState.Disconnected:
          return "Notification/disconnected";
        case ServerActivityState.Filing:
          return "Notification/import";
        case ServerActivityState.Connecting:
          return "Notification/info";
        default:
          return "";
      }
    }

    /// <summary>
    /// The one word the glyph is read with. Only the three connection states get one, and only because a glyph on its
    /// own leaves "is it working or not" open: a household that sees the disconnected glyph wants that answered before
    /// it wants anything else. Filing has no word of its own, because the percentage - or the sentence about which
    /// directory is being filed - says more in the same space.
    /// </summary>
    public static string Words(ServerActivityState state)
    {
      switch (state)
      {
        case ServerActivityState.Connected:
          return "connected";
        case ServerActivityState.Disconnected:
          return "disconnected";
        case ServerActivityState.Connecting:
          return "connecting";
        default:
          return "";
      }
    }
  }
}
