import {Injectable, EventEmitter} from 'angular2/core';
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from 'angular2/http';

@Injectable()
export class MediaLibrary {
    BASE_URL: string = 'http://localhost:5555';

    constructor(private http: Http) {
    }

    public Search(necessaryMiaIds?: string[], optionalMiaIds?: string[], sortInformationStrings = null, offset = null, limit = null) {
        return this.http.request(new Request({
            method: RequestMethod.Get,
            url: this.BASE_URL + '/api/v1/MediaLibrary/MediaItems/?necessaryMiaIds='+necessaryMiaIds.join(', ')
        }));
    }

    public GetMediaItem(id: string, filterOnlyOnline: boolean = false) {
        return this.http.request(new Request({
            method: RequestMethod.Get,
            url: this.BASE_URL + '/api/v1/MediaLibrary/MediaItems/' + id + '?filterOnlyOnline=' + filterOnlyOnline
        }));
    }

    public GetAspect(item, aspect) {
        for (var i = 0; i < item.Aspects.length; i++) {
            if (item.Aspects[i].MediaItemAspectId.toUpperCase() == aspect.toUpperCase()) {
                return item.Aspects[i];
            }
        }
    }
}

export class SeriesAspect {
    static ASPECT_ID: string =  '287A2809-D38D-4F98-B613-E9C09904392D';

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
    static ASPECT_ID: string =  '2AD64410-5BA3-4163-AF03-F8CBBD0EC252';

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
    static ASPECT_ID: string =  'FEA2DA04-1FDC-4836-B669-F3CA73ADF120';

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
    static ASPECT_ID: string =  '29146287-00C3-417B-AC10-BED1A84DB1A9';

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
    static AudioAspect =            '493F2B3B-8025-4DB1-80DC-C3CD39683C9F';
    static DirectoryAspect =        '1EEEF2D0-D52F-40f7-A12A-9908C2DAED1A';
    static ImageAspect =            '2E6C3C20-0B0B-4EE3-8A0C-550C6791EAD0';
    static ImporterAspect =         'CC0163FE-55A5-426c-A29C-F1D64AF7E683';
    static MediaAspect =            '29146287-00C3-417B-AC10-BED1A84DB1A9';
    static MovieAspect =            '2AD64410-5BA3-4163-AF03-F8CBBD0EC252';
    static ProviderResourceAspect = '0A296ACD-F95B-4a28-90A2-E4FD2A4CC4ED';
    static SeriesAspect =           '287A2809-D38D-4F98-B613-E9C09904392D';
    static ThumbnailLargeAspect =   '1FDA5774-9AC5-4873-926C-E84E3C36A966';
    static VideoAspect =            'FEA2DA04-1FDC-4836-B669-F3CA73ADF120';
}