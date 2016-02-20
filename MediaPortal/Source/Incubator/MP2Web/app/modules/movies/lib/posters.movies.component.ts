import {Directive, ElementRef, Input} from "angular2/core";

import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";

@Directive({
  selector: '[movie-poster]',
  inputs: ["id: movie-poster", "width: movie-poster-width", "height: movie-poster-height"],
})
export class MoviePosterComponent {
  BASE_URL: string;

  id: number;
  width: number = 200;
  height: number = 300;
  element: any;

  constructor(element: ElementRef, private configurationService: ConfigurationService) {
    this.BASE_URL = configurationService.config.WebApiUrl;

    this.element = element;
  }

  ngOnInit() {
    this.element.nativeElement.src = this.BASE_URL + "/api/v1/MediaLibrary/MediaItems/" + this.id + "/FanArt/Movie/Poster/0?maxWidth=" + this.width + "&maxHeight=" + this.height;
  }
}