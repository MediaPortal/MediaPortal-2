/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');
var clean = require('gulp-clean');

var config = {
  setup: [
    'node_modules/**/*.*',
    'bower_components/**/*.*'
  ],
  //Include all js files but exclude any min.js files
  src: [
    'app/**/*.+(js|css|html|json)',
    'images/**/*.*',
    '*.html'
  ]
}

gulp.task('setup', function () {
  return gulp.src(config.setup, {
    base: "." })
    .pipe(gulp.dest('../../../Bin/MP2-Server/bin/x86/Debug/Plugins/MP2Web/wwwroot/'));
  });

gulp.task('default', function () {
  return gulp.src(config.src, { base: "." })
    .pipe(gulp.dest('../../../Bin/MP2-Server/bin/x86/Debug/Plugins/MP2Web/wwwroot/'));
});