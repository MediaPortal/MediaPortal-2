using System;
using System.Collections.Generic;
using Avalonia;
using SkiaSharp;

namespace LiquidGlassAvaloniaUI
{
    internal sealed class LiquidGlassBackdropSnapshot : IDisposable
    {
        internal readonly struct FilteredKey : IEquatable<FilteredKey>
        {
            public FilteredKey(
                int brightnessQ,
                int contrastQ,
                int saturationQ,
                int exposureEvQ,
                int opacityQ,
                int blurSigmaPxQ,
                int cropX,
                int cropY,
                int cropWidth,
                int cropHeight,
                int renderContextId)
            {
                BrightnessQ = brightnessQ;
                ContrastQ = contrastQ;
                SaturationQ = saturationQ;
                ExposureEvQ = exposureEvQ;
                OpacityQ = opacityQ;
                BlurSigmaPxQ = blurSigmaPxQ;
                CropX = cropX;
                CropY = cropY;
                CropWidth = cropWidth;
                CropHeight = cropHeight;
                RenderContextId = renderContextId;
            }

            public int BrightnessQ { get; }
            public int ContrastQ { get; }
            public int SaturationQ { get; }
            public int ExposureEvQ { get; }
            public int OpacityQ { get; }
            public int BlurSigmaPxQ { get; }
            public int CropX { get; }
            public int CropY { get; }
            public int CropWidth { get; }
            public int CropHeight { get; }
            public int RenderContextId { get; }

            public bool Equals(FilteredKey other)
            {
                return BrightnessQ == other.BrightnessQ
                       && ContrastQ == other.ContrastQ
                       && SaturationQ == other.SaturationQ
                       && ExposureEvQ == other.ExposureEvQ
                       && OpacityQ == other.OpacityQ
                       && BlurSigmaPxQ == other.BlurSigmaPxQ
                       && CropX == other.CropX
                       && CropY == other.CropY
                       && CropWidth == other.CropWidth
                       && CropHeight == other.CropHeight
                       && RenderContextId == other.RenderContextId;
            }

            public override bool Equals(object? obj)
            {
                return obj is FilteredKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = BrightnessQ;
                    hashCode = hashCode * 397 ^ ContrastQ;
                    hashCode = hashCode * 397 ^ SaturationQ;
                    hashCode = hashCode * 397 ^ ExposureEvQ;
                    hashCode = hashCode * 397 ^ OpacityQ;
                    hashCode = hashCode * 397 ^ BlurSigmaPxQ;
                    hashCode = hashCode * 397 ^ CropX;
                    hashCode = hashCode * 397 ^ CropY;
                    hashCode = hashCode * 397 ^ CropWidth;
                    hashCode = hashCode * 397 ^ CropHeight;
                    hashCode = hashCode * 397 ^ RenderContextId;
                    return hashCode;
                }
            }
        }

        internal readonly struct FilteredResult
        {
            public FilteredResult(SKImage image, PixelPoint originInPixels)
            {
                Image = image ?? throw new ArgumentNullException(nameof(image));
                OriginInPixels = originInPixels;
            }

            public SKImage Image { get; }

            public PixelPoint OriginInPixels { get; }
        }

        private int _leases;
        private int _disposeRequested;
        private int _disposed;
        private readonly object _filteredLock = new();
        private Dictionary<FilteredKey, FilteredResult>? _filtered;
        private Queue<FilteredKey>? _filteredOrder;
        private const int MaxFilteredEntries = 8;

        public LiquidGlassBackdropSnapshot(SKImage image, PixelPoint originInPixels, PixelSize pixelSize, double scaling)
        {
            Image = image ?? throw new ArgumentNullException(nameof(image));
            OriginInPixels = originInPixels;
            PixelSize = pixelSize;
            Scaling = scaling;
        }

        public SKImage Image { get; }

        /// <summary>
        /// Snapshot origin in device pixels (TopLevel coordinate space).
        /// </summary>
        public PixelPoint OriginInPixels { get; }

        public PixelSize PixelSize { get; }

        public double Scaling { get; }

        public bool TryGetFiltered(FilteredKey key, out FilteredResult result)
        {
            lock (_filteredLock)
            {
                if (_filtered is null)
                {
                    result = default;
                    return false;
                }

                return _filtered.TryGetValue(key, out result);
            }
        }

        public void StoreFiltered(FilteredKey key, FilteredResult result)
        {
            lock (_filteredLock)
            {
                _filtered ??= new Dictionary<FilteredKey, FilteredResult>();
                _filteredOrder ??= new Queue<FilteredKey>();

                if (_filtered.TryGetValue(key, out FilteredResult existing))
                {
                    existing.Image.Dispose();
                    _filtered[key] = result;
                    return;
                }

                _filtered[key] = result;
                _filteredOrder.Enqueue(key);

                while (_filtered.Count > MaxFilteredEntries && _filteredOrder.Count > 0)
                {
                    FilteredKey evictKey = _filteredOrder.Dequeue();
                    if (_filtered.TryGetValue(evictKey, out FilteredResult evictValue) && !evictKey.Equals(key))
                    {
                        _filtered.Remove(evictKey);
                        evictValue.Image.Dispose();
                    }
                }
            }
        }

        public bool TryAddLease()
        {
            while (true)
            {
                if (System.Threading.Volatile.Read(ref _disposed) != 0)
                    return false;

                int current = System.Threading.Volatile.Read(ref _leases);
                if (System.Threading.Interlocked.CompareExchange(ref _leases, current + 1, current) == current)
                {
                    if (System.Threading.Volatile.Read(ref _disposed) != 0)
                    {
                        ReleaseLease();
                        return false;
                    }

                    return true;
                }
            }
        }

        public void ReleaseLease()
        {
            int remaining = System.Threading.Interlocked.Decrement(ref _leases);
            if (remaining < 0)
                return;

            if (remaining == 0 && System.Threading.Volatile.Read(ref _disposeRequested) != 0)
                Dispose();
        }

        public void RequestDispose()
        {
            System.Threading.Volatile.Write(ref _disposeRequested, 1);
            if (System.Threading.Volatile.Read(ref _leases) == 0)
                Dispose();
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            lock (_filteredLock)
            {
                if (_filtered is not null)
                {
                    foreach (KeyValuePair<FilteredKey, FilteredResult> pair in _filtered)
                        pair.Value.Image.Dispose();
                    _filtered = null;
                    _filteredOrder = null;
                }
            }

            Image.Dispose();
        }
    }
}
