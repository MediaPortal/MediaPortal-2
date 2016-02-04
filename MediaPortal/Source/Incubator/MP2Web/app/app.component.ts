import {Component, View} from "angular2/core";
import {ROUTER_DIRECTIVES, Location, RouteConfig, RouterLink, Router} from "angular2/router";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";
import {TranslateService, TranslatePipe} from "ng2-translate/ng2-translate";

import {ComponentHelper} from "./ComponentHelper";

//import {CrisisListComponent}   from "./crisis-list.component";
//import {HeroListComponent}     from "./hero-list.component";
import {HomeComponent}     from "./modules/home/lib/home.component";
//import {MoviesComponent}     from "./modules/movies/lib/movies.component";
import {ConfigurationService} from "./common/lib/ConfigurationService/ConfigurationService";
import {MP2WebAppRouterConfiguration} from "./common/lib/ConfigurationService/interface.RouteConfiguration";

/*
Main MP2Web Component

This Component get's bootstraped and is responsible for the further Application start
 */
@Component({
    selector: "mp2web",
    templateUrl: "app/modules/main/main.html",
    //templateUrl: "app/modules/main/main.html",
    directives: [ROUTER_DIRECTIVES, COMMON_DIRECTIVES, NgFor, NgIf],
    pipes: [TranslatePipe]
})
export class AppComponent {
    routes: MP2WebAppRouterConfiguration[];
    addedCategories: string[] = [];
    categorieAddedBeforeElement: string[] = [];
    routerRoutes = [
        // Base Path route, we want to have this one hardcoded
        {path:"/", name: "Home", component: HomeComponent, loader: null, defaultRoute: true}
    ];

    loaded: boolean = false;
    configurationServiceLoadedSubscription: any;

    constructor(public router: Router, public location: Location, private translate: TranslateService, private configurationService: ConfigurationService) {
        /*
        Wait for Configuration Service
         */
        this.configurationServiceLoadedSubscription = this.configurationService.getLoadedChangeEmitter().subscribe(() => {
            console.log("AppComponent: Config loaded");
            this.setup();
        });
    }

    setup() {
        /*
         Router setup
         */
        this.routes = this.configurationService.config.Routes;
        this.buildRoutes();
        this.router.config(this.routerRoutes);

        /*
         Translation Setup
         */
        var userLang = navigator.language.split("-")[0]; // use navigator lang if available
        //userLang = /(de|en)/gi.test(userLang) ? userLang : "en";
        userLang = "en";

        // this language will be used as a fallback when a translation isn't found in the current language
        this.translate.setDefaultLang("en");

        var prefix = "app/lang";
        var suffix = ".json";
        this.translate.useStaticFilesLoader(prefix, suffix);

        // the lang to use, if the lang isn't available, it will use the current loader to get them
        this.translate.use(userLang);

        this.translate.getTranslation(userLang);

        // Everything is ready -> tell the App/Template
        console.log("AppComponent: Setup done");
        this.loaded = true;
    }

    /*
    Build the route definitions for the actual AngularJS Router
     */
    buildRoutes() {
        for (var i = 0; i < this.routes.length; i++) {
            // no dropdown
            if (this.routes[i].Pages == null || this.routes[i].Pages.length == 0) {
                this.addInitialRoute(this.routes[i].Path, this.routes[i].Name, this.routes[i].Component, this.routes[i].ComponentPath)
            // dropdown
            }else {
                for (var x = 0; x < this.routes[i].Pages.length; x++) {
                    this.addInitialRoute(this.routes[i].Path + this.routes[i].Pages[x].Path,
                        this.generateSubentryRoutename(this.routes[i].Name, this.routes[i].Pages[x].Name),
                        this.routes[i].Pages[x].Component, this.routes[i].Pages[x].ComponentPath)
                }
            }
        }
    }

    /*
    Add a Route to the initial routes
     */
    addInitialRoute(path, name, component, componentPath) {
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
    Checks which route is active
     */
    isSubrouteActive(categoryName, pages: MP2WebAppRouterConfiguration[]) {
        for (var i = 0; i < pages.length; i++) {
            var name: any = this.generateSubentryRoutename(categoryName, [pages[i].Name]);
            if (this.router.isRouteActive(this.router.generate([name]))){
                return true;
            }
        }
        return false;
    }

    /*
    Check if a Category divider needs to be added
     */
    isCategoryNeeded(routeName: string, pageName: string, category: string): boolean {
        var routeCategoryName: string = routeName + "_" + category;
        var routePageCategoryName: string = routeName + "_" + pageName + "_" + category;

        // Don't add a header if there is no category defined
        if (category == null || category == "") {
            return false;
        }

        // We need to keep track of the item which triggered the Category header
        // The reason is that it is not allowed to have changes triggered by the change detection.
        // In short: Going through the loops in the model a second time would give different results for isCategoryNeeded
        // => always false
        if (this.categorieAddedBeforeElement.indexOf(routePageCategoryName) > -1) {
            return true;
        }

        // Add the Category if defined and not shown already
        if (this.addedCategories.indexOf(routeCategoryName) == -1) {
            this.addedCategories.push(routeCategoryName);
            this.categorieAddedBeforeElement.push(routePageCategoryName);
            return true;
        }

        return false;
    }
}