using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using NUnit.Framework;
using SlimTv.TvMosaicProvider;
using SlimTv.TvMosaicProvider.Settings;
using TvMosaic.API;

namespace Test.TVMosaic
{
  [TestFixture]
  public class TvMosaic
  {
    private ITvProvider _provider;

    [SetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      FakeSettings<TvMosaicProviderSettings> settings = new FakeSettings<TvMosaicProviderSettings>(new TvMosaicProviderSettings { Host = "localhost" });
      ServiceRegistration.Set<ISettingsManager>(settings);

      _provider = new TvMosaicProvider();
      _provider.Init();
    }

    [TearDown]
    public void DeInit()
    {
      _provider.DeInit();
    }

    [Test]
    public async Task TestChannelGroups()
    {
      var channelInfo = _provider as IChannelAndGroupInfoAsync;
      Assert.IsNotNull(channelInfo);
      var channelGroupResult = await channelInfo.GetChannelGroupsAsync();
      Assert.IsTrue(channelGroupResult.Success);
      Assert.IsNotNull(channelGroupResult.Result);
    }

    [Test]
    public async Task TestSerializeFavorites()
    {
      var serialize = HttpDataProvider.Serialize(new Favorites
      {
        new Favorite
        {
          Flags = 1,
          Id = Guid.NewGuid(),
          Name = "Unit Test",
          Channels = new FavoriteChannels
          {
            "1:2:3",
            "4:5:6"
          }
        }
      });
    }

    [Test]
    public async Task TestChannels()
    {
      var channelInfo = _provider as IChannelAndGroupInfoAsync;
      Assert.IsNotNull(channelInfo);
      var channelResult = await channelInfo.GetChannelAsync(1);
      Assert.IsTrue(channelResult.Success);
      Assert.IsNotNull(channelResult.Result);
      Assert.AreEqual(1, channelResult.Result.ChannelId);
    }

    [Test]
    public async Task TestPrograms()
    {
      var channelInfo = _provider as IChannelAndGroupInfoAsync;
      Assert.IsNotNull(channelInfo);
      var programInfo = _provider as IProgramInfoAsync;
      Assert.IsNotNull(programInfo);
      var channelGroupResult = await channelInfo.GetChannelGroupsAsync();
      Assert.IsTrue(channelGroupResult.Success);
      var channelResult = await channelInfo.GetChannelsAsync(channelGroupResult.Result.First());
      Assert.IsTrue(channelResult.Success);
      var programResult = await programInfo.GetProgramsAsync(channelResult.Result.First(), DateTime.Now, DateTime.Now.AddHours(4));
      Assert.IsTrue(programResult.Success);
      Assert.IsNotNull(programResult.Result);
    }
  }

  public class FakeSettings<T> : ISettingsManager
  {
    private T _settings;

    public FakeSettings(T settings)
    {
      _settings = settings;
    }

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return _settings as SettingsType;
    }

    public object Load(Type settingsType)
    {
      return _settings;
    }

    public void Save(object settingsObject)
    {
    }

    public void StartBatchUpdate()
    {
    }

    public void EndBatchUpdate()
    {
    }

    public void CancelBatchUpdate()
    {
    }

    public void ClearCache()
    {
    }

    public void ChangeUserContext(string userName)
    {
    }

    public void RemoveSettingsData(Type settingsType, bool user, bool global)
    {
    }

    public void RemoveAllSettingsData(bool user, bool global)
    {
    }
  }
}
