using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.SlimTv.Interfaces;
using NUnit.Framework;
using SlimTv.TvMosaicProvider;
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
  }
}
