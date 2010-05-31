using System.IO;

namespace Ui.Players.BassPlayer.Utils
{
  public class URLUtils
  {
    /// <summary>
    /// Determines if the given <paramref name="filePath"/> represents a MOD music file.
    /// </summary>
    /// <param name="filePath">The path of the file to be examined.</param>
    /// <returns><c>true</c> if the extension of the given file path is one of the known MOD file extensions,
    /// else <c>false</c>.</returns>
    public static bool IsMODFile(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      string ext = Path.GetExtension(filePath).ToLower();
      switch (ext)
      {
        case ".mod":
        case ".mo3":
        case ".it":
        case ".xm":
        case ".s3m":
        case ".mtm":
        case ".umx":
          return true;
        default:
          return false;
      }
    }

    public static bool IsLastFMStream(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      if (filePath.StartsWith(@"http://"))
      {
        if (filePath.IndexOf(@"/last.mp3?") > 0)
          return true;
        if (filePath.Contains(@"last.fm/"))
          return true;
      }

      return false;
    }

    public static bool IsCDDA(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      return filePath.IndexOf("cdda:") >= 0 || filePath.IndexOf(".cda") >= 0;
    }
  }
}