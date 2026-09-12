# Migrations & seed-data

## Nulstil migrations (ren start — én migration)

Gør dette når I vil smide historikken væk og starte forfra med **alt skema i én fil**:

1. **Stop** den kørende server.
2. Slet alle `.cs`-filer i `Migrations/` (behold denne README).
3. Drop databasen:

```powershell
cd D:\VidenDjurs\ProjektFarmerQuest\server\server\server
dotnet ef database drop --force
```

4. Opret én ny initial migration (inkl. Users, sessions, social, …):

```powershell
dotnet ef migrations add InitialCreate
dotnet build
```

5. `dotnet run` — `Migrate()` opretter hele skemaet.

Under aktiv udvikling er det fint at **kun** have `InitialCreate` og genopbygge den ved større skema-ændringer (drop DB først). Inkrementelle migrations giver mere mening, når databasen er i produktion.

---

## Tilføj skema senere (ny tabel/kolonne)

1. Ret `Models/` + `FarmerQuestDbContext`.
2. Kør:

```powershell
dotnet ef migrations add BeskrivendeNavn
dotnet ef database update   # valgfrit; sker også ved dotnet run
```

3. Commit både `.cs` migration-filer og `*ModelSnapshot.cs`.

---

## Tilføj spil-data (template / seeder)

1. Kopiér `Databases/Seed/FarmContentDevSeeder.cs` → nyt navn, fx `ShopItemDevSeeder.cs`.
2. Implementér `SeedAsync` **idempotent** (tjek om data findes før insert).
3. Registrér i `Program.cs`:

```csharp
builder.Services.AddSingleton<IDevDataSeeder, ShopItemDevSeeder>();
```

4. Seed kører kun i **Development** efter `Migrate()`.

Produktion: brug gerne eksplicitte migrations + separate prod-seed scripts — ikke tilfældig demo-data.
