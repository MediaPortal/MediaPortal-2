import {Injectable, EventEmitter, Component} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {IMessage} from "./IMessage";
import {MessageType} from "./IMessageType";

const MESSAGE_LIFETIME: number = 1; // the message will stay in the overview for x minutes after it was viewed

@Injectable()
export class MessageService {
  private BASE_URL: string;
  private configurationServiceLoadedSubscription: any;
  private cleanupInterval: any = null;

  // Events
  newNotificationMessages: EventEmitter<IMessage> = new EventEmitter();

  // public variables to access the messages
  public notificationMessages: IMessage[] = []; // newest message is always at [0]
  public unreadNotificationMessages: number = 0;

  constructor(private http: Http, private configurationService: ConfigurationService) {
    /*
    Wait for Configuration Service
    */
    this.configurationServiceLoadedSubscription = this.configurationService.getLoadedChangeEmitter().subscribe(() => {
      console.log("MessageService: Config loaded");
      this.setup();
    });
  }

  private setup() {
    this.BASE_URL = this.configurationService.config.WebApiUrl;
    this.addNotificationMessage("MessageService Started", MessageType.Info);
    this.cleanupInterval = setInterval(() => { this.onCleanup(); }, 60 * 1000);
  }

  ngOnDestroy() {
    clearInterval(this.cleanupInterval);
  }

  addNotificationMessage(title: string, type: MessageType, text: string = "", persist: boolean = true) {
    var message: IMessage = {
      title: title,
      text: text,
      type: type,
      created: Date.now(),
      viewed: 0 // 0 indicates not yet viewed
    }
    if (persist) {
      this.notificationMessages.unshift(message);
      this.unreadNotificationMessages++;
    }
    // notify all subscribers
    this.newNotificationMessages.emit(message);
  }

  getFontAweasomeIcon(messageType: MessageType): string {
    var icon: string = "fa ";
    switch (messageType) {
      case MessageType.Error:
        icon += "fa-exclamation-circle";
        break;
      case MessageType.Warning:
        icon += "fa-exclamation-triangle";
        break;
      case MessageType.Info:
        icon += "fa-info-circle";
        break;
      case MessageType.Success:
        icon += "fa-check";
        break;
      default:
        icon += "fa-info-circle";
    }

    return icon;
  }

  /*
  Events
  */
  getNotificationMessageEmitter() {
    return this.newNotificationMessages;
  }

  /*
  Gets called if one views the list of messages and resets the unread messages counter
  */
  onMessagesView() {
    this.unreadNotificationMessages = 0;
    // set the viewed time on the messages
    for (var i = 0; i < this.notificationMessages.length; i++) {
      if (this.notificationMessages[i].viewed == 0) {
        this.notificationMessages[i].viewed = Date.now();
      }
    }
  }

  /*
  Removes old messages
  */
  private onCleanup() {
    for (var i = 0; i < this.notificationMessages.length; i++) {
      // Date.now() returns ms
      if (this.notificationMessages[i].viewed != 0 && Date.now() - this.notificationMessages[i].viewed >= MESSAGE_LIFETIME * 60000) {
        this.notificationMessages.splice(i);
      }
    }
  }
}

/*
      Favicon
       */
/*
console.log("Change Favicon");
var canvas = document.createElement("canvas"),
  ctx,
  img = new Image(),
  link = <HTMLLinkElement>document.getElementById("favicon"),
  day = (new Date).getDate() + '';

if (canvas.getContext) {
  img.onload = function () { // once the image has loaded
    canvas.height = canvas.width = img.width; // set the size
    ctx = canvas.getContext("2d");
    ctx.drawImage(this, 0, 0);
    ctx.font = 'bold 350px "helvetica", sans-serif';

    // create Rectangle
    var rctHeight;
    var rctWidth = rctHeight = canvas.width / 2;
    var rctX = this.width - rctWidth - 50;
    var rctY = 50;
    var cornerRadius = 20;

    ctx.fillStyle = "#f03d25";
    ctx.strokeStyle = "#f03d25";
    ctx.lineJoin = "round";
    ctx.lineWidth = cornerRadius;
    ctx.strokeRect(rctX + (cornerRadius / 2), rctY + (cornerRadius / 2), rctWidth - cornerRadius, rctHeight - cornerRadius);
    ctx.fillRect(rctX + (cornerRadius / 2), rctY + (cornerRadius / 2), rctWidth - cornerRadius, rctHeight - cornerRadius);
            
    // create Text
    ctx.fillStyle = "#FFFFFF";
    ctx.textBaseline = "middle";
    ctx.textAlign = "center";
    if (day.length == 1) day = "0" + day;
    ctx.fillText(day, rctX + rctWidth / 2, rctY + rctHeight / 2);
    link.href = canvas.toDataURL("image/png");
  };
  img.src = link.href;
}


*/