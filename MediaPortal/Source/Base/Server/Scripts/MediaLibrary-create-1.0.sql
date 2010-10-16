-- This script creates the MediaLibrary schema. DO NOT MODIFY!


-- Shares

CREATE TABLE SHARES (
  SHARE_ID %GUID% NOT NULL PRIMARY KEY,
  SYSTEM_ID %STRING(100)% NOT NULL,
  BASE_RESOURCE_PATH %STRING(2000)% NOT NULL,
  NAME %STRING(2000)% NOT NULL
);
  
CREATE TABLE SHARES_CATEGORIES (
  SHARE_ID %GUID% NOT NULL,
  CATEGORYNAME %STRING(100)% NOT NULL,

  CONSTRAINT SHARESCATEGORIES_PK PRIMARY KEY (SHARE_ID, CATEGORYNAME),
  CONSTRAINT SHARESCATEGORIES_SHARE_FK FOREIGN KEY (SHARE_ID) REFERENCES SHARES (SHARE_ID) ON DELETE CASCADE
);


-- Media item aspect management

-- Stores all managed MIAM instances. Each entry in this table has corresponding schema objects which
-- belong to its attributes and which store the related media item aspect instances.
CREATE TABLE MIA_TYPES (
  MIAM_ID %GUID% NOT NULL PRIMARY KEY,
  NAME %STRING(2000)% NOT NULL,
  MIAM_SERIALIZATION %STRING(8000)% NOT NULL
);

-- Automatically generated identifiers for MIA tables and collection attribute tables may be too long as database
-- object names, so we need to create shorter names which will be mapped to their original created identifiers in this table.
CREATE TABLE MIA_NAME_ALIASES (
  IDENTIFIER %STRING(200)% NOT NULL PRIMARY KEY,
  DATABASE_OBJECT_NAME %STRING(100)% NOT NULL,
  MIAM_ID %GUID% NOT NULL,

  CONSTRAINT MIA_NAME_ALIASES_MIA_TYPES_FK FOREIGN KEY (MIAM_ID) REFERENCES MIA_TYPES (MIAM_ID) ON DELETE CASCADE
);


-- Media items

-- Contains the actual media item instances. Each of those media item instances has associated entries in
-- several media item aspect tables which describe their data.
CREATE TABLE MEDIA_ITEMS (
  MEDIA_ITEM_ID %GUID% NOT NULL PRIMARY KEY
);

CREATE UNIQUE INDEX MEDIA_ITEMS_PK_IDX ON MEDIA_ITEMS(MEDIA_ITEM_ID);


-- Playlists

-- Contains a collection of playlist ids and names.
CREATE TABLE PLAYLISTS (
  PLAYLIST_ID %GUID% NOT NULL PRIMARY KEY,
  NAME %STRING(2000)% NOT NULL,
  PLAYLIST_TYPE %STRING(100)%
);

-- Contains the actual playlist contents for each playlist.
CREATE TABLE PLAYLIST_CONTENTS (
  PLAYLIST_ID %GUID% NOT NULL,
  PLAYLIST_POS %INTEGER% NOT NULL,
  MEDIA_ITEM_ID %GUID% NOT NULL,

  CONSTRAINT PLAYLIST_CONTENTS_PK PRIMARY KEY (PLAYLIST_ID, PLAYLIST_POS),
  CONSTRAINT PL_CONTENTS_PL_FK FOREIGN KEY (PLAYLIST_ID) REFERENCES PLAYLISTS (PLAYLIST_ID) ON DELETE CASCADE
);

CREATE INDEX PLAYLIST_CONTENTS_ORDER_IDX ON PLAYLIST_CONTENTS(PLAYLIST_ID, PLAYLIST_POS);
