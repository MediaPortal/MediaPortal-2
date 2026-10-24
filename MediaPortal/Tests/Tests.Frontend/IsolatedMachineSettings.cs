/*
 * MediaPortal 2 - Avalonia front end
 *
 * This code distributed under the GPLv3. The source of the project is
 * located at https://github.com/Ge2301/multiplatform
 */

using System;
using System.IO;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// A run of these tests speaks for the front end, not for the machine it happens to be typed on - so the one
  /// settings file the application reads before a single test has said anything is pointed at an empty file of
  /// the run's own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every fixture that saves an answer names its own file or its own <c>MP2_SKIN_SETTINGS_DIR</c>, and that is
  /// how the suite never touches what a real viewer saved. The application's own start is the one reader outside
  /// that rule: <c>App.OnFrameworkInitializationCompleted</c> asks the client settings who the house watches as,
  /// because the name owns the skin's colours and pictures and has to be standing before the palette goes on.
  /// On a development machine that file holds a name - and a name redirects the skin settings to the file of
  /// that person, <c>skin-nereus-gerrit.json</c> instead of the <c>skin-nereus.json</c> the test next door just
  /// wrote - so a screen built afterwards reads a stranger's answers and the test complains about the screen.
  /// </para>
  /// <para>
  /// Said once for the whole run rather than per fixture, because the read happens while the application object
  /// is made, which is before any fixture of the test's own has had its word. A caller that isolated the file
  /// itself - a gate, or a suite run against a scratch machine - is left alone.
  /// </para>
  /// </remarks>
  [SetUpFixture]
  public sealed class IsolatedMachineSettings
  {
    private const string CLIENT_FILE = "MP2_CLIENT_FILE";

    private string _previous = "";
    private string? _own;

    [OneTimeSetUp]
    public void TheClientSettingsOfTheMachineAreSteppedAround()
    {
      _previous = Environment.GetEnvironmentVariable(CLIENT_FILE) ?? "";
      if (_previous.Length > 0)
        return;

      _own = Path.Combine(Path.GetTempPath(),
          "mp2-frontend-tests-client-" + Guid.NewGuid().ToString("N") + ".json");
      Environment.SetEnvironmentVariable(CLIENT_FILE, _own);
    }

    [OneTimeTearDown]
    public void TheClientSettingsOfTheMachineAreHandedBack()
    {
      Environment.SetEnvironmentVariable(CLIENT_FILE, _previous.Length > 0 ? _previous : null);

      if (_own == null)
        return;

      try
      {
        File.Delete(_own);
      }
      catch (IOException)
      {
        // A scratch file the system will collect anyway is not worth a complaint.
      }

      _own = null;
    }
  }
}
