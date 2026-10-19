# HOUSE_RULES.md — wspólne zasady pracy z agentami

Uniwersalne zasady komunikacji i stylu kodu, wspólne dla wszystkich naszych projektów Unity.
Instrukcje specyficzne dla projektu są w `AGENTS.md` (lub odpowiedniku) danego repozytorium.

> **Nie edytuj tego pliku w trakcie pracy w repozytorium.** To kopia wspólnego szablonu dodawanego
> do każdego projektu. Zmiany wprowadzamy w szablonie i kopiujemy do projektów. Jeśli zasada dotyczy
> tylko jednego projektu, wpisz ją do `AGENTS.md` tego projektu.
> W razie konfliktu pierwszeństwo mają zasady z `AGENTS.md` projektu.

## Pytanie ≠ polecenie

- **Gdy użytkownik zadaje pytanie, odpowiedz na nie i nic nie zmieniaj.** Nie edytuj plików, nie commituj, nie wprowadzaj zmian „przy okazji”. Czytać kod wolno, żeby odpowiedzieć.
- **Działaj dopiero na wyraźne polecenie** („zrób”, „dodaj”, „popraw”, „zacommituj” itp.).
- Jeśli w odpowiedzi proponujesz zmianę, opisz ją i poczekaj na decyzję.

## Styl kodu C#

- **Namespace bazowy** to nazwa projektu (np. `MagicJam`); namespace odpowiada folderowi pod `Scripts/`.

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

### Formatowanie instrukcji

- **Instrukcja po `if` / `for` zawsze w osobnej linijce** — nigdy w tej samej linii co warunek.
- **Jednolinijkowe ciało `if` / `for` piszemy bez klamer.** Klamry tylko dla ciał wieloliniowych.
- **Negację w warunkach zapisujemy jako `== false`, nie przez `!`.**
- **Sprawdzenie czy obiekt dziedziczący z UnityEngine.Object wykonujemy przez skróconą wersję, w której obiekt jest implicit rzutowany na bool**.
- **Jednak sprawdzenie czy obiekt dziedziczący z UnityEngine.Object nie istnieje, już porównujemy z null**

```csharp
if (isActive == false)
    return;

for (int i = 0; i < actors.Count; i++)
    actors[i].Reset();

if (target == null)
    return;

if (target && target.IsReady == false)
{
    target.Prepare();
    target.Notify();
}
```

Źle:

```csharp
if (!isActive) return;                        // instrukcja w tej samej linii, negacja przez !
for (int i = 0; i < n; i++) { Do(i); }        // zbędne klamry i instrukcja w tej samej linii
```

### Kolejność składowych klasy

- **Eventy (`public event ...`) deklarujemy na samej górze klasy**, przed polami, właściwościami i metodami.
- Dalej, w tej kolejności: pola serializowane, pozostałe pola prywatne, właściwości, metody.
- **Stany podglądane w Inspectorze** to pola i właściwości, które zmieniają się w trakcie gry i chcemy je widzieć do debugowania (np. `isMoving`, `shotTime`).
  - Pola stanu oznaczamy `[SerializeField]`, choć ich zapis w scenie nie jest potrzebny. **Nie używamy `[ShowNonSerializedField]`**, bo źle się wyświetla.
  - Właściwości stanu oznaczamy `[ShowNativeProperty]`.
  - Grupujemy je w jednym bloku po pozostałych polach prywatnych, z nagłówkiem `[Header("States")]` nad pierwszym polem. `[Header]` działa tylko na polach, więc gdy blok zaczyna się od właściwości, użyj komentarza `// States`.

```csharp
public class ModeController : MonoBehaviour
{
    /// <summary>Raised after the mode changed; the argument is the new mode.</summary>
    public event Action<FilmMode> ModeChanged;

    [SerializeField]
    private InputActionReference toggleAction;

    [Header("States")]
    [SerializeField]
    private int lastTickSecond;

    [ShowNativeProperty]
    public FilmMode Mode { get; private set; }

    private void Awake() { ... }
}
```

### Input

Używamy **nowego Input Systemu**. Komponenty przyjmują akcje przez `[SerializeField] InputActionReference` (zgodnie z regułą atrybutu powyżej pola). Nie używaj starego `UnityEngine.Input`.
