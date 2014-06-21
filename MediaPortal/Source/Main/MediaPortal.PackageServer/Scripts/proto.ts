/// <reference path="typings/jquery/jquery.d.ts"/>
/// <reference path="typings/moment/moment.d.ts"/>
declare var $: JQueryStatic;
declare var dust: any;

module MP2 {
  //"use strict";

  export class PackageFilter {
    packageType: string;
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

    updateDetails(packageId: number): void {
      var url = '/packages/' + packageId + '/details';
      var templateName = 'package-details';
      var domTargetElement = '#package-details';

      var self = this;
      this.net.get(url).done((data: any) => {
        self.renderer.render(templateName, data).done((html) => {
          $(domTargetElement).html(html);
        });                  
      });
    }

    updateList(): void {
      var url = '/packages/list';
      var templateName = 'package-list';
      var domTargetElement = '#package-list';

      var self = this;
      this.net.post(url, this.filter).done((data: any) => {
        self.feed = data;
        // render list
        self.renderer.render(templateName, {packages: data}).done((html) => {
          console.log("html: " + html);
          $(domTargetElement).html(html);
          // parse and render dates
          $('.package .released').each((index, domElement) => {
            var jqElement = $(domElement);
            var date = moment.utc(jqElement.data('value'));
            jqElement.find('.value').html(date.format('L'));
          });
          // click handler
          $(".package").click(event => {
            var jqElement = $(event.currentTarget);
            // make sure only the clicked item is selected
            $('.package').removeClass('selected');
            jqElement.addClass('selected');
            // update details panel
            var packageId = jqElement.data('package-id');
            self.updateDetails(packageId);
            // disable any other handling
            event.preventDefault();
            return false;
          });
        });
        // preselect first item to render details panel
        if (data != null && data.length > 0) {
          var id = data[0].id;
          self.updateDetails(id);
        }
      });
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
        this.ui.updateList();
      });
    }
  }
} 

var app = new MP2.App();
$(document).ready(() => {  
  app.run();
});
