import {bootstrap}    from "angular2/platform/browser"
import {provide} from "angular2/core";
import { ROUTER_PROVIDERS, APP_BASE_HREF, LocationStrategy, HashLocationStrategy } from "angular2/router";
import {AppComponent} from "./app.component"
import {HTTP_PROVIDERS} from "angular2/http";
import {TranslateService} from "ng2-translate/ng2-translate";
import {MediaLibrary} from "./common/lib/MediaLibrary/MediaLibrary";
import {ConfigurationService} from "./common/lib/ConfigurationService/ConfigurationService";

bootstrap(AppComponent, [
    ROUTER_PROVIDERS,
    HTTP_PROVIDERS,
    provide(APP_BASE_HREF, {useValue: "/"}),
    provide(LocationStrategy, {useClass: HashLocationStrategy}),
    TranslateService,
    ConfigurationService,    // starts the ConfigurationService to make the MP2Web Config globally available
    MediaLibrary           // starts our MediaLibrary Service
]);