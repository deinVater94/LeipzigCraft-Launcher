# LeipzigCraft Launcher v0.1

Windows-Launcher für **Minecraft 1.21 + Fabric Loader 0.18.4**.

## Was v0.1 bereits vorbereitet

- Eigene, isolierte Installation unter `%APPDATA%\LeipzigCraft\game`
- Microsoft-/Minecraft-Anmeldung
- Installation von Minecraft 1.21
- Installation von Fabric Loader 0.18.4
- LeipzigCraft-Modpack-Sync über `https://leipzigcraft.com/launcher/pack.json`
- SHA256-Prüfung des heruntergeladenen Packs
- Entfernen alter Mods vor einem Pack-Update
- Großer `SPIELEN`-Button
- GitHub-Actions-Workflow zum Erzeugen einer selbstständigen Windows-EXE
- Import-Skript für eure aktuell funktionierende lokale Installation

Aus deinem Mod-Manifest wurden **29 JAR-Dateien** als Referenz übernommen.
Die `.rar`-Archive aus dem Mods-Ordner werden absichtlich **nicht** als Mods verteilt.

---

# Erster Entwicklertest

## 1. Aktuelles Pack in die getrennte Instanz importieren

Auf dem Rechner, auf dem eure funktionierende Minecraft-Installation liegt:

Rechtsklick auf:

`tools\Import-Current-Pack.ps1`

und mit PowerShell ausführen.

Das Skript kopiert nur `.jar`-Mods und Config nach:

`%APPDATA%\LeipzigCraft\game`

Die normale `.minecraft`-Installation wird nicht verändert.

## 2. Launcher bauen

### Einfachster Weg: GitHub Actions

Lege dieses Projekt in ein GitHub-Repository und pushe es.

Dann:

`Actions -> Build LeipzigCraft Launcher -> Run workflow`

Nach dem Build liegt unter **Artifacts**:

`LeipzigCraft-Launcher.exe`

### Lokal

Benötigt .NET 8 SDK:

```powershell
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Danach:

`publish\LeipzigCraft-Launcher.exe`

---

# Modpack für Spieler veröffentlichen

Wenn der Launcher später das Pack automatisch herunterladen soll:

## 1. Pack bauen

```powershell
.\tools\Build-Pack.ps1 -Version "0.1.0"
```

Das erzeugt:

`dist\LeipzigCraft-Pack-0.1.0.zip`

plus SHA256.

## 2. ZIP hosten

Empfehlung: als GitHub-Release-Asset hochladen.

## 3. `pack.json` ausfüllen

Datei:

`web\launcher\pack.json`

Beispiel:

```json
{
  "version": "0.1.0",
  "zipUrl": "DEINE-DIREKTE-RELEASE-URL",
  "sha256": "SHA256-DES-ZIPS",
  "size": 1334000000
}
```

Diese Datei anschließend auf der Website unter folgendem Pfad veröffentlichen:

`https://leipzigcraft.com/launcher/pack.json`

Beim nächsten Launcher-Start wird das Pack automatisch geprüft und bei einer neuen Versionsnummer heruntergeladen.

---

# Resourcepack

Das Resourcepack ist absichtlich **nicht** Teil des Launchers.
Wie besprochen kann es als Minecraft-Server-Resource-Pack verteilt werden.

---

# Wichtig für v0.1

Der Launcher benutzt die **Client-Konfiguration, die bei euch tatsächlich funktioniert**:

- Minecraft: `1.21`
- Fabric Client: `0.18.4`

Dass euer Server Fabric `0.17.3` verwendet, muss der Client-Launcher nicht nachbilden.

## Noch offen für v0.2

- Server-IP automatisch verbinden
- Launcher-Selbstupdate
- Download-Prozentanzeige statt nur Ladebalken
- RAM-Regler
- News/Changelog
- Code Signing
- optional eigene Java-21-Bootstrap-Logik, falls auf einem frischen PC keine passende Runtime gefunden wird

---

## Projektstruktur

- `MainWindow.xaml` – UI
- `MainWindow.xaml.cs` – Login/Play-Ablauf
- `Services/MinecraftService.cs` – Minecraft + Login
- `Services/FabricService.cs` – Fabric Installer
- `Services/PackService.cs` – Pack-Download/Hash/Installation
- `tools/Import-Current-Pack.ps1` – bestehendes funktionierendes Pack importieren
- `tools/Build-Pack.ps1` – Spieler-Pack bauen
- `web/launcher/pack.json` – kleines Online-Manifest
- `docs/mods-reference.json` – Referenz der aktuell gemeldeten Mods

## Open Source

LeipzigCraft Launcher is open-source software licensed under the [MIT License](LICENSE).

## Code signing policy

See our [Code signing policy](CODE_SIGNING_POLICY.md).

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Privacy and uninstallation

- [Privacy Policy](PRIVACY.md)
- [Uninstallation instructions](UNINSTALL.md)
