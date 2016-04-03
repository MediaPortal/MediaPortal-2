import {Component, View} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, RequestMethod, Headers} from "angular2/http";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";

import {MessageService} from "../../../common/lib/MessageService/MessageService";
import {MessageType} from "../../../common/lib/MessageService/IMessageType";
import {DisplaySettingsComponent} from "./display.settings.component";
import {ISettingData} from "./ISettingData";

/*
Main component for the Movies module
 */
@Component({
    templateUrl: "app/modules/settings/home.settings.html",
    directives: [COMMON_DIRECTIVES, NgIf, NgFor, DisplaySettingsComponent]
})
export class HomeSettingsComponent {

  constructor(private http: Http, private messageService: MessageService) {
  }

}