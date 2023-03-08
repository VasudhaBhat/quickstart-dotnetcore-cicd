
Method 1:

Set Auth.Server as the startup project
Implement IDesignTimeDbContextFactory


ADD:
Add-Migration 'ApplicationUserMapping' -Context IdentityContext -OutputDir Identity/Migrations

UPDATE:
Update-Database -Context IdentityContext 20180423235401_Identity-InitialMigration

REMOVE:
Remove-Migration -Context IdentityContext


Method 2: 

open Command Prompt from the database project root directory


For Add using below command

dotnet ef migrations add PersistedGrant-InitialMigration -c PersistedGrantDbContext -o PersistedGrant/Migrations --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj
dotnet ef migrations add Configuration-InitialMigration -c ConfigurationDbContext -o Configuration/Migrations --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj
dotnet ef migrations add Identity-InitialMigration -c IdentityContext -o Identity/Migrations --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj


To remove migration use below command

dotnet ef migrations remove -c PersistedGrantDbContext --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj
dotnet ef migrations remove -c ConfigurationDbContext --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj
dotnet ef migrations remove -c IdentityContext --startup-project ..\..\Server\Auth.Server\Auth.Server.csproj