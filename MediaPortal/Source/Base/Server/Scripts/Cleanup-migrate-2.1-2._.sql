-- This script cleans up migration data from database version 2.1 to version 2.*. DO NOT MODIFY!

DELETE FROM MEDIA_ITEMS WHERE MEDIA_ITEM_ID IN (SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE PATH LIKE '%folder.jpg')

