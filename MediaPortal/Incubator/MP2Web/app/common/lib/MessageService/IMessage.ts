import {MessageType} from "./IMessageType";

export interface IMessage {
  type: MessageType;
  title: string;
  text: string;
  created: number;
  // 0 indicates not yet viewed, otherwise it will containe the time it was viewed
  viewed: number;
}