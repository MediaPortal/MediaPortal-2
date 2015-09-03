/*
 * Grammar based on http://upnp.org/specs/av/UPnP-av-ContentDirectory-v4-Service.pdf
 */

grammar UPnP;

/*
 * Parser Rules
 */

compileUnit : EOF;

/*
 * Lexer Rules
 */

WS : ( ' ' | '\t' | '\r' | '\n' )+ -> skip;

QUOTED_VAL : '"' ( ESC_SEQ | ~ ( '\\' | '"' ) )* '"';

fragment ESC_SEQ : '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\') | UNICODE_ESC | OCTAL_ESC;

fragment HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F');

fragment OCTAL_ESC : '\\' ('0'..'3') ('0'..'7') ('0'..'7') | '\\' ('0'..'7') ('0'..'7') | '\\' ('0'..'7');

fragment UNICODE_ESC  : '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT;

/*
 * 2.3.16.1 SearchCriteria String Syntax
 */

searchCrit : searchExp | '*';

searchExp : relExp
          | searchExp logOp searchExp
		  | innerExp
		  ;

innerExp : '(' searchExp ')';

logOp : 'and' | 'or';

relExp : property binOp quotedVal
       | property existsOp boolVal
	   ;

binOp : relOp | stringOp;

relOp : '=' | '!=' | '<' | '<=' | '>' | '>=';

stringOp : 'contains' | 'doesNotContain' | 'derivedfrom' | 'startsWith' | 'derivedFrom';

existsOp : 'exists';

boolVal : 'true' | 'false';

quotedVal : QUOTED_VAL;

/* See Table B-1 for a full list */
property : 'upnp:class' | 'upnp:genre' | 'upnp:artist' | 'upnp:album' | 'dc:title' | 'dc:date' | 'dc:creator' | 'res@size' | '@id' | '@refID' | '@parentId';
