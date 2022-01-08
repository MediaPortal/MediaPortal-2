using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors;
using MediaPortal.Plugins.SlimTv.Interfaces;
using NUnit.Framework;
using SlimTv.TvMosaicProvider;
using SlimTv.TvMosaicProvider.Settings;
using Tests.Common;
using TvMosaic.API;

namespace Test.TVMosaic
{
  [TestFixture]
  public class TvMosaic
  {
    private TvMosaicProvider _provider;

    [SetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      FakeSettings<TvMosaicProviderSettings> settings = new FakeSettings<TvMosaicProviderSettings>(new TvMosaicProviderSettings { Host = "localhost" });
      ServiceRegistration.Set<ISettingsManager>(settings);

      IMediaItemAspectTypeRegistration miatr = new TestMediaItemAspectTypeRegistration();
      ServiceRegistration.Set(miatr);

      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(MediaAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(AudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(RelationshipAspect.Metadata);

      _provider = new TvMosaicProvider();
      _provider.Init();
    }

    [TearDown]
    public void DeInit()
    {
      _provider.DeInit();
    }

    [Test]
    public async Task TestSchedules()
    {
      var scheduleControl = _provider as IScheduleControlAsync;
      Assert.IsNotNull(scheduleControl);
      var scheduleResult = await scheduleControl.GetSchedulesAsync();
      Assert.IsTrue(scheduleResult.Success);
      Assert.IsNotNull(scheduleResult.Result);
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
    public async Task TestChannelsForGroup()
    {
      var channelInfo = _provider as IChannelAndGroupInfoAsync;
      Assert.IsNotNull(channelInfo);
      var channelGroupResult = await channelInfo.GetChannelGroupsAsync();
      Assert.IsNotNull(channelGroupResult);
      var channelsResult = await channelInfo.GetChannelsAsync(channelGroupResult.Result.First());
      Assert.IsNotNull(channelsResult.Result);
      Assert.Greater(channelsResult.Result.Count, 0);
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

    [Test]
    public async Task TestProgramsDeserialize()
    {
      string xmlProgram = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
       <program>
        <program_id>18350</program_id>
				<name>Alkohol - Der globale Rausch</name>
				<start_time>1641323700</start_time>
				<duration>5400</duration>
				<short_desc>Deutschland 2019
Alkohol: Kein Stoff der Welt ist uns so vertraut und in seiner Wirkung so unglaublich vielfältig. Man bekommt ihn überall, und das kleine Molekül ist in der Lage, sämtliche 200 Milliarden Neuronen eines menschlichen Gehirns völlig unterschiedlich zu beeinflussen. Doch kaum jemand bezeichnet Alkohol trotz seiner psychoaktiven und Zellen zerstörenden Wirkung als Droge. Aber warum lassen wir den Tod von jährlich drei Millionen Menschen einfach so zu? 
Grimme-Preisträger Andreas Pichler sucht Antworten auf die Fragen, warum wir überhaupt trinken, was Alkohol mit uns macht und wie stark die Industrie Gesellschaft und Politik beeinflusst.</short_desc>
				<language>DEU</language>
				<is_series>true</is_series>
			</program>";

      var program = HttpDataProvider.Deserialize<Program>(xmlProgram);
      Assert.IsNotNull(program);
      Assert.IsNotNull(program.ShortDesc);
      Assert.IsNotNull(program.Language);
      Assert.IsTrue(program.IsSeries);
    }

    [Test]
    public async Task TestRecordings()
    {
      //var recordingSettings = await _provider.GetRecordingSettings();
      //Assert.IsNotNull(recordingSettings);

      var recordings = await _provider.GetRecording();
      Assert.IsNotNull(recordings);
    }

    [Test]
    public async Task TestRecordingsMDE()
    {
      var mde = new TvMosaicRecordingMetadataExtractor();
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();

      string path = @"C:\ProgramData\DVBLogic\TVMosaic\data\RecordedTV\Tagesschau-0125-20220104.ts";
      using (ILocalFsResourceAccessor ra = new LocalFsResourceAccessor(new LocalFsResourceProvider(), path))
      {
        var result = mde.TryExtractMetadataAsync(ra, extractedAspectData, false);
      }
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
