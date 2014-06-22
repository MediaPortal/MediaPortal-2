/// <reference path="typings/jquery/jquery.d.ts"/>
/// <reference path="typings/moment/moment.d.ts"/>
/// <reference path="collections.ts" />
declare var $: JQueryStatic;
declare var dust: any;

module MP2 {
  //"use strict";

  export class PackageFilter {
    packageType: string;
    categoryTags: collections.Set<string> = new collections.Set<string>();
    partialPackageName: string;
    searchDescriptions: boolean;
    partialAuthor: string;

    toJson(): string {
      return JSON.stringify({
        packageType: this.packageType,
        categoryTags: this.categoryTags.toArray(),
        partialPackageName: this.partialPackageName,
        searchDescriptions: this.searchDescriptions,
        partialAuthor: this.partialAuthor
      });
    }
  }

  export class Template {
    net: NetworkManager = new NetworkManager();
    compiled: any;

    constructor(public name: string, public resourceUrl: string) {
    }

    prepare(): JQueryPromise<boolean> {
      var self = this;
      var templateReadyPromise = $.Deferred((d) => {
        this.net.get(self.resourceUrl).done((data: string) => {
          if (data != null) {
            self.compiled = dust.compile(data, self.name);
            dust.loadSource(self.compiled);
            d.resolve(true);
          } else {
            d.resolve(false);
          }
        });
      }).promise();
      return templateReadyPromise;
    }

    render(data: any): JQueryPromise<string> {
      var self = this;
      var htmlReadyPromise = $.Deferred((d) => {
        dust.render(self.name, data, (err, out) => {
          if (err)
            console.log('dust template error: ' + err);
          d.resolve(out);
        });
      }).promise();
      return htmlReadyPromise;
    }

    static registerHelpers() {
      //dust.debugLevel = 'DEBUG';

      // date formatting helper
      dust.helpers.moment = (chunk, ctx, bodies, params) => {
        // get parameter values (the date to format and an optional format string)
        var date = dust.helpers.tap(params.date, chunk, ctx),
          format = dust.helpers.tap(params.format, chunk, ctx) || "L";
        // use moment.js to emit a formatted date string
        var utcMoment = moment.utc(date);
        return chunk.write(utcMoment.format(format));
      }      
    }
  }

  export class NetworkManager {
    get<T>(url): JQueryPromise<T> {
      var dataReadyPromise = $.Deferred((d) => {
        $.get(url, (data, status, jqxhr) => {
          d.resolve(status === 'success' ? data : null);
        });
      }).promise();
      return dataReadyPromise;
    }

    post<T>(url, args): JQueryPromise<T> {
      var dataReadyPromise = $.Deferred((d) => {
        $.post(url, args, (data, status, jqxhr) => {
          d.resolve(status === 'success' ? data : null);
        });
      }).promise();
      return dataReadyPromise;
    }
  }

  export class Renderer {
    templates: Template[] = [
      new Template('package-filter', '/content/dust/package-filter.dust'),
      new Template('package-list', '/content/dust/package-list.dust'),
      new Template('package-details', '/content/dust/package-details.dust')
    ];

    initialize(): JQueryPromise<boolean> {
      var self = this;
      var ready = $.Deferred((d) => {
        var prepareTemplatePromises: JQueryPromise<boolean>[] = [];
        for (var i = 0; i < self.templates.length; i++) {
          var template = self.templates[i];
          prepareTemplatePromises.push(template.prepare());
        }
        $.when(prepareTemplatePromises).then(() => {
          Template.registerHelpers();
          //console.log("templates ready");
          d.resolve(true);
        });
      }).promise();
      return ready;
    }

    render(templateName: string, data: any): JQueryPromise<string> {
      var self = this;
      for (var i = 0; i < self.templates.length; i++) {
        var template = self.templates[i];
        if (template.name === templateName) {
          return template.render(data);
        }
      }
      return $.Deferred().resolve("Invalid template name: "+templateName);
    }
  }

  export class ViewManager {
    net: NetworkManager = new NetworkManager();
    renderer: Renderer = new Renderer();
    filter: PackageFilter = new PackageFilter();
    feed: any[];

    initialize(): JQueryPromise<boolean> {
      return this.renderer.initialize();
    }

    show(): void {
      // loading filter causes list to refresh which causes first item to be selected for details view
      this.updateFilter();
    }

    updateFilter(): void {
      var url = '/home/filterOptions';
      var templateName = 'package-filter';
      var domTargetElement = '#package-filter-container';

      var self = this;
      this.net.get(url).done((data: PackageFilter) => {
        //self.filter = data;
        self.renderer.render(templateName, data).done((html) => {
          $(domTargetElement).html(html);
          // update list using filter
          self.updateList();
          // click handler wirings
          $("#package-filter .packageType").click(event => self.uiFilterTypeClick(event));
          $("#package-filter .tag").click(event => self.uiFilterTagClick(event));
          $("#package-filter .searchText").change(event => self.uiFilterSearchTextChange(event));
          $("#package-filter .searchDesc").click(event => self.uiFilterSearchDescClick(event));
          $("#package-filter .authorText").change(event => self.uiFilterAuthorTextChange(event));
        });
      });
    }

    updateList(): void {
      var url = '/packages/list';
      var templateName = 'package-list';
      var domTargetElement = '#package-list-container';

      var self = this;
      this.net.post(url, this.filter.toJson()).done((data: any) => {
        self.feed = data;
        // render list
        self.renderer.render(templateName, { packages: data }).done((html) => {
          //console.log("html: " + html);
          $(domTargetElement).html(html);
          // pre-select first item
          self.updateDetails();
          // click handler wiring
          $(".package").click(event => self.uiPackageListClick(event));
        });
      });
    }

    updateDetails(packageId?: number): void {
      // pre-select first item in package feed if no id was supplied
      if (packageId === undefined || packageId <= 0) {
        // exit if we have no packages in feed to choose from
        if (this.feed == null || this.feed.length == 0)
          return;
        packageId = this.feed[0].id;
      }

      var url = '/packages/' + packageId + '/details';
      var templateName = 'package-details';
      var domTargetElement = '#package-details-container';

      var self = this;
      this.net.get(url).done((data: any) => {
        self.renderer.render(templateName, data).done((html) => {
          $(domTargetElement).html(html);
        });
      });
    }

    uiPackageListClick(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      // make sure only the clicked item is selected
      $('.package').removeClass('selected');
      jqElement.addClass('selected');
      // update details panel
      var packageId = jqElement.data('package-id');
      this.updateDetails(packageId);
      // disable any other handling
      event.preventDefault();
      return false;
    }

    uiFilterTypeClick(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      // make sure only the clicked item is selected
      this.filter.packageType = jqElement.find('input').val();
      this.updateList();
      return true;
    }

    uiFilterTagClick(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      // make sure only the clicked item is selected
      var tag = jqElement.find('span').html();
      if (tag && tag.length > 0)
        if (!this.filter.categoryTags.remove(tag)) {
          this.filter.categoryTags.add(tag);
        }     
      jqElement.toggleClass("selected");
      this.updateList();
      // disable any other handling
      event.preventDefault();
      return false;
    }

    uiFilterSearchDescClick(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      // make sure only the clicked item is selected
      this.filter.searchDescriptions = jqElement.find('input[type=checkbox]').val();
      this.updateList();
      return true;
    }

    uiFilterSearchTextChange(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      this.filter.partialPackageName = jqElement.find('input[type=text]').val();
      this.updateList();
      // disable any other handling
      event.preventDefault();
      return false;
    }

    uiFilterAuthorTextChange(event: JQueryEventObject): boolean {
      var jqElement = $(event.currentTarget);
      this.filter.partialAuthor = jqElement.find('input').val();
      this.updateList();
      // disable any other handling
      event.preventDefault();
      return false;
    }
  }

  export class App {
    ui: ViewManager = new ViewManager();

    run() {
      this.ui.initialize().done((success) => {
        if (success !== true) {
          console.log('App initialization failed, aborting.');
          return;
        }
        this.ui.show();
      });
    }
  }
} 

var app = new MP2.App();
$(document).ready(() => {  
  app.run();
});
