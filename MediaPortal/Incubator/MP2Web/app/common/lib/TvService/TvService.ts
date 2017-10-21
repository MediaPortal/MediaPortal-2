import {Injectable, EventEmitter} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, Response, RequestMethod} from "angular2/http";
import {Observable} from "rxjs/Observable";
import "rxjs/add/operator/catch";
import "rxjs/add/observable/throw";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {MessageService} from "../../../common/lib/MessageService/MessageService";
import {MessageType} from "../../../common/lib/MessageService/IMessageType";

import {IChannel, IChannelGroup, MediaType, ScheduleRecordingType} from "./TvServiceInterfaces";

@Injectable()
export class TvService {
  private BASE_URL: string;

  constructor(private http: Http, private configurationService: ConfigurationService, private messageService: MessageService) {
    this.BASE_URL = configurationService.config.WebApiUrl;
  }

  /*
  Construtor class for all http requests. Saves code for the error handling
  */
  private newHttp(request: Request, errorTitle: string = "Http Error in TvService"): Observable<Response> {
    return this.http.request(request).catch(err => this.onHttpError(errorTitle, err));
  }

  // ########## Programs and Channels #########

  public GetProgramsByGroup(channelGroupId: number, from: Date, to: Date) {
    var url: string = this.BASE_URL + "/api/v1/Tv/ProgramInfo/ProgramsByGroup/" + channelGroupId + "/" + from.toJSON() + "/" + to.toJSON();

    return this.newHttp(new Request({
      method: RequestMethod.Get,
      url: url
    }), "Failed to retrieve Programs");
  }

  public GetGroups() {
    var url: string = this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/Groups";

    return this.newHttp(new Request({
      method: RequestMethod.Get,
      url: url
    }), "Failed to retrieve Groups");
  }

  public GetChannelsByGroup(groupId: number) {
    var url: string = this.BASE_URL + "/api/v1/Tv/ChannelAndGroupInfo/ChannelsByGroup/" + groupId;

    return this.newHttp(new Request({
      method: RequestMethod.Get,
      url: url
    }), "Failed to retrieve Channels for Group");
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

    return this.newHttp(new Request({
      method: RequestMethod.Get,
      url: url
    }), "Failed to retrieve Channels by id");
  }

  // ########## Schedules #########

  public ScheduleByProgram(programId: number, recordingType: ScheduleRecordingType) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/ScheduleByProgram/" + programId + "/" + recordingType;

    return this.newHttp(new Request({
      method: RequestMethod.Put,
      url: url
    }), "Failed to create Schedule by Program");
  }

  public RemoveScheduleByProgram(programId: number, recordingType: ScheduleRecordingType) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/ScheduleByProgram/" + programId + "/" + recordingType;

    return this.newHttp(new Request({
      method: RequestMethod.Delete,
      url: url
    }), "Failed to delete Schedule by Program");
  }

  public RemoveScheduleById(scheduleIds: number[]) {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/Schedules";
    for (var i = 0; i < scheduleIds.length; i++) {
      if (i == 0) {
        url += "?scheduleIds=" + scheduleIds[i];
      } else {
        url += "&scheduleIds=" + scheduleIds[i];
      }
    }

    return this.newHttp(new Request({
      method: RequestMethod.Delete,
      url: url
    }), "Failed to delete Schedule by Id");
  }

  public GetAllSchedules() {
    var url: string = this.BASE_URL + "/api/v1/Tv/Schedule/Schedules";

    return this.newHttp(new Request({
      method: RequestMethod.Get,
      url: url
    }), "Failed to get Schedules");
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

  /*
  Error handling
  - send a notification to the user
  - log to the console
  - pass the error to the calling Component
  */
  private onHttpError(title: string, err: Response) {
    this.messageService.addNotificationMessage(title, MessageType.Error, "Status: " + err.status + " " + err.statusText);
    console.error(title);
    console.error(err.url);
    console.error(err);
    return Observable.throw(err); // pass the error to the calling Component e.g. the EPG component
  }
}