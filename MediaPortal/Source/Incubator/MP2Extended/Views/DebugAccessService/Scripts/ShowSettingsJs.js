$(function () {

  $(".ui-icon-gear").button().on("click", function () {
    gearAction(this);
  });

  function gearAction(handler) {
    var currentRow = $(handler).closest("tr");
    var valueCell = currentRow.children("#value");
    var currentValueInCell = valueCell.text();

    // remove the current text in the "value" cell
    valueCell.empty();

    // Boolean
    if (currentRow.data("type") === "Boolean") {
      var arr = [
        { val: "true", text: "True", selected: "True" == currentValueInCell },
        { val: "false", text: "False", selected: "False" == currentValueInCell }
      ];

      var sel = $('<select>').appendTo(valueCell);
      $(arr).each(function () {
        sel.append($("<option>").attr("value", this.val).prop('selected', this.selected).text(this.text));
      });
      valueCell.children("select").selectmenu();
    }
      // Int32
    else if (currentRow.data("type") === "Int32") {
      var input = $("<input>").appendTo(valueCell);
      input.attr("value", currentRow.data("currentvalue"))
      input.spinner({
        min: 1,
        max: 100,
        change: function (event, ui) {
          $(this).spinner('value', parseInt($(this).spinner('value'), 10) || currentRow.data("currentvalue"));
        }
      });
    }// String
    else if (currentRow.data("type") === "String") {
      var input = $("<input>").appendTo(valueCell);
      input.attr({ value: currentRow.data("currentvalue"), class: "text ui-widget-content ui-corner-all" })
    } else {
      // restore text
      valueCell.text(currentRow.data("currentvalue"));
      currentRow.effect("highlight", {
        color: "#FF0000"
      }, 1000);
      return;
    }

    changeButtonsEdit(handler, currentRow);
  }

  function changeButtonsEdit(handler, currentRow) {
    // remove the gear button
    $(handler).remove();

    // add two new buttons
    currentRow.children("td:last").append("<span class=\"ui-icon ui-icon-check\"></span><span class=\"ui-icon ui-icon-close\"></span>");

    // add functiosn to the button
    $(".ui-icon-close").button().on("click", function () {
      cancelEdit(this);
    });
    $(".ui-icon-check").button().on("click", function () {
      acceptEdit(this);
    });
  }

  function cancelEdit(handler) {
    var currentRow = $(handler).closest("tr");
    var valueCell = currentRow.children("#value");

    // remove everything
    valueCell.empty();
    valueCell.text(currentRow.data("currentvalue"));

    var lastCell = currentRow.children("td:last");
    // remove all other buttons
    lastCell.empty();
    // add gear button again
    lastCell.append("<span class=\"ui-icon ui-icon-gear\">");
    lastCell.children(".ui-icon-gear").button().on("click", function () {
      gearAction(this);
    });
  }

  function acceptEdit(handler) {
    var currentRow = $(handler).closest("tr");
    var settingName = currentRow.data("name");
    var lastCell = currentRow.children("td:last");
    var valueCell = currentRow.children("#value");
    var inputValue = valueCell.find("input").val();
    if (inputValue === undefined)  // oribably a select?!
      inputValue = valueCell.find("select").val();

    var url = "/MPExtended/DebugAccessService/json/ChangeSetting?name=" + settingName + "&value=" + inputValue;
    $.getJSON(url, function (data) {
      if (data.Result == true) {
        // change the data value
        if (currentRow.data("type") === "Boolean")
          inputValue = capitalizeFirstLetter(inputValue);
        currentRow.data("currentvalue", inputValue);

        // use cancel edit to close everything
        cancelEdit(handler);
        currentRow.effect("highlight", {}, 1000);
      } else {
        dialogError.dialog("open").parent().addClass("ui-state-error");
      }
    });
  }

  function capitalizeFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
  }

  dialogError = $("#dialogError").dialog({
    autoOpen: false,
    resizable: false,
    height: 250,
    modal: true,
    buttons: {
      "Ok": function () {
        $(this).dialog("close");
      }
    }
  });

});