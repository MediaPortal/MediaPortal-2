using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan
{
  public class FileInformation
  {
    public FileAttributes Attributes;
    public DateTime CreationTime;
    public DateTime LastAccessTime;
    public DateTime LastWriteTime;
    public long Length;
    public string FileName;
  }
}
