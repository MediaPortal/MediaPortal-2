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
    static ASPECT_ID: string =  "287A2809-D38D-4F98-B613-E9C09904392D";

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
    static ASPECT_ID: string =  "2AD64410-5BA3-4163-AF03-F8CBBD0EC252";

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
    static ASPECT_ID: string =  "FEA2DA04-1FDC-4836-B669-F3CA73ADF120";

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
    static ASPECT_ID: string =  "29146287-00C3-417B-AC10-BED1A84DB1A9";

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
    static AudioAspect =            "493F2B3B-8025-4DB1-80DC-C3CD39683C9F";
    static DirectoryAspect =        "1EEEF2D0-D52F-40f7-A12A-9908C2DAED1A";
    static ImageAspect =            "2E6C3C20-0B0B-4EE3-8A0C-550C6791EAD0";
    static ImporterAspect =         "CC0163FE-55A5-426c-A29C-F1D64AF7E683";
    static MediaAspect =            "29146287-00C3-417B-AC10-BED1A84DB1A9";
    static MovieAspect =            "2AD64410-5BA3-4163-AF03-F8CBBD0EC252";
    static ProviderResourceAspect = "0A296ACD-F95B-4a28-90A2-E4FD2A4CC4ED";
    static SeriesAspect =           "287A2809-D38D-4F98-B613-E9C09904392D";
    static ThumbnailLargeAspect =   "1FDA5774-9AC5-4873-926C-E84E3C36A966";
    static VideoAspect =            "FEA2DA04-1FDC-4836-B669-F3CA73ADF120";
}