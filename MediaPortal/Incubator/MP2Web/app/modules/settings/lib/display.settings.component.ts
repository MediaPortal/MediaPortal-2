import {Component, View, EventEmitter, OnChanges, SimpleChange, OnInit, ElementRef} from "angular2/core";
import {COMMON_DIRECTIVES, CORE_DIRECTIVES} from "angular2/common";
import {AgGridNg2} from "ag-grid-ng2/main";
import {GridOptions} from "ag-grid/main";

import {ConfigurationService} from "../../../common/lib/ConfigurationService/ConfigurationService";
import {ISettingData} from "./ISettingData";

@Component({
  templateUrl: "app/modules/settings/display.settings.html",
  selector: "settings",
  inputs: ["input: input"],
  events: ["onSettingChanged: on-setting-changed"],
  directives: [COMMON_DIRECTIVES, CORE_DIRECTIVES, AgGridNg2]
})
export class DisplaySettingsComponent {
  private input: any = null;
  private supportedTypes: string[] = ["String", "Int32", "Boolean", "List`1"]; // data types we support to edit

  // GUI
  columnDefs = [
    { headerName: "Setting", field: "name", width: 150 },
    { headerName: "Value", field: "value", cellRenderer: this.customEditor/*, editable: true*/, width: 150, onCellValueChanged: this.onValueChanged }
  ];
  gridOptions: GridOptions = <GridOptions>{};
  rowData: ISettingData[];

  // Events
  onSettingChanged: EventEmitter<ISettingData> = new EventEmitter();


  constructor(private configurationService: ConfigurationService, private elements: ElementRef) {
    this.setGridOptions();
  }

  ngOnInit() {
    this.processInput();
  }

  ngOnChanges() {
    this.processInput();
  }

  ngOnDestroy() {

  }

  setGridOptions() {
    this.gridOptions.context = this;
    this.gridOptions.enableSorting = true;
    this.gridOptions.suppressCellSelection = true;
    this.gridOptions.onGridReady = this.onGridReady;
  }

  processInput() {
    var data: ISettingData[] = [];
    for (var property in this.input) {
      if (this.input.hasOwnProperty(property)) {
        // check if the type is supported
        if (this.supportedTypes.indexOf(this.input[property].Type) !== -1) {
          data.push(<ISettingData>{
            name: this.input[property].Name,
            value: this.input[property].Value,
            type: this.input[property].Type
          });
        }
      }
    }

    this.rowData = data;
  }

  customEditor(params) {
    var editing = false;
    console.log(params);
    // this copied from ag-grid.js to be close to the original editing function
    var paramsForCallbacks = {
      node: params.node,
      data: params.data,
      oldValue: params.value,
      newValue: params.value,
      colDef: params.colDef,
      api: params.api,
      context: params.context
    };
    var eCell = document.createElement("div");
    eCell.setAttribute("style", "width: 100%;");
    console.log(params.column.actualWidth);
    var eLabel = document.createTextNode(params.value);
    eCell.appendChild(eLabel);
    
    // select for true/false
    var eSelect = document.createElement("select");
    var eOption = document.createElement("option");
    eOption.setAttribute("value", "true");
    eOption.innerHTML = "true";
    eSelect.appendChild(eOption);
    eOption = document.createElement("option");
    eOption.setAttribute("value", "false");
    eOption.innerHTML = "false";
    eSelect.appendChild(eOption);
    eSelect.value = params.value;

    // input for text
    var eInput = document.createElement("input");
    eInput.setAttribute("value", params.value);

    eCell.addEventListener("click", () => {
      if (!editing) {
        eCell.removeChild(eLabel);
        if (params.data.type === "Boolean") {
          eCell.appendChild(eSelect);
          eSelect.focus();
        } else {
          eCell.appendChild(eInput);
          eInput.focus();
        }
        editing = true;
        console.log(params);
      }
    });

    
    // events for editing

    eSelect.addEventListener("blur", () => {
      if (editing) {
        editing = false;
        eCell.removeChild(eSelect);
        eCell.appendChild(eLabel);
      }
    });

    eSelect.addEventListener("change", () => {
      if (editing) {
        editing = false;
        var newValue = eSelect.value;
        params.data[params.colDef.field] = newValue;
        paramsForCallbacks.newValue = newValue;
        eLabel.nodeValue = newValue;
        eCell.removeChild(eSelect);
        eCell.appendChild(eLabel);
        params.colDef.onCellValueChanged(paramsForCallbacks); // fire onCellValueChanged event
      }
    });

    eInput.addEventListener("change", () => {
      if (editing) {
        editing = false;
        var newValue = eInput.value;
        params.data[params.colDef.field] = newValue;
        paramsForCallbacks.newValue = newValue;
        eLabel.nodeValue = newValue;
        eCell.removeChild(eInput);
        eCell.appendChild(eLabel);
        params.colDef.onCellValueChanged(paramsForCallbacks); // fire onCellValueChanged event
      }
    });

    eInput.addEventListener("blur", () => {
      if (editing) {
        editing = false;
        eCell.removeChild(eInput);
        eCell.appendChild(eLabel);
      }
    });

    return eCell;
  }

  /*
  Events
  */
  
  onValueChanged(params) {
    // only emit changes if the value has changed
    console.log("EVNT FIRED");
    console.log(params);
    if (params.oldValue != params.newValue) {
      params.context.onSettingChanged.emit(<ISettingData>{
        name: params.data.name,
        value: params.newValue,
        type: params.data.type
      });
    }
  }

  onGridReady(params) {
    console.log("Grid Ready!");
    console.log(params);
    if (this.rowData.length > 0 && params.api) {  // prevent exception in case there is no data
      console.log("Resize");
      params.api.sizeColumnsToFit();
    }
  }

}