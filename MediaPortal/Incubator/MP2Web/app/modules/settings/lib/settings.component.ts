import {Component, View} from "angular2/core";
import {ROUTER_DIRECTIVES, RouteDefinition, Location, RouteConfig, RouterLink, Router} from "angular2/router";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";

import {sideMenuComponent} from "../../../common/Components/SideMenu/lib/sideMenu.component";
import {SideMenuConfiguration} from "../../../common/Components/SideMenu/lib/interface.SideMenuConfiguration";
import {HomeSettingsComponent} from "./home.settings.component";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";

// TODO: Find a better way to remove routes and reinitialize them
// Unfortunately there is no documentation on how to remove routes
var routesConfiguredBoolGlobal: boolean = false;

/*
Main component for the Settings module
 */
@Component({
    templateUrl: "app/modules/settings/settings.html",
    directives: [ROUTER_DIRECTIVES, COMMON_DIRECTIVES, CORE_DIRECTIVES, sideMenuComponent]
})
@RouteConfig([
    {path:"/", name: "Home", component: HomeSettingsComponent, useAsDefault: true},
    {path: "/**", name: "Fallback", component: HomeSettingsComponent}
])
export class SettingsComponent {
    settingsMenu: SideMenuConfiguration[] = [];
    routesConfiguredBool: boolean = routesConfiguredBoolGlobal;
    BASE_URL: string;
    ready: boolean = false;

    constructor(public router: Router, private location: Location, private http: Http, private configurationService: ConfigurationService) {
      this.BASE_URL = configurationService.config.WebApiUrl;

      this.http.request(new Request({
        method: RequestMethod.Get,
        url: this.BASE_URL + "/api/v1/Server/ServerPlugins/PluginSettings"
      })).map(res => res.json()).subscribe(res => {
        this.settingsMenu = [];
        for (let setting in res) {
          if (res.hasOwnProperty(setting)) {
            var menuPoint: SideMenuConfiguration = {
              Name: res[setting].Id,
              Label: res[setting].Name,
              LabelClass: "fa fa-cog fa-lg",
              Path: "/viewSetting/" + res[setting].Id,
              ComponentPath: "./app/modules/settings/lib/view.settings.component",
              Component: "ViewSettingsComponent",
              Visible: true,
              Pages: []
            }

            this.settingsMenu.push(menuPoint);

          }
        }
        this.ready = true;

        console.log(this.settingsMenu);
      });
    }

    /*ngOnDestroy() {
        console.log("OnDestroy");
        console.log(this.router.registry)
    }*/


    routesConfigured(event: RouteDefinition[]) {
      console.log("SettingsComponent: Event received");
      // See todo above. It doesn't seem to be possible to destroy a route yet. So we just check if we already created the route
      if (routesConfiguredBoolGlobal) {
        console.log("SettingsComponent: Route is already configured, return true");
        return;
      }

      // configure the Router
      this.router.childRouter(SettingsComponent).config(event);
      console.log("PATH: '" + this.location.path() + "'");
      // If you hit F5 while you are on e.g. "#/settings/multiEntry/subEntry" you wouldn't get to the same page again
      // because the routes aren't configured yet. => You would get to the Fallback route.
      // This line of code ensures that you get back to the right page
      if (this.location.path().startsWith("/settings")) {
          this.router.navigateByUrl(this.location.path());
      }

      this.routesConfiguredBool = true;
      routesConfiguredBoolGlobal = true;
    }
}