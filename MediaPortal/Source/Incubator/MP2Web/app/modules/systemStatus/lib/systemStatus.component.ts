import {Component, View, ViewEncapsulation, EventEmitter, OnInit} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {HTTP_PROVIDERS, Http, Request, RequestMethod} from "angular2/http";

import { PROGRESSBAR_DIRECTIVES, TOOLTIP_DIRECTIVES } from 'ng2-bootstrap/ng2-bootstrap';


import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {ISystemInformation} from "./ISystemInformation";

declare var Chart:any;

@Component({
    templateUrl: "app/modules/systemStatus/systemStatus.html",
    directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, PROGRESSBAR_DIRECTIVES, TOOLTIP_DIRECTIVES],
    encapsulation: ViewEncapsulation.None // don't use View Encapsulation because we wan't to overwrite some css
})
export class SystemStatusComponent {
    BASE_URL: string;
    URL: string;
    MAX_DATAPOINTS: number = 10;
    UPDATE_INTERVAL: number = 2 * 1000;

    cpuChartCtx: any;
    cpuChart: any = null;
    cpuInterval: number = 0;

    ramChartCtx: any;
    ramChart: any = null;
    ramInterval: number = 0;

    systemInformation: ISystemInformation;
    updateInterval: any;

    dataCpu = {
        labels: [],
        datasets: [
            {
                label: "CPU",
                fillColor: "rgba(220,220,220,0.2)",
                strokeColor: "rgba(220,220,220,1)",
                pointColor: "rgba(220,220,220,1)",
                pointStrokeColor: "#fff",
                pointHighlightFill: "#fff",
                pointHighlightStroke: "rgba(220,220,220,1)",
                data: []
            }
        ]
    };

    dataRam = {
        labels: [],
        datasets: [
            {
                label: "RAM",
                fillColor: "rgba(220,220,220,0.2)",
                strokeColor: "rgba(220,220,220,1)",
                pointColor: "rgba(220,220,220,1)",
                pointStrokeColor: "#fff",
                pointHighlightFill: "#fff",
                pointHighlightStroke: "rgba(220,220,220,1)",
                data: []
            }
        ]
    };

    chartOptions = {
        bezierCurve: false,
        scaleBeginAtZero: true,
        tooltipEvents: [],
        animation: false
    };


    constructor(private http: Http, private configurationService: ConfigurationService) {
        this.BASE_URL = configurationService.config.WebApiUrl;
        this.URL = this.BASE_URL + "/api/v1/Server/Information";
    }

    ngOnInit() {
        this.updateInformation();

        this.updateInterval = setInterval(() => {
            this.updateInformation();
        }, this.UPDATE_INTERVAL);
    }

    ngOnDestroy() {
        clearInterval(this.updateInterval);

        if (this.cpuChart) {
            this.cpuChart.destroy();
            this.cpuChart = null;
        }

        if (this.ramChart) {
            this.ramChart.destroy();
            this.ramChart = null;
        }
    }

    updateInformation() {
        if (this.cpuChart == null && this.ramChart == null) {
            this.cpuChartCtx = (<HTMLCanvasElement> document.getElementById("cpuChart")).getContext("2d");
            this.cpuChart = new Chart(this.cpuChartCtx).Line(this.dataCpu, this.chartOptions);

            this.ramChartCtx = (<HTMLCanvasElement> document.getElementById("ramChart")).getContext("2d");
            this.ramChart = new Chart(this.ramChartCtx).Line(this.dataRam, this.chartOptions);
        }

        this.cpuInterval += (this.UPDATE_INTERVAL / 1000);
        this.ramInterval += (this.UPDATE_INTERVAL / 1000);

        if (this.cpuChart.scale.xLabels.length >= this.MAX_DATAPOINTS) {
            this.cpuChart.removeData();
        }

        if (this.ramChart.scale.xLabels.length >= this.MAX_DATAPOINTS) {
            this.ramChart.removeData();
        }

        this.http.request(new Request({
            method: RequestMethod.Get,
            url: this.URL
        })).map(res => res.json()).subscribe(res => {
            this.systemInformation = res;

            this.cpuChart.addData([res.CpuUsage], this.cpuInterval);
            this.ramChart.addData([res.Ram.Used.toFixed(0)], this.ramInterval);
        });
    }

    getPercent(value: number, total: number, decimal: number = 0): number {
        var result: number = (value / total * 100);
        return isNaN(result) ? 0 : parseFloat(result.toFixed(decimal));
    }

    BytesToXBString(bytes: number): string {
        var extension: string = "bytes";
        var convertedBytes: number = bytes;

        // to TB
        if (bytes > 1e+12) {
            convertedBytes = bytes / 1e+12;
            extension = "TB";
        }
        // to GB
        else if (bytes >= 1e+9) {
            convertedBytes = bytes / 1e+9;
            extension = "GB";
        }
        // to MB
        else if (bytes >= 1000000) {
            convertedBytes = bytes / 1000000;
            extension = "MB";
        }

        return convertedBytes.toFixed(2) + " " + extension;
    }
}