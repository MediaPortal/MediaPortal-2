/// <binding ProjectOpened='watch, tsd' />
/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

var gulp = require("gulp");
var del = require("del");
var tsd = require('gulp-tsd');

var config = {
  destination: "wwwroot/",
  debug: {
    pluginappdestination: "../../../Bin/MP2-Server/bin/x86/Debug/Plugins/MP2Web/wwwroot/app/",
    watchfiles: ["app/**/*.+(js|css|html|json|map|ts)"],
    projectwwwrootapp: "wwwroot/app/",
    libraries: [
      "node_modules/es6-shim/es6-shim.min.js",

      "node_modules/systemjs/dist/system-polyfills.js",
      "node_modules/systemjs/dist/system.src.js",

      "node_modules/rxjs/bundles/Rx.js",

      "node_modules/angular2/bundles/angular2-polyfills.js",
      "node_modules/angular2/bundles/angular2.dev.js",
      "node_modules/angular2/bundles/router.dev.js",
      "node_modules/angular2/bundles/http.dev.js",

      "node_modules/ng2-translate/bundles/ng2-translate.js",

      "node_modules/ng2-toastr/bundles/ng2-toastr.js",
      "node_modules/ng2-toastr/bundles/ng2-toastr.min.css",

      "node_modules/moment/moment.js",
      "node_modules/ng2-bootstrap/bundles/ng2-bootstrap.min.js",
      "node_modules/chart.js/Chart.min.js",

      "node_modules/jquery/dist/jquery.js",

      "node_modules/bootstrap/dist/js/bootstrap.js",
      "node_modules/bootstrap/dist/css/bootstrap.css",
      "node_modules/bootstrap/dist/fonts/*",

      "node_modules/font-awesome/css/font-awesome.css",
      "node_modules/font-awesome/fonts/*",

      "bower_components/ihover/src/ihover.css"
    ],
    application: [
      "app/**/*.+(js|css|html|json|map|ts)",
      "images/**/*.*",
      "*.html"
    ]
  }
}

// General Tasks

gulp.task("clean:project", function () {
  return del([config.destination]);
});

// Tasks executed AfterCompile in Debug Configuration
// build.targets calls after:compile:debug

gulp.task("copy:libraries:debug", ["clean:project"], function () {
  return gulp.src(config.debug.libraries, {base: "." })
    .pipe(gulp.dest(config.destination));
});

gulp.task("copy:application:debug", ["clean:project"], function () {
  return gulp.src(config.debug.application, { base: "." })
    .pipe(gulp.dest(config.destination));
});

gulp.task("after:compile:debug", ["copy:libraries:debug", "copy:application:debug"]);

// Tasks executed AfterCompile in Debug Configuration
// build.targets calls after:compile:release
// ToDo: Currently the same as for Debug Configuration - can be amended later

gulp.task("after:compile:release", ["after:compile:debug"]);

// Tasks executed BeforeBuild in both, Release and Debug Configuration
// build targets calls before:build

gulp.task('tsd', function () {
  return gulp.src('./gulp_tsd.json').pipe(tsd());
});

gulp.task("before:build", ["tsd"]);

// Watch Tasks
// Task Runner Explorer executes watch on Project Open

gulp.task("clean:plugin:debug:app", function () {
  return del([config.debug.pluginappdestination], { force: true });
});

gulp.task("copy:watchfiles:plugin:debug", ["clean:plugin:debug:app"], function () {
  return gulp.src(config.debug.watchfiles, { base: "./app" })
    .pipe(gulp.dest(config.debug.pluginappdestination));
});

gulp.task('watch', function () {
  gulp.watch(config.debug.watchfiles, ["copy:watchfiles:plugin:debug"]);
});