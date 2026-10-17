# AGENTS.md — jak pracować z tym projektem

Instrukcje dla agentów AI (Claude Code, Codex, Copilot itp.) i dla ludzi dołączających do projektu.

## Pytanie ≠ polecenie

- **Gdy użytkownik zadaje pytanie, odpowiedz na nie i nic nie zmieniaj.** Nie edytuj plików, nie commituj, nie wprowadzaj zmian „przy okazji”. Czytać kod wolno, żeby odpowiedzieć.
- **Działaj dopiero na wyraźne polecenie** („zrób”, „dodaj”, „popraw”, „zacommituj” itp.).
- Jeśli w odpowiedzi proponujesz zmianę, opisz ją i poczekaj na decyzję.

## O projekcie

- **Gra:** „Cięcie!” (robocza nazwa) — Game Jam 24h, temat *Beyond Reality*. Opis gry, mechaniki i zakres: [gdd.md](gdd.md). Przeczytaj GDD przed implementowaniem nowych mechanik.
- **Silnik:** Unity 6 (`6000.3.26f1`), URP, nowy Input System, C#.
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

## Styl kodu C#

### Hermetyzacja (najważniejsze)

Stosujemy zasady **SOLID**, a przede wszystkim **hermetyzację**:

- **Pola są zawsze prywatne.** Żadnych publicznych pól.
- **Na zewnątrz wystawiamy tylko właściwości** (najlepiej tylko do odczytu) i metody.
- Pola widoczne w Inspectorze to `private` + `[SerializeField]`, nigdy `public`.
- Setter publiczny tylko wtedy, gdy naprawdę jest potrzebny; preferuj `private set` lub metodę opisującą intencję (`Enable()`, `ApplyDamage(...)`).
- Nie wystawiaj mutowalnych kolekcji — zwracaj `IReadOnlyList<T>` / `IReadOnlyCollection<T>`.

### Atrybuty — w linijce POWYŻEJ pola

`[SerializeField]` (i pozostałe atrybuty: `[Header]`, `[Tooltip]`, `[Range]` itd.) piszemy **w osobnej linijce nad polem**, nie w tej samej linii.

```csharp
public class OperatorCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 3f;

    [Tooltip("Time to reach target velocity. 0 = instant.")]
    [SerializeField]
    private float moveSmoothTime = 0.12f;

    private Camera cam;

    public Camera Camera => cam;
    public float MoveSpeed => moveSpeed;
    public bool InputEnabled { get; private set; } = true;
}
```

Źle:

```csharp
public float moveSpeed = 3f;                    // publiczne pole
[SerializeField] private float moveSpeed = 3f;  // atrybut w tej samej linii
```

### Pozostałe zasady SOLID w praktyce

- **Single Responsibility:** jeden `MonoBehaviour` = jedna odpowiedzialność. Zamiast jednego wielkiego managera — kilka małych komponentów.
- **Open/Closed, Dependency Inversion:** zależności przez interfejsy lub referencje ustawiane w Inspectorze (`[SerializeField]`), nie przez `FindObjectOfType` / `GameObject.Find` w runtime.
- **Interface Segregation:** małe, wąskie interfejsy (`IInteractable`, a nie `IEverything`).
- Komunikacja między systemami przez zdarzenia (`event Action<...>`) zamiast bezpośrednich wywołań w obie strony.

### Konwencje nazewnicze

- Klasy, metody, właściwości, eventy: `PascalCase`.
- Pola prywatne i zmienne lokalne: `camelCase` (bez prefiksu `_` / `m_`).
- Stałe: `PascalCase` (`private const float Skin = 0.01f;`).
- Interfejsy z prefiksem `I`.
- Jeden typ publiczny na plik, nazwa pliku = nazwa klasy (wymóg Unity dla `MonoBehaviour`/`ScriptableObject`).
- Komentarze i nazwy w kodzie po angielsku; `<summary>` dla publicznych klas i nieoczywistych członków.

### Input

Używamy **nowego Input Systemu**. Akcje są w `Assets/_Project/InputSystem_Actions.inputactions`; komponenty przyjmują je przez `[SerializeField] InputActionReference` (zgodnie z regułą atrybutu powyżej pola). Nie używaj starego `UnityEngine.Input`.

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
