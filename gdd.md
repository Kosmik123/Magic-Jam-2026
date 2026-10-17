# GDD – „Cięcie!” (robocza nazwa)

Game Jam 24h, temat: **Beyond Reality**
Silnik: Unity (URP, nowy Input System), C#
Zespół: 1 osoba + Claude Code

---

## 1. Koncept w jednym zdaniu

Jesteś operatorem kamery na planie filmowym. Nie sterujesz aktorem – to, jak ustawisz kamerę (plan filmowy, kadr, perspektywa), decyduje o tym, co aktor zrobi i co w filmie stanie się prawdą.

## 2. Fabuła i cel gry

- Kręcimy komedię o zwykłym poranku: aktor budzi się i jedzie do pracy.
- Producent chce, żeby wyszło epicko, więc każda scena ma scenariusz do odhaczenia.
- Przez prawie całą grę film i plan zdjęciowy działają normalnie. Jedyne sygnały, że coś jest nie tak, są drobne i łatwe do przeoczenia (np. odbicie ekipy w lustrze w poziomie 2).
- **Finał (koniec poziomu 6):** po ostatnim ujęciu aktor nie wychodzi z roli. Ignoruje przerwę, patrzy w kamerę i odchodzi z planu w głąb studia. Gracz może tylko za nim podążyć. Interpretację zostawiamy graczowi.

## 3. Sterowanie

| Klawisz | Akcja |
|---|---|
| WASD | ruch kamery w obrębie pokoju |
| Mysz | obrót kamery |
| Q / E | kamera w dół / w górę (kąty: żabia, ptasia) |
| Scroll | zoom (zmiana planu) |
| Spacja | przełącz: **Akcja!** ↔ **Cięcie!** |
| R | dubel (reset ujęcia) |
| Tab | wysuń / schowaj scenariusz |
| Esc | pauza / menu |

Kamera ma ograniczenia ruchu w obrębie pokoju (bounding box, bez przenikania przez ściany).

## 4. Pętla rozgrywki

1. Gracz czyta scenariusz (panel z lewej).
2. W trybie **Cięcie** ustawia kamerę, obserwuje plan zdjęciowy, słucha plotek.
3. Wciska **Akcja!** – aktor gra rolę, reagując na to, co i jak jest w kadrze.
4. Gracz może ruszać kamerą w trakcie akcji (zmiana planu w czasie rzeczywistym).
5. Zdarzenia ze scenariusza nagrane w kadrze zostają odhaczone.
6. Błąd: **R** (dubel) albo **Cięcie** i nowe podejście.
7. Wszystkie punkty scenariusza odhaczone → ekran wyniku → następny poziom.

## 5. Tryby

### 5.1 Akcja (nagrywanie)
- Na ekranie: wizjer, czerwona kropka REC, czarne pasy, wskaźnik planu.
- Aktorzy grają postacie: reagują według systemu planów (sekcja 6).
- Zdarzenie liczy się jako nagrane tylko wtedy, gdy zaszło w trakcie akcji i było w kadrze.

### 5.2 Cięcie (przerwa)
- Bez UI wizjera, światło planu przygaszone, słychać ekipę.
- Aktorzy zachowują się jak aktorzy: piją kawę, jedzą, plotkują, poprawiają makijaż.
- Ekipa (NPC techniczni) wykonuje deterministyczny harmonogram, np. technik odsuwa wózek po 5 s przerwy.
- Zmiany dokonane przez ekipę **zostają** po powrocie do akcji (to element zagadek).
- Plotki to dymki tekstowe z podpowiedziami do zagadek.

### 5.3 Przejścia
- **Cięcie → Akcja:** aktorzy wracają na pozycję i pozę z momentu cięcia (ciągłość), potem grają dalej.
- **R (dubel):** pełny reset do stanu z początku ujęcia (snapshot), łącznie z rekwizytami i zmianami ekipy. Licznik dubli +1.

## 6. System planów filmowych (rdzeń)

Plan jest liczony automatycznie co klatkę na podstawie kadru, względem **aktywnego aktora**.

Wszystkie mechaniki gry opierają się wyłącznie na pracy kamery i planach filmowych – nie ma innych systemów sterowania aktorem.

| Plan | Warunek w kadrze | Akcja aktora |
|---|---|---|
| Ogólny | aktor mały w kadrze (< 30% wysokości) | idzie do obiektu / punktu nawigacyjnego w centrum kadru (eksploracja, przemieszczanie) |
| Pełny | widać całą postać | akcja fizyczna: wspinaczka, pchnięcie, skok |
| Amerykański | widać od kolan w górę | użycie trzymanego przedmiotu na obiekcie w kadrze |
| Średni | widać od pasa w górę | rozmowa z postacią / zbadanie obiektu |
| Detal | obiekt interaktywny > 40% kadru | aktor podnosi przedmiot (jeśli w zasięgu, inaczej podchodzi) |

**Implementacja:**
- Aktor ma punkty kontrolne: `Head`, `Waist`, `Knees`, `Feet`.
- `Camera.WorldToViewportPoint` + raycast na zasłonięcie → które punkty są widoczne → plan.
- **Obiekt w fokusie** = obiekt interaktywny najbliżej środka kadru, widoczny.
- Akcja odpala się, gdy plan i obiekt w fokusie są stabilne przez ~0.5 s (żeby nie wywoływać akcji przy przypadkowym ruchu kamery).
- Wskaźnik planu w rogu wizjera: nazwa planu + ikona akcji, która się wykona.
- Progi trzymać w jednym `ScriptableObject` (`ShotConfig`) do tuningu.

**Plan ogólny = nawigacja:**
- Plan ogólny to podstawowe narzędzie eksploracji i poruszania aktora po pomieszczeniach – używany najczęściej, zwłaszcza na początku poziomów.
- Gracz „prowadzi” aktora, kadrując szeroko z celem (obiekt, drzwi, miejsce) w centrum kadru; aktor idzie tam przez NavMesh.
- Cele ruchu to nie tylko obiekty interaktywne: także punkty nawigacyjne (drzwi, przejścia, znaczniki na podłodze), żeby można było przemieszczać aktora między strefami pokoju.
- Pozostałe plany (pełny, amerykański, średni, detal) służą do interakcji, gdy aktor jest już na miejscu.

**Kąty (późniejsze poziomy, opcjonalne):**
- Żabia perspektywa (kamera nisko, patrzy w górę) – postać w kadrze „silna”.
- Ptasia perspektywa (kamera wysoko, patrzy w dół) – postać „słaba”, np. pies się boi.

## 7. Aktor – AI

Maszyna stanów:
- `Idle` – stoi na znaczniku, gra niewielką animację.
- `MoveTo` – NavMeshAgent do celu.
- `Interact` – wykonuje akcję zależną od planu (sekcja 6).
- `Flee` – ucieka od zagrożenia w kadrze (priorytet najwyższy).
- `OffDuty` – tryb przerwy: deterministyczna lista zachowań (kawa, plotki, catering).
- `Fail` – śmieszna porażka (potknięcie, zdziwienie), ujęcie nieudane.

Priorytet w akcji: `Flee` > akcja planu na obiekcie w fokusie > `Idle`.

**Wymóg:** całe zachowanie deterministyczne (brak `Random` bez stałego seeda), żeby dubel dawał ten sam wynik przy tym samym kadrowaniu.

## 8. Obiekty i interakcje

Komponent `Interactable` z typem:
- `Pickup` – przedmiot do podniesienia (detal).
- `Usable` – obiekt, na którym używa się przedmiotu (amerykański), z polem `requiredItem`.
- `Talkable` – NPC lub obiekt do rozmowy/zbadania (średni), z listą kwestii.
- `Physical` – do wspinaczki/pchnięcia (pełny).
- `Threat` – zagrożenie wywołujące ucieczkę.

Komponenty dodatkowe:
- `NavPoint` – nieinteraktywny cel ruchu dla planu ogólnego (drzwi, przejście, znacznik na podłodze).
- `FilmOnly` – obiekt istnieje (kolizja, zachowanie) tylko wtedy, gdy jest w kadrze w trakcie akcji. Używać oszczędnie, tylko do glitchy i zagrożeń.
- `CrewSchedule` – lista zdarzeń ekipy w przerwie (czas od początku przerwy → akcja).

## 9. Zasady „Beyond Reality”

- To, co pokazuje kamera, decyduje o tym, co aktor robi (plany filmowe).
- Plan zdjęciowy (przerwa) i film (akcja) to dwie warstwy rzeczywistości, które na siebie wpływają.
- Warstwy mieszają się dopiero w finale: aktor przestaje respektować granicę między filmem a planem.
- Ingerencja w świat poza filmem ma być subtelna, bez efektów specjalnych i rozpadania się dekoracji.

## 10. Poziomy

Każdy poziom = jeden pokój/lokacja. Dane w `ScriptableObject` (`LevelData`): scena, lista punktów scenariusza, startowe pozycje, harmonogram ekipy, plotki.

### Poziom 1 – Pobudka (tutorial)
- **Uczy:** ruch kamery, Akcja/Cięcie, plan ogólny.
- **Scenariusz:** aktor wstaje z łóżka. Aktor idzie do okna i odsłania zasłony.
- **Rozwiązanie:** plan ogólny z oknem w centrum kadru.

### Poziom 2 – Łazienka
- **Uczy:** detal, średni.
- **Scenariusz:** aktor bierze szczoteczkę. Aktor przegląda się w lustrze.
- **Rozwiązanie:** detal na szczoteczkę → podnosi. Średni z lustrem → bada odbicie.
- **Smaczek:** w lustrze widać ekipę (zapowiedź twistu).

### Poziom 3 – Śniadanie
- **Uczy:** amerykański, rozmowa z NPC, plotki w przerwie.
- **Scenariusz:** aktor rozmawia ze współlokatorem. Aktor nalewa kawę. Aktor podaje kawę współlokatorowi.
- **Rozwiązanie:** średni (rozmowa – współlokator prosi o kawę) → detal na dzbanek → amerykański na współlokatora.
- **Przerwa:** plotka podpowiada, co lubi współlokator.

### Poziom 4 – Klatka schodowa (początek glitchy)
- **Uczy:** harmonogram ekipy, ciemność.
- **Przeszkoda:** wózek cateringowy blokuje schody + ciemny korytarz.
- **Rozwiązanie:** ogłosić cięcie, odczekać, aż technik odjedzie wózkiem → akcja. Detal na włącznik → aktor zapala światło → ogólny na wyjście.
- **Glitch:** aktor raz patrzy w kamerę i mówi kwestię spoza scenariusza.

### Poziom 5 – Przystanek
- **Uczy:** plan pełny (akcja fizyczna).
- **Scenariusz:** aktor dociera na przystanek. Aktor wsiada do autobusu.
- **Przeszkoda:** [DO UZUPEŁNIENIA – rozwiązanie przeszkody blokującej drogę do przystanku]
- **Brak glitchy.** Poziom ma być w pełni „normalny”, żeby finał był zaskoczeniem.

### Poziom 6 – Biuro (finał)
- **Uczy:** nic nowego – sprawdzian wszystkich planów.
- **Scenariusz:** aktor wchodzi do biura. Aktor wita się z szefem. Aktor oddaje raport. Aktor siada przy biurku.
- **Zagadka:** połączenie mechanik:
  - średni na szefa (rozmowa – szef pyta o raport),
  - przerwa: ekipa w harmonogramie przynosi zapomniany rekwizyt (teczkę) na plan,
  - detal na teczkę → amerykański na szefa (oddanie raportu),
  - ogólny na biurko → aktor siada.
- **Finał (po odhaczeniu ostatniego punktu):**
  1. Gracz wciska Cięcie. Ekipa przechodzi w tryb przerwy, ale aktor nie.
  2. Aktor wstaje, patrzy prosto w kamerę (1–2 s ciszy).
  3. Aktor odchodzi poza dekoracje biura, przez szparę między ściankami, w ciemne studio.
  4. Panel scenariusza pokazuje jeden nowy punkt bez checkboxa: „…”.
  5. Gracz może podążać kamerą. Aktor przechodzi przez studio i wychodzi drzwiami ewakuacyjnymi na światło.
  6. Drzwi się zamykają. Czerń. Napisy końcowe.
- **Zasada:** zero efektów glitch (bez migotania, zniekształceń, rozpadu świata). Niepokój budują tylko zachowanie aktora i cisza.
## 11. UI

- **Panel scenariusza (lewa strona, Tab):** stylizowana kartka maszynopisu, punkty z checkboxami, odhaczanie z animacją.
- **Wizjer (tryb akcji):** REC, czarne pasy, wskaźnik planu + ikona akcji, licznik czasu ujęcia.
- **Licznik dubli** (prawy górny róg).
- **Klaps filmowy** – animacja przy przejściu Akcja/Cięcie.
- **Ekran wyniku:** liczba dubli, gwiazdki (1–3) według progów z `LevelData`.
- **Menu:** start, wybór poziomu, wyjście.

## 12. Assety

**3D (low-poly, darmowe):**
- Kenney (meble, postacie, rekwizyty), animacje z Mixamo.
- Aktor + współlokator + technik + 1–2 członków ekipy.
- Lokacje: sypialnia, łazienka, kuchnia, klatka, przystanek, biuro, studio.
- Elementy studia: rusztowania, lampy, kable, krzesła reżyserskie, catering.

**Animacje aktora:** idle, chód, podnoszenie, użycie przedmiotu, rozmowa, wspinaczka/pchnięcie, porażka, picie kawy, gadanie (off-duty).

**Dźwięk:** klaps, „Akcja!”/„Cięcie!” (nagrane samodzielnie), kroki, podniesienie, porażka (puzon), gwar ekipy w przerwie, 2 podkłady (spokojny do przerwy, filmowy do akcji).

## 13. Architektura (Unity)

```
Assets/
  Scripts/
    Core/        GameManager, LevelManager, ModeController (Action/Cut), SnapshotSystem
    Camera/      OperatorCamera (ruch, ograniczenia), ShotDetector (plan + fokus)
    Actors/      ActorController (FSM), ActorCheckpoints, OffDutyBehaviour
    Crew/        CrewSchedule, CrewMember
    Interaction/ Interactable, NavPoint, FilmOnly
    Script/      ScriptPanel, ScriptBeat, BeatTracker (nagrane w kadrze?)
    UI/          ViewfinderUI, ResultsScreen, MainMenu
  Data/          ShotConfig.asset, Levels/*.asset (LevelData)
  Scenes/        MainMenu, Level01..Level06
```

**Kluczowe zasady:**
- `ModeController` emituje eventy `OnActionStart`, `OnCut`, `OnRetake`. Systemy subskrybują, nie odpytują.
- `SnapshotSystem` zapisuje stan (pozycje, rotacje, stany obiektów, ekwipunek aktora) na starcie ujęcia, przywraca przy R.
- `ShotDetector` to jedyne miejsce liczące plan – reszta czyta jego wynik.
- Poziomy data-driven: nowy poziom = nowa scena + `LevelData`, bez zmian w kodzie.

## 14. Priorytety (MVP → dodatki)

**MVP (musi działać):**
1. Kamera operatora z ograniczeniami.
2. Akcja/Cięcie/Dubel + snapshot.
3. ShotDetector: ogólny, amerykański, średni, detal.
4. Aktor FSM + Interactable (Pickup, Usable, Talkable).
5. Panel scenariusza + BeatTracker.
6. Poziomy 1–3.

**Druga faza:**
7. Tryb przerwy (off-duty + harmonogram ekipy + plotki).
8. Plan pełny + rozwiązanie przeszkody z poziomu 5.
9. Poziomy 4–6 finał.

**Polish (jeśli zostanie czas):**
10. Kąty kamery, FilmOnly, ekran wyniku z gwiazdkami, dźwięk i animacje przejść.

## 15. Instrukcje dla Claude Code

- Trzymaj się tego dokumentu. Nie dodawaj funkcji spoza sekcji 14 bez pytania.
- Realizuj zadania w kolejności priorytetów z sekcji 14.
- Kod po angielsku (nazwy, komentarze), teksty w grze po polsku.
- Każdy system ma działać w izolacji i być testowalny w osobnej scenie testowej.
- Wartości do tuningu wyciągaj do `ScriptableObject` lub `[SerializeField]`, nie hardcoduj.
- Zachowanie deterministyczne – żadnej losowości bez stałego seeda.
- Po każdym większym kroku opisz, co trzeba ustawić ręcznie w edytorze Unity (komponenty, referencje, warstwy, NavMesh).
- Czas jest krytyczny (24h): wybieraj najprostsze działające rozwiązanie, refaktor tylko gdy blokuje dalszą pracę.