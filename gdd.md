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
- **Cięcie → Akcja:** aktorzy wracają na pozycję i pozę z momentu cięcia (ciągłość), potem grają dalej. Pamięć aktora zostaje bez zmian (kadrowanie w czasie Cięcia jej nie zmienia).
- **R (dubel):** powrót do stanu z początku ujęcia (snapshot), łącznie z rekwizytami, zmianami ekipy, ekwipunkiem i pamięcią aktora. Licznik dubli +1.
  - Dubel przełącza w tryb Cięcia. Aktorzy **wracają na pozycje startowe pieszo, po NavMeshu** – bez teleportacji, jak na prawdziwym planie filmowym.
  - Po dotarciu na znacznik aktor przyjmuje pozę startową i sygnalizuje gotowość (np. stoi w pozie idle ujęcia).
  - Gotowość wszystkich aktorów = ujęcie w pełni zresetowane.
- **Potencjalna mechanika – falstart:** gracz może wcisnąć Akcję, zanim aktor wróci na swoją pozycję. Aktor zaczyna grać z miejsca, w którym akurat jest. Pozwala to celowo „ustawić” aktora w innym punkcie startowym. Do decyzji przy projektowaniu poziomów (kandydat do syntezy w poziomie 5).

## 6. System planów filmowych (rdzeń)

Plan jest liczony automatycznie co klatkę na podstawie kadru, względem **aktywnego aktora**.

Wszystkie mechaniki gry opierają się wyłącznie na pracy kamery i planach filmowych – nie ma innych systemów sterowania aktorem.

**Zasada filmowa:** jak w montażu – najpierw zbliżenie pokazuje widzowi obiekt, zaraz potem aktor wchodzi z nim w interakcję. Zbliżenie zapisuje obiekt w pamięci aktora (sekcja 7.2), a szersze plany decydują, co aktor z tą pamięcią zrobi.

| Plan | Warunek w kadrze | Skutek |
|---|---|---|
| Zbliżenie na obiekt (detal) | w kadrze tylko jeden obiekt interaktywny (> 40% kadru) | obiekt trafia do **pamięci aktora** (nadpisuje poprzedni); brak akcji |
| Zbliżenie na twarz aktora | twarz aktora dominuje w kadrze | **pamięć aktora się czyści** – aktor „myśli o sobie”, zapomina o obiektach |
| Średni / amerykański | aktor (od pasa / od kolan w górę) + obiekt w kadrze | **interakcja** (sekcja 8) |
| Pełny | widać całą postać | akcja fizyczna: wspinaczka, pchnięcie, skok (druga faza) |
| Ogólny / szerszy | aktor mały w kadrze (< 30% wysokości) | **ruch**: aktor tylko podchodzi do zapamiętanego obiektu, bez interakcji |

**Plan szerszy = nawigacja:**
- Aktor idzie do zapamiętanego obiektu tylko wtedy, gdy **obiekt i aktor są w kadrze**. Obiekty poza kadrem istnieją, ale nie są dla aktora istotne.
- Kadr wyznacza obszar ruchu: plan na cały pokój – aktor może przejść przez cały pokój; węższy plan – aktor porusza się tylko w obrębie kadru.
- Po dojściu aktor staje przy obiekcie. Żeby wykonał interakcję, trzeba zawęzić plan co najmniej do średniego/amerykańskiego.
- Bez obiektu w pamięci (lub gdy jest poza kadrem) aktor stoi.

**Implementacja:**
- Aktor ma punkty kontrolne: `Head`, `Waist`, `Knees`, `Feet`.
- `Camera.WorldToViewportPoint` + raycast na zasłonięcie → które punkty są widoczne → plan.
- **Obiekt w fokusie** = obiekt interaktywny najbliżej środka kadru, widoczny.
- Skutek planu (zapis do pamięci, czyszczenie, interakcja) odpala się, gdy plan i obiekt w fokusie są stabilne przez ~0.5 s (żeby nie reagować na przypadkowy ruch kamery).
- Wskaźnik planu w rogu wizjera: nazwa planu + ikona akcji, która się wykona.
- Progi trzymać w jednym `ScriptableObject` (`ShotConfig`) do tuningu.

**Kąty (późniejsze poziomy, opcjonalne):**
- Żabia perspektywa (kamera nisko, patrzy w górę) – postać w kadrze „silna”.
- Ptasia perspektywa (kamera wysoko, patrzy w dół) – postać „słaba”, np. pies się boi.

## 7. Aktor – AI

### 7.1 Warstwy

```
OperatorCamera ─► ShotDetector ─► SetDirector ──zapisuje──► ActorKnowledge (pamięć głównego aktora)
                                      │                           │ czyta
                                      │                           ▼
                                      │                      ActorBrain (decyzje głównego aktora)
                                      │                           │ komendy
                                      ▼                           ▼
                        ActorController ekipy          ActorController głównego aktora
```

- **`SetDirector`** – nadrzędny system planu filmowego. Czyta wynik `ShotDetector`, prowadzi cykl Akcja/Cięcie/Dubel i wydaje komendy wszystkim aktorom. Aktorami są też postacie pomocnicze i ekipa (obsługa planu).
- **`ActorKnowledge`** – centralne źródło danych o głównym aktorze: obiekt, na którym się skupia (pamięć), i docelowa pozycja. Tylko przechowuje stan i emituje zdarzenia o jego zmianach. Zapisuje go `SetDirector`, nie kamera.
- **`Inventory`** – co aktor ma przy sobie. **Osobna struktura niż pamięć**: pamięć = na czym aktor się skupia, ekwipunek = czego może użyć.
- **`ActorBrain`** – zamienia pamięć + bieżący plan na komendy (`GoTo`, `Interact`).
- **`ActorController`** – wykonawca (ruch, interakcje, animacje), bez własnej inteligencji. Używany przez głównego aktora i ekipę; ekipa dostaje komendy bezpośrednio od `SetDirector`.

### 7.2 Pamięć aktora

| Zdarzenie | Pamięć |
|---|---|
| Zbliżenie na obiekt (stabilne ~0.5 s) w trakcie akcji | zapisuje obiekt, **nadpisuje poprzedni** |
| Zbliżenie na twarz aktora | **czyści** |
| **Każda interakcja** (z zapamiętanym obiektem czy innym, udana czy nie) | **czyści** – zbliżenie ma sens tylko tuż przed interakcją |
| Brak drogi do obiektu | **czyści** |
| Kadrowanie w trybie Cięcia | bez zmian – to nie dzieje się w filmie |
| Cięcie → Akcja | bez zmian – aktor pamięta obiekt poza ujęciem |
| Dubel | przywracana ze snapshotu z początku ujęcia (aktor zapomina wszystko z nieudanego ujęcia) |

### 7.3 Maszyna stanów

- `Idle` – stoi na znaczniku, gra niewielką animację.
- `MoveTo` – NavMeshAgent do celu.
- `Interact` – wykonuje interakcję (sekcja 8).
- `OffDuty` – tryb przerwy: deterministyczna lista zachowań (kawa, plotki, catering).
- `ReturnToMark` – po dublu idzie po NavMeshu na pozycję startową ujęcia, potem przyjmuje pozę startową.
- `Fail` – śmieszna porażka (sekcja 7.4).
- `Flee` – ucieczka od zagrożenia. **Odłożone na później** – na razie aktor tylko celowo dąży do obiektów.

Priorytet w akcji: interakcja z planu średniego/amerykańskiego > ruch do zapamiętanego obiektu > `Idle`.

### 7.4 Porażki

| Sytuacja | Zachowanie | Pamięć | Ekwipunek |
|---|---|---|---|
| Brak drogi (np. wózek blokuje schody) | podchodzi jak najbliżej, staje, zdziwiona mina | czyści (o przeszkodzie też nie pamięta) | bez zmian |
| Ciemność (wejście w ciemny korytarz) | potyka się i upada, leży do dubla | bez znaczenia – dubel przywraca snapshot | bez znaczenia |
| Zły przedmiot (np. szczoteczka na współlokatora) | próbuje go użyć, potem chowa przedmiot do kieszeni | czyści (jak każda interakcja) | przedmiot **zostaje** |
| Za wcześnie w scenariuszu (np. raport, zanim szef o niego poprosił) | interakcja się odbywa, obiekt odmawia („jeszcze nie potrzebuję”) | czyści (jak każda interakcja) | przedmiot zostaje |

**Wymóg:** całe zachowanie deterministyczne (brak `Random` bez stałego seeda), żeby dubel dawał ten sam wynik przy tym samym kadrowaniu.

## 8. Obiekty i interakcje

**Interakcja** odpala się w planie średnim/amerykańskim, gdy aktor i obiekt są w kadrze:
- Obiekt z pamięci w kadrze → interakcja z nim według typu obiektu (klamka → otwiera drzwi, włącznik → naciska, NPC → rozmawia, przedmiot → podnosi).
- Aktor trzyma przedmiot, a w kadrze jest druga postać / obiekt → używa przedmiotu na nim.
- Wynik zależy od: obiektu, przedmiotu w ręce i stanu scenariusza (obiekt może odmówić).
- Po każdej interakcji pamięć się czyści (sekcja 7.2).

Komponent `Interactable` z typem:
- `Pickup` – przedmiot do podniesienia (trafia do ekwipunku).
- `Usable` – obiekt do użycia (klamka, włącznik) lub do użycia na nim przedmiotu, z polem `requiredItem`.
- `Talkable` – NPC lub obiekt do rozmowy/zbadania, z listą kwestii.
- `Physical` – do wspinaczki/pchnięcia (plan pełny, druga faza).
- `Threat` – zagrożenie wywołujące ucieczkę (odłożone na później).

Komponenty dodatkowe:
- `NavPoint` – nieinteraktywny cel ruchu (drzwi, przejście, znacznik na podłodze).
- `FilmOnly` – obiekt istnieje (kolizja, zachowanie) tylko wtedy, gdy jest w kadrze w trakcie akcji. Używać oszczędnie, tylko do glitchy i zagrożeń.
- `CrewSchedule` – lista zdarzeń ekipy w przerwie (czas od początku przerwy → akcja).

### 8.1 Do decyzji

- Czy `NavPoint` zapamiętuje się zbliżeniem jak obiekt interaktywny, czy działa inaczej?
- Kryteria zbliżenia na twarz aktora (progi w `ShotConfig`).
- Zagrożenia i ucieczka (`Threat`, `Flee`).

## 9. Zasady „Beyond Reality”

- To, co pokazuje kamera, decyduje o tym, co aktor robi (plany filmowe).
- Plan zdjęciowy (przerwa) i film (akcja) to dwie warstwy rzeczywistości, które na siebie wpływają.
- Warstwy mieszają się dopiero w finale: aktor przestaje respektować granicę między filmem a planem.
- Ingerencja w świat poza filmem ma być subtelna, bez efektów specjalnych i rozpadania się dekoracji.

## 10. Poziomy

Każdy poziom = jeden pokój/lokacja. Dane w `ScriptableObject` (`LevelData`): scena, lista punktów scenariusza, startowe pozycje, harmonogram ekipy, plotki.

### Poziom 1 – Pobudka (tutorial)
- **Uczy:** ruch kamery, Akcja/Cięcie, zbliżenie → plan ogólny → plan średni.
- **Scenariusz:** aktor wstaje z łóżka. Aktor idzie do okna i odsłania zasłony.
- **Rozwiązanie:** zbliżenie na zasłony (aktor zapamiętuje) → ogólny z aktorem i oknem (aktor podchodzi) → średni (odsłania zasłony).

### Poziom 2 – Łazienka
- **Uczy:** pamięć aktora (zbliżenie nadpisuje poprzednie), interakcja w planie średnim.
- **Scenariusz:** aktor bierze szczoteczkę. Aktor przegląda się w lustrze.
- **Rozwiązanie:** zbliżenie na szczoteczkę → średni z aktorem i szczoteczką → podnosi. Zbliżenie na lustro → średni z lustrem → bada odbicie.
- **Smaczek:** w lustrze widać ekipę (zapowiedź twistu).

### Poziom 3 – Śniadanie
- **Uczy:** użycie przedmiotu z ekwipunku (amerykański), rozmowa z NPC, plotki w przerwie.
- **Scenariusz:** aktor rozmawia ze współlokatorem. Aktor nalewa kawę. Aktor podaje kawę współlokatorowi.
- **Rozwiązanie:** zbliżenie na współlokatora → średni (rozmowa – współlokator prosi o kawę) → zbliżenie na dzbanek → średni (aktor nalewa kawę) → amerykański z aktorem i współlokatorem (podaje kawę).
- **Śmieszna porażka:** amerykański na współlokatora ze szczoteczką w ręce → aktor próbuje umyć mu zęby i chowa szczoteczkę do kieszeni.
- **Przerwa:** plotka podpowiada, co lubi współlokator.

### Poziom 4 – Klatka schodowa (początek glitchy)
- **Uczy:** harmonogram ekipy, ciemność.
- **Przeszkoda:** wózek cateringowy blokuje schody + ciemny korytarz.
- **Rozwiązanie:** ogłosić cięcie, odczekać, aż technik odjedzie wózkiem → akcja. Zbliżenie na włącznik → średni (aktor zapala światło) → zbliżenie na wyjście → ogólny z aktorem i wyjściem (aktor idzie).
- **Porażki:** wózek na drodze → aktor podchodzi, staje zdziwiony i zapomina cel. Wejście w ciemny korytarz → aktor się przewraca, potrzebny dubel.
- **Glitch:** aktor raz patrzy w kamerę i mówi kwestię spoza scenariusza.

### Poziom 5 – Przystanek
- **Uczy:** plan pełny (akcja fizyczna).
- **Scenariusz:** aktor dociera na przystanek. Aktor wsiada do autobusu.
- **Przeszkoda:** [DO WYMYŚLENIA PÓŹNIEJ – zagadka będzie syntezą różnych mechanik z wcześniejszych poziomów]
- **Brak glitchy.** Poziom ma być w pełni „normalny”, żeby finał był zaskoczeniem.

### Poziom 6 – Biuro (finał)
- **Uczy:** nic nowego – sprawdzian wszystkich planów.
- **Scenariusz:** aktor wchodzi do biura. Aktor wita się z szefem. Aktor oddaje raport. Aktor siada przy biurku.
- **Zagadka:** połączenie mechanik:
  - zbliżenie na szefa → średni (rozmowa – szef pyta o raport),
  - przerwa: ekipa w harmonogramie przynosi zapomniany rekwizyt (teczkę) na plan,
  - zbliżenie na teczkę → średni (aktor podnosi) → amerykański z aktorem i szefem (oddanie raportu); oddanie, zanim szef zapyta → szef odmawia,
  - zbliżenie na biurko → ogólny (aktor podchodzi) → średni (aktor siada).
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
- **Wskaźnik pamięci aktora:** mała ikona / podświetlenie zapamiętanego obiektu, żeby gracz wiedział, o czym aktor „myśli” i że interakcja lub zbliżenie na twarz to wyczyści.
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
  _Project/            pliki gry (zewnętrzne biblioteki bezpośrednio w Assets/)
    Scripts/
      Core/        GameManager, LevelManager, ModeController (Action/Cut), SnapshotSystem
      Director/    SetDirector (nadrzędny system planu: kamera → komendy dla aktorów)
      Camera/      OperatorCamera (ruch, ograniczenia), ShotDetector (plan + fokus)
      Actors/      ActorController (FSM), ActorKnowledge (pamięć), ActorBrain, Inventory,
                   ActorCheckpoints, OffDutyBehaviour
      Crew/        CrewSchedule, CrewMember
      Interaction/ Interactable, NavPoint, FilmOnly
      Script/      ScriptPanel, ScriptBeat, BeatTracker (nagrane w kadrze?)
      UI/          ViewfinderUI, ResultsScreen, MainMenu
    Data/          ShotConfig.asset, Levels/*.asset (LevelData)
    Scenes/        MainMenu, Level01..Level06
```

**Kluczowe zasady:**
- `ModeController` emituje eventy `OnActionStart`, `OnCut`, `OnRetake`. Systemy subskrybują, nie odpytują.
- `SnapshotSystem` zapisuje stan (pozycje, rotacje, stany obiektów, ekwipunek i pamięć aktora) na starcie ujęcia, przywraca przy R. Pozycji aktorów nie ustawia bezpośrednio – zleca im powrót po NavMeshu (`MoveTo` na pozycję ze snapshotu).
- `ShotDetector` to jedyne miejsce liczące plan – reszta czyta jego wynik.
- `SetDirector` to jedyne miejsce zamieniające plan na komendy i zapisy do `ActorKnowledge`. Kamera nie zna aktorów, `ActorController` nie zna kamery.
- Poziomy data-driven: nowy poziom = nowa scena + `LevelData`, bez zmian w kodzie.

## 14. Priorytety (MVP → dodatki)

**MVP (musi działać):**
1. Kamera operatora z ograniczeniami.
2. Akcja/Cięcie/Dubel + snapshot.
3. ShotDetector: ogólny, amerykański, średni, zbliżenie na obiekt, zbliżenie na twarz aktora.
4. Aktor FSM + SetDirector + ActorKnowledge/ActorBrain/Inventory + Interactable (Pickup, Usable, Talkable).
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