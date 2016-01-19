import {Pipe} from "angular2/core"

@Pipe({
    name: "range"
})
export class RangePipe {
    transform(input, total) {
        total = parseInt(total);
        // reset the input
        // TODO: this looks like a hack, but can't find a solution
        input = [];
        for (var i=0; i<total; i++) {
            input.push(i);
        }

        return input;
    }
}