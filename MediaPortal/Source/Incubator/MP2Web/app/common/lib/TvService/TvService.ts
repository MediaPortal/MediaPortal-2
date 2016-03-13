import {Injectable, EventEmitter} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";

import {IChannel, IChannelGroup, MediaType, ScheduleRecordingType} from "./TvServiceInterfaces";

@Injectable()
export class TvService {
  private BASE_URL: string;

  constructor(private http: Http, private configurationService: ConfigurationService) {
    this.BASE_URL = configurationService.config.WebApiUrl;
  }

  public GetProgramsByGroup(channelGroupId: number, from: Date, to: Date) {
      var url: string = this.BASE_URL + "/api/v1/Tv/ProgramInfo/ProgramsByGroup/" + channelGroupId + "/" + from.toJSON() + "/" + to.toJSON();

      return this.http.request(new Request({
          method: RequestMethod.Get,
          url: url
      }));
  }

  public GetGroups() {
    var url: string = this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/Groups";

    return this.http.request(new Request({
      method: RequestMethod.Get,
      url: url
    }));
  }

  public GetChannelsByGroup(groupId: number) {
    var url: string = this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/ChannelsByGroup/" + groupId;

    return this.http.request(new Request({
      method: RequestMethod.Get,
      url: url
    }));
  }

  public GetChannelsByIds(ids: number[]) {
    var url: string = this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/Channels";
    for(var i = 0; i < ids.length; i++) {
      if (i == 0) {
        url += "?chIds="+ids[i];
      }else {
        url += "&chIds="+ids[i];
      }
    }
    console.log(url);

    return this.http.request(new Request({
      method: RequestMethod.Get,
      url: url
    }));
  }

  // ########## Schedules #########

  public ScheduleByProgram(programId: number, recordingType: ScheduleRecordingType) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/ScheduleByProgram/" + programId + "/" + recordingType;

    return this.http.request(new Request({
      method: RequestMethod.Put,
      url: url
    }));
  }

  public RemoveScheduleByProgram(programId: number, recordingType: ScheduleRecordingType) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/ScheduleByProgram/" + programId + "/" + recordingType;

    return this.http.request(new Request({
      method: RequestMethod.Delete,
      url: url
    }));
  }

  public RemoveScheduleById(scheduleId: number) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/Schedule/" + scheduleId;

    return this.http.request(new Request({
      method: RequestMethod.Delete,
      url: url
    }));
  }

  public GetAllSchedules() {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/Schedules";

    return this.http.request(new Request({
      method: RequestMethod.Get,
      url: url
    }));
  }

  public GetChannelLogo(channelName: string, radio: boolean = false): string {
    return this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/" + encodeURIComponent(channelName) + "/Logo?radio=" + radio;
  }

  /*
  Converter Methods
  */

  public ToGroupList(json: any): IChannelGroup[] {
    var output: IChannelGroup[] = [];
    console.log("JSON");
    console.log(json);
    for (let group of json) {
      console.log(group);
      var channelGroup: IChannelGroup = {
        Id: group.ChannelGroupId,
        Name: group.Name
      };
      console.log(channelGroup);
      output.push(channelGroup);
    }
    return output;
  }

  public ToChannelList(json: any): IChannel[] {
    var output: IChannel[] = [];
    for (let channel of json) {
      var channelTmp: IChannel = {
        Id: channel.ChannelId,
        Name: channel.Name,
        MediaType: channel.MediaType
      };
      output.push(channelTmp);
    }
    return output;
  }
}