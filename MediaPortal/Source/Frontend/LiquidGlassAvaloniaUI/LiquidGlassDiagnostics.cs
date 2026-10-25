using System.Threading;

namespace LiquidGlassAvaloniaUI
{
    public readonly struct LiquidGlassDiagnosticsSnapshot
    {
        internal LiquidGlassDiagnosticsSnapshot(
            long captureQueueRequests,
            long captureQueueCoalesced,
            long capturesStarted,
            long capturesSkippedReentrant,
            long capturesSkippedNoSubscribers,
            long capturesSkippedEmptyClip,
            long capturesSkippedByCadence,
            long capturesSkippedByDirtyRect,
            long capturesSkippedByHash,
            long capturesPublished,
            long scratchBitmapRecreated,
            long rendererInvalidations,
            long dirtyRectAvailable,
            long dirtyRectUnavailable,
            long subscriberInvalidations,
            long sampledHashChecks,
            long sampledHashBytes,
            long fullBitmapCopies,
            long fullBitmapCopyBytes,
            long filterIdentityHits,
            long filterCacheHits,
            long filterCacheMisses,
            long filterGpuSurfaces,
            long filterCpuSurfaces,
            long filterSurfaceFailures)
        {
            CaptureQueueRequests = captureQueueRequests;
            CaptureQueueCoalesced = captureQueueCoalesced;
            CapturesStarted = capturesStarted;
            CapturesSkippedReentrant = capturesSkippedReentrant;
            CapturesSkippedNoSubscribers = capturesSkippedNoSubscribers;
            CapturesSkippedEmptyClip = capturesSkippedEmptyClip;
            CapturesSkippedByCadence = capturesSkippedByCadence;
            CapturesSkippedByDirtyRect = capturesSkippedByDirtyRect;
            CapturesSkippedByHash = capturesSkippedByHash;
            CapturesPublished = capturesPublished;
            ScratchBitmapRecreated = scratchBitmapRecreated;
            RendererInvalidations = rendererInvalidations;
            DirtyRectAvailable = dirtyRectAvailable;
            DirtyRectUnavailable = dirtyRectUnavailable;
            SubscriberInvalidations = subscriberInvalidations;
            SampledHashChecks = sampledHashChecks;
            SampledHashBytes = sampledHashBytes;
            FullBitmapCopies = fullBitmapCopies;
            FullBitmapCopyBytes = fullBitmapCopyBytes;
            FilterIdentityHits = filterIdentityHits;
            FilterCacheHits = filterCacheHits;
            FilterCacheMisses = filterCacheMisses;
            FilterGpuSurfaces = filterGpuSurfaces;
            FilterCpuSurfaces = filterCpuSurfaces;
            FilterSurfaceFailures = filterSurfaceFailures;
        }

        public long CaptureQueueRequests { get; }
        public long CaptureQueueCoalesced { get; }
        public long CapturesStarted { get; }
        public long CapturesSkippedReentrant { get; }
        public long CapturesSkippedNoSubscribers { get; }
        public long CapturesSkippedEmptyClip { get; }
        public long CapturesSkippedByCadence { get; }
        public long CapturesSkippedByDirtyRect { get; }
        public long CapturesSkippedByHash { get; }
        public long CapturesPublished { get; }
        public long ScratchBitmapRecreated { get; }
        public long RendererInvalidations { get; }
        public long DirtyRectAvailable { get; }
        public long DirtyRectUnavailable { get; }
        public long SubscriberInvalidations { get; }
        public long SampledHashChecks { get; }
        public long SampledHashBytes { get; }
        public long FullBitmapCopies { get; }
        public long FullBitmapCopyBytes { get; }
        public long FilterIdentityHits { get; }
        public long FilterCacheHits { get; }
        public long FilterCacheMisses { get; }
        public long FilterGpuSurfaces { get; }
        public long FilterCpuSurfaces { get; }
        public long FilterSurfaceFailures { get; }
    }

    public static class LiquidGlassDiagnostics
    {
        private static long s_captureQueueRequests;
        private static long s_captureQueueCoalesced;
        private static long s_capturesStarted;
        private static long s_capturesSkippedReentrant;
        private static long s_capturesSkippedNoSubscribers;
        private static long s_capturesSkippedEmptyClip;
        private static long s_capturesSkippedByCadence;
        private static long s_capturesSkippedByDirtyRect;
        private static long s_capturesSkippedByHash;
        private static long s_capturesPublished;
        private static long s_scratchBitmapRecreated;
        private static long s_rendererInvalidations;
        private static long s_dirtyRectAvailable;
        private static long s_dirtyRectUnavailable;
        private static long s_subscriberInvalidations;
        private static long s_sampledHashChecks;
        private static long s_sampledHashBytes;
        private static long s_fullBitmapCopies;
        private static long s_fullBitmapCopyBytes;
        private static long s_filterIdentityHits;
        private static long s_filterCacheHits;
        private static long s_filterCacheMisses;
        private static long s_filterGpuSurfaces;
        private static long s_filterCpuSurfaces;
        private static long s_filterSurfaceFailures;

        public static LiquidGlassDiagnosticsSnapshot Snapshot
        {
            get => new(
                Volatile.Read(ref s_captureQueueRequests),
                Volatile.Read(ref s_captureQueueCoalesced),
                Volatile.Read(ref s_capturesStarted),
                Volatile.Read(ref s_capturesSkippedReentrant),
                Volatile.Read(ref s_capturesSkippedNoSubscribers),
                Volatile.Read(ref s_capturesSkippedEmptyClip),
                Volatile.Read(ref s_capturesSkippedByCadence),
                Volatile.Read(ref s_capturesSkippedByDirtyRect),
                Volatile.Read(ref s_capturesSkippedByHash),
                Volatile.Read(ref s_capturesPublished),
                Volatile.Read(ref s_scratchBitmapRecreated),
                Volatile.Read(ref s_rendererInvalidations),
                Volatile.Read(ref s_dirtyRectAvailable),
                Volatile.Read(ref s_dirtyRectUnavailable),
                Volatile.Read(ref s_subscriberInvalidations),
                Volatile.Read(ref s_sampledHashChecks),
                Volatile.Read(ref s_sampledHashBytes),
                Volatile.Read(ref s_fullBitmapCopies),
                Volatile.Read(ref s_fullBitmapCopyBytes),
                Volatile.Read(ref s_filterIdentityHits),
                Volatile.Read(ref s_filterCacheHits),
                Volatile.Read(ref s_filterCacheMisses),
                Volatile.Read(ref s_filterGpuSurfaces),
                Volatile.Read(ref s_filterCpuSurfaces),
                Volatile.Read(ref s_filterSurfaceFailures));
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref s_captureQueueRequests, 0);
            Interlocked.Exchange(ref s_captureQueueCoalesced, 0);
            Interlocked.Exchange(ref s_capturesStarted, 0);
            Interlocked.Exchange(ref s_capturesSkippedReentrant, 0);
            Interlocked.Exchange(ref s_capturesSkippedNoSubscribers, 0);
            Interlocked.Exchange(ref s_capturesSkippedEmptyClip, 0);
            Interlocked.Exchange(ref s_capturesSkippedByCadence, 0);
            Interlocked.Exchange(ref s_capturesSkippedByDirtyRect, 0);
            Interlocked.Exchange(ref s_capturesSkippedByHash, 0);
            Interlocked.Exchange(ref s_capturesPublished, 0);
            Interlocked.Exchange(ref s_scratchBitmapRecreated, 0);
            Interlocked.Exchange(ref s_rendererInvalidations, 0);
            Interlocked.Exchange(ref s_dirtyRectAvailable, 0);
            Interlocked.Exchange(ref s_dirtyRectUnavailable, 0);
            Interlocked.Exchange(ref s_subscriberInvalidations, 0);
            Interlocked.Exchange(ref s_sampledHashChecks, 0);
            Interlocked.Exchange(ref s_sampledHashBytes, 0);
            Interlocked.Exchange(ref s_fullBitmapCopies, 0);
            Interlocked.Exchange(ref s_fullBitmapCopyBytes, 0);
            Interlocked.Exchange(ref s_filterIdentityHits, 0);
            Interlocked.Exchange(ref s_filterCacheHits, 0);
            Interlocked.Exchange(ref s_filterCacheMisses, 0);
            Interlocked.Exchange(ref s_filterGpuSurfaces, 0);
            Interlocked.Exchange(ref s_filterCpuSurfaces, 0);
            Interlocked.Exchange(ref s_filterSurfaceFailures, 0);
        }

        internal static void RecordCaptureQueueRequest() => Interlocked.Increment(ref s_captureQueueRequests);
        internal static void RecordCaptureQueueCoalesced() => Interlocked.Increment(ref s_captureQueueCoalesced);
        internal static void RecordCaptureStarted() => Interlocked.Increment(ref s_capturesStarted);
        internal static void RecordCaptureSkippedReentrant() => Interlocked.Increment(ref s_capturesSkippedReentrant);
        internal static void RecordCaptureSkippedNoSubscribers() => Interlocked.Increment(ref s_capturesSkippedNoSubscribers);
        internal static void RecordCaptureSkippedEmptyClip() => Interlocked.Increment(ref s_capturesSkippedEmptyClip);
        internal static void RecordCaptureSkippedByCadence() => Interlocked.Increment(ref s_capturesSkippedByCadence);
        internal static void RecordCaptureSkippedByDirtyRect() => Interlocked.Increment(ref s_capturesSkippedByDirtyRect);
        internal static void RecordCaptureSkippedByHash() => Interlocked.Increment(ref s_capturesSkippedByHash);
        internal static void RecordCapturePublished() => Interlocked.Increment(ref s_capturesPublished);
        internal static void RecordScratchBitmapRecreated() => Interlocked.Increment(ref s_scratchBitmapRecreated);
        internal static void RecordRendererInvalidation() => Interlocked.Increment(ref s_rendererInvalidations);
        internal static void RecordDirtyRectAvailable() => Interlocked.Increment(ref s_dirtyRectAvailable);
        internal static void RecordDirtyRectUnavailable() => Interlocked.Increment(ref s_dirtyRectUnavailable);
        internal static void RecordSubscriberInvalidation() => Interlocked.Increment(ref s_subscriberInvalidations);
        internal static void RecordSampledHash(long bytesCopied)
        {
            Interlocked.Increment(ref s_sampledHashChecks);
            Interlocked.Add(ref s_sampledHashBytes, bytesCopied);
        }

        internal static void RecordFullBitmapCopy(long bytesCopied)
        {
            Interlocked.Increment(ref s_fullBitmapCopies);
            Interlocked.Add(ref s_fullBitmapCopyBytes, bytesCopied);
        }

        internal static void RecordFilterIdentityHit() => Interlocked.Increment(ref s_filterIdentityHits);
        internal static void RecordFilterCacheHit() => Interlocked.Increment(ref s_filterCacheHits);
        internal static void RecordFilterCacheMiss() => Interlocked.Increment(ref s_filterCacheMisses);
        internal static void RecordFilterGpuSurface() => Interlocked.Increment(ref s_filterGpuSurfaces);
        internal static void RecordFilterCpuSurface() => Interlocked.Increment(ref s_filterCpuSurfaces);
        internal static void RecordFilterSurfaceFailure() => Interlocked.Increment(ref s_filterSurfaceFailures);
    }
}
