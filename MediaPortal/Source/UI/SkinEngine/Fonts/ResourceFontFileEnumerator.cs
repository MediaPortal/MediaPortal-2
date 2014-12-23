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

using SharpDX;
using SharpDX.DirectWrite;

namespace MediaPortal.UI.SkinEngine.Fonts
{
  /// <summary>
  /// Resource FontFileEnumerator.
  /// </summary>
  public class ResourceFontFileEnumerator : CallbackBase, FontFileEnumerator
  {
    private readonly Factory _factory;
    private readonly FontFileLoader _loader;
    private readonly DataStream _keyStream;
    private FontFile _currentFontFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFontFileEnumerator"/> class.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="loader">The loader.</param>
    /// <param name="key">The key.</param>
    public ResourceFontFileEnumerator(Factory factory, FontFileLoader loader, DataPointer key)
    {
      _factory = factory;
      _loader = loader;
      _keyStream = new DataStream(key.Pointer, key.Size, true, false);
    }

    /// <summary>
    /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned before the first element of the collection and the first call to MoveNext advances to the first file.
    /// </summary>
    /// <returns>
    /// the value TRUE if the enumerator advances to a file; otherwise, FALSE if the enumerator advances past the last file in the collection.
    /// </returns>
    /// <unmanaged>HRESULT IDWriteFontFileEnumerator::MoveNext([Out] BOOL* hasCurrentFile)</unmanaged>
    bool FontFileEnumerator.MoveNext()
    {
      bool moveNext = _keyStream.RemainingLength != 0;
      if (moveNext)
      {
        if (_currentFontFile != null)
          _currentFontFile.Dispose();

        _currentFontFile = new FontFile(_factory, _keyStream.PositionPointer, 4, _loader);
        _keyStream.Position += 4;
      }
      return moveNext;
    }

    /// <summary>
    /// Gets a reference to the current font file.
    /// </summary>
    /// <value></value>
    /// <returns>a reference to the newly created <see cref="SharpDX.DirectWrite.FontFile"/> object.</returns>
    /// <unmanaged>HRESULT IDWriteFontFileEnumerator::GetCurrentFontFile([Out] IDWriteFontFile** fontFile)</unmanaged>
    FontFile FontFileEnumerator.CurrentFontFile
    {
      get
      {
        ((IUnknown)_currentFontFile).AddReference();
        return _currentFontFile;
      }
    }
  }
}
