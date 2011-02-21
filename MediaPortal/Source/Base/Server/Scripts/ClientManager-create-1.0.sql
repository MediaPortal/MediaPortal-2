-- This script creates the ClientManager schema. DO NOT MODIFY!

-- Contains entries for all attached clients.
CREATE TABLE ATTACHED_CLIENTS (
  SYSTEM_ID %STRING(50)% NOT NULL PRIMARY KEY,
  LAST_HOSTNAME %STRING(50)%,
  LAST_CLIENT_NAME %STRING(100)%
);
