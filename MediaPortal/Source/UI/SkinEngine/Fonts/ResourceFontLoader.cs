#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
#region Third Party Copyright

// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpDX;
using SharpDX.DirectWrite;

namespace MediaPortal.UI.SkinEngine.Fonts
{
  /// <summary>
  /// ResourceFont main loader. This classes implements FontCollectionLoader and FontFileLoader.
  /// It reads all fonts embedded as resource in the current assembly and expose them.
  /// </summary>
  public class ResourceFontLoader : CallbackBase, FontCollectionLoader, FontFileLoader
  {
    private readonly ICollection<String> _fontPaths = new HashSet<String>();
    private readonly List<ResourceFontFileStream> _fontStreams = new List<ResourceFontFileStream>();
    private readonly List<ResourceFontFileEnumerator> _enumerators = new List<ResourceFontFileEnumerator>();
    private FontCollection _fontCollection;
    private DataStream _keyStream;
    private bool _registered = false;

    public void QueueRegisterFont(string ttfPath)
    {
      _fontPaths.Add(ttfPath);
    }

    public void LoadFonts(Factory factory)
    {
      if (_registered)
        return;
      _registered = true;

      factory.RegisterFontFileLoader(this);
      factory.RegisterFontCollectionLoader(this);
      if (_fontPaths.Count > 0)
      {
        foreach (var fontPath in _fontPaths)
          RegisterFont(fontPath);

        _fontCollection = new FontCollection(factory, this, Key);
      }
    }

    public void RegisterFont(string ttfPath)
    {
      byte[] buffer = new byte[512];
      try
      {
        using (var fontBytes = new FileStream(ttfPath, FileMode.Open, FileAccess.Read))
        {
          var stream = new DataStream((int)fontBytes.Length, true, true);
          int read;
          while ((read = fontBytes.Read(buffer, 0, buffer.Length)) > 0)
            stream.Write(buffer, 0, read);

          stream.Position = 0;
          _fontStreams.Add(new ResourceFontFileStream(stream));
        }
        ResetKeyStream();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("DirectWrite FontLoader: Error loading font '{0}'", ex, ttfPath);
      }
    }

    private void ResetKeyStream()
    {
      // Build a Key storage that stores the index of the font
      if (_keyStream != null)
        _keyStream.Dispose();
      _keyStream = new DataStream(sizeof(int) * _fontStreams.Count, true, true);
      for (int i = 0; i < _fontStreams.Count; i++)
        _keyStream.Write(i);
      _keyStream.Position = 0;
    }

    /// <summary>
    /// Gets the key used to identify the FontCollection as well as storing index for fonts.
    /// </summary>
    /// <value>The key.</value>
    public DataStream Key
    {
      get
      {
        return _keyStream;
      }
    }

    /// <summary>
    /// Creates a font file enumerator object that encapsulates a collection of font files. The font system calls back to this interface to create a font collection.
    /// </summary>
    /// <param name="factory">Pointer to the <see cref="SharpDX.DirectWrite.Factory"/> object that was used to create the current font collection.</param>
    /// <param name="collectionKey">A font collection key that uniquely identifies the collection of font files within the scope of the font collection loader being used. The buffer allocated for this key must be at least  the size, in bytes, specified by collectionKeySize.</param>
    /// <returns>
    /// a reference to the newly created font file enumerator.
    /// </returns>
    /// <unmanaged>HRESULT IDWriteFontCollectionLoader::CreateEnumeratorFromKey([None] IDWriteFactory* factory,[In, Buffer] const void* collectionKey,[None] int collectionKeySize,[Out] IDWriteFontFileEnumerator** fontFileEnumerator)</unmanaged>
    FontFileEnumerator FontCollectionLoader.CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey)
    {
      var enumerator = new ResourceFontFileEnumerator(factory, this, collectionKey);
      _enumerators.Add(enumerator);

      return enumerator;
    }

    /// <summary>
    /// Creates a font file stream object that encapsulates an open file resource.
    /// </summary>
    /// <param name="fontFileReferenceKey">A reference to a font file reference key that uniquely identifies the font file resource within the scope of the font loader being used. The buffer allocated for this key must at least be the size, in bytes, specified by  fontFileReferenceKeySize.</param>
    /// <returns>
    /// a reference to the newly created <see cref="SharpDX.DirectWrite.FontFileStream"/> object.
    /// </returns>
    /// <remarks>
    /// The resource is closed when the last reference to fontFileStream is released.
    /// </remarks>
    /// <unmanaged>HRESULT IDWriteFontFileLoader::CreateStreamFromKey([In, Buffer] const void* fontFileReferenceKey,[None] int fontFileReferenceKeySize,[Out] IDWriteFontFileStream** fontFileStream)</unmanaged>
    FontFileStream FontFileLoader.CreateStreamFromKey(DataPointer fontFileReferenceKey)
    {
      var index = SharpDX.Utilities.Read<int>(fontFileReferenceKey.Pointer);
      return _fontStreams[index];
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      if (_keyStream != null)
        _keyStream.Dispose();
      if (_fontStreams != null)
        foreach (var stream in _fontStreams)
          stream.Dispose();
      if (_enumerators != null)
        foreach (var stream in _enumerators)
          stream.Dispose();
      if (_fontCollection != null)
        _fontCollection.Dispose();
    }
  }
}
