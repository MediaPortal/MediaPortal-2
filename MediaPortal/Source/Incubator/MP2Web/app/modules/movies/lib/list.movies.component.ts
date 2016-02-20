import {Component, View} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {ROUTER_DIRECTIVES, Location, RouterLink, Router} from "angular2/router";
import {TranslateService, TranslatePipe} from "ng2-translate/ng2-translate";
import {RangePipe} from "../../../pipes/range-pipe";
import {MediaLibrary, Aspects, SeriesAspect, MovieAspect, MovieAspectAttributes, MediaAspect, MediaAspectAttributes} from "../../../common/lib/MediaLibrary/MediaLibrary";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {MovieObject, MovieObjInterface} from "./common.movies";
import {MoviePosterComponent} from "./posters.movies.component";
import {infiniteScroll} from "../../../common/lib/infinite-scroll";
import {StarratingComponent} from "../../../common/Components/Starrating/lib/starrating";


@Component({
    templateUrl: "app/modules/movies/list.movies.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, infiniteScroll, StarratingComponent, MoviePosterComponent],
    pipes: [RangePipe, TranslatePipe]
})
export class ListMoviesComponent {
    movieCoversPerRow: any;
    movieList: MovieObjInterface[] = [];
    moviesForLoop: any;
    moviesLoadingBussy: boolean = false;
    moviesPerQuery: number;
    mlOffset = 0;

    constructor(public mediaLibrary: MediaLibrary, public router: Router, private configurationService: ConfigurationService) {
        /*
        Configure
         */
        this.movieCoversPerRow = configurationService.config.MoviesPerRow;
        this.moviesPerQuery = configurationService.config.MoviesPerQuery;

        /*
        Load first set of Movies
         */
        mediaLibrary.Search([MovieAspect.ASPECT_ID], null, null, null, this.moviesPerQuery).subscribe(res => {
            var items = res.json();
            for (var i = 0; i < items.length; i++) {
                var item : MovieObjInterface = MovieObject.CreateObj(items[i]);
                this.movieList.push(item)
            }
        });
        this.mlOffset = this.moviesPerQuery;

        this.moviesForLoop = [];
    }

    loadMoreMovies() {
        // inidcate that we are bussy loading data -> we don't want to request new data while we are still waiting for data
        this.moviesLoadingBussy = true;
        console.log("ListMoviesComponent: Loading more...");
        this.mediaLibrary.Search([MovieAspect.ASPECT_ID], null, null, this.mlOffset, this.moviesPerQuery).subscribe(res => {
            var items = res.json();
            for (var i = 0; i < items.length; i++) {
                var item : MovieObjInterface = MovieObject.CreateObj(items[i]);
                this.movieList.push(item)
            }
            // we aren't bussy anymore
            this.moviesLoadingBussy = false;
        });
        this.mlOffset += this.moviesPerQuery;
    }

    errorLoadingCover = function(event) {
        event.target.src = "images/noCover.png";
    }

    go(linkParams: any[]) {
        //var link = this.router.generate(linkParams);
        this.router.navigate(linkParams);
    }
}
