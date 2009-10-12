-- This script creates the MediaPortal basis schema. DO NOT MODIFY!
-- Albert, 2009-08-15

-- Basis-Table where all sub schemas are registered with their current version number.
CREATE TABLE MEDIAPORTAL_BASIS (
  SUBSCHEMA_NAME %STRING(100)% NOT NULL PRIMARY KEY,
  VERSION_MAJOR %INTEGER% NOT NULL,
  VERSION_MINOR %INTEGER% NOT NULL
);

INSERT INTO MEDIAPORTAL_BASIS (SUBSCHEMA_NAME, VERSION_MAJOR, VERSION_MINOR) VALUES (
  'MediaPortal-Basis', 1, 0
);

