import {Component, View} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {Router} from "angular2/router";

import {MediaLibrary, Aspects, SeriesAspect, MovieAspect, MovieAspectAttributes, MediaAspect, MediaAspectAttributes} from "../../../common/lib/MediaLibrary/MediaLibrary";
import {MovieObjInterface, MovieObject} from "../../movies/lib/common.movies";
import {StarratingComponent} from "../../../common/Components/Starrating/lib/starrating";
import {RouteConfig} from "angular2/router";

@Component({
    templateUrl: "app/modules/home/home.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, StarratingComponent],
})
export class HomeComponent {
    sortInformationString: string = Aspects.ImporterAspect+".DateAdded.Descending"; // [MediaItemAspectId].[AttributeName].[SortDirection]
    limit: number = 5;
    latestMovies: MovieObjInterface[] = [];

    constructor(public mediaLibrary: MediaLibrary, private router: Router) {
        this.getLatestMedia();
    }

    getLatestMedia() {
        this.mediaLibrary.Search([MovieAspect.ASPECT_ID], null, this.sortInformationString, null, this.limit).subscribe(res => {
            var items = res.json();
            for (var i = 0; i < items.length; i++) {
                var item : MovieObjInterface = MovieObject.CreateObj(items[i]);
                this.latestMovies.push(item)
            }
        });
    }

    getCovers = function(id) {
        return "http://localhost:5555/api/v1/MediaLibrary/MediaItems/" + id + "/FanArt/Movie/Poster/0?maxWidth=200&maxHeight=300";
    };

    errorLoadingCover = function(event) {
        event.target.src = "images/noCover.png";
    };

    go(linkParams: any[]) {
        this.router.parent.navigate(linkParams);
    }
}