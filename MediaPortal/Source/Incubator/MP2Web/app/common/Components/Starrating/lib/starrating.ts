import {Component, View} from "angular2/core";

@Component({
    templateUrl: "app/common/Components/Starrating/starrating.html",
    selector: "starrating",
    inputs: ["rating: rating"],
})
export class StarratingComponent {
    rating: number = 0;

    GetStars = function(number): string {
        if (number < 1)
            return "";
        if (number == 1)
            return "one";
        if (number < 2)
            return "onehalf";
        if (number == 2)
            return "two";
        if (number < 3)
            return "twohalf";
        if (number == 3)
            return "three";
        if (number < 4)
            return "threehalf";
        if (number == 4)
            return "four";
        if (number < 5)
            return "fourhalf";
        if (number == 5)
            return "five";
        if (number < 6)
            return "fivehalf";
        if (number == 6)
            return "six";
        if (number < 7)
            return "sixhalf";
        if (number == 7)
            return "seven";
        if (number < 8)
            return "sevenhalf";
        if (number == 8)
            return "eight";
        if (number < 9)
            return "eighthalf";
        if (number == 9)
            return "nine";
        if (number < 10)
            return "ninehalf";
        if (number == 10)
            return "ten";

        return "";
    }
}