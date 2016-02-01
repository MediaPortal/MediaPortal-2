import {Component, View, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";
import {ROUTER_DIRECTIVES, RouteParams} from "angular2/router";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";
import {TranslateService, TranslatePipe} from "ng2-translate/ng2-translate";
import {MediaLibrary, Aspects, SeriesAspect, MovieAspect, MovieAspectAttributes, MediaAspect, MediaAspectAttributes} from "../../../common/lib/MediaLibrary/MediaLibrary";
import {MovieObject, MovieObjInterface, Starrating} from "./common.movies";


@Component({
    templateUrl: "app/modules/movies/details.movies.html",
    directives: [ROUTER_DIRECTIVES, COMMON_DIRECTIVES, NgIf, NgFor],
    providers: [Starrating],
    pipes: [TranslatePipe]
})
export class DetailsMoviesComponent {
    movieId: string;
    movieObj: MovieObjInterface = <MovieObjInterface>{};
    trailers: any;
    trailerHtml: string = "";

    constructor(mediaLibrary: MediaLibrary, params: RouteParams, private http: Http, private elem: ElementRef, public starrating: Starrating) {
        this.movieId = params.get("id");

        mediaLibrary.GetMediaItem(this.movieId).map(res => res.json()).subscribe(res => {
            this.movieObj = MovieObject.CreateObj(res[0]);
            this.getTrailers(this.movieObj.IMDBID);
        });
    }

    getCovers(id) {
        return "http://localhost:5555/api/v1/MediaLibrary/MediaItems/" + id + "/FanArt/Movie/Poster/0?maxWidth=200&maxHeight=300";
    }

    getTrailers(imdbId) {
        this.http.request(new Request({
            method: RequestMethod.Get,
            url: "http://localhost:8080/api/Trailers/" + imdbId + "?count=4&width=680"
        })).map(res => res.json()).subscribe(res => this.trailers = res);
    }

    onSelectTrailer(value) {
        console.log("ddd" + this.trailers[value].Embed);
        this.trailerHtml = this.trailers[value].Embed;
    }

}

