using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan
{
  [StructLayout(LayoutKind.Auto)]
  public struct FileInformation
  {
    public string FileName { get; set; }
    public FileAttributes Attributes { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public long Length { get; set; }

  }
}
