import {Component, View, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {AgGridNg2} from "ag-grid-ng2/main";
import {GridOptions} from "ag-grid/main";
import * as moment from "moment";

import {EpgComponent} from "../../../common/Components/EPG/lib/epg.component";
import {TvService} from "../../../common/lib/TvService/TvService";
import {ISchedule, IChannel} from "../../../common/lib/TvService/TvServiceInterfaces";

const momentConstructor: (value?: any) => moment.Moment = (<any>moment).default || moment;

@Component({
    templateUrl: "app/modules/tv/schedule.tv.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, EpgComponent, AgGridNg2],
    providers: [TvService]
})
export class SchedulesTvComponent {
  private channelIds: number[] = [];
  private channelMap: { [key:number]: IChannel; } = {};

  scheduleListByChannel: { [key:number]: ISchedule[]; } = {};
  scheduleList: ISchedule[] = [];
  channelList: IChannel[] = [];
  processing: boolean = false;

  columnDefs = [
    { headerName: "Name", field: "Name", width: 150 },
    {
      headerName: "Channel", field: "ChannelId", width: 150, cellRenderer: params => params.context.getChannel(params.value).Name },
    {
      headerName: "Start Time", field: "StartTime", width: 150, cellRenderer: params => params.context.dateToString(params.value, "DD.MM.YYYY - HH:mm")
    },
    {
      headerName: "End Time", field: "EndTime", width: 150, cellRenderer: params => params.context.dateToString(params.value, "DD.MM.YYYY - HH:mm") }
  ];
  private rowData: any[];

  gridOptions: GridOptions = <GridOptions>{}

  constructor(public tvService: TvService) {
    this.getAllSchedules();
    this.setGridOptions();
  }

  setGridOptions() {
    this.gridOptions.context = this;
    this.gridOptions.enableSorting = true;
    this.gridOptions.suppressCellSelection = true;
    this.gridOptions.rowSelection = "multiple";
    this.gridOptions.rowDeselection = true;
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

        this.rowData = this.scheduleList;
        this.gridOptions.api.sizeColumnsToFit();
      },
      err => console.error(err));
  }

  getChannels() {
    this.tvService.GetChannelsByIds(this.channelIds).map(res => res.json()).map(res => this.tvService.ToChannelList(res)).subscribe(
      res => {
        for (let channel of res) {
          this.channelMap[channel.Id] = channel;
        }
        this.gridOptions.api.refreshView(); // update grid view to show channel names
      },
      err => console.error(err));
  }

  getChannel(channelId: number): IChannel {
    if (this.channelMap[channelId]) {
      return this.channelMap[channelId];
    }
    return {Id: 0, MediaType: 0, Name: ""};
  }

  onQuickFilterChanged(value) {
    this.gridOptions.api.setQuickFilter(value);
  }

  onDeleteSelectedSchedules() {
    var selectedRows = this.gridOptions.api.getSelectedRows();
    var scheduleIds: number[] = [];
    for (let row of selectedRows) {
      scheduleIds.push(row.ScheduleId);
    }

    this.tvService.RemoveScheduleById(scheduleIds).subscribe(
      res => {
        console.log("Schedules deleteds");
        this.getAllSchedules();
      },
      err => console.error(err));
  }

  dateToString(date: Date, format: string): string {
    return momentConstructor(date).format(format);
  }

}

