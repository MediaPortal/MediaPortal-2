import {ScheduleRecordingType} from "./IScheduleRecordingType";
import {KeepMethodType} from "./IKeepMethodType";
import {PriorityType} from "./IPriorityType";

export interface ISchedule {
    ScheduleId: number;
    ParentScheduleId: number;
    ChannelId: number;
    Name: string;
    StartTime: Date;
    EndTime: Date;
    IsSeries: boolean;
    RecordingType: ScheduleRecordingType;
    Priority: PriorityType;
    PreRecordInterval: string;
    PostRecordInterval: string;
    KeepMethod: KeepMethodType;
    KeepDate: Date;
}