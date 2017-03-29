-- This script updates the MediaLibrary schema from version 1.1 to version 1.2. DO NOT MODIFY!

ALTER TABLE SHARES ADD WATCHER %INTEGER% DEFAULT 0 NOT NULL;
