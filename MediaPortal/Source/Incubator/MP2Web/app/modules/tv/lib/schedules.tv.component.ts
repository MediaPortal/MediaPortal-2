import {Component, View, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import * as moment from "moment";

import {EpgComponent} from "../../../common/Components/EPG/lib/epg.component";
import {TvService} from "../../../common/lib/TvService/TvService";
import {ISchedule, IChannel} from "../../../common/lib/TvService/TvServiceInterfaces";

const momentConstructor: (value?: any) => moment.Moment = (<any>moment).default || moment;

@Component({
    templateUrl: "app/modules/tv/schedule.tv.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, EpgComponent],
    providers: [TvService]
})
export class SchedulesTvComponent {
  private channelIds: number[] = [];
  private channelMap: { [key:number]: IChannel; } = {};

  scheduleListByChannel: { [key:number]: ISchedule[]; } = {};
  scheduleList: ISchedule[] = [];
  channelList: IChannel[] = [];
  processing: boolean = false;

  constructor(public tvService: TvService) {
    this.getAllSchedules();
  }

  getAllSchedules() {
    // Clear Lists
    this.scheduleListByChannel = [];
    this.scheduleList = [];
    this.channelIds = [];

    this.tvService.GetAllSchedules().map(res => res.json()).subscribe(
      res => {
        this.scheduleListByChannel = res;
        for (var key in this.scheduleListByChannel) {
          for (let schedule of this.scheduleListByChannel[key]) {
            this.scheduleList.push(schedule);
            this.channelIds.push(schedule.ChannelId);
          }
        }

        this.getChannels();
      },
      err => console.error(err));
  }

  getChannels() {
    this.tvService.GetChannelsByIds(this.channelIds).map(res => res.json()).map(res => this.tvService.ToChannelList(res)).subscribe(
      res => {
        for (let channel of res) {
          this.channelMap[channel.Id] = channel;
        }
      },
      err => console.error(err));
  }

  getChannel(channelId: number): IChannel {
    if (this.channelMap[channelId]) {
      return this.channelMap[channelId];
    }
    return {Id: 0, MediaType: 0, Name: ""};
  }

  onDeleteSchedule(scheduleId: number) {
    this.processing = true;
    this.tvService.RemoveScheduleById(scheduleId).subscribe(
      res => {
        console.log("Schedule deleted");
        this.getAllSchedules();
        this.processing = false;
      },
      err => console.error(err));
  }

  dateToString(date: Date, format: string): string {
    return momentConstructor(date).format(format);
  }
}

