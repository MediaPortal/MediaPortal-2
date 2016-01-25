import {Component, View} from 'angular2/core';
import {ROUTER_DIRECTIVES, Location, RouteConfig, RouterLink, Router} from 'angular2/router';
import {COMMON_DIRECTIVES, NgIf, NgFor} from 'angular2/common';

import {ListMoviesComponent}     from './list.movies.component';
import {DetailsMoviesComponent}     from './details.movies.component';

/*
Main component for the Movies module
 */
@Component({
    templateUrl: 'app/modules/movies/movies.html',
    directives: [ROUTER_DIRECTIVES, COMMON_DIRECTIVES, NgIf, NgFor]
})
@RouteConfig([
    {path:'/list', name: 'List', component: ListMoviesComponent, useAsDefault: true},
    {path:'/details/:id', name: 'Details', component: DetailsMoviesComponent}
])
export class MoviesComponent {

    constructor(public router: Router) {

    }
}