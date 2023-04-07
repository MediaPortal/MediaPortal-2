-- This script updates the ClientManager schema from version 1.0 to version 1.1. DO NOT MODIFY!

ALTER TABLE ATTACHED_CLIENTS ADD LAST_CLIENT_VERSION %STRING(50)%;
