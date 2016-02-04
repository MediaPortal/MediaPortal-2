import {Component, View, EventEmitter, OnChanges, SimpleChange, OnInit} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {ROUTER_DIRECTIVES, RouteDefinition, Location, RouteConfig, RouterLink, Router} from "angular2/router";
import {ComponentHelper} from "../../../../ComponentHelper";
import {SideMenuConfiguration} from "./interface.SideMenuConfiguration";


/*
 This component provides an easy way to add a side menu to subpages.
 I made the decision to let the calling component handle the routes. This gives you more freedom from a
 design point of view (where you want to position the router outlet).
 */
@Component({
    templateUrl: "app/common/Components/SideMenu/sideMenu.html",
    selector: "sideMenu",
    inputs: ["title: title", "titleClass: title-class", "menuItems: menu-items", "routesConfigured: routes-configured"],
    events: ["routesConfiguredEvent: routes-configured"],
    directives: [ROUTER_DIRECTIVES, COMMON_DIRECTIVES, CORE_DIRECTIVES]
})
export class sideMenuComponent {
    title: string;  // header
    titleClass: string; // if you would like to pass additional css for e.g. fontawesome
    menuItems: SideMenuConfiguration[] = [];
    routerRoutes: RouteDefinition[] = [];
    routesConfiguredEvent: EventEmitter<any> = new EventEmitter();
    routesConfigured: boolean;

    constructor(public router: Router) {
        console.log("sideMenuComponent: constructor");
    }

    /*
    The input variables are initialized!
    They aren't inside the constructor!
     */
    ngOnInit() {
        console.log("sideMenuComponent: building Routes");
        this.buildRoutes();
        console.log("sideMenuComponent: send routes-configured event");
        this.routesConfiguredEvent.emit(this.routerRoutes);
    }

    /*
     Checks which route is active
     */
    isSubRouteActive(categoryName, pages: SideMenuConfiguration[]) {
        for (var i = 0; i < pages.length; i++) {
            var name: any = this.generateSubentryRoutename(categoryName, [pages[i].Name]);
            if (this.router.isRouteActive(this.router.generate([name]))){
                return true;
            }
        }
        return false;
    }

    /*
     Build the route definitions for the actual AngularJS Router
     */
    public buildRoutes(): RouteDefinition[] {
        if (this.routesConfigured) {
            return;
        }

        for (var i = 0; i < this.menuItems.length; i++) {
            // no dropdown
            if (this.menuItems[i].Pages == null || this.menuItems[i].Pages.length == 0) {
                this.addRoute(this.menuItems[i].Path, this.menuItems[i].Name, this.menuItems[i].Component, this.menuItems[i].ComponentPath)
            // dropdown
            }else {
                for (var x = 0; x < this.menuItems[i].Pages.length; x++) {
                    this.addRoute(this.menuItems[i].Path + this.menuItems[i].Pages[x].Path,
                        this.generateSubentryRoutename(this.menuItems[i].Name, this.menuItems[i].Pages[x].Name),
                        this.menuItems[i].Pages[x].Component, this.menuItems[i].Pages[x].ComponentPath)
                }
            }
        }

        return this.routerRoutes;
    }

    /*
     Add a Route to the initial routes
     */
    addRoute(path, name, component, componentPath) {
        var routeObj = {
            path: path,
            name: this.firstToUpperCase(name),
            loader: () => ComponentHelper.LoadComponentAsync(component,componentPath),
            component: null,
            defaultRoute: false
        };
        if (!this.doesPathExist(routeObj.path))
            this.routerRoutes.push(routeObj);
    }

    /*
     We highlight the active section in the navigation bar, so we build routes for every subsection even if they
     are pointing to the same target.
     Because of this we need names like "CATEGORY_SUBROUTENAME"
     */
    generateSubentryRoutename(categoryName, routeName) {
        return this.firstToUpperCase(categoryName + "_" + routeName);
    }

    /*
     AngularJS requires the first letter to be upper case, so we make sure that it is
     */
    firstToUpperCase(str: string) {
        return str.substr(0, 1).toUpperCase() + str.substr(1);
    }

    /*
     Every route is only allowed to be present once. So check if a route with this path already exists
     */
    doesPathExist(path) {
        for (var i = 0; i < this.routerRoutes.length; i++) {
            if (this.routerRoutes[i].path == path)
                return true;
        }
        return false;
    }

    /*
    Click action for the Menu
    There is also a a href in the html code
     */
    onClick(routeName: string) {
        this.router.navigate([routeName]);
    }
}