/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// A picture that is fetched over the internet rather than read from the worn skin: it hangs on a URL,
  /// asks for the bytes once, and shows nothing until they arrive. The home wall's rain radar is the one
  /// picture the wall asks the internet for by itself; everything else the tiles wear is either in the skin
  /// or comes from the server through the API client.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A picture that does not arrive is no news and no complaint: the tile keeps its ground and the card its
  /// other half, exactly as a weather card without a reading keeps the readings it did get. The one shared
  /// client is slow to anger (fifteen seconds) and never retried - a radar tile that timed out once will
  /// likely time out again, and the next wall reading asks with a fresh URL anyway.
  /// </para>
  /// <para>
  /// Answers are kept by address, the newest handful, because walking off the weather tile and back should
  /// not fetch the sky again; a stale tile's address carries the five-minute bucket it was asked for, so a
  /// wall that re-reads gets a URL nobody has asked with before.
  /// </para>
  /// </remarks>
  public sealed class HttpImage : Image
  {
    public static readonly StyledProperty<string?> UrlProperty =
        AvaloniaProperty.Register<HttpImage, string?>(nameof(Url));

    /// <summary>
    /// The width to decode at, 0 for the picture's own. Feed CDNs serve their heroes enormous - FAZ answers
    /// 6144 wide for a tile 430 across - and a wall of 19 headlines decoded at full size is a wall of a
    /// gigabyte of pixels; the tile says how wide it hangs and the picture is born that wide.
    /// </summary>
    public static readonly StyledProperty<int> DecodeWidthProperty =
        AvaloniaProperty.Register<HttpImage, int>(nameof(DecodeWidth), 0);

    private static readonly HttpClient HTTP = CreateClient();
    private static readonly object GATE = new object();
    private static readonly Dictionary<string, IImage?> ANSWERED = new Dictionary<string, IImage?>();

    /// <summary>Answers kept between walls: the news wall hangs nineteen story pictures and six feed marks,
    /// and walking off to the weather and back should not fetch the newspapers again.</summary>
    private const int KEPT_ANSWERS = 64;

    private static int _generation;

    public string? Url
    {
      get => GetValue(UrlProperty);
      set => SetValue(UrlProperty, value);
    }

    public int DecodeWidth
    {
      get => GetValue(DecodeWidthProperty);
      set => SetValue(DecodeWidthProperty, value);
    }

    private static HttpClient CreateClient()
    {
      HttpClient client = new HttpClient();
      client.Timeout = TimeSpan.FromSeconds(15);
      client.DefaultRequestHeaders.UserAgent.ParseAdd("MediaPortal 2");
      return client;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);
      if (change.Property == UrlProperty)
        Ask(change.GetNewValue<string?>());
    }

    private async void Ask(string? url)
    {
      int mine = Interlocked.Increment(ref _generation);
      _asked = mine;

      if (string.IsNullOrEmpty(url))
      {
        Source = null;
        return;
      }

      int width = DecodeWidth;
      string key = width > 0 ? url + "@" + width : url!;
      lock (GATE)
      {
        if (ANSWERED.TryGetValue(key, out IImage? held))
        {
          Source = held;
          return;
        }
      }

      try
      {
        byte[] bytes = await HTTP.GetByteArrayAsync(url!).ConfigureAwait(false);
        // The bytes may come back on a pool thread, but a bitmap is only ever born on the UI thread -
        // decoding on another one throws, and the throw would leave the tile as blank as a refused key.
        await Dispatcher.UIThread.InvokeAsync(delegate
        {
          Bitmap bitmap;
          using (MemoryStream picture = new MemoryStream(bytes, false))
            bitmap = width > 0
                ? Bitmap.DecodeToWidth(picture, width, BitmapInterpolationMode.MediumQuality)
                : new Bitmap(picture);
          lock (GATE)
          {
            if (ANSWERED.Count >= KEPT_ANSWERS)
              ANSWERED.Clear();
            ANSWERED[key] = bitmap;
          }
          if (Volatile.Read(ref _asked) == mine)
            Source = bitmap;
        });
      }
      catch (Exception)
      {
        // No picture is an answer of its own: the card keeps the ground and the rest of its readings,
        // and the next wall reading asks again with a fresh address.
        if (Volatile.Read(ref _asked) == mine)
          Dispatcher.UIThread.Post(delegate { Source = null; });
      }
    }

    private int _asked;
  }
}
