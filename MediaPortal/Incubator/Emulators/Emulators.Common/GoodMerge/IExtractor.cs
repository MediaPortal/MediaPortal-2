using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.GoodMerge
{
  public class ExtractionEventArgs : EventArgs
  {
    public ExtractionEventArgs(int percent)
    {
      Percent = percent;
    }

    public int Percent { get; private set; }
  }

  public interface IExtractor : IDisposable
  {
    event EventHandler<ExtractionEventArgs> ExtractionProgress;
    event EventHandler ExtractionComplete;
    List<string> GetArchiveFiles();
    bool ExtractArchiveFile(string archiveFile, string extractionPath);
    bool ExtractAll(string extractionPath);
    bool IsArchive();
  }
}
