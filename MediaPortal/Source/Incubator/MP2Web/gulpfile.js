/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');

var config = {
    //Include all js files but exclude any min.js files
  src: [
    'node_modules/**/*.*',
    'app/**/*.js',
    '*.html'
  ]
}

gulp.task('default', function () {
  return gulp.src(config.src, {base: "."})
    .pipe(gulp.dest('../../../Bin/MP2-Server/bin/x86/Debug/Plugins/MP2Web/wwwroot/'));
});