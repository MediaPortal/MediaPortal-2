#region Copyright (C) 2007-2023 Team MediaPortal

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

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;

namespace Tests.Frontend
{
  /// <summary>
  /// The four services the shares endpoint asks for, with an in-memory share list behind them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Only these four are faked, and only because they need a running server: the library wants a database and a
  /// plugin manager, the importer wants a worker thread. The endpoint above them, the HTTP layer, the client and
  /// the screen are all the real thing, which is the point - a share request that never left the process has not
  /// been tested.
  /// </para>
  /// <para>
  /// It starts with two folders: one on this machine and one on another, because that difference is the one the
  /// screen has to get right. A folder the server cannot reach cannot be imported, and offering the button for it
  /// is offering a refusal.
  /// </para>
  /// </remarks>
  internal sealed class SharesFakes
  {
    public const string LOCAL_SYSTEM = "11111111-1111-1111-1111-111111111111";
    public const string OTHER_SYSTEM = "22222222-2222-2222-2222-222222222222";

    public static readonly Guid LOCAL_SHARE = new Guid("33333333-3333-3333-3333-333333333333");
    public static readonly Guid REMOTE_SHARE = new Guid("44444444-4444-4444-4444-444444444444");

    private readonly string _folder;
    private int _emptiesOnRead;
    private int _reads;

    public SharesFakes(string folder)
    {
      _folder = folder;

      Shares[REMOTE_SHARE] = new Share(REMOTE_SHARE, OTHER_SYSTEM,
          LocalFsResourceProviderBase.ToResourcePath(@"\\nas\series"), "Alien Earth", true, new[] { "Series" });
      Shares[LOCAL_SHARE] = new Share(LOCAL_SHARE, LOCAL_SYSTEM,
          LocalFsResourceProviderBase.ToResourcePath(folder), "Films", true, new[] { "Movie" });

      Library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      Library.Setup(get => get.GetShares(It.IsAny<string>()))
          .Returns<string>(systemId => Shares
              .Where(pair => systemId == null || pair.Value.SystemId == systemId)
              .ToDictionary(pair => pair.Key, pair => pair.Value));
      Library.Setup(get => get.GetShare(It.IsAny<Guid>()))
          .Returns<Guid>(id => Shares.TryGetValue(id, out Share share) ? share : null);
      Library.Setup(get => get.RegisterShare(It.IsAny<Share>()))
          .Callback<Share>(share =>
          {
            Registered.Add(share);
            if (Shares.Values.Any(other => other.SystemId == share.SystemId
                    && other.BaseResourcePath == share.BaseResourcePath))
              throw new ShareExistsException("a share for that folder is already registered");
            Shares[share.ShareId] = share;
          });
      Library.Setup(get => get.RemoveShare(It.IsAny<Guid>()))
          .Callback<Guid>(id => Shares.Remove(id));
      Library.Setup(get => get.UpdateShare(It.IsAny<Guid>(), It.IsAny<ResourcePath>(), It.IsAny<string>(),
              It.IsAny<bool>(), It.IsAny<IEnumerable<string>>(),
              It.IsAny<MediaPortal.Backend.MediaLibrary.RelocationMode>()))
          .Returns<Guid, ResourcePath, string, bool, IEnumerable<string>,
              MediaPortal.Backend.MediaLibrary.RelocationMode>(
              (id, path, name, watcher, categories, relocation) =>
              {
                Updates.Add(new Update(id, name, categories.ToArray(), watcher, path, relocation));
                if (!Shares.TryGetValue(id, out Share existing))
                  return 0;

                Shares[id] = new Share(id, existing.SystemId, path, name, watcher, categories);
                return 0;
              });

      Resolver = new Mock<ISystemResolver>(MockBehavior.Strict);
      Resolver.SetupGet(get => get.LocalSystemId).Returns(LOCAL_SYSTEM);

      Accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      Accessor.SetupGet(get => get.MediaCategories).Returns(new Dictionary<string, MediaCategory>
      {
          { "Movie", new MediaCategory("Movie", null) },
          { "Series", new MediaCategory("Series", null) },
          { "Video", new MediaCategory("Video", null) }
      });

      Importer = new Mock<IImporterWorker>(MockBehavior.Strict);
      Importer.SetupGet(get => get.IsSuspended).Returns(() => Suspended);
      Importer.SetupGet(get => get.ImportJobs).Returns(() =>
      {
        if (_emptiesOnRead > 0 && _reads++ >= _emptiesOnRead)
          Jobs.Clear();
        return Jobs;
      });
      Importer.Setup(get => get.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(),
              It.IsAny<bool>()))
          .Callback<ResourcePath, IEnumerable<string>, bool>(
              (path, categories, subFolders) => Scheduled.Add(path.Serialize()));
    }

    public Mock<IMediaLibrary> Library { get; }
    public Mock<ISystemResolver> Resolver { get; }
    public Mock<IMediaAccessor> Accessor { get; }
    public Mock<IImporterWorker> Importer { get; }

    /// <summary>The folders the server knows.</summary>
    public Dictionary<Guid, Share> Shares { get; } = new Dictionary<Guid, Share>();

    /// <summary>What the server was asked to register, in the order it was asked.</summary>
    public List<Share> Registered { get; } = new List<Share>();

    /// <summary>One edit the server was asked to make, as the fake library holds it.</summary>
    internal sealed record Update(Guid Id, string Name, string[] Categories, bool Watcher,
        ResourcePath BasePath, MediaPortal.Backend.MediaLibrary.RelocationMode Relocation);

    /// <summary>What the edit call was asked to change, in the order it was asked.</summary>
    public List<Update> Updates { get; } = new List<Update>();

    /// <summary>The paths the server scheduled an import for.</summary>
    public List<string> Scheduled { get; } = new List<string>();

    /// <summary>
    /// The jobs the importer holds. One is in it from the start: a library that is always idle would let every
    /// test pass without ever having looked at the "still importing" line.
    /// </summary>
    public List<ImportJobInformation> Jobs { get; } = new List<ImportJobInformation>
    {
        new ImportJobInformation(ImportJobType.Import,
            LocalFsResourceProviderBase.ToResourcePath(@"\\nas\series"), Array.Empty<Guid>(), true)
    };

    /// <summary>The folder this machine has, which the "Films" share points at.</summary>
    public string LocalFolder { get { return _folder; } }

    /// <summary>Whether the importer is holding its jobs without working on them.</summary>
    public bool Suspended { get; set; }

    /// <summary>
    /// Makes the queue empty itself after the given number of further looks. A queue that is always empty or always
    /// full lets every test pass without the screen ever having distinguished "still working" from "finished", and
    /// counting reads rather than waiting on a clock keeps the answer the same on every machine.
    /// </summary>
    public void ArmQueueDrainAfterReads(int reads)
    {
      _reads = 0;
      _emptiesOnRead = reads;
    }

    /// <summary>Forgets every folder, which is what a test about an empty library asks for.</summary>
    public void ClearShares()
    {
      Shares.Clear();
    }
  }
}
