  $(function() {
    var dialog, form,
 
    name = $( "#name" ),
    type = $( "#type" ),
    password = $( "#password" ),
    allFields = $( [] ).add( name ).add( type ).add( password ),
    tips = $( ".validateTips" );

 
    function updateTips( t ) {
      tips
        .text( t )
        .addClass( "ui-state-highlight" );
      setTimeout(function() {
        tips.removeClass( "ui-state-highlight", 1500 );
      }, 500 );
    }
 
    function checkLength( o, n, min, max ) {
      if ( o.val().length > max || o.val().length < min ) {
        o.addClass( "ui-state-error" );
        updateTips( "Length of " + n + " must be between " +
          min + " and " + max + "." );
        return false;
      } else {
        return true;
      }
    }
 
    function checkRegexp( o, regexp, n ) {
      if ( !( regexp.test( o.val() ) ) ) {
        o.addClass( "ui-state-error" );
        updateTips( n );
        return false;
      } else {
        return true;
      }
    }
 
    function addUser() {
      var valid = true;
      allFields.removeClass( "ui-state-error" );
 
      valid = valid && checkLength( name, "username", 3, 16 );
      valid = valid && checkLength( password, "password", 5, 16 );
 
      valid = valid && checkRegexp( name, /^[a-z]([0-9a-z_])+$/i, "Username may consist of a-z, 0-9, underscores and must begin with a letter." );
 
      if ( valid ) {
        // add User to the DB, sending the PW in plain text is not very secure, but this is not a high security application
        // it is intended for home use. If you use it in a more public enviroment be careful!
        var url = '/MPExtended/DebugAccessService/json/CreateUser?username=' + name.val() + '&password=' + password.val() + '&type=' + type.val();
        $.getJSON(url, function(data) {
          if (data.Result == true) {
            $( "#users tbody" ).append( "<tr>" +
			      "<td>" + name.val() + "</td>" +
			      "<td>" + type.val() + "</td>" +
            "<td><span class=\"ui-icon ui-icon-trash\"></span></td>" + 
			      "</tr>" );

            dialog.dialog( "close" );
            $( "#users tbody" ).children("tr:last").effect("highlight", {}, 1000)
          }else {
            valid = false;
            updateTips( 'An error occurred! Please check the logs for more details.' );
          }
        });
      }
      return valid;
    }
 
    dialog = $( "#dialog-form" ).dialog({
      autoOpen: false,
      height: 450,
      width: 500,
      modal: true,
      buttons: {
        "Create an account": addUser,
        Cancel: function() {
          dialog.dialog( "close" );
        }
      },
      close: function() {
        form[ 0 ].reset();
        allFields.removeClass( "ui-state-error" );
      }
    });
 
    form = dialog.find( "form" ).on( "submit", function( event ) {
      event.preventDefault();
      addUser();
    });
 
    $( "#create-user" ).button().on( "click", function() {
      dialog.dialog( "open" );
    });

    // Delete User action

    dialogDeleteUser = $("#dialogDeleteUserError").dialog({
      autoOpen: false,
      resizable: false,
      height: 250,
      modal: true,
      buttons: {
        "Ok" : function () {
          $(this).dialog("close");
        }
      }
    });

    $( ".ui-icon-trash" ).button().on( "click", function() {
      var effectOptions = {};
      var currentRow = $(this).closest("tr");
      var url = '/MPExtended/DebugAccessService/json/DeleteUser?id=' + currentRow.data("id");
      $.getJSON(url, function(data) {
        if (data.Result == true) {
          // remove the row from the table with a nice effect
          currentRow.children("td").each(function() {
            $(this).wrapInner("<div />").children("div").slideUp(function() {
              currentRow.remove();
            })
          });
        }else {
          dialogDeleteUser.dialog( "open" ).parent().addClass("ui-state-error");
        }
      });
    });

    // create the select menu for the user types
    type.selectmenu();
  });