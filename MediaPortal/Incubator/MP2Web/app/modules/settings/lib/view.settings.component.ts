import {Component, View} from "angular2/core";
import {Location, ComponentInstruction} from "angular2/router";
import {HTTP_PROVIDERS, Http, Request, RequestMethod, Headers} from "angular2/http";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";

import {MessageService} from "../../../common/lib/MessageService/MessageService";
import {MessageType} from "../../../common/lib/MessageService/IMessageType";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {DisplaySettingsComponent} from "./display.settings.component";
import {ISettingData} from "./ISettingData";

/*
Main component for the Movies module
 */
@Component({
    templateUrl: "app/modules/settings/view.settings.html",
    directives: [COMMON_DIRECTIVES, NgIf, NgFor, DisplaySettingsComponent]
})
export class ViewSettingsComponent {
  BASE_URL: string;
  output: any;
  settingId: string;
  loading: boolean = true;

  constructor(private location: Location, private http: Http, private configurationService: ConfigurationService, private messageService: MessageService) {
    this.BASE_URL = configurationService.config.WebApiUrl;

    this.loadNewSetting();
  }

  // reuse the component
  routerCanReuse(next: ComponentInstruction, prev: ComponentInstruction) { return true; }
  // update the setting id
  routerOnReuse(next: ComponentInstruction, prev: ComponentInstruction) {
    this.loadNewSetting();
  }

  loadNewSetting() {
    this.loading = true;
    this.settingId = this.location.path().split("/").pop();

    this.http.request(new Request({
      method: RequestMethod.Get,
      url: this.BASE_URL + "/api/v1/Server/ServerPlugins/SettingProperties/" + this.settingId
    })).map(res => res.json()).subscribe(res => {
      this.output = res;
      this.loading = false;
    });
  }

  onSettingChanged(data: ISettingData) {
    console.log("Event received");
    console.log(data);
    var headers = new Headers();
    headers.append("Content-Type", "application/json");
    this.http.request(new Request({
      method: RequestMethod.Put,
      url: this.BASE_URL + "/api/v1/Server/ServerPlugins/SettingProperty/" + this.settingId + "/" + data.name,
      headers: headers,
      body: JSON.stringify(data.value)
    })).map(res => res.json()).subscribe(res => {
      console.log("Setting saved!");
      console.log(res);
      this.messageService.addNotificationMessage(res ? "Setting saved" : "Errow while saving Setting",
        res ? MessageType.Success : MessageType.Error,
        data.name + ": " + data.value,
        false);
    });
  }
}