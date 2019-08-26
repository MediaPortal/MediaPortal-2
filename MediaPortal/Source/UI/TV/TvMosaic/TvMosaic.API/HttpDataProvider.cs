///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///
/// Modified and implemented as portable class library by Christian Riedl (ric@rts.co.at)
/// 

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TvMosaic.API
{
  [DataContract(Name = "response", Namespace = "")]
  class XmlResponse
  {
    [DataMember(Name = "status_code")]
    public int Status { get; set; }
    [DataMember(Name = "xml_result")]
    public string Result { get; set; }
  }

  [DataContract(Namespace = "")]
  public class DVBLinkResponse<T> where T : new()
  {
    [DataMember(Name = "status_code")]
    public StatusCode Status { get; set; }
    [DataMember(Name = "xml_result")]
    public T Result { get; set; }
    public HttpStatusCode HttpStatus { get; set; }
    public string ReasonText { get; set; }
  }
  [DataContract(Namespace = "")]
  public class DVBLinkStatusResponse
  {
    [DataMember(Name = "status_code")]
    public StatusCode Status { get; set; }
    public HttpStatusCode HttpStatus { get; set; }
    public string ReasonText { get; set; }
  }

  public class HttpDataProvider
  {
    string _serverUrl;             // like "http://{0}:{1}/mobile/
    string _user;
    string _password;
    HttpClient _currentHttpClient;     // Used for cancel only

    public const string SERVER_URI_FORMAT = "http://{0}:{1}/mobile/";
    public const string GET_CHANNELS_CMD = "get_channels";
    public const string GET_FAVORITES_CMD = "get_favorites";
    public const string PLAY_CHANNEL_CMD = "play_channel";
    public const string STOP_CHANNEL_CMD = "stop_channel";
    public const string SEARCH_EPG_CMD = "search_epg";
    public const string GET_RECORDINGS_CMD = "get_recordings";
    public const string GET_SCHEDULES_CMD = "get_schedules";
    public const string ADD_SCHEDULE_CMD = "add_schedule";
    public const string REMOVE_SCHEDULE_CMD = "remove_schedule";
    public const string REMOVE_RECORDING_CMD = "remove_recording";
    public const string SET_PARENTAL_LOCK_CMD = "set_parental_lock";
    public const string GET_PARENTAL_STATUS_CMD = "get_parental_status";
    public const string GET_PLAYLIST_M3U = "get_playlist_m3u";
    public const string GET_STREAMING_CAPABILITIES = "get_streaming_capabilities";
    public const string GET_OBJECT_CMD = "get_object";
    public const string REMOVE_OBJECT_CMD = "remove_object";

    public HttpDataProvider(string address, int port)
    {
      _serverUrl = string.Format(SERVER_URI_FORMAT, address, port); _user = _password = null;
    }
    public HttpDataProvider(string address, int port, string user, string password)
    {
      _serverUrl = string.Format(SERVER_URI_FORMAT, address, port); _user = user; _password = password;
    }

    HttpClient GetHttpClient(int timeoutSec = 60)
    {
      HttpClient httpClient;

      if (string.IsNullOrEmpty(_user))
        httpClient = new HttpClient();
      else
      {
        HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(_user, _password) };
        httpClient = new HttpClient(handler, true);
      }
      httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
      return httpClient;
    }
    static public string Serialize<T>(T data)
    {
      using (MemoryStream memory_stream = new MemoryStream())
      {
        using (XmlWriter xml_writer = XmlWriter.Create(memory_stream))
        {
          DataContractSerializer serializer = new DataContractSerializer(typeof(T));
          serializer.WriteObject(xml_writer, data);
          xml_writer.Flush();

          memory_stream.Seek(0, SeekOrigin.Begin);
          StreamReader reader = new StreamReader(memory_stream);
          return reader.ReadToEnd();
        }
      }
    }

    static public T Deserialize<T>(string xml)
    {
      using (MemoryStream memory_stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
      {
        using (XmlReader xml_reader = XmlReader.Create(memory_stream))
        {
          DataContractSerializer serializer = new DataContractSerializer(typeof(T));
          return (T)serializer.ReadObject(xml_reader);
        }
      }
    }
    static public T Deserialize<T>(Stream xmlStrm)
    {
      using (XmlReader xml_reader = XmlReader.Create(xmlStrm))
      {
        DataContractSerializer serializer = new DataContractSerializer(typeof(T));
        return (T)serializer.ReadObject(xml_reader);
      }
    }
    public async Task<DVBLinkResponse<Response>> GetData<Request, Response>(string command, Request request) where Response : new()
    {
      try
      {
        using (HttpClient httpClient = GetHttpClient())
        {
          string xmlRequest = Serialize<Request>(request);
          string postContent = string.Format("command={0}&xml_param={1}", command, xmlRequest);
          _currentHttpClient = httpClient;
          using (HttpResponseMessage httpresp = await httpClient.PostAsync(_serverUrl, new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded")))
          {
            _currentHttpClient = null;
            if (httpresp.Content.Headers.ContentLength == 0 || !httpresp.IsSuccessStatusCode)
            {
              DVBLinkResponse<Response> errorResponse = new DVBLinkResponse<Response>
              {
                HttpStatus = httpresp.StatusCode,
                ReasonText = httpresp.ReasonPhrase,
                Status = StatusCode.STATUS_ERROR
              };
              return errorResponse;
            }
            else
            {
              DVBLinkResponse<Response> response = new DVBLinkResponse<Response>();
              string str = await httpresp.Content.ReadAsStringAsync();
              XmlResponse dvbResponse = Deserialize<XmlResponse>(str);
              if (dvbResponse.Status == 0) response.Result = Deserialize<Response>(dvbResponse.Result);
              response.Status = (StatusCode)dvbResponse.Status;
              response.HttpStatus = httpresp.StatusCode;
              response.ReasonText = httpresp.ReasonPhrase;
              return response;
            }
          }
        }
      }
      catch (Exception ex)
      {
        _currentHttpClient = null;
        DVBLinkResponse<Response> errorResponse = new DVBLinkResponse<Response>
        {
          HttpStatus = 0,
          ReasonText = ex.Message,
          Status = StatusCode.STATUS_CONNECTION_ERROR
        };
        return errorResponse;
      }
    }
    public async Task<DVBLinkStatusResponse> GetStatus<Request>(string command, Request request)
    {
      try
      {
        using (HttpClient httpClient = GetHttpClient())
        {
          string xmlRequest = Serialize<Request>(request);
          string postContent = string.Format("command={0}&xml_param={1}", command, xmlRequest);
          _currentHttpClient = httpClient;
          using (HttpResponseMessage httpresp = await httpClient.PostAsync(_serverUrl, new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded")))
          {
            _currentHttpClient = null;
            if (httpresp.Content.Headers.ContentLength == 0 || !httpresp.IsSuccessStatusCode)
            {
              DVBLinkStatusResponse errorResponse = new DVBLinkStatusResponse
              {
                HttpStatus = httpresp.StatusCode,
                ReasonText = httpresp.ReasonPhrase,
                Status = StatusCode.STATUS_ERROR
              };
              return errorResponse;
            }
            else
            {
              DVBLinkStatusResponse response = new DVBLinkStatusResponse();
              string str = await httpresp.Content.ReadAsStringAsync();
              XmlResponse dvbResponse = Deserialize<XmlResponse>(str);
              response.Status = (StatusCode)dvbResponse.Status;
              response.HttpStatus = httpresp.StatusCode;
              response.ReasonText = httpresp.ReasonPhrase;
              return response;
            }
          }
        }
      }
      catch (Exception ex)
      {
        _currentHttpClient = null;
        DVBLinkStatusResponse errorResponse = new DVBLinkStatusResponse
        {
          HttpStatus = 0,
          ReasonText = ex.Message,
          Status = StatusCode.STATUS_CONNECTION_ERROR
        };
        return errorResponse;
      }
    }
    public bool Cancel()
    {
      HttpClient httpClient = _currentHttpClient;
      if (httpClient == null)
        return false;
      httpClient.CancelPendingRequests();
      return true;
    }
    public async Task<DVBLinkResponse<StreamingCapabilities>> GetStreamingCapabilities(CapabilitiesRequest request)
    {
      DVBLinkResponse<StreamingCapabilities> resp = await GetData<CapabilitiesRequest, StreamingCapabilities>(GET_STREAMING_CAPABILITIES, request);
      return resp;
    }
    public async Task<DVBLinkResponse<Channels>> GetChannels(ChannelsRequest request)
    {
      DVBLinkResponse<Channels> resp = await GetData<ChannelsRequest, Channels>(GET_CHANNELS_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<Favorites>> GetFavorites(FavoritesRequest request)
    {
      DVBLinkResponse<Favorites> resp = await GetData<FavoritesRequest, Favorites>(GET_FAVORITES_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<ChannelsIdWithPrograms>> SearchEpg(EpgSearcher request)
    {
      DVBLinkResponse<ChannelsIdWithPrograms> resp = await GetData<EpgSearcher, ChannelsIdWithPrograms>(SEARCH_EPG_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<Recordings>> GetRecordings(RecordingsRequest request)
    {
      DVBLinkResponse<Recordings> resp = await GetData<RecordingsRequest, Recordings>(GET_RECORDINGS_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<Schedules>> GetSchedules(SchedulesRequest request)
    {
      DVBLinkResponse<Schedules> resp = await GetData<SchedulesRequest, Schedules>(GET_SCHEDULES_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<Streamer>> PlayChannel(RequestStream request)
    {
      DVBLinkResponse<Streamer> resp = await GetData<RequestStream, Streamer>(PLAY_CHANNEL_CMD, request);
      return resp;
    }
    public async Task<DVBLinkStatusResponse> StopStream(StopStream request)
    {
      DVBLinkStatusResponse resp = await GetStatus<StopStream>(STOP_CHANNEL_CMD, request);
      return resp;
    }
    public async Task<DVBLinkStatusResponse> AddSchedule(Schedule request)
    {
      DVBLinkStatusResponse resp = await GetStatus<Schedule>(ADD_SCHEDULE_CMD, request);
      return resp;
    }
    public async Task<DVBLinkStatusResponse> RemoveSchedule(ScheduleRemover request)
    {
      DVBLinkStatusResponse resp = await GetStatus<ScheduleRemover>(REMOVE_SCHEDULE_CMD, request);
      return resp;
    }
    public async Task<DVBLinkStatusResponse> RemoveRecording(RecordingRemover request)
    {
      DVBLinkStatusResponse resp = await GetStatus<RecordingRemover>(REMOVE_RECORDING_CMD, request);
      return resp;
    }
    public async Task<DVBLinkResponse<ObjectResponse>> GetObject(ObjectRequester request)
    {
      DVBLinkResponse<ObjectResponse> resp = await GetData<ObjectRequester, ObjectResponse>(GET_OBJECT_CMD, request);
      return resp;
    }
    public async Task<DVBLinkStatusResponse> RemoveObject(ObjectRemover request)
    {
      DVBLinkStatusResponse resp = await GetStatus<ObjectRemover>(REMOVE_OBJECT_CMD, request);
      return resp;
    }

    public async Task<DVBLinkResponse<PlayList>> GetPlayListM3u(PlayListRequest request)
    {
      try
      {
        using (HttpClient httpClient = GetHttpClient())
        {
          string xmlRequest = Serialize<PlayListRequest>(request);
          string postContent = string.Format("command={0}&xml_param={1}", GET_PLAYLIST_M3U, xmlRequest);
          _currentHttpClient = httpClient;
          using (HttpResponseMessage httpresp = await httpClient.PostAsync(_serverUrl, new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded")))
          {
            _currentHttpClient = null;
            if (httpresp.Content.Headers.ContentLength == 0 || !httpresp.IsSuccessStatusCode)
            {
              DVBLinkResponse<PlayList> errorResponse = new DVBLinkResponse<PlayList>
              {
                HttpStatus = httpresp.StatusCode,
                ReasonText = httpresp.ReasonPhrase,
                Status = StatusCode.STATUS_ERROR
              };
              return errorResponse;
            }
            else
            {
              int num = 0;
              string name = string.Empty;
              DVBLinkResponse<PlayList> response = new DVBLinkResponse<PlayList>();

              string result = await httpresp.Content.ReadAsStringAsync();
              response.Result = new PlayList();
              string[] lines = result.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
              foreach (string line in lines)
              {
                if (line[0] == '#')
                {
                  if (line.StartsWith("#EXTINF"))
                  {
                    int pos = line.IndexOf(',');
                    if (pos > 0)
                    {
                      string str = line.Substring(pos + 1);
                      pos = str.IndexOf(" - ");
                      if (pos > 0)
                      {
                        num = Int32.Parse(str.Substring(0, pos));
                        name = str.Substring(pos + 3);
                      }
                    }
                  }
                }
                else
                {
                  if (num > 0)
                  {
                    PlayListMember m = new PlayListMember { Name = name, Number = num, Url = line };
                    response.Result.Add(m);
                    num = -1; name = null;
                  }
                }
              }

              response.Status = StatusCode.STATUS_OK;
              response.HttpStatus = httpresp.StatusCode;
              response.ReasonText = httpresp.ReasonPhrase;
              return response;
            }
          }
        }
      }
      catch (Exception ex)
      {
        _currentHttpClient = null;
        DVBLinkResponse<PlayList> errorResponse = new DVBLinkResponse<PlayList>
        {
          HttpStatus = 0,
          ReasonText = ex.Message,
          Status = StatusCode.STATUS_CONNECTION_ERROR
        };
        return errorResponse;
      }
    }
  }
}
