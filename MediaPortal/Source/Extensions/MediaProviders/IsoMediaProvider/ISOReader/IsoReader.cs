using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ISOReader
{
  public class IsoReader : IsoReaderBase, IDisposable
  {
    protected uint _rootDirExtLoc;
    protected uint _rootDirLength;
    
    public VolumeInfo Open(string discImageFile)
    {
      switch (Path.GetExtension(discImageFile).ToLower())
      {
        // ISO 9660.
        case ".iso":
          base.DataBeginSector = 0;
          base.SectorSize = 2048;
          break;
       /* //Binary Mode1.
        case ".bin":
          base.DataBeginSector = 16;
          base.SectorSize = 2352;
          break;
        //Binaire Mode2 form1.
        case diff.ImageFileFormat.BIN_Mode2_Form1:
          base.DataBeginSector = 24;
          base.SectorSize = 2352;
          break;
        //Binaire Mode2 form2.
        case diff.ImageFileFormat.BIN_Mode2_Form2:
          base.DataBeginSector = 8;
          base.SectorSize = 2336;
          break;
        //MDF.
        case ".mdf":
          base.DataBeginSector = 16;
          base.SectorSize = 2352;
          break;
        //CloneCD Mode1.
        case ".img":
          base.DataBeginSector = 16;
          base.SectorSize = 2352;
          break;
        //CloneCD Mode2.
        case diff.ImageFileFormat.CCD_Mode2:
          base.DataBeginSector = 24;
          base.SectorSize = 2352;
          break;
        */
        default:
          break;
      }

      return base.ReadVolumeDescriptor(discImageFile, ref _rootDirExtLoc, ref _rootDirLength);
    }

    public void Close()
    {
      base.CloseDiscImageFile();
    }

    public string[] GetPathsTable()
    {
      return this.GetTable();
    }

    public string[] GetDirectories()
    {
      return FilterEntries(@"\", true, SearchOption.AllDirectories);
    }

    public string[] GetDirectories(string path)
    {
      return FilterEntries(path, true, SearchOption.AllDirectories);
    }

    public string[] GetDirectories(string path, SearchOption recursive)
    {
      return FilterEntries(path, true, recursive);
    }

    public string[] GetFiles()
    {
      return FilterEntries(@"\", false, SearchOption.AllDirectories);
    }

    public string[] GetFiles(string path)
    {
      return FilterEntries(path, false, SearchOption.AllDirectories);
    }

    public string[] GetFiles(string path, SearchOption recursive)
    {
      return FilterEntries(path, false, recursive);
    }

    public string[] GetFileSystemEntries()
    {
      return this.GetFileSystemEntries(@"\", SearchOption.AllDirectories);
    }

    public string[] GetFileSystemEntries(string path)
    {
      return this.GetFileSystemEntries(path, SearchOption.AllDirectories);
    }

    public string[] GetFileSystemEntries(string path, SearchOption recursive)
    {
      try
      {
        this.GetTable();

        PathTableRecordPub rec = _tableRecords[path.StartsWith(@"\") ? path : string.Concat(@"\", path)];

        base.BaseFileStream = new FileStream(base.DiscFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
        base.BaseBinaryReader = new BinaryReader(base.BaseFileStream);

        if (!string.IsNullOrEmpty(rec.Name))
        {
          List<string> szEntries = base.GetFileSystemEntries(path, rec.ExtentLocation,
              _rootDirLength, recursive == SearchOption.AllDirectories ? true : false, true);

          return szEntries.ToArray();
        }
        else
          return null;
      }
      catch (ArgumentNullException argExNull)
      {
        throw new ArgumentNullException(argExNull.ParamName, argExNull.Message);
      }
      catch (ArgumentException argEx)
      {
        throw new ArgumentException(argEx.Message, argEx.ParamName);
      }
      catch (KeyNotFoundException)
      {
        throw new DirectoryNotFoundException(string.Format(@"The entry ""{0}"" does not exists in the paths table", path));
      }
      catch (Exception ex)
      {
        throw new Exception(@"Method failed, an internal error occured.", ex);
      }
    }

    private string[] FilterEntries(string path, bool directory, SearchOption recursive)
    {
      if ((path == @"\") && (recursive == SearchOption.AllDirectories) && directory)
        return this.GetTable();
      else
      {
        this.GetFileSystemEntries(path, recursive);

        List<string> lEntries = new List<string>();
        if (base._basicFilesInfo.Count > 0)
        {
          Dictionary<string, RecordEntryInfo>.ValueCollection dicVal = base._basicFilesInfo.Values;

          List<RecordEntryInfo> lrecs = new List<RecordEntryInfo>(dicVal);
          lrecs.FindAll(delegate(RecordEntryInfo rec)
          {
            if (directory)
            {
              if (rec.Directory)
              {
                lEntries.Add(rec.FullPath);
                return true;
              }
              else
                return false;
            }
            else
            {
              if (rec.Directory)
                return false;
              else
              {
                lEntries.Add(rec.FullPath);
                return true;
              }
            }
          });
        }

        return lEntries.ToArray();
      }
    }

    public RecordEntryInfo GetRecordEntryInfo(string path)
    {
      string dir = Path.GetDirectoryName(path);
      if (!dir.StartsWith("\\"))
        dir = "\\" + dir;

      if (!path.StartsWith("\\"))
        path = "\\" + path;


      this.GetFileSystemEntries(dir);
      
      if (!_basicFilesInfo.ContainsKey(path))
      {
        throw new ArgumentException("IsoReader::GetRecordEntryInfo - does not contain key (" + path + ")");
      }  
      return _basicFilesInfo[path];
    }
    
    public Stream GetFileStream(string FileName)
    {
      RecordEntryInfo entry = GetRecordEntryInfo(FileName);
      if (entry.FullPath.Equals(FileName))
      {
        return new IsoFileStream(BaseFileStream, entry.Extent * _sectorSize, entry.Size);
      }
      return null;
    }
  }
}
