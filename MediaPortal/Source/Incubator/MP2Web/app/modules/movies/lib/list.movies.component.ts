import {Component, View} from 'angular2/core';
import {COMMON_DIRECTIVES, NgIf, NgFor} from 'angular2/common';
import {RangePipe} from "../../../pipes/range-pipe";

import {HeroListComponent}     from '../../../hero-list.component';

var movieCoversPerRow = 5;

@Component({
    templateUrl: 'app/modules/movies/list.movies.html',
    directives: [COMMON_DIRECTIVES, NgIf, NgFor],
    pipes: [RangePipe]
})
export class ListMoviesComponent {
    movieCoversPerRow: any;
    moviesBasic: any;
    moviesForLoop: any;

    constructor() {
        this.movieCoversPerRow = movieCoversPerRow;
        this.moviesBasic = [
            {Title: "test1", Watched: false},
            {Title: "test2", Watched: false},
            {Title: "test3", Watched: false},
            {Title: "test4", Watched: false},
            {Title: "test5", Watched: false},
            {Title: "test6", Watched: false}
        ];
        this.moviesForLoop = [];
    }

    getCovers = function(id) {
        return /*GetArtworkResizedUrl + */"?mediatype=Movie&id=" + id + "&artworktype=Poster&maxWidth=200&maxHeight=300&borders=transparent"
    }
}
