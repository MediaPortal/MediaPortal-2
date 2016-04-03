import {Injectable, EventEmitter} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";
import {IConfiguration} from "./interface.ConfigurationService";

var API_GET_CONFIG = "/api/Configuration";

/*
MP2Web Configuration Service
 */
@Injectable()
export class ConfigurationService {
  config: IConfiguration = <IConfiguration>{};
  loadedEvent: EventEmitter<any> = new EventEmitter();

  constructor(private http: Http) {
    console.log("ConfigurationService: Started!");
    this.loadConfig();
  }

  private loadConfig() {
    this.http.request(new Request({
      method: RequestMethod.Get,
      url: API_GET_CONFIG
    })).map(res => res.json()).subscribe(res => {
      this.config = res;

      // send the loaded event
      this.loadedEvent.emit(null);
      console.log("ConfigurationService: Data Loaded");
    });
  }

  /*
  Notify the Client that the config is loaded now.
    */
  getLoadedChangeEmitter() {
      return this.loadedEvent;
  }
}