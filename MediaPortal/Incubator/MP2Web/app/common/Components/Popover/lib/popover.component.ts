import {Component, View, EventEmitter, OnChanges, SimpleChange, OnInit, ElementRef, Inject} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES, NgClass, NgStyle} from "angular2/common";

import {positionService} from "ng2-bootstrap/components/position";
import {TooltipOptions} from "ng2-bootstrap/components/tooltip/tooltip-options.class";

/*
 This component provides an easy way to add a side menu to subpages.
 I made the decision to let the calling component handle the routes. This gives you more freedom from a
 design point of view (where you want to position the router outlet).
 */
@Component({
  templateUrl: "app/common/Components/Popover/popover.html",
  selector: "popover-container",
  //inputs: ["title: title", "titleClass: title-class", "menuItems: menu-items", "routesConfigured: routes-configured"],
  //events: ["routesConfiguredEvent: routes-configured"],
  directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, NgClass, NgStyle]
})
export class popoverComponent {
  private classMap:any;
  private positionMap:any;
  private top:string;
  private left:string;
  private display:string;
  private content:string;
  private placement:string;
  private appendToBody:boolean;

  constructor(public element:ElementRef, @Inject(TooltipOptions) options:TooltipOptions) {
    console.log("popoverComponent: constructor");
    Object.assign(this, options);
    this.classMap = {'in': false};
    this.classMap[options.placement] = true;
  }

  ngOnInit() {
    //this.position(this.element);
    console.log("Append to pody: " + this.appendToBody);
  }

  public position(hostEl:ElementRef) {
    console.log("Position Append to pody: " + this.appendToBody);
    this.display = 'block';
    this.top = '0px';
    this.left = '0px';
    let p = positionService
      .positionElements(hostEl.nativeElement,
        this.element.nativeElement.children[0],
        this.placement, this.appendToBody);
    this.top = p.top + 'px';
    this.left = p.left + 'px';
    this.classMap['in'] = true;
  }

}