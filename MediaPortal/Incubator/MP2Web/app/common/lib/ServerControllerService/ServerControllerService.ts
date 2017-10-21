import {Injectable, EventEmitter} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {IAttachedClients} from "./interface.attachedClients";

@Injectable()
export class ServerControllerService {
    private BASE_URL: string;
    private configurationServiceLoadedSubscription: any;
    private intervalTime: number = 2 * 60 * 1000; // every 2 min
    private checkInterval: any;

    public attachedClients: IAttachedClients[] = [];
    public onlineClients: number = 0;

    constructor(private http: Http, private configurationService: ConfigurationService) {
        /*
         Wait for Configuration Service
         */
        this.configurationServiceLoadedSubscription = this.configurationService.getLoadedChangeEmitter().subscribe(() => {
            console.log("ServerControllerService: Config loaded");
            this.setup();
        });
    }

    setup() {
        this.BASE_URL = this.configurationService.config.WebApiUrl;
        console.log("..."+this.BASE_URL)
        this.updateInformation();

        this.checkInterval = setInterval(() => {
            this.updateInformation();
        }, this.intervalTime);
    }

    ngOnDestroy() {
        clearInterval(this.checkInterval);
    }

    private updateInformation() {
        console.log("ServerControllerService: update information")

        // Attached Clients
        var urlAttached: string = this.BASE_URL + "/api/v1/Server/ServerController/GetAttachedClients";

        this.http.request(new Request({
            method: RequestMethod.Get,
            url: urlAttached
        })).map(res => res.json()).subscribe(res => {
            for (let client of res) {
                var clientAlreadyExists: boolean = false;
                var newAttachedClient: IAttachedClients = {
                    LastClientName: client.LastClientName,
                    LastSystem: client.LastSystem,
                    SystemId: client.SystemId,
                    Online: false
                };

                for (let attachedClient of this.attachedClients) {
                    if (attachedClient.SystemId == newAttachedClient.SystemId) {
                        clientAlreadyExists = true;
                        break;
                    }
                }

                if (!clientAlreadyExists) {
                    this.attachedClients.push(newAttachedClient);
                }
            }
        });

        // Online Clients
        var urlOnline: string = this.BASE_URL + "/api/v1/Server/ServerController/GetConnectedClients";

        this.http.request(new Request({
            method: RequestMethod.Get,
            url: urlOnline
        })).map(res => res.json()).subscribe(res => {
            this.onlineClients = res.length;

            for (var i = 0; i < this.attachedClients.length; i++) {
                if (res.indexOf(this.attachedClients[i].SystemId) != -1) {
                    this.attachedClients[i].Online = true;
                }else {
                    this.attachedClients[i].Online = false;
                }
            }
        });
    }
}