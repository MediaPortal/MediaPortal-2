import {Component, View} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {ROUTER_DIRECTIVES, Location, RouterLink, Router} from "angular2/router";
import {TranslateService, TranslatePipe} from "ng2-translate/ng2-translate";
import {RangePipe} from "../../../pipes/range-pipe";
import {MediaLibrary, Aspects, SeriesAspect, MovieAspect, MovieAspectAttributes, MediaAspect, MediaAspectAttributes} from "../../../MediaLibrary/MediaLibrary";
import {MovieObject, MovieObjInterface, Starrating} from "./common.movies";
import {infiniteScroll} from '../../../common/lib/infinite-scroll';


var movieCoversPerRow = 5;
var moviesProQuery = 6;

@Component({
    templateUrl: "app/modules/movies/list.movies.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, infiniteScroll],
    providers: [Starrating],
    pipes: [RangePipe, TranslatePipe]
})
export class ListMoviesComponent {
    movieCoversPerRow: any;
    movieList: MovieObjInterface[] = [];
    moviesForLoop: any;
    moviesLoadingBussy: boolean = false;
    mlOffset = 0;

    constructor(public mediaLibrary: MediaLibrary, public location: Location, public router: Router, public starrating: Starrating) {
        mediaLibrary.Search([MovieAspect.ASPECT_ID], null, null, null, moviesProQuery).subscribe(res => {
            var items = res.json();
            for (var i = 0; i < items.length; i++) {
                var item : MovieObjInterface = MovieObject.CreateObj(items[i]);
                this.movieList.push(item)
            }
        });
        this.mlOffset = moviesProQuery;

        this.movieCoversPerRow = movieCoversPerRow;
        this.moviesForLoop = [];
    }

    loadMoreMovies() {
        // inidcate that we are bussy loading data -> we don't want to request new data while we are still waiting for data
        this.moviesLoadingBussy = true;
        console.log("Loading more...");
        this.mediaLibrary.Search([MovieAspect.ASPECT_ID], null, null, this.mlOffset, moviesProQuery).subscribe(res => {
            var items = res.json();
            for (var i = 0; i < items.length; i++) {
                var item : MovieObjInterface = MovieObject.CreateObj(items[i]);
                this.movieList.push(item)
            }
            // we aren't bussy anymore
            this.moviesLoadingBussy = false;
        });
        this.mlOffset += moviesProQuery;
    }

    getCovers = function(id) {
        return "http://localhost:5555/api/v1/MediaLibrary/MediaItems/" + id + "/FanArt/Movie/Poster/0?maxWidth=200&maxHeight=300";
        //return /*GetArtworkResizedUrl + */"?mediatype=Movie&id=" + id + "&artworktype=Poster&maxWidth=200&maxHeight=300&borders=transparent"
    }

    go(linkParams: any[]) {
        //var link = this.router.generate(linkParams);
        this.router.navigate(linkParams);
    }
}
