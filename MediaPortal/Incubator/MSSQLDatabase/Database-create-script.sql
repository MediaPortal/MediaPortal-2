--Create database
CREATE DATABASE [MP2Server] COLLATE Latin1_General_CI_AS;
ALTER DATABASE [MP2Server] MODIFY FILE (NAME = N'MP2Server', SIZE = 200MB, FILEGROWTH = 100MB, MAXSIZE = UNLIMITED);
ALTER DATABASE [MP2Server] MODIFY FILE (NAME = N'MP2Server_Log', SIZE = 50MB, FILEGROWTH = 25MB, MAXSIZE = UNLIMITED);
GO

--Ensure that transaction logging is set to simple
ALTER DATABASE [MP2Server] SET RECOVERY SIMPLE;
GO

--Ensure that database is always available
ALTER DATABASE [MP2Server] SET AUTO_CLOSE OFF;
GO

--Create MP user
CREATE LOGIN [MPUser] WITH PASSWORD=N'MediaPortal', DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF;
GO

--Give user the necessary rights
GRANT VIEW ANY DEFINITION TO [MPUser];
USE [MP2Server];
GO
CREATE USER [MPUser] FOR LOGIN [MPUser] WITH DEFAULT_SCHEMA=dbo;
EXEC SP_DEFAULTDB N'MPUser', N'MP2Server';
EXEC SP_ADDROLEMEMBER N'db_owner', N'MPUser';
GO

--Allow mixed mode authentication
EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'LoginMode', REG_DWORD, 2