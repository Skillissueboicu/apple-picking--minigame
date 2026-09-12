# FarmQuest Online

Let Unity-klient til **kun online-flowet**: login → menu → lobby → fælles farm-session.  
Samme API/server som det fulde FarmerQuest-spil (`http://localhost:5192` som standard).

## Hvad projektet indeholder

| Del | Indhold |
|-----|---------|
| **Boot** | Startscene: init + auto-login / routing til Login eller MainMenu |
| **Login** | Opret konto / log ind, “Husk mig” (credentials i 7 dage) |
| **MainMenu** | Vælg online-spil, log ud |
| **Lobby** | Opret session, join via kode, invite, spillere, host starter spillet |
| **FarmerQuestScene** | Simpel 3D-session med spillerliste (polling) + menu/hovedmenu |

Ingen SignalR i denne klient — lobby/session opdateres via **HTTP polling**.

## Kode (`Assets/_Online/Scripts`)

| Mappe | Rolle |
|-------|--------|
| **Core** | `App` / `AppHost` (singleton), config, scene-navne, `SceneFlow`, netværks-settings |
| **Net** | `ApiClient`, auth, session-API, DTO’er, `PollingNetworkSession` |
| **UI** | Scene-controllers (`Boot`, `Login`, `MainMenu`, `Lobby`, `FarmerQuestController`) |
| **Editor** | Kun editor-menuer til opsætning (se nedenfor) — bruges ikke i runtime |

Assets der følger med:

- `Assets/_Online/Scenes/` — de fem scener i Build Settings (Boot først)
- `Assets/_Online/Resources/OnlineNetworkSettings.asset` — API-URL, remember-dage
- `Assets/_Online/Materials/` — materiale til jorden

## Kom i gang (efter projektet er konfigureret)

1. Åbn mappen i **Unity 6.3** (samme major som FarmerQuest)
2. Start serveren: `cd C:\Users\Fred\FarmQuest\server\server` → `dotnet run`
3. Tryk Play (starter i Boot)

### Test med to spillere

ParrelSync eller to Editor-instanser med forskellige logins (gemte credentials er pr. `dataPath`).

---

## Editor-opsætning (Configure / bake / jord)

*Kun relevant første gang, eller hvis UI/jord skal genopbygges. Runtime behøver ikke Editor-mappen.*

| Handling | Menu / resultat |
|----------|-----------------|
| **Configure Project** | `FarmQuest Online → Configure Project` — Build Settings, settings-asset, scener |
| **Bake UI** | Samme flow / `Bake UI Into All Scenes` — lægger Canvas, knapper, felter ind som **normale scene-objekter** (Hierarchy, ikke kun runtime) |
| **Jord (Ground)** | I **FarmerQuestScene**: en Plane (+ lys) i scenen. Overlay (spillerliste / menu) ligger i Canvas ovenpå |

Efter bake kan UI redigeres direkte i Hierarchy. Genkør kun Configure/Bake hvis scenerne er tomme eller skal nulstilles.
