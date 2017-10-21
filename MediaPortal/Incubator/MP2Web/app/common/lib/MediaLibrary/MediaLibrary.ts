import {Injectable, EventEmitter} from "angular2/core";
import {HTTP_PROVIDERS, Http, Request, Response, RequestMethod} from "angular2/http";
import {Observable} from "rxjs/Observable";
import "rxjs/add/operator/catch";
import "rxjs/add/observable/throw";
import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {MessageService} from "../../../common/lib/MessageService/MessageService";
import {MessageType} from "../../../common/lib/MessageService/IMessageType";

@Injectable()
export class MediaLibrary {
    BASE_URL: string;

    constructor(private http: Http, private configurationService: ConfigurationService, private messageService: MessageService) {
        this.BASE_URL = configurationService.config.WebApiUrl;
    }

    /*
    Construtor class for all http requests. Saves code for the error handling
    */
    private newHttp(request: Request, errorTitle: string = "Http Error in TvService"): Observable<Response> {
      return this.http.request(request).catch(err => this.onHttpError(errorTitle, err));
    }

    public Search(necessaryMiaIds?: string[], optionalMiaIds?: string[], sortInformationStrings = null, offset = null, limit = null) {
        console.log("MediaLibrary: BaseUrl: " + this.BASE_URL);
        var url: string = this.BASE_URL + "/api/v1/MediaLibrary/MediaItems/?necessaryMiaIds="+necessaryMiaIds.join(", ");

        if (offset != null) {
            url += "&offset="+offset;
        }

        if (limit != null) {
            url += "&limit="+limit;
        }

        return this.newHttp(new Request({
          method: RequestMethod.Get,
          url: url
        }), "Failed to search for MediaItem");
    }

    public GetMediaItem(id: string, filterOnlyOnline: boolean = false) {
        return this.newHttp(new Request({
          method: RequestMethod.Get,
          url: this.BASE_URL + "/api/v1/MediaLibrary/MediaItems/" + id + "?filterOnlyOnline=" + filterOnlyOnline
        }), "Failed to retrieve MediaItem");
    }

    public GetAspect(item, aspect) {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == aspect.toUpperCase()) {
                return item.Aspects[i];
            }
        }
    }

    /*
    Error handling
    - send a notification to the user
    - log to the console
    - pass the error to the calling Component
    */
    private onHttpError(title: string, err: Response) {
      this.messageService.addNotificationMessage(title, MessageType.Error, "Status: " + err.status + " " + err.statusText);
      console.error(title);
      console.error(err.url);
      console.error(err);
      return Observable.throw(err); // pass the error to the calling Component e.g. the EPG component
    }
}

export class SeriesAspect {
    static ASPECT_ID: string =  "14169484-C425-4294-9693-6902211039CF";

    static GetAttributes(item): SeriesAspectAttributes {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == this.ASPECT_ID.toUpperCase()) {
                return item.Aspects[i].Attributes;
            }
        }
    }
}

interface SeriesAspectAttributes {
    TVDBID: string,
    IMDBID: string,
    SeriesName: string,
    Season: number,
    SeriesSeasonName: string,
    Episode: number,
    DvdEpisode: number,
    EpisodeName: string,
    FirstAired: string,
    TotalRating: number,
    RatingCount: number
}

export class MovieAspect {
    static ASPECT_ID: string =  "C5C21762-FA8A-4C51-8C5B-6B0BF7FB162A";

    static GetAttributes(item): MovieAspectAttributes {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == this.ASPECT_ID.toUpperCase()) {
                return item.Aspects[i].Attributes;
            }
        }
    }
}

export interface MovieAspectAttributes {
    MovieName: string,
    OrigName: string,
    TMDBID: number,
    IMDBID: string,
    CollectionName: string,
    CollectionID: number,
    Runtime: number,
    Certification: string,
    Tagline: string,
    Popularity: number,
    Budget: number,
    Revenue: number,
    Score: number,
    TotalRating: number,
    RatingCount: number
}

export class VideoAspect {
    static ASPECT_ID: string =  "55D6A91B-8867-4A8D-BED3-9CB7F3AECD24";

    static GetAttributes(item): VideoAspectAttributes {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == this.ASPECT_ID.toUpperCase()) {
                return item.Aspects[i].Attributes;
            }
        }
    }
}

export interface VideoAspectAttributes {
    Genres: string[],
    Duration: number,
    AudioStreamCount: number,
    AudioEncoding: string,
    AudioBitRate: number,
    AudioLanguages: string[],
    VideoEncoding: string,
    VideoBitRate: number,
    Width: number,
    Height: number,
    AspectRatio: number,
    FPS: number,
    Actors: string[],
    Directors: string[],
    Writers: string[],
    IsDVD: boolean,
    StoryPlot: string
}


export class MediaAspect {
    static ASPECT_ID: string =  "B91FCB5E-E424-43BE-9577-EF32896067D9";

    static GetAttributes(item): MediaAspectAttributes {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == this.ASPECT_ID.toUpperCase()) {
                return item.Aspects[i].Attributes;
            }
        }
    }
}

export interface MediaAspectAttributes {
    Title: string,
    MimeType: string,
    Size: number,
    RecordingTime: string,
    Rating: number,
    Comment: string,
    PlayCount: number,
    LastPlayed: string
}

export class Aspects {
    static AudioAspect =            "739AC022-2CF5-4921-B4EF-108BA28C62E5";
    static DirectoryAspect =        "1CECB026-2204-4432-8408-706414C44DBA";
    static ImageAspect =            "A3A42A4C-3365-4A85-AE3E-0438D67FF52F";
    static ImporterAspect =         "A531385E-771B-48B3-8CE0-EE0611A84A17";
    static MediaAspect =            "B91FCB5E-E424-43BE-9577-EF32896067D9";
    static MovieAspect =            "C5C21762-FA8A-4C51-8C5B-6B0BF7FB162A";
    static ProviderResourceAspect = "B05EE7E4-087E-4958-B05B-E73D5B1DAACA";
    static SeriesAspect =           "14169484-C425-4294-9693-6902211039CF";
    static ThumbnailLargeAspect =   "2E492453-269A-49EF-B3F1-FD71FE13FAB9";
    static VideoAspect =            "55D6A91B-8867-4A8D-BED3-9CB7F3AECD24";
}