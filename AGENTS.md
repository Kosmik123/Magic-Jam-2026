# AGENTS.md — jak pracować z tym projektem

Instrukcje dla agentów AI (Claude Code, Codex, Copilot itp.) i dla ludzi dołączających do projektu.

## Wspólne zasady

**Najpierw przeczytaj [BIPOLAR_RULES.md](BIPOLAR_RULES.md)** — zasady komunikacji (pytanie ≠ polecenie) i styl kodu C#. Ten plik zawiera tylko to, co specyficzne dla projektu.

## O projekcie

- **Gra:** „Cięcie!” (robocza nazwa) — Game Jam 24h, temat *Beyond Reality*. Opis gry, mechaniki i zakres: [gdd.md](gdd.md). Przeczytaj GDD przed implementowaniem nowych mechanik.
- **Silnik:** Unity 6 (`6000.3.26f1`), URP, Input System, C#.
- **Namespace bazowy:** `MagicJam` (np. `MagicJam.CameraSystem`).

## Struktura folderów

```
Assets/
├── _Project/            ← WSZYSTKIE pliki naszej gry
│   ├── Scenes/
│   ├── Scripts/         ← podfoldery wg systemów (Camera/, Actors/, ...)
│   ├── Settings/        ← assety URP, profile Volume
│   └── InputSystem_Actions.inputactions
└── <ZewnętrznaBiblioteka>/  ← paczki z Asset Store / zewnętrzne biblioteki
```

Zasady:

- **Nasze pliki trafiają wyłącznie do `Assets/_Project/`.** Nowe skrypty, sceny, prefaby, materiały, tekstury, audio — wszystko tam, w odpowiednim podfolderze.
- **Bezpośrednio w `Assets/` leżą tylko zewnętrzne biblioteki** (Asset Store, pluginy, importowane paczki). Nie twórz tam własnych plików.
- **Nie modyfikuj zewnętrznych bibliotek.** Jeśli trzeba zmienić ich zachowanie — rozszerz je (wrapper, dziedziczenie, adapter) w `Assets/_Project/`.
- Namespace odpowiada folderowi pod `Scripts/` (np. `Scripts/Camera/` → `MagicJam.CameraSystem`).

## Pliki `.meta`

- Każdy asset w `Assets/` ma plik `.meta` z GUID-em — **commituj je razem z assetem**.
- Przenosząc lub zmieniając nazwę pliku, przenieś też jego `.meta` (inaczej zerwą się referencje w scenach i prefabach). Najbezpieczniej robić to w edytorze Unity.
- Nie twórz `.meta` ręcznie — Unity wygeneruje je przy imporcie.
- Nie edytuj ręcznie scen (`.unity`) ani prefabów, chyba że to drobna, pewna zmiana — YAML Unity łatwo zepsuć.

## Czego nie dotykać / nie commitować

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, wygenerowane `*.csproj` / `*.sln*` — to pliki lokalne, generowane przez Unity/IDE.

## Input

Akcje Input Systemu są w `Assets/_Project/InputSystem_Actions.inputactions`.

## Git flow

| Gałąź | Przeznaczenie |
|---|---|
| `master` | **Stabilna wersja gry.** Trafiają tu tylko sprawdzone, działające zmiany (grywalny build). |
| `develop` | **Aktywny rozwój.** Tu na bieżąco lądują nowe funkcje i poprawki. |
| `feature/<nazwa>` | Opcjonalne gałęzie na większe funkcje, odgałęziane od `develop` i do niego mergowane. |

Zasady:

- **Codzienna praca odbywa się na `develop`** (lub na gałęzi `feature/*` odbitej od `develop`).
- **Nie commituj bezpośrednio do `master`.** Na `master` trafia tylko merge z `develop`, gdy gra na `develop` się kompiluje, uruchamia i działa stabilnie.
- Gałęzie `feature/*` merguj z powrotem do `develop`, nigdy prosto do `master`.
- Pull requesty kierujemy do `develop`; PR do `master` tylko przy wydawaniu stabilnej wersji.
- Małe, opisowe commity. Commituj assety razem z ich `.meta`.
