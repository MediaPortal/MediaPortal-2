import {Component, View, EventEmitter, OnChanges, SimpleChange, OnInit, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import * as moment from "moment";

import {INavigation} from "./INavigation";
import {ITimeindicator} from "./ITimeindicator";
import {TvService} from "../../../lib/TvService/TvService";
import {IChannelGroup, IChannel, IProgram, RecordingStatus, ScheduleRecordingType, MediaType} from "../../../lib/TvService/TvServiceInterfaces";
import {Popover} from "../../../Components/Popover/lib/popover.directive";

const momentConstructor: (value?: any) => moment.Moment = (<any>moment).default || moment;
const TIME_FORMAT = "HH:mm";
const DAY_FORMAT = "dddd";
const DATE_FORMAT = "DD.MM";

declare var jQuery:JQueryStatic;

@Component({
    templateUrl: "app/common/Components/EPG/epg.html",
    selector: "epg",
    //inputs: ["title: title", "titleClass: title-class", "menuItems: menu-items", "routesConfigured: routes-configured"],
    //events: ["routesConfiguredEvent: routes-configured"],
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, Popover],
    providers: [TvService]
})
export class EpgComponent {
  private stepSize: number = 15;
  private startTime: any = this.getCurrentRoundedTime();
  private timeSteps: number = 13;
  private timeindicatorInterval: any = null;
  // GUI
  channelGroups: IChannelGroup[] = [];
  selectedGroup: IChannelGroup = null;
  channelList: IChannel[] = [];
  programList: { [key:number]: IProgram[]; } = {};
  loadingProgramlist: boolean = true;
  // used for Modal and Popover
  currentProgram: IProgram = {ServerIndex: "", ProgramId: 0, ChannelId: 0, Title: "", Description: "", Genre: "", StartTime: new Date(), EndTime: new Date(), RecordingStatus: RecordingStatus.None, SeasonNumber: 0, EpisodeNumber: 0, EpisodeTitle: ""};
  hidePopover: boolean = true;
  offsetPopover: {top: number, left: number} = {top: 0, left: 0};
  selectedScheduleType: ScheduleRecordingType = ScheduleRecordingType.Once;
  mediaType = MediaType;  // used in the Template

  timeLine: INavigation[] = [];
  dayLine: INavigation[] = [];
  timeindicator: ITimeindicator = {Visible: false, Width: 0};

  constructor(public tvService: TvService, private elements: ElementRef) {
    // Load Channel groups
    tvService.GetGroups().map(res => res.json()).map(res => tvService.ToGroupList(res)).subscribe(
      res => {
        this.channelGroups = res;
        this.selectedGroup = res[0]; // TVE always has at least the "All Channels" Group
        // get the Channels for the Group
        this.GetChannelsByGroup(this.selectedGroup.Id);
        // get the Programs for the Group
        this.GetPrograms();
      },
      err => console.error(err));
  }

  ngOnInit() {
    this.createTimeLine();
    this.createDayLine(this.startTime);
    this.createTimeindicator();
  }

  ngOnDestroy() {
    clearInterval(this.timeindicatorInterval);
  }

  createDayLine(startTime) {
    // Clear Dayline
    this.dayLine = [];

    for (var i = 0; i < 7; i++) {  // A week has 7 days
      var time: any = this.dateAddDays(startTime, i);
      var dayLabel: string = time.format(DAY_FORMAT);
      if (time.isSame(momentConstructor(), "day")) {
        dayLabel = "Today";
      }else {
        if (time.diff(momentConstructor(), "days") > 5) {
          dayLabel = time.format(DATE_FORMAT);
        }
      }
      this.dayLine.push({
        Active: (time.isSame(this.startTime, "day")),
        Label: dayLabel,
        DateTime: time
      });
    }
  }

  createTimeLine() {
    // Clear Timeline
    this.timeLine = [];

    for (var i = 0; i < this.timeSteps; i++) {
      var time: any = this.dateAddMinutes(this.startTime, i * this.stepSize);
      var timeLabel: string = time.format(TIME_FORMAT);
      this.timeLine.push({
        Active: (this.startTime.format(TIME_FORMAT) == timeLabel),
        Label: timeLabel,
        DateTime: time
      });
    }
  }

  createTimeindicator() {
    var endTime = this.getEndTime();
    var currentTime = momentConstructor();
    var programContainerWidth: number = jQuery(this.elements.nativeElement).find("#programContainer").width();
    var programTimeindicatorMarginLeft: number = parseInt(jQuery(this.elements.nativeElement).find("#programTimeindicator").css("margin-left"));

    if (this.timeindicatorInterval == null) {
      this.timeindicatorInterval = setInterval(() => { this.createTimeindicator(); }, 60 * 1000);
    }

    if (currentTime.isBetween(this.startTime, endTime, "second")) {
      console.log("In Range");
      this.timeindicator.Visible = true;
      // TODO: Remove Workaround
      // I couldn't figure out how to make the indicator stay inside the parent div even with width=100%
      // The problem is the css margin: programContainer has a width of 1140px and the margin-left is 80px
      // On programTimeindicator with width set to 100% it would overlap exactly 80px to the right.
      // So I needed to use pixels. If someone knows a better fix, please go ahead
      //this.timeindicator.Width = this.startTime.diff(currentTime, "minutes") / this.startTime.diff(endTime, "minutes") * 100;
      this.timeindicator.Width = (programContainerWidth - programTimeindicatorMarginLeft) * (this.startTime.diff(currentTime, "minutes") / this.startTime.diff(endTime, "minutes"));
    }else {
      console.log("Out of Range");
      this.timeindicator.Visible = false;
    }
  }

  /*
  This function must be called if this.startTime changed!
   */
  onStartTimeChanged() {
    this.createTimeLine();
    this.createDayLine(this.startTime);
    this.createTimeindicator();
    this.GetPrograms();
  }

  /*
  Navigation
   */

  onGroupToRight()  {
    var length: number = this.channelGroups.length;
    for (var i = 0; i < length; i++) {
      if (this.channelGroups[i].Id == this.selectedGroup.Id) {
        // at the end of the Array -> Go back to the start
        if (i == length - 1) {
          this.selectedGroup = this.channelGroups[0];
        // Select next Group
        }else {
          this.selectedGroup = this.channelGroups[i+1];
        }
        break;
      }
    }
    // Update Channels
    this.GetChannelsByGroup(this.selectedGroup.Id);
    // Update Programs
    this.GetPrograms();
  }

  onGroupToLeft()  {
    var length: number = this.channelGroups.length;
    for (var i = 0; i < length; i++) {
      if (this.channelGroups[i].Id == this.selectedGroup.Id) {
        // at the start of the Array -> Go back to the end
        if (i == 0) {
          console.log("i == 0");
          this.selectedGroup = this.channelGroups[length - 1];
          // Select next Group
        }else {
          console.log("i--");
          this.selectedGroup = this.channelGroups[i-1];
        }
        break;
      }
    }
    // Update Channels
    this.GetChannelsByGroup(this.selectedGroup.Id);
    // Update Programs
    this.GetPrograms();
  }

  onDaysToRight() {
    var lastDay = this.dayLine[this.dayLine.length - 1];
    this.createDayLine(this.dateAddDays(lastDay.DateTime, 1));
  }

  onDaysToLeft() {
    var firstDay = this.dayLine[0];
    var startTime = this.dateAddDays(firstDay.DateTime, -7);
    // don't go before today
    if (startTime.isBefore(momentConstructor(), "day")) {
      startTime = momentConstructor();
    }
    this.createDayLine(startTime);
  }

  onDaySelect(date) {
    var diffToCurrentStarTime: number = Math.ceil(date.diff(this.startTime, "days", true));
    this.startTime = this.dateAddDays(this.startTime, diffToCurrentStarTime);
    this.onStartTimeChanged();
  }

  onTimeToRight() {
    this.startTime = this.dateAddMinutes(this.startTime, this.timeSteps * this.stepSize);
    this.onStartTimeChanged();
  }

  onTimeToLeft() {
    var newTime = this.dateAddMinutes(this.startTime, this.timeSteps * this.stepSize * (-1));
    // don't go before today
    if (newTime.isBefore(this.getCurrentRoundedTime(), "minute")) {
      newTime = this.getCurrentRoundedTime();
    }
    this.startTime = newTime;
    this.onStartTimeChanged();
  }

  onTimeSelect(time) {
    this.startTime = momentConstructor(time);
    this.onStartTimeChanged();
  }

  onNow() {
    this.startTime = this.getCurrentRoundedTime();
    this.onStartTimeChanged();
  }

  onProgramHover(event: Event, program: IProgram) {
    this.currentProgram = program;

    var popover = jQuery("#popover");
    //popover.show();
    this.hidePopover = false;
    var popoverWidth = popover.width();
    var caller = jQuery(event.target);
    var callerHeight = caller.height();
    var callerWidth = caller.width();

    this.offsetPopover.top = caller.offset().top + callerHeight;
    this.offsetPopover.left = caller.offset().left + callerWidth / 2 - popoverWidth / 2;
    /*this.offsetPopover = {
      top: caller.offset().top + callerHeight,
      left: caller.offset().left + callerWidth / 2 - popoverWidth / 2
    };*/
    //popover.offset({ top: caller.offset().top + callerHeight, left: caller.offset().left + callerWidth / 2 - popoverWidth / 2});
  }

  onPopoverOut() {
    this.hidePopover = true;
  }

  onProgramClick(program: IProgram) {
    this.currentProgram = program;
    jQuery("#myModal").modal("toggle");
  }

  onScheduleSave(scheduleType: ScheduleRecordingType) {
    this.tvService.ScheduleByProgram(this.currentProgram.ProgramId, scheduleType).subscribe(
      res => {
        console.log("Schedule saved!");
        console.log(res.json());
        // reload programs
        this.GetPrograms();
      },
      err => console.error(err));
    // close modal
    jQuery("#myModal").modal("toggle");
  }

  onScheduleCancel() {
    this.tvService.RemoveScheduleByProgram(this.currentProgram.ProgramId, ScheduleRecordingType.Once).subscribe(
      res => {
        console.log("Schedule canceled!");
        // reload programs
        this.GetPrograms();
      },
      err => console.error(err));
    // close modal
    jQuery("#myModal").modal("toggle");
  }

  /*
  Validations
   */

  isScheduled(recordingStatus: RecordingStatus): boolean {
    return recordingStatus != RecordingStatus.None;
  }

  /*
  Request Functions
   */
  GetChannelsByGroup(groupId: number) {
    this.tvService.GetChannelsByGroup(groupId).map(res => res.json()).map(res => this.tvService.ToChannelList(res)).subscribe(
      res => {
        this.channelList = res;
      },
      err => console.error(err));
  }

  GetPrograms() {
    this.loadingProgramlist = true;
    this.tvService.GetProgramsByGroup(this.selectedGroup.Id, this.startTime, this.getEndTime().toDate()).map(res => res.json()).subscribe(
      res => {
        this.programList = res;
        this.loadingProgramlist = false;
      },
      err => {
        this.loadingProgramlist = false;
        console.error(err)
      });
  }

  /*
  * Helper Functions
  * */

  date_round(date, duration) {
    return momentConstructor(Math.ceil((+date)/(+duration)) * (+duration));
  }

  dateAddMinutes(date, duration) {
    return momentConstructor(date).add(duration, "minutes")
  }

  dateAddDays(date, duration) {
    return momentConstructor(date).add(duration, "days")
  }

  getCurrentRoundedTime() {
    return this.date_round(momentConstructor().subtract(this.stepSize, "minutes"), moment.duration(this.stepSize, "minutes"));
  }

  getEndTime() {
    return this.dateAddMinutes(this.startTime, this.timeSteps * this.stepSize);
  }

  getProgramWidth(program: IProgram): number {
    var startTime = momentConstructor(program.StartTime);
    var endTime = momentConstructor(program.EndTime);
    if (startTime.isBefore(this.startTime)) {
      startTime = this.startTime;
    }

    if (endTime.isAfter(this.getEndTime())) {
      endTime = this.getEndTime();
    }

    return startTime.diff(endTime, "minutes") / this.startTime.diff(this.getEndTime(), "minutes") * 100;
  }

  dateToString(date: Date, format: string): string {
    return momentConstructor(date).format(format);
  }

  noLogo(event) {
    event.target.src = "images/noLogo.png";
  }

}