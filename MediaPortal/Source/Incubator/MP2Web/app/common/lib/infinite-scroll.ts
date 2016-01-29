import { Directive, EventEmitter, ElementRef } from "angular2/core";
import {window} from "angular2/src/facade/browser";

@Directive({
    selector: "[infinite-scroll]",
    inputs: ["infiniteScrollDisabled: infinite-scroll-disabled", "infiniteScrollDistance: infinite-scroll-distance"],
    events: ["infiniteScrollAction: infinite-scroll-action"],
    host: {
        "(window:scroll)": "onScroll($event)"
    }
})
export class infiniteScroll {
    infiniteScroll;
    infiniteScrollDistance: number = 0;
    infiniteScrollDisabled: boolean;
    infiniteScrollAction: EventEmitter<any> = new EventEmitter();

    windowElement;
    scrollEnabled = null;
    container = null;
    useDocumentBottom = false;
    checkInterval: any;

    constructor(public element:ElementRef) {
        this.windowElement = window;
        this.container = window;

        // if there are not enough items to actually scroll, load more!
        this.checkInterval = setInterval(() => {
            this.handler(this.element.nativeElement);
        }, 1000);
    }

    height(elem) {
        if (isNaN(elem.offsetHeight)) {
            return elem.document.documentElement.clientHeight;
        }else {
            return elem.offsetHeight;
        }
    }

    offsetTop (elem) {
        return elem.getBoundingClientRect().top + this.pageYOffset(elem);
    }

    pageYOffset (elem) {
        var elemTmp;
        if (elem[0] == undefined) {
            elemTmp = elem;
        }else {
            elemTmp = elem[0];
        }
        //elem = elem[0] || elem;

        if (isNaN(window.pageYOffset)) {
            return elemTmp.document.documentElement.scrollTop;
        }else {
            return elemTmp.ownerDocument.defaultView.pageYOffset;
        }
    }

    handler (elem) {
        console.log("Status: " + this.infiniteScrollDisabled);
        if (this.container == this.windowElement) {
            var containerBottom = this.height(this.container) + this.pageYOffset(this.container.document.documentElement);
            var elementBottom = this.offsetTop(elem) + this.height(elem)
        }else{
            var containerBottom = this.height(this.container);
            var containerTopOffset = 0;
            if (this.offsetTop(this.container) != undefined) {
                containerTopOffset = this.offsetTop(this.container)
            }
            var elementBottom = this.offsetTop(elem) - containerTopOffset + this.height(elem);
        }

        if(this.useDocumentBottom) {
            elementBottom = this.height((elem[0].ownerDocument || elem[0].document).documentElement)
        }

        var remaining = elementBottom - containerBottom;
        var shouldScroll = remaining <= this.height(this.container) * this.infiniteScrollDistance + 1;
        console.log("infiniteScroll: remaining value:" + remaining);
        console.log("infiniteScroll: shouldScroll value:" + shouldScroll);

        if (shouldScroll && !this.infiniteScrollDisabled) {
            console.log("infiniteScroll: Send Event!! ");
            this.infiniteScrollAction.emit(null);
        }else {
            console.log("infiniteScroll: Clear interval!");
            clearInterval(this.checkInterval);
        }

    }

    onScroll(event) {
        this.handler(this.element.nativeElement);
    }
}