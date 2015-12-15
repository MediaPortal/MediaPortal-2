using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using System.IO;
using System.Globalization;
using HttpServer.Sessions;
using MediaPortal.Plugins.MP2Extended.Attributes;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.StreamInfo
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  internal class GetMediaInfo : IRequestMicroModuleHandler
  {
    private const string UNDEFINED = "undef";

    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["itemId"].Value;
      if (id == null)
        throw new BadRequestException("GetMediaInfo: itemId is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);
      optionalMIATypes.Add(TranscodeItemAudioAspect.ASPECT_ID);
      optionalMIATypes.Add(TranscodeItemImageAspect.ASPECT_ID);
      optionalMIATypes.Add(TranscodeItemVideoAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes, optionalMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetMediaInfo: No MediaItem found with id: {0}", id));

      long duration = 0;
      string container = string.Empty;
      List<WebVideoStream> webVideoStreams = new List<WebVideoStream>();
      List<WebAudioStream> webAudioStreams = new List<WebAudioStream>();
      List<WebSubtitleStream> webSubtitleStreams = new List<WebSubtitleStream>();

      // decide which type of media item we have
      if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        var videoAspect = item.Aspects[VideoAspect.ASPECT_ID];
        duration = Convert.ToInt64(videoAspect.GetAttributeValue(VideoAspect.ATTR_DURATION) ?? 0);

        // Video Stream
        WebVideoStream webVideoStream = new WebVideoStream();
        webVideoStream.Codec = Convert.ToString(videoAspect.GetAttributeValue(VideoAspect.ATTR_VIDEOENCODING) ?? string.Empty);
        webVideoStream.DisplayAspectRatio = Convert.ToDecimal(videoAspect.GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO) ?? 0);
        webVideoStream.DisplayAspectRatioString = AspectRatioHelper.AspectRatioToString(Convert.ToDecimal(videoAspect.GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO) ?? 0));
        webVideoStream.Height = Convert.ToInt32(videoAspect.GetAttributeValue(VideoAspect.ATTR_HEIGHT) ?? 0);
        webVideoStream.Width = Convert.ToInt32(videoAspect.GetAttributeValue(VideoAspect.ATTR_WIDTH) ?? 0);
        webVideoStreams.Add(webVideoStream);

        if (item.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID))
        {
          var transcodeVideoAspect = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID];
          webVideoStream.ID = 0;
          webVideoStream.Index = 0;
          //webVideoStream.Interlaced = transcodeVideoAspect[TranscodeItemVideoAspect.];

          container = (string)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_CONTAINER];

          // Audio streams
          var audioStreams = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_AUDIOSTREAMS];
          var audioChannels = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_AUDIOCHANNELS];
          var audioCodecs = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_AUDIOCODECS];
          var audioLanguages = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_AUDIOLANGUAGES];
          if (audioStreams != null)
            for (int i = 0; i < audioStreams.Count; i++)
            {
              WebAudioStream webAudioStream = new WebAudioStream();
              if (audioChannels != null)
              {
                var audioChannelsList = audioChannels.Cast<string>().ToList();
                webAudioStream.Channels = int.Parse(i < audioChannelsList.Count ? audioChannelsList[i] : audioChannelsList[0]);
              }
              if (audioCodecs != null)
                webAudioStream.Codec = i < audioCodecs.Cast<string>().ToList().Count ? audioCodecs.Cast<string>().ToList()[i] : audioCodecs.Cast<string>().ToList()[0];
              webAudioStream.ID = int.Parse(audioStreams.Cast<string>().ToList()[i]);
              webAudioStream.Index = i;
              if (audioLanguages != null && i < audioLanguages.Cast<string>().ToList().Count)
              {
                string language = audioLanguages.Cast<string>().ToList()[i] == string.Empty ? UNDEFINED : audioLanguages.Cast<string>().ToList()[i];
                webAudioStream.Language = language;
                if (language != UNDEFINED)
                {
                  webAudioStream.LanguageFull = new CultureInfo(language).EnglishName;
                  if (string.IsNullOrEmpty(webAudioStream.Codec) == false) webAudioStream.Title = webAudioStream.Codec.ToUpperInvariant();
                }
              }

              webAudioStreams.Add(webAudioStream);
            }

          // Subtitles
          var subtitleStreams = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBSTREAMS];
          var subtitleLanguages = (HashSet<object>)transcodeVideoAspect[TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBLANGUAGES];
          if (subtitleStreams != null)
            for (int i = 0; i < subtitleStreams.Count; i++)
            {
              WebSubtitleStream webSubtitleStream = new WebSubtitleStream();
              webSubtitleStream.Filename = "embedded";
              webSubtitleStream.ID = webSubtitleStreams.Count;
              webSubtitleStream.Index = webSubtitleStreams.Count;
              if (subtitleLanguages != null && i < subtitleLanguages.Cast<string>().ToList().Count)
              {
                string language = subtitleLanguages.Cast<string>().ToList()[i] == string.Empty ? UNDEFINED : subtitleLanguages.Cast<string>().ToList()[i];
                webSubtitleStream.Language = language;
                webSubtitleStream.LanguageFull = language;
                if (language != UNDEFINED) webSubtitleStream.LanguageFull = new CultureInfo(language).EnglishName;
              }

              webSubtitleStreams.Add(webSubtitleStream);
            }

          IResourceAccessor mediaItemAccessor = item.GetResourceLocator().CreateAccessor();
          if (mediaItemAccessor is IFileSystemResourceAccessor)
          {
            using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
            {
              if (fsra.IsFile)
                using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
                {
                  List<SubtitleStream> externalSubtitles = MediaConverter.FindExternalSubtitles(lfsra);
                  if(externalSubtitles != null)
                    for (int i = 0; i < externalSubtitles.Count; i++)
                    {
                      WebSubtitleStream webSubtitleStream = new WebSubtitleStream();
                      webSubtitleStream.Filename = Path.GetFileName(externalSubtitles[i].Source);
                      webSubtitleStream.ID = webSubtitleStreams.Count;
                      webSubtitleStream.Index = webSubtitleStreams.Count;
                      if (string.IsNullOrEmpty(externalSubtitles[i].Language) == false)
                      {
                        webSubtitleStream.Language = externalSubtitles[i].Language;
                        webSubtitleStream.LanguageFull = new CultureInfo(externalSubtitles[i].Language).EnglishName;
                      }
                      else
                      {
                        webSubtitleStream.Language = UNDEFINED;
                        webSubtitleStream.LanguageFull = UNDEFINED;
                      }

                      webSubtitleStreams.Add(webSubtitleStream);
                    }
                }
            }
          }
        }
      }

      // Audio File
      if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        var audioAspect = item.Aspects[AudioAspect.ASPECT_ID];
        duration = (long)audioAspect[AudioAspect.ATTR_DURATION];
        if (item.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID))
        {
          container = (string)item.Aspects[TranscodeItemAudioAspect.ASPECT_ID][TranscodeItemAudioAspect.ATTR_CONTAINER];
        }
      }

      // Image File
      if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        var imageAspect = item.Aspects[ImageAspect.ASPECT_ID];
        if (item.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID))
        {
          container = (string)item.Aspects[TranscodeItemImageAspect.ASPECT_ID][TranscodeItemImageAspect.ATTR_CONTAINER];
        }
      }

      WebMediaInfo webMediaInfo = new WebMediaInfo
      {
        Duration = duration * 1000,
        Container = container,
        VideoStreams = webVideoStreams,
        AudioStreams = webAudioStreams,
        SubtitleStreams = webSubtitleStreams
      };

      return webMediaInfo;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
