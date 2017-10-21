import {
  Directive,
  OnInit, Input, HostListener,
  ElementRef, EventEmitter,
  DynamicComponentLoader, ComponentRef,
  Provider,
  Injectable, forwardRef, ResolvedBinding, Injector
} from "angular2/core";
import {NgClass, NgStyle} from 'angular2/common';

import {TooltipOptions} from "ng2-bootstrap/components/tooltip/tooltip-options.class";
import {popoverComponent} from "./popover.component";

import {IAttribute} from "ng2-bootstrap/components/common";

@Directive({selector: "[popover]"})
export class Popover implements OnInit {
  @Input('tooltip') public content:string;
  @Input('tooltipPlacement') public placement:string = 'top';
  @Input('tooltipIsOpen') public isOpen:boolean;
  @Input('tooltipEnable') public enable:boolean;
  @Input('tooltipAppendToBody') public appendToBody:boolean;

  private visible:boolean = false;
  private tooltip:Promise<ComponentRef>;

  constructor(public element:ElementRef,
              public loader:DynamicComponentLoader) {
    console.log("Popover init!")
  }

  ngOnInit() {
  }

  // todo: filter triggers
  // params: event, target
  @HostListener('focusin', ['$event', '$target'])
  @HostListener('mouseenter', ['$event', '$target'])
  show() {
    console.log("Show!")
    if (this.visible) {
      return;
    }
    this.visible = true;

    let options = new TooltipOptions({
      content: this.content,
      placement: this.placement
    });

    let binding = Injector.resolve([
      new Provider(TooltipOptions, {useValue: options})
    ]);

    this.tooltip = this.loader
      .loadNextToLocation(popoverComponent, this.element, binding)
      .then((componentRef:ComponentRef) => {
        componentRef.instance.position(this.element);
        return componentRef;
      });
  }

  // params event, target
  @HostListener('focusout', ['$event', '$target'])
  @HostListener('mouseleave', ['$event', '$target'])
  hide() {
    if (!this.visible) {
      return;
    }
    this.visible = false;
    this.tooltip.then((componentRef:ComponentRef) => {
      componentRef.dispose();
      return componentRef;
    });
  }
}
