-- This script migrates ImportedItem aspect data from database version 2.* to version 2.*. DO NOT MODIFY!

INSERT INTO M_IMPORTEDITEM SELECT * FROM M_IMPORTEDITEM%SUFFIX%;

