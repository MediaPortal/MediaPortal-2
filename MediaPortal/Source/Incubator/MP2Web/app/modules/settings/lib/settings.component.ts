import {Component, View} from "angular2/core";
import {ROUTER_DIRECTIVES, RouteDefinition, Location, RouteConfig, RouterLink, Router} from "angular2/router";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";

import {sideMenuComponent} from "../../../common/Components/SideMenu/lib/sideMenu.component";
import {SideMenuConfiguration} from "../../../common/Components/SideMenu/lib/interface.SideMenuConfiguration";
import {HomeSettingsComponent} from "./home.settings.component";

// TODO: Find a better way to remove routes and reinitialize them
// Unfortunately there is no documentation on how to remove routes
var routesConfiguredBoolGlobal: boolean = false;

/*
Main component for the Movies module
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
    settingsMenu: SideMenuConfiguration[] = [
        {
            Name: "SingleEntry",
            Label: "SingleEntry",
            LabelClass: "fa fa-users fa-lg",
            Path: "/singleEntry",
            ComponentPath: "./app/hero-list.component",
            Component: "HeroListComponent",
            Visible: true,
            Pages: []
        },
        {
            Name: "MultiEntry",
            Label: "MultiEntry",
            LabelClass: "",
            Path: "/multiEntry",
            ComponentPath: "",
            Component: "",
            Visible: true,
            Pages: [
                {
                    Name: "SubEntry",
                    Label: "SubEntry",
                    LabelClass: "",
                    Path: "/subEntry",
                    ComponentPath: "./app/crisis-list.component",
                    Component: "CrisisListComponent",
                    Visible: true,
                    Pages: []
                }
            ]
        }
    ];
    routesConfiguredBool: boolean = routesConfiguredBoolGlobal;

    constructor(public router: Router, private location: Location) {

    }

    /*ngOnDestroy() {
        console.log("OnDestroy");
        console.log(this.router.registry)
    }*/


    routesConfigured(event: RouteDefinition[]) {
        console.log("SettingsComponent: Event received");
        // See todo above. It doesn't seem to be possible to destroy a route yet. So we just check if we already created the route
        if (routesConfiguredBoolGlobal) {
            console.log("SettingsComponent: Route is already configured, return true")
            return;
        }

        // configure the Router
        this.router.childRouter(SettingsComponent).config(event);
        console.log("PATH: '"+this.location.path()+"'")
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