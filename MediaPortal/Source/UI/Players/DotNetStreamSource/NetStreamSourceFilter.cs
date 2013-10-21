using System;
using System.IO;
using System.Runtime.InteropServices;
using DirectShow;
using DirectShow.BaseClasses;

namespace MediaPortal.UI.Players
{
  [ComVisible(true)]
  [Guid("BD32F0D8-1E0D-46BE-8DE6-E3C4C5746633")]
  public interface IDotNetStreamSourceFilter
  {
    int SetSourceStream(Stream sourceStream, string fileName);
  }

  /// <summary>
  /// A simple DirectShow source filter implementation that implements the <see cref="IDotNetStreamSourceFilter"/> interface,
  /// supports the pull model and has one output pin named "Output" after <see cref="SetSourceStream"/> was called with a valid <see cref="Stream"/>.
  /// <see cref="IFileSourceFilter"/> is implemented, so downstream filters that parse the name of the file that is played to guess the type can function properly.
  /// </summary>
  [ComVisible(true)]
  [Guid("19651B59-6AD6-4FD7-882A-914ED5592BFA")]
  [ClassInterface(ClassInterfaceType.None)]
  public class DotNetStreamSourceFilter : BaseFilter, IDotNetStreamSourceFilter, IFileSourceFilter
  {
    protected Stream sourceStream = null;
    protected DotNetStreamOutputPin outputPin = null;
    protected string fileName = null;

    public DotNetStreamSourceFilter()
      : base(".Net Stream Source Filter")
    {
    }

    public override int Stop()
    {
      var result = base.Stop();
      if (sourceStream != null)
      {
        sourceStream.Close();
        sourceStream.Dispose();
        sourceStream = null;
      }
      return result;
    }

    ~DotNetStreamSourceFilter()
    {
      sourceStream = null;
    }

    protected override int OnInitializePins()
    {
      outputPin = new DotNetStreamOutputPin("Output", this, sourceStream);
      AddPin(outputPin);
      return NOERROR;
    }

    #region IDotNetStreamSourceFilter Members

    public int SetSourceStream(Stream sourceStream, string fileName)
    {
      if (this.sourceStream != null)
        return E_UNEXPECTED;
      else
      {
        this.sourceStream = sourceStream;
        this.fileName = fileName;
        return NOERROR;
      }
    }

    #endregion

    #region IFileSourceFilter Members

    public int Load(string pszFileName, AMMediaType pmt)
    {
      if (this.sourceStream != null)
        return E_UNEXPECTED;
      else
      {
        this.fileName = pszFileName;
        this.sourceStream = new System.IO.FileStream(pszFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return NOERROR;
      }
    }

    public int GetCurFile(out string pszFileName, AMMediaType pmt)
    {
      pszFileName = this.fileName;
      if (string.IsNullOrEmpty(pszFileName)) return VFW_E_NOT_CONNECTED;
      else return NOERROR;
    }

    #endregion
  }

  /// <summary>
  /// Output <see cref="IPin"/> that implements <see cref="IAsyncReader"/> as it perform asynchronous read operations, 
  /// delivers data in the form of a byte stream (<see cref="MediaType.Stream"/>) and supports the pull model.
  /// </summary>
  [ComVisible(true)]
  [Guid("8CF6F982-E2A4-4DC4-A437-8E9F8533EA1D")]
  public class DotNetStreamOutputPin : BasePin, IAsyncReader
  {
    protected Stream sourceStream = null;

    public DotNetStreamOutputPin(string _name, BaseFilter _filter, Stream sourceStream)
      : base(_name, _filter, _filter.FilterLock, PinDirection.Output)
    {
      if (sourceStream == null)
      {
        throw new ArgumentException("Parameter cannot be null!", "sourceStream");
      }
      else
      {
        this.sourceStream = sourceStream;
      }
    }

    ~DotNetStreamOutputPin()
    {
      sourceStream = null;
    }

    #region Overridden Methods of BasePin

    public override int BeginFlush()
    {
      return E_UNEXPECTED;
    }

    public override int EndFlush()
    {
      return E_UNEXPECTED;
    }

    public override int CheckMediaType(AMMediaType pmt)
    {
      lock (m_Lock)
      {
        // the given mediatype as acceptable when the major type is Stream, no subtype and no specific format
        if (pmt.majorType == MediaType.Stream && pmt.subType == MediaSubType.Null && pmt.formatType == FormatType.None)
          return NOERROR;
        else
          return E_FAIL;
      }
    }

    public override int GetMediaType(int iPosition, ref AMMediaType pMediaType)
    {
      lock (m_Lock)
      {
        if (iPosition < 0)
        {
          return E_INVALIDARG;
        }
        if (iPosition > 0)
        {
          return VFW_S_NO_MORE_ITEMS;
        }
        if (sourceStream == null)
        {
          return E_UNEXPECTED;
        }
        // set our MediaType requirements
        pMediaType.majorType = MediaType.Stream;
        pMediaType.subType = MediaSubType.Null;
        pMediaType.formatType = FormatType.None;
        pMediaType.temporalCompression = false;
        pMediaType.fixedSizeSamples = false;
        pMediaType.sampleSize = 0;

        return NOERROR;
      }
    }

    #endregion

    #region IAsyncReader Members

    /// <summary>
    /// Requests an allocator during the pin connection.
    /// </summary>
    /// <param name="pPreferred">Pointer to the <see cref="IMemAllocator"/> interface on the input pin's preferred allocator, or NULL.</param>
    /// <param name="pProps">An <see cref="AllocatorProperties"/> structure, allocated by the caller. The caller should fill in any allocator properties that the input pin requires, and set the remaining members to zero.</param>
    /// <param name="ppActual">Pointer that receives an <see cref="IMemAllocator"/>.</param>
    /// <returns></returns>
    public int RequestAllocator(IntPtr pPreferred, AllocatorProperties pProps, out IntPtr ppActual)
    {
      lock (m_Lock)
      {
        // we are not working with Allocators, set the outgoing pointer to 0
        ppActual = IntPtr.Zero;
        return S_OK;
      }
    }

    /// <summary>
    /// Queues an asynchronous request for data.
    /// </summary>
    /// <param name="pSample">Pointer to an <see cref="IMediaSample"/> provided by the caller.</param>
    /// <param name="dwUser">Specifies an arbitrary value that is returned when the request completes.</param>
    /// <returns></returns>
    public int Request(IntPtr pSample, IntPtr dwUser)
    {
      // we are not working in async mode
      throw new NotImplementedException();
    }

    /// <summary>
    /// Waits for the next pending read request to complete.
    /// </summary>
    /// <param name="dwTimeout">Specifies a time-out in milliseconds. Use the value INFINITE to wait indefinitely.</param>
    /// <param name="ppSample">Pointer that receives an <see cref="IMediaSample"/>.</param>
    /// <param name="pdwUser">Pointer that receives the value of the dwUser parameter specified in the <see cref="Request"/> method.</param>
    /// <returns></returns>
    public int WaitForNext(int dwTimeout, out IntPtr ppSample, out IntPtr pdwUser)
    {
      // we are not working in async mode
      throw new NotImplementedException();
    }

    /// <summary>
    /// Performs a synchronous read. The method blocks until the request is completed. 
    /// The file positions and the buffer address must be aligned.
    /// This method performs an unbuffered read, so it might be faster than the <see cref="SyncRead "/> method.
    /// </summary>
    /// <param name="pSample">Pointer to an <see cref="IMediaSample"/> provided by the caller.</param>
    /// <returns></returns>
    public int SyncReadAligned(IntPtr pSample)
    {
      // we are not working with Allocators
      throw new NotImplementedException();
    }

    /// <summary>
    /// Performs a synchronous read. The method blocks until the request is completed. 
    /// The file positions and the buffer address do not have to be aligned.
    /// </summary>
    /// <param name="llPosition">Specifies the byte offset at which to begin reading. The method fails if this value is beyond the end of the file.</param>
    /// <param name="lLength">Specifies the number of bytes to read.</param>
    /// <param name="pBuffer">Pointer to a buffer that receives the data.</param>
    /// <returns></returns>
    public int SyncRead(long llPosition, int lLength, IntPtr pBuffer)
    {
      lock (m_Lock)
      {
        // seek to the requested position if neccessary
        if (sourceStream.Position != llPosition)
        {
          if (sourceStream.Seek(llPosition, SeekOrigin.Begin) != llPosition)
            return S_FALSE;
        }
        // try to read the requested amount
        byte[] array = new byte[lLength];
        int totalRead = 0;
        int stalls = 0;
        // keep reading until we either have all data or are at the end of the stream or got 0 bytes after 100 tries
        while (totalRead < lLength && sourceStream.Position < sourceStream.Length && stalls < 100)
        {
          int read = sourceStream.Read(array, totalRead, lLength - totalRead);
          totalRead += read;
          if (read == 0)
            stalls++;
          else
            stalls = 0;
        }
        // copy everything we read into the target buffer
        if (totalRead > 0)
          Marshal.Copy(array, 0, pBuffer, totalRead);
        // only return with success if the requested amount of data was read
        return totalRead == lLength ? S_OK : S_FALSE;
      }
    }

    /// <summary>
    /// Retrieves the total length of the stream.
    /// </summary>
    /// <param name="pTotal">Length of the stream (in bytes).</param>
    /// <param name="pAvailable">Portion of the stream that is currently available (in bytes).</param>
    /// <returns></returns>
    public int Length(out long pTotal, out long pAvailable)
    {
      lock (m_Lock)
      {
        pTotal = pAvailable = sourceStream.Length;
        return NOERROR;
      }
    }

    #endregion
  }
}
