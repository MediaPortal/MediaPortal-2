/// <reference path="typings/jquery/jquery.d.ts"/>

var MP2;
(function (MP2) {
    //"use strict";
    var PackageFilter = (function () {
        function PackageFilter() {
        }
        return PackageFilter;
    })();
    MP2.PackageFilter = PackageFilter;

    var Template = (function () {
        function Template(name, resourceUrl) {
            this.name = name;
            this.resourceUrl = resourceUrl;
            this.net = new NetworkManager();
        }
        Template.prototype.prepare = function () {
            var _this = this;
            var self = this;
            var templateReadyPromise = $.Deferred(function (d) {
                _this.net.get(self.resourceUrl).done(function (data) {
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
        };

        Template.prototype.render = function (data) {
            var self = this;
            var htmlReadyPromise = $.Deferred(function (d) {
                dust.render(self.name, data, function (err, out) {
                    if (err)
                        console.log('dust template error: ' + err);
                    d.resolve(out);
                });
            }).promise();
            return htmlReadyPromise;
        };
        return Template;
    })();
    MP2.Template = Template;

    var NetworkManager = (function () {
        function NetworkManager() {
        }
        NetworkManager.prototype.get = function (url) {
            var dataReadyPromise = $.Deferred(function (d) {
                $.get(url, function (data, status, jqxhr) {
                    d.resolve(status === 'success' ? data : null);
                });
            }).promise();
            return dataReadyPromise;
        };

        NetworkManager.prototype.post = function (url, args) {
            var dataReadyPromise = $.Deferred(function (d) {
                $.post(url, args, function (data, status, jqxhr) {
                    d.resolve(status === 'success' ? data : null);
                });
            }).promise();
            return dataReadyPromise;
        };
        return NetworkManager;
    })();
    MP2.NetworkManager = NetworkManager;

    var Renderer = (function () {
        function Renderer() {
            this.templates = [
                new Template('package-list', '/content/dust/package-list.dust'),
                new Template('package-details', '/content/dust/package-details.dust')
            ];
        }
        Renderer.prototype.initialize = function () {
            var self = this;
            var ready = $.Deferred(function (d) {
                var prepareTemplatePromises = [];
                for (var i = 0; i < self.templates.length; i++) {
                    var template = self.templates[i];
                    prepareTemplatePromises.push(template.prepare());
                }
                $.when(prepareTemplatePromises).then(function () {
                    //console.log("templates ready");
                    d.resolve(true);
                });
            }).promise();
            return ready;
        };

        Renderer.prototype.render = function (templateName, data) {
            var self = this;
            for (var i = 0; i < self.templates.length; i++) {
                var template = self.templates[i];
                if (template.name === templateName) {
                    return template.render(data);
                }
            }
            return $.Deferred().resolve("Invalid template name: " + templateName);
        };
        return Renderer;
    })();
    MP2.Renderer = Renderer;

    var ViewManager = (function () {
        function ViewManager() {
            this.net = new NetworkManager();
            this.renderer = new Renderer();
            this.filter = new PackageFilter();
        }
        ViewManager.prototype.initialize = function () {
            return this.renderer.initialize();
        };

        ViewManager.prototype.updateDetails = function (packageId) {
            var url = '/packages/' + packageId + '/details';
            var templateName = 'package-details';
            var domTargetElement = '#package-details';

            var self = this;
            this.net.get(url).done(function (data) {
                self.renderer.render(templateName, data).done(function (html) {
                    $(domTargetElement).html(html);
                });
            });
        };

        ViewManager.prototype.updateList = function () {
            var url = '/packages/list';
            var templateName = 'package-list';
            var domTargetElement = '#package-list';

            var self = this;
            this.net.post(url, this.filter).done(function (data) {
                self.feed = data;

                // render list
                self.renderer.render(templateName, { packages: data }).done(function (html) {
                    console.log("html: " + html);
                    $(domTargetElement).html(html);
                });

                // preselect first item to render details panel
                if (data != null && data.length > 0) {
                    var id = data[0].id;
                    self.updateDetails(id);
                }
            });
        };
        return ViewManager;
    })();
    MP2.ViewManager = ViewManager;

    var App = (function () {
        function App() {
            this.ui = new ViewManager();
        }
        App.prototype.run = function () {
            var _this = this;
            this.ui.initialize().done(function (success) {
                if (success !== true) {
                    console.log('App initialization failed, aborting.');
                    return;
                }
                _this.ui.updateList();
            });
        };
        return App;
    })();
    MP2.App = App;
})(MP2 || (MP2 = {}));

var app = new MP2.App();
$(document).ready(function () {
    app.run();
});
//# sourceMappingURL=proto.js.map
