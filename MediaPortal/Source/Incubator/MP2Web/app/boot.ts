import {bootstrap}    from 'angular2/platform/browser'
import {provide} from 'angular2/core';
import { ROUTER_PROVIDERS, APP_BASE_HREF, LocationStrategy, HashLocationStrategy } from 'angular2/router';
import {AppComponent} from './app.component'

import {CrisisListComponent}   from './crisis-list.component';
import {HeroListComponent}     from './hero-list.component';

bootstrap(AppComponent, [
    ROUTER_PROVIDERS,
    provide(APP_BASE_HREF, {useValue: '/'}),
    provide(LocationStrategy, {useClass: HashLocationStrategy})
]);