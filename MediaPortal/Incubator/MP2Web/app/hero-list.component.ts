import {Component} from 'angular2/core';
import {infiniteScroll} from './common/lib/infinite-scroll';

@Component({
    templateUrl: "app/hero.html",
    directives: [infiniteScroll]
})
export class HeroListComponent {
    content: string = "";
    bussy: boolean = false;

    loadData(event: any) {
        console.log("LOAD DATA");
        this.bussy = true;
        //setTimeout("this.addData()", 2000);
        this.addData();
        this.bussy = false;
        console.log("LOAD DATA");
    }

    addData() {
        this.content += "<h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2><h2>MoreData</h2>";
    }
}