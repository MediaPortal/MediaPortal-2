import {RecordingStatus} from "./IRecordingStatus";

export interface IProgram {
    ServerIndex: string;
    ProgramId: number;
    ChannelId: number;
    Title: string;
    Description: string;
    Genre: string;
    StartTime: Date;
    EndTime: Date;
    RecordingStatus: RecordingStatus;
    SeasonNumber: number;
    EpisodeNumber: number;
    EpisodeTitle: string;
}