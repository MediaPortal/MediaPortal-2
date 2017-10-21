import {IChannel, IProgram} from "../../../lib/TvService/TvServiceInterfaces";

export interface IChannelRow {
  Channel: IChannel;
  Programs: IProgram[];
}