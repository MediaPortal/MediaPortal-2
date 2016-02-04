import {Component, View} from "angular2/core";
import {COMMON_DIRECTIVES, NgIf, NgFor} from "angular2/common";


/*
Main component for the Movies module
 */
@Component({
    templateUrl: "app/modules/settings/home.settings.html",
    directives: [COMMON_DIRECTIVES, NgIf, NgFor]
})
export class HomeSettingsComponent {
    constructor() {

    }
}