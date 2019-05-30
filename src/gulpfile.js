var gulp = require('gulp')
gulp.task('build', function () {
    return gulp
        .src('./node_modules/@groupdocs.examples.angular/viewer/**')
        .pipe(gulp.dest('./Resources/'))
})