import {Component, View, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {EpgComponent} from "./../../../common/Components/EPG/lib/epg.component";


@Component({
    templateUrl: "app/modules/tv/epg.tv.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, EpgComponent]
})
export class EpgTvComponent {

    constructor() {

    }
}

