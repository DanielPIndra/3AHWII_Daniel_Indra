# NBA Playoff Predictor

C# / ASP.NET-Core-Website, die NBA-Playoff-Matchups bewertet. Die App ruft die NBA.com Stats API direkt in C# ab und bildet damit die Endpunkte nach, die auch das Python-Projekt `swar/nba_api` kapselt.

Zusätzlich lädt die App kommende Spiele über den `scoreboardv2`-Endpunkt und berechnet für jedes Spiel eine Win-Ratio für Heim- und Auswärtsteam.

## Start

```powershell
dotnet run
```

Dann die im Terminal angezeigte lokale URL im Browser oeffnen, zum Beispiel:

```text

```

## Modell

Die Standard-Gewichtung ist:

- 55% Offense
- 45% Defense

Offense besteht aus `OFF_RATING`, `EFG_PCT`, `TS_PCT`, `AST_TO` und einem kleinen Anteil `NET_RATING`.
Defense besteht aus `DEF_RATING`, `DREB_PCT`, `REB_PCT` und einem kleinen Anteil `NET_RATING`.

Falls NBA.com nicht antwortet, zeigt die Seite Demo-Daten, damit die Website trotzdem laeuft.

## API-Endpunkte

```text
GET /api/playoffs
```

Die Antwort enthält:

- `matchups`: Playoff-Serien mit Favorit und Rating-Edge
- `games`: kommende NBA-Spiele aus `scoreboardv2` mit `homeWinRatio` und `awayWinRatio`
