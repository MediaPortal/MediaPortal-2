import {VideoAspect, SeriesAspect, MovieAspect, MediaAspect} from "../../../common/lib/MediaLibrary/MediaLibrary";
import {Injectable} from "angular2/core";

export class MovieObject {
    // item is the json object coming from the web api
    static CreateObj(item): MovieObjInterface {
        var videoAspect = VideoAspect.GetAttributes(item);
        var movieAspect = MovieAspect.GetAttributes(item);
        var mediaAspect = MediaAspect.GetAttributes(item);
        var output : MovieObjInterface = {
            ID: item.MediaItemId,
            IMDBID: movieAspect.IMDBID,
            Title: movieAspect.MovieName,
            Watched: (mediaAspect.PlayCount > 0),
            Runtime: movieAspect.Runtime,
            ReleaseDate: mediaAspect.RecordingTime.split("-")[0],
            Language: "",
            Certification: movieAspect.Certification,
            Summary: videoAspect.StoryPlot,
            Tagline: movieAspect.Tagline,
            TotalRating: Math.round(movieAspect.TotalRating * 100)/100,
            Directors: videoAspect.Directors,
            Writers: videoAspect.Writers,
            Actors: videoAspect.Actors,
            Genres: videoAspect.Genres
        }
        return output;
    }
}

export interface MovieObjInterface {
    ID: string,
    IMDBID: string,
    Title: string,
    Watched: boolean,
    Runtime: number,
    ReleaseDate: string,
    Language: string,
    Certification: string,
    Summary: string,
    Tagline: string,
    TotalRating: number,
    Directors: string[],
    Writers: string[],
    Actors: string[],
    Genres: string[]
}