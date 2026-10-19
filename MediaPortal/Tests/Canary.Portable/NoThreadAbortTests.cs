using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// Nothing in the product sources may abort a thread.
  ///
  /// <c>Thread.Abort</c>, <c>Thread.ResetAbort</c> and <c>Thread.Suspend</c> all answer with
  /// <see cref="PlatformNotSupportedException"/> on every .NET that is not the Windows Framework, and they used
  /// to be scattered through the shutdown paths of this tree - the request queue, the render loop, the
  /// clipboard, the thread pool. A shutdown path that throws instead of stopping is exactly the kind of defect
  /// that only appears when the machine is busy, so it is checked here instead of in a review comment.
  ///
  /// The alternative in every case is the same: ask the thread to stop with a flag, a cancellation token, or a
  /// handle it waits on, wait as long as the caller can afford, and leave the thread alone after that - which
  /// is only safe because such threads are background threads.
  /// </summary>
  [TestFixture]
  public class NoThreadAbortTests
  {
    /// <summary>
    /// A bare <c>.Abort()</c> is enough to catch, since the only thing still calling it in the whole tree is a
    /// web request - see <see cref="_allowed"/>.
    /// </summary>
    private static readonly Regex _forbidden = new Regex(
        @"ThreadAbortException|\bResetAbort\b|Thread\.Suspend|\.Abort\(\s*\)");

    /// <summary>
    /// Files allowed to call <c>Abort()</c>, keyed by their path under the sources root with forward slashes,
    /// with the reason. Anything new has to be justified here before it can exist.
    /// </summary>
    private static readonly Dictionary<string, string> _allowed =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Core/UPnP/Infrastructure/Utils/NetworkHelper.cs"] =
                "request.Abort() is HttpWebRequest.Abort(), which cancels a web request; it is not a thread and works everywhere.",
        };

    [Test]
    public void NoSourceFileAbortsOrSuspendsAThread()
    {
      string root = Path.GetFullPath(SourceRoot());
      Assert.That(Directory.Exists(root), Is.True, "the sources root '{0}' does not exist", root);

      var offenders = new List<string>();
      foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
      {
        string relative = file.Substring(root.Length + 1).Replace('\\', '/');
        if (_allowed.ContainsKey(relative))
          continue;

        int lineNumber = 0;
        foreach (string text in File.ReadLines(file))
        {
          lineNumber++;
          string trimmed = text.TrimStart();
          // Prose about aborting is not aborting, and the comments explaining why a call is gone are worth keeping.
          if (trimmed.StartsWith("//", StringComparison.Ordinal)
              || trimmed.StartsWith("*", StringComparison.Ordinal)
              || trimmed.StartsWith("/*", StringComparison.Ordinal))
            continue;

          if (_forbidden.IsMatch(text))
            offenders.Add(relative + ":" + lineNumber + ": " + trimmed);
        }
      }

      Assert.That(offenders, Is.Empty,
          "Aborting a thread is not supported outside the Windows .NET Framework; stop the thread by asking it "
          + "to stop and leave it behind if it does not. Found:\n" + string.Join("\n", offenders)
          + "\nIf a hit is not about a thread at all, add the file to the allow list with the reason.");
    }

    private static string SourceRoot()
    {
      string value = typeof(NoThreadAbortTests).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, "SourceRoot", StringComparison.Ordinal))?.Value;

      Assert.That(value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand the sources root to the tests as assembly metadata");
      return value;
    }
  }
}
