# FarmerQuest Server

## Database (lokal SQL Server)

I Development finder API'en automatisk din lokale SQL Server:

1. `{dit-pc-navn}\SQLEXPRESS`
2. `localhost\SQLEXPRESS` og `.\SQLEXPRESS`
3. Default instance på samme maskine
4. `(localdb)\MSSQLLocalDB`

Kør `dotnet run`.

### Fast forbindelse på én PC (valgfrit)

Kopiér `.env.example` → `.env.local` og sæt `CONNECTIONSTRINGS__DEFAULTCONNECTION`.

Sådan finder du den rigtige `Server=`-værdi (PowerShell):

```powershell
Get-Service *SQL*
# MSSQL$SQLEXPRESS Running  →  Server=.\SQLEXPRESS
# MSSQLSERVER Running       →  Server=.   (eller localhost)

Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
# viser installationsnavne (fx SQLEXPRESS)

sqllocaldb info
# hvis LocalDB: Server=(localdb)\MSSQLLocalDB
```

Brug **ikke** kun `$env:COMPUTERNAME`, medmindre du har en default-instans (`MSSQLSERVER`).  
SQL Express kræver næsten altid `.\SQLEXPRESS` (eller `PC-NAVN\SQLEXPRESS`).

I **SSMS** skriver du kun instansen i feltet *Server name*, fx `.\SQLEXPRESS` — ikke hele connection string’en (det giver fejl 26/40). Database og Windows-login vælges i de andre felter / efter connect.

Eksempler til `.env.local`:

```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=.\SQLEXPRESS;Database=FarmerQuest;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=(localdb)\MSSQLLocalDB;Database=FarmerQuest;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

### Setup

1. Kopiér `.env.example` → `.env` og sæt `JWT__KEY` (min. 32 tegn). Valgfrit: kopiér også til `.env.local` for PC-specifik SQL.
2. Installer [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) med instance **SQLEXPRESS** (eller brug eksisterende).
3. `dotnet run`

## Migrations & spil-data

- **Skema** (tabeller): EF migrations - se `Migrations/README.md`
- **Indhold** (demo-data): `Databases/Seed/` - kopiér `FarmContentDevSeeder` som template når du tilføjer nyt

### Nulstil database + migrations

Hold 1 migration (`InitialCreate`) med hele skemaet. Ved nulstilling (valgfrit i development fasen):

```powershell
cd D:\VidenDjurs\ProjektFarmerQuest\server\server\server
dotnet ef database drop --force
# slet .cs-filer i Migrations/ (behold README.md)
dotnet ef migrations add InitialCreate
dotnet run
```


## Environment variables

- `DATABASE__MODE` — `auto` (default), `express`, `localdb`
- `CONNECTIONSTRINGS__DEFAULTCONNECTION` — valgfri fast forbindelse (sæt i `.env.local`)
- `JWT__KEY` — påkrævet

Commit ikke `.env`, `.env.local`, eller `appsettings.Development.local.json`.
