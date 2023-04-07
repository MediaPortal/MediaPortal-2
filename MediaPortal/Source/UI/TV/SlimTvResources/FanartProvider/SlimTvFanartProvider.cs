#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.LogoManager;
using MediaPortal.LogoManager.ChannelManagerService;
using MediaPortal.LogoManager.Design;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider
{
  public class SlimTvFanartProvider : IFanArtProvider, IDisposable
  {
    protected readonly SlimTvLogoSettings _settings;
    protected string _dataFolder;
    protected RegionInfo _country;
    // Allow cached logos to be updated every 2 weeks
    protected TimeSpan MAX_CACHE_DURATION = TimeSpan.FromDays(14);

    private readonly string _designsFolder;
    private readonly LogoRepository _repo;
    private readonly MemoryCache _failureCache = new MemoryCache("MissingLogoNames");

    public SlimTvFanartProvider()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvLogoSettings>();
      _dataFolder = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>\\Logos\\");
      var currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _country = new RegionInfo(currentCulture.Name);
      _repo = new LogoRepository(_settings.RepositoryUrl);
    }

    public SlimTvFanartProvider(string designsFolder)
      : this()
    {
      _designsFolder = designsFolder;
    }

    public FanArtProviderSource Source { get { return FanArtProviderSource.File; } }

    /// <summary>
    /// Gets a list of <see cref="FanArtImage"/>s for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
    /// The name can be: Series name, Actor name, Artist name depending on the <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Requested FanArtMediaType</param>
    /// <param name="fanArtType">Requested FanArtType</param>
    /// <param name="name">Requested name of Series, Actor, Artist...</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
    /// <param name="result">Result if return code is <c>true</c>.</param>
    /// <returns><c>true</c> if at least one match was found.</returns>
    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != FanArtMediaTypes.ChannelTv && mediaType != FanArtMediaTypes.ChannelRadio)
        return false;

      try
      {
        ChannelType logoChannelType = mediaType == FanArtMediaTypes.ChannelTv ? ChannelType.Tv : ChannelType.Radio;
        var processed = UpdateLogosAsync(new[] { name }, logoChannelType).Result;

        string logoFileName = null;
        if (processed != null && processed.TryGetValue(name, out logoFileName))
        { }

        return BuildLogoResourceLocatorAndReturn(ref result, logoFileName);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTv Logos: Error processing logo image.", ex);
      }
      return false;
    }

    private static bool BuildLogoResourceLocatorAndReturn(ref IList<IResourceLocator> result, string logoFileName)
    {
      if (!File.Exists(logoFileName))
        return false;
      result = new List<IResourceLocator> { new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, logoFileName)) };
      return true;
    }

    /// <summary>
    /// Checks if the cached logo is still valid, if it is too old it will be re-downloaded. Logo themes
    /// can prevent this behavior by setting the <see cref="Theme.SkipOnlineUpdate"/> to <c>true</c>.
    /// </summary>
    /// <param name="theme">Current logo theme</param>
    /// <param name="logoFileName">Cached logo name</param>
    /// <returns><c>true</c> if logo exists and cache duration is valid</returns>
    private bool IsCacheValid(Theme theme, string logoFileName)
    {
      FileInfo fi = new FileInfo(logoFileName);
      return !_settings.EnableAutoUpdate || theme.SkipOnlineUpdate || DateTime.Now - fi.CreationTime <= MAX_CACHE_DURATION;
    }

    /// <summary>
    /// Downloads all logos for TV and Radio channels.
    /// </summary>
    public async Task<bool> UpdateLogosAsync()
    {
      ITvProvider handler = ServiceRegistration.Get<ITvProvider>(false);
      var channelAndGroupInfo = handler as IChannelAndGroupInfoAsync;
      if (handler == null || channelAndGroupInfo == null)
        return false;

      var allGroups = await channelAndGroupInfo.GetChannelGroupsAsync();
      HashSet<string> tvChannels = new HashSet<string>();
      HashSet<string> radioChannels = new HashSet<string>();
      if (!allGroups.Success)
        return false;

      foreach (var group in allGroups.Result)
      {
        var allChannels = await channelAndGroupInfo.GetChannelsAsync(group);
        if (allChannels.Success)
        {
          CollectionUtils.AddAll(tvChannels, allChannels.Result.Where(c => c.MediaType == MediaType.TV).Select(c => c.Name));
          CollectionUtils.AddAll(radioChannels, allChannels.Result.Where(c => c.MediaType == MediaType.Radio).Select(c => c.Name));
        }
      }

      await UpdateLogosAsync(tvChannels, ChannelType.Tv);
      await UpdateLogosAsync(radioChannels, ChannelType.Radio);

      return true;
    }

    /// <summary>
    /// Updates channel logos for the given list of <paramref name="names"/> of type <paramref name="logoChannelType"/>.
    /// First this method checks every logo if it already exists and is not yet expired. All others are looked up in batch and are processed.
    /// </summary>
    /// <param name="names">Channel names</param>
    /// <param name="logoChannelType">Channel type</param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> UpdateLogosAsync(ICollection<string> names, ChannelType logoChannelType)
    {
      var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

      string designsFolder = _designsFolder ?? FileUtils.BuildAssemblyRelativePath("Designs");
      ThemeHandler themeHandler = new ThemeHandler();
      Theme theme = themeHandler.Load(Path.Combine(designsFolder, _settings.LogoTheme));

      string logoFolder = Path.Combine(_dataFolder, string.Format("{0}-{1}-{2}", logoChannelType, theme.DesignName, theme.ThemeName));

      if (!Directory.Exists(logoFolder))
        Directory.CreateDirectory(logoFolder);

      foreach (string name in names)
      {
        string logoFileName = Path.Combine(logoFolder, FileUtils.GetSafeFilename(string.Format("{0}.png", name)));
        var needsUpdate = !File.Exists(logoFileName) || !IsCacheValid(theme, logoFileName);
        result[name] = needsUpdate ? null : logoFileName;
      }

      var nonExistingOrExpiredNames = result.Where(r => r.Value == null).Select(r => r.Key).ToArray();

      if (nonExistingOrExpiredNames.Length == 0)
        return result;

      LogoProcessor processor = new LogoProcessor { DesignsFolder = designsFolder };

      ServiceRegistration.Get<ILogger>().Info("SlimTv Logos: Starting processing of channel logos for {0} {1} channel(s): {2}", nonExistingOrExpiredNames.Length, logoChannelType, String.Join(", ",  nonExistingOrExpiredNames.ToArray()));

      // From repository
      var logos = await _repo.DownloadAsync(nonExistingOrExpiredNames, logoChannelType, _country.TwoLetterISORegionName);

      foreach (KeyValuePair<string, Stream> nameAndLogoPair in logos)
      {
        // First process the new logo, but keep the existing file if something fails.
        var channelName = nameAndLogoPair.Key;
        var logoStream = nameAndLogoPair.Value;

        ServiceRegistration.Get<ILogger>().Info("SlimTv Logos:  - processing logo for {0} channel '{1}'", logoChannelType, channelName);

        string logoFileName = Path.Combine(logoFolder, FileUtils.GetSafeFilename(string.Format("{0}.png", channelName)));
        string tmpLogoFileName = Path.ChangeExtension(logoFileName, ".tmplogo");
        if (processor.CreateLogo(theme, logoStream, tmpLogoFileName))
        {
          if (File.Exists(logoFileName))
            File.Delete(logoFileName);
          File.Move(tmpLogoFileName, logoFileName);
          result[channelName] = logoFileName;
        }
      }

      var missingOnline = nonExistingOrExpiredNames.Except(logos.Keys).ToArray();
      if (missingOnline.Length > 0)
      {
        ServiceRegistration.Get<ILogger>().Info("SlimTv Logos: Could not find logo for {0} {1} channels: {2}", missingOnline.Length, logoChannelType, String.Join(", ", missingOnline));
      }

      return result;
    }

    public void Dispose()
    {
      _repo.Dispose();
    }
  }
}
