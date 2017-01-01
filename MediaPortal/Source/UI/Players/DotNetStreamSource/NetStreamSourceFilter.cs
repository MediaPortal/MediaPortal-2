#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

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
    protected Stream _sourceStream = null;
    protected DotNetStreamOutputPin _outputPin = null;
    protected string _fileName = null;
    protected bool _streamCreated = false;

    public DotNetStreamSourceFilter()
      : base(".Net Stream Source Filter")
    {
    }

    ~DotNetStreamSourceFilter()
    {
      // Only dispose the underlying stream if we created it.
      if (_streamCreated && _sourceStream != null)
      {
        _sourceStream.Close();
        _sourceStream.Dispose();
        _sourceStream = null;
      }
    }

    protected override int OnInitializePins()
    {
      _outputPin = new DotNetStreamOutputPin("Output", this, _sourceStream);
      AddPin(_outputPin);
      return NOERROR;
    }

    #region IDotNetStreamSourceFilter Members

    public int SetSourceStream(Stream sourceStream, string fileName)
    {
      if (_sourceStream != null)
        return E_UNEXPECTED;
      _sourceStream = sourceStream;
      _fileName = fileName;
      _streamCreated = false;
      return NOERROR;
    }

    #endregion

    #region IFileSourceFilter Members

    public int Load(string pszFileName, AMMediaType pmt)
    {
      if (_sourceStream != null)
        return E_UNEXPECTED;
      _fileName = pszFileName;
      _sourceStream = new FileStream(pszFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
      _streamCreated = true;
      return NOERROR;
    }

    public int GetCurFile(out string pszFileName, AMMediaType pmt)
    {
      pszFileName = _fileName;
      return string.IsNullOrEmpty(pszFileName) ? VFW_E_NOT_CONNECTED : NOERROR;
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
    protected Stream _sourceStream = null;

    public DotNetStreamOutputPin(string name, BaseFilter filter, Stream sourceStream)
      : base(name, filter, filter.FilterLock, PinDirection.Output)
    {
      if (sourceStream == null)
        throw new ArgumentException("Parameter cannot be null!", "sourceStream");

      _sourceStream = sourceStream;
    }

    ~DotNetStreamOutputPin()
    {
      _sourceStream = null;
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
        // The given mediatype as acceptable when the major type is Stream, no subtype and no specific format
        if (pmt.majorType == MediaType.Stream && pmt.subType == MediaSubType.Null && pmt.formatType == FormatType.None)
          return NOERROR;

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
        if (_sourceStream == null)
        {
          return E_UNEXPECTED;
        }
        // Set our MediaType requirements
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
        // We are not working with Allocators, set the outgoing pointer to 0
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
      // We are not working in async mode
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
      // We are not working in async mode
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
      // We are not working with Allocators
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
        // Seek to the requested position if neccessary
        if (_sourceStream.Position != llPosition)
        {
          if (_sourceStream.Seek(llPosition, SeekOrigin.Begin) != llPosition)
            return S_FALSE;
        }
        // Try to read the requested amount
        byte[] array = new byte[lLength];
        int totalRead = 0;
        int stalls = 0;
        // Keep reading until we either have all data or are at the end of the stream or got 0 bytes after 100 tries
        while (totalRead < lLength && _sourceStream.Position < _sourceStream.Length && stalls < 100)
        {
          int read = _sourceStream.Read(array, totalRead, lLength - totalRead);
          totalRead += read;
          if (read == 0)
            stalls++;
          else
            stalls = 0;
        }
        // Copy everything we read into the target buffer
        if (totalRead > 0)
          Marshal.Copy(array, 0, pBuffer, totalRead);
        // Only return with success if the requested amount of data was read
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
        pTotal = pAvailable = _sourceStream.Length;
        return NOERROR;
      }
    }

    #endregion
  }
}
