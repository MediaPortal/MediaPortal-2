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
using System.Globalization;
using System.IO;

namespace MediaPortal.Frontend.Settings
{
  /// <summary>
  /// What killed the front end, written where somebody looking for it will find it.
  /// </summary>
  /// <remarks>
  /// A client on a television that dies says nothing on its own. Windows takes the exception and writes it into
  /// its own application log, which is a place nobody looks at and a thing nobody standing in front of a black
  /// screen reads - the crash of the settings wall was only found because somebody thought to ask
  /// <c>Get-WinEvent</c>. The last words are therefore kept beside the front end's own settings, in the same
  /// folder as <see cref="ServerSettings"/>, which is the folder anybody told to "prüfe die logs" ends up in.
  ///
  /// Writing this does not save the process. An exception the dispatcher could not hold is an exception the
  /// window cannot be trusted behind, and a client that carries on after one shows a half-drawn screen to
  /// whoever is standing in front of it. What the file changes is the difference between a bug report and a
  /// shrug.
  /// </remarks>
  public static class CrashLog
  {
    /// <summary>The file the last words go into, beside the settings the front end keeps.</summary>
    public static string FilePath => Path.Combine(
        Path.GetDirectoryName(ServerSettings.DefaultFilePath) ?? Path.GetTempPath(), "frontend-crash.log");

    /// <summary>
    /// Appending one death to that file. Never throws: a crash whose writing also failed is still a crash that
    /// was tried to be written, and a second exception on the way out of a dying process hides the first one.
    /// </summary>
    /// <param name="word">What the runtime handed over - usually the exception itself, sometimes its text.</param>
    /// <param name="terminating">Whether the process ended here, which is the answer to "did it survive?".</param>
    /// <returns>The words that were written, so that a caller printing them to a terminal says the same thing
    /// the file does.</returns>
    public static string Write(object? word, bool terminating)
    {
      string body = word?.ToString() ?? "unknown";

      try
      {
        string folder = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(folder);
        File.AppendAllText(FilePath,
            DateTime.Now.ToString("u", CultureInfo.InvariantCulture)
            + (terminating ? "  (the process ended here)" : "") + Environment.NewLine + body
            + Environment.NewLine + Environment.NewLine);
      }
      catch
      {
        // Said above: nothing may be thrown from here.
      }

      return body;
    }
  }
}
