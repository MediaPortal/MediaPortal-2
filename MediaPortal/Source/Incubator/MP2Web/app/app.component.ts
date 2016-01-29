import {Component, View} from "angular2/core";
import {ROUTER_DIRECTIVES, Location, RouteConfig, RouterLink, Router} from "angular2/router";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";
import {TranslateService, TranslatePipe} from "ng2-translate/ng2-translate";

import {CrisisListComponent}   from "./crisis-list.component";
import {HeroListComponent}     from "./hero-list.component";
import {HomeComponent}     from "./modules/home/lib/home.component";
import {MoviesComponent}     from "./modules/movies/lib/movies.component";
import {ConfigurationService} from "./common/lib/ConfigurationService/ConfigurationService";

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
    routes = [
        {path: "/MediaLibrary", name: "MediaLibrary", label:"Media Library", component: "", pages: [
            {path:"/", name: "Home", label: "Home", category: "test", component: HomeComponent},
            {path:"/crisis-center", name: "CrisisCenter", label: "CrisisCenter", category: "test", component: CrisisListComponent},
            {path:"/heroes",        name: "Heroes", label: "Heroes", component: HeroListComponent},
            {path:"/movies/...",        name: "Movies", label: "Movies",       component: MoviesComponent}
        ]},
        {path: "", name: "test", label: "test", component: "", pages: [
            {path: "/crisis-center", name: "CrisisCenter", label: "CrisisCenter", component: CrisisListComponent}
        ]},
        {path:"/heroes", name: "Heroes", label: "Heroes", pages: [], component: HeroListComponent}
    ];
    routerRoutes = [
        {path:"/", name: "Home", component: HomeComponent, defaultRoute: true}
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
            if (this.routes[i].pages.length == 0) {
                this.addInitialRoute(this.routes[i].path, this.routes[i].name, this.routes[i].component)
            // dropdown
            }else {
                for (var x = 0; x < this.routes[i].pages.length; x++) {
                    this.addInitialRoute(this.routes[i].path + this.routes[i].pages[x].path,
                        this.generateSubentryRoutename(this.routes[i].name, this.routes[i].pages[x].name),
                        this.routes[i].pages[x].component)
                }
            }
        }
    }

    /*
    Add a Route to the initial routes
     */
    addInitialRoute(path, name, component) {
        var routeObj = {
            path: path,
            name: this.firstToUpperCase(name),
            component: component,
            defaultRoute: false
        }
        if (!this.doesPathExist(routeObj.path))
            this.routerRoutes.push(routeObj)
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
    firstToUpperCase( str ) {
        return str.substr(0, 1).toUpperCase() + str.substr(1);
    }

    /*
    Checks which route is active
     */
    isSubrouteActive(CategoryName, pages) {
        for (var i = 0; i < pages.length; i++) {
            var name: any = this.generateSubentryRoutename(CategoryName, [pages[i].name]);
            if (this.router.isRouteActive(this.router.generate([name]))){
                return true;
            }
        }
        return false;
    }
}