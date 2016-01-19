import {Component, View} from 'angular2/core';
import {COMMON_DIRECTIVES, NgIf, NgFor} from 'angular2/common';

@Component({
    templateUrl: 'app/modules/home/home.html',
    directives: [COMMON_DIRECTIVES, NgIf, NgFor],
})
export class HomeComponent {


    constructor() {

    }
}