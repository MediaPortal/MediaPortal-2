/// <reference path="typings/jquery/jquery.d.ts"/>
/// <reference path="typings/moment/moment.d.ts"/>
/// <reference path="collections.ts" />
var MP2;
(function (MP2) {
    //"use strict";
    var PackageFilter = (function () {
        function PackageFilter() {
            this.categoryTags = new collections.Set();
        }
        PackageFilter.prototype.toJson = function () {
            return JSON.stringify({
                packageType: this.packageType,
                categoryTags: this.categoryTags.toArray(),
                partialPackageName: this.partialPackageName,
                searchDescriptions: this.searchDescriptions,
                partialAuthor: this.partialAuthor
            });
        };
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
                    }
                    else {
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
        Template.registerHelpers = function () {
            //dust.debugLevel = 'DEBUG';
            // date formatting helper
            dust.helpers.moment = function (chunk, ctx, bodies, params) {
                // get parameter values (the date to format and an optional format string)
                var date = dust.helpers.tap(params.date, chunk, ctx), format = dust.helpers.tap(params.format, chunk, ctx) || "L";
                // use moment.js to emit a formatted date string
                var utcMoment = moment.utc(date);
                return chunk.write(utcMoment.format(format));
            };
        };
        return Template;
    })();
    MP2.Template = Template;
    var NetworkManager = (function () {
        function NetworkManager() {
            $.ajaxSettings.contentType = "application/json";
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
                new Template('package-filter', '/content/dust/package-filter.dust'),
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
                    Template.registerHelpers();
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
        ViewManager.prototype.show = function () {
            // loading filter causes list to refresh which causes first item to be selected for details view
            this.updateFilter();
        };
        ViewManager.prototype.updateFilter = function () {
            var url = '/home/filterOptions';
            var templateName = 'package-filter';
            var domTargetElement = '#package-filter-container';
            var self = this;
            this.net.get(url).done(function (data) {
                self.renderer.render(templateName, data).done(function (html) {
                    $(domTargetElement).html(html);
                    // update list using filter
                    self.updateList();
                    // click handler wirings
                    $("#package-filter .packageType").click(function (event) { return self.uiFilterTypeClick(event); });
                    $("#package-filter .tag").click(function (event) { return self.uiFilterTagClick(event); });
                    $("#package-filter .searchText").change(function (event) { return self.uiFilterSearchTextChange(event); });
                    $("#package-filter .searchDesc").click(function (event) { return self.uiFilterSearchDescClick(event); });
                    $("#package-filter .authorText").change(function (event) { return self.uiFilterAuthorTextChange(event); });
                });
            });
        };
        ViewManager.prototype.updateList = function () {
            var url = '/packages/list';
            var templateName = 'package-list';
            var domTargetElement = '#package-list-container';
            var self = this;
            this.net.post(url, this.filter.toJson()).done(function (data) {
                self.feed = data;
                // render list
                self.renderer.render(templateName, { packages: data }).done(function (html) {
                    //console.log("html: " + html);
                    $(domTargetElement).html(html);
                    // pre-select first item
                    self.updateDetails();
                    // click handler wiring
                    $(".package").click(function (event) { return self.uiPackageListClick(event); });
                });
            });
        };
        ViewManager.prototype.updateDetails = function (packageId) {
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
            this.net.get(url).done(function (data) {
                self.renderer.render(templateName, data).done(function (html) {
                    $(domTargetElement).html(html);
                });
            });
        };
        ViewManager.prototype.uiPackageListClick = function (event) {
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
        };
        ViewManager.prototype.uiFilterTypeClick = function (event) {
            var jqElement = $(event.currentTarget);
            // make sure only the clicked item is selected
            this.filter.packageType = jqElement.find('input').val();
            this.updateList();
            return true;
        };
        ViewManager.prototype.uiFilterTagClick = function (event) {
            var jqElement = $(event.currentTarget);
            // make sure only the clicked item is selected
            var tag = jqElement.find('span').html();
            if (tag && tag.length > 0) {
                if (!this.filter.categoryTags.remove(tag)) {
                    this.filter.categoryTags.add(tag);
                }
            }
            jqElement.toggleClass("selected");
            this.updateList();
            // disable any other handling
            event.preventDefault();
            return false;
        };
        ViewManager.prototype.uiFilterSearchDescClick = function (event) {
            var jqElement = $(event.currentTarget);
            // make sure only the clicked item is selected
            this.filter.searchDescriptions = jqElement.find('input[type=checkbox]').val();
            this.updateList();
            return true;
        };
        ViewManager.prototype.uiFilterSearchTextChange = function (event) {
            var jqElement = $(event.currentTarget);
            this.filter.partialPackageName = jqElement.find('input[type=text]').val();
            this.updateList();
            // disable any other handling
            event.preventDefault();
            return false;
        };
        ViewManager.prototype.uiFilterAuthorTextChange = function (event) {
            var jqElement = $(event.currentTarget);
            this.filter.partialAuthor = jqElement.find('input').val();
            this.updateList();
            // disable any other handling
            event.preventDefault();
            return false;
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
                _this.ui.show();
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