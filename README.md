# Attractions – System Rezerwacji Atrakcji, Ofert Grupowych i Weryfikacji Kompatybilności (REST API + Web SPA)

Aplikacja napisana w języku **C# (.NET 10 / ASP.NET Core)** z wykorzystaniem programowania obiektowego (**OOP**, zasady **SOLID**, inicjalizacja modeli przez konstruktory) oraz zapytań **LINQ**. System udostępnia kompletne **REST API** oraz nowoczesny **interfejs webowy (Single-Page Application)** dla klientów i organizatorów, a także wspiera klasyczny tryb konsolowy (`--console`).

Projekt obsługuje atrakcje turystyczne Krakowa, Wzgórza Wawelskiego i Małopolski, dynamiczny słownik cech zapisany w pliku JSON (`Features.json`), weryfikację kompatybilności ofert i całych pakietów względem profilu uczestnika/grupy (`ParticipantProfile`), limity pojemności i oferty ściśle grupowe (np. 4-osobowe) oraz wielopoziomowy kalkulator zniżek biletowych.

---

## Spis treści

1. [Wymagania systemowe i narzędziowe](#1-wymagania-systemowe-i-narzędziowe)
2. [Poradnik: Pobieranie z GitHuba i uruchomienie projektu](#2-poradnik-pobieranie-z-githuba-i-uruchomienie-projektu)
3. [Architektura i struktura projektu](#3-architektura-i-struktura-projektu)
4. [Główne funkcjonalności systemu](#4-główne-funkcjonalności-systemu)
5. [Dynamiczny system cech JSON i weryfikacja kompatybilności](#5-dynamiczny-system-cech-json-i-weryfikacja-kompatybilności)
6. [Grupy, pojemność atrakcji i system zniżek](#6-grupy-pojemność-atrakcji-i-system-zniżek)
7. [Dokumentacja endpointów REST API](#7-dokumentacja-endpointów-rest-api)

---

## 1. Wymagania systemowe i narzędziowe

Aby pobrać, skompilować i uruchomić projekt na swoim komputerze, potrzebujesz:

- **System operacyjny:** Windows 10/11, macOS lub Linux.
- **.NET 10 SDK** (wersja `10.0.x` lub nowsza) – do pobrania z oficjalnej strony: [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).
- **Git** (opcjonalnie, do sklonowania repozytorium z wiersza poleceń): [https://git-scm.com/downloads](https://git-scm.com/downloads).
- **Przeglądarka internetowa:** Chrome, Edge, Firefox lub Safari (obsługująca ES6 Modules).
- **IDE (opcjonalnie do edycji kodu):** Visual Studio 2022/2026 (z pakietem *ASP.NET i tworzenie aplikacji internetowych*), Cursor lub Visual Studio Code.

> **Brak zewnętrznej bazy danych:** Projekt nie wymaga instalowania SQL Servera, PostgreSQL ani Dockera. Słownik cech i przypisania cech zapisywane są automatycznie w plikach JSON (`Attractions/Data/Features.json` oraz `Attractions/Data/FeatureAssignments.json`), natomiast kolekcje obiektów i zamówień sesyjnych utrzymywane są w pamięci operacyjnej.

Sprawdzenie poprawności instalacji pakietu `.NET SDK` w terminalu (PowerShell / CMD / Bash):

```powershell
dotnet --version
```
*(Polecenie powinno zwrócić wersję zaczynającą się od `10.0`, np. `10.0.402`).*

---

## 2. Poradnik: Pobieranie z GitHuba i uruchomienie projektu

### Krok 1: Pobranie projektu z GitHuba

#### Sposób A – za pomocą programu Git (zalecany)
Otwórz terminal (**PowerShell**, **CMD** lub **Git Bash**), przejdź do wybranego katalogu (np. na Pulpit) i wykonaj polecenia:

```powershell
git clone https://github.com/Rghjr/PB.git
cd PB
```

#### Sposób B – pobranie paczki ZIP (bez programu Git)
1. Wejdź na stronę repozytorium w przeglądarce: [https://github.com/Rghjr/PB](https://github.com/Rghjr/PB).
2. Kliknij zielony przycisk **`<> Code`**, a następnie wybierz **`Download ZIP`**.
3. Rozpakuj pobrane archiwum `.zip` do dowolnego folderu na dysku.
4. Otwórz terminal wewnątrz rozpakowanego folderu (w systemie Windows: kliknij prawym przyciskiem myszy w folderze i wybierz **„Otwórz w terminalu”**).

---

### Krok 2: Kompilacja i uruchomienie aplikacji Web + REST API

#### Opcja 1 – Uruchomienie z terminala (wiersza poleceń)

Będąc w głównym katalogu projektu (tam, gdzie znajduje się plik `Projekt Biznesowy.slnx`), wykonaj:

```powershell
dotnet build
dotnet run --project Attractions
```

Po uruchomieniu w konsoli pojawi się komunikat:
```text
Now listening on: http://localhost:5150
Application started. Press Ctrl+C to shut down.
```

Otwórz w przeglądarce adres:
👉 **[http://localhost:5150](http://localhost:5150)**

Aby zatrzymać serwer, naciśnij w terminalu kombinację klawiszy **`Ctrl + C`**.

#### Opcja 2 – Uruchomienie w Visual Studio lub Cursorze
1. Otwórz plik solucji **`Projekt Biznesowy.slnx`** w programie Visual Studio lub otwórz cały folder w edytorze Cursor / VS Code.
2. Upewnij się, że jako projekt startowy wybrany jest profil **`Attractions.Web`**.
3. Naciśnij klawisz **`F5`** (lub `Ctrl + F5`) – projekt skompiluje się, uruchomi serwer na porcie `5150` i automatycznie otworzy stronę w przeglądarce.

#### Opcja 3 – Uruchomienie w klasycznym trybie konsolowym (CLI)
Aplikacja zachowuje pełną obsługę interaktywnego menu konsolowego. Aby uruchomić wersję terminalową zamiast serwera WWW, dodaj argument `--console`:

```powershell
dotnet run --project Attractions -- --console
```

---

## 3. Architektura i struktura projektu

```text
Projekt Biznesowy/
│
├── Attractions/
│   ├── Controllers/                 # Kontrolery ASP.NET Core REST API
│   │   ├── AttractionsController.cs # Endpointy CRUD i walidacji atrakcji
│   │   ├── OffersController.cs      # Endpointy ofert, dodatków i powiązań N:M
│   │   ├── FeaturesController.cs    # Endpointy słownika cech (Features.json)
│   │   ├── PackagesController.cs    # Ewaluacja pakietów, symulacja zmian i rabatów
│   │   └── OrdersController.cs      # Składanie i podgląd zamówień
│   │
│   ├── Models/                      # Modele domenowe (inicjalizowane przez konstruktory)
│   │   ├── Attraction.cs            # Atrakcja turystyczna + pojemność + ValidateAgainst()
│   │   ├── Offer.cs                 # Oferta/trasa + dziedziczenie cech + limity grup
│   │   ├── Addon.cs                 # Dodatek + modyfikatory cech (Wymóg / Udogodnienie)
│   │   ├── FeatureDefinition.cs     # Definicja cechy ze słownika JSON i logika reguł
│   │   ├── FeatureAssignment.cs     # Przypisanie wartości cechy do obiektu
│   │   ├── EffectiveFeatureValue.cs # Wynikowa cecha ze źródłem + FeatureSetEvaluator
│   │   ├── ParticipantProfile.cs    # Profil uczestnika lub grupy uczestników
│   │   ├── CompatibilityReport.cs   # Raport spełnionych i niespełnionych wymagań
│   │   ├── ParticipantGroupBreakdown.cs # Podział grupy na kategorie zniżkowe
│   │   ├── PackagePriceBreakdown.cs # Kalkulator ulg indywidualnych i rabatu grupowego
│   │   ├── TicketCategory.cs        # Kategorie biletowe i polityka rabatowa
│   │   ├── Order.cs                 # Zamówienie klienta z rozliczeniem rabatów
│   │   ├── OrderOfferItem.cs        # Snapshot pozycji i jej cech w zamówieniu
│   │   └── OrderAddonItem.cs        # Snapshot wybranego dodatku w zamówieniu
│   │
│   ├── DTOs/                        # Obiekty transferu danych i konfiguracji pakietu
│   │   ├── OfferConfiguration.cs    # Rozszerzony pakiet ofert + ValidateAgainst()
│   │   ├── SelectedRelatedOffer.cs  # Wybrana oferta powiązana wraz z dodatkami
│   │   ├── ServiceResult.cs         # Zunifikowany wynik operacji biznesowej
│   │   └── Api/                     # Kontrakty JSON i mapper warstwy REST API
│   │
│   ├── Helpers/
│   │   └── DomainValueHelper.cs     # Wspólne parsowanie, walidacja i normalizacja danych
│   │
│   ├── Data/
│   │   ├── Features.json            # Dynamiczny słownik cech i wymagań
│   │   ├── FeatureAssignments.json  # Zapis przypisań cech oraz limitów grupowych
│   │   ├── JsonFeatureStore.cs      # Bezpieczny odczyt i zapis plików JSON
│   │   ├── InMemoryData.cs          # Kolekcje danych i generatory ID
│   │   └── DataSeeder.cs            # Baza 15 atrakcji i 50 ofert (Wawel, Kraków, okolice)
│   │
│   ├── Services/                    # Warstwa serwisów logiki aplikacyjnej
│   │   ├── FeatureService.cs        # Zarządzanie słownikiem cech i kontrola użycia
│   │   ├── AttractionService.cs     # CRUD atrakcji i kaskadowe usuwanie
│   │   ├── OfferService.cs          # Zarządzanie ofertami, filtrami i powiązaniami N:M
│   │   ├── AddonService.cs          # Zarządzanie dodatkami ofert
│   │   ├── OfferExtensionService.cs # Konfiguracja pakietu i symulacja kompatybilności
│   │   └── OrderService.cs          # Walidacja pojemności, profilu i składanie zamówień
│   │
│   ├── Menus/                       # Warstwa interfejsu dla trybu konsolowego (--console)
│   └── wwwroot/                     # Warstwa frontendowa SPA (HTML5 + CSS3 + ES Modules)
│       ├── index.html
│       ├── css/styles.css
│       └── js/ (app.js, api.js, state.js, ui.js, organizer.js)
│
├── Projekt Biznesowy.slnx
└── README.md
```

---

## 4. Główne funkcjonalności systemu

### Strefa Klienta (Użytkownik)
- **Katalog atrakcji i ofert:** przeglądanie 15 atrakcji oraz 50 ofert z filtrami (*Wszystkie dostępne*, *Na dzisiaj*, *Tylko oferty grupowe*, *Tylko grupy 4-osobowe*), wyszukiwarką tekstową, filtrem liczebności grupy i przełącznikiem *„Tylko zgodne z moim profilem”*.
- **Profil uczestnika / grupy (`ParticipantProfile`):** definiowanie wieku pojedynczej osoby lub wielopokoleniowej grupy (np. `38, 68, 20, 10`), wzrostu, potrzeby dostępu dla wózka inwalidzkiego, liczby ulg OzN, akceptowalnego wysiłku fizycznego oraz automatycznie generowanych pól dla wszystkich dodatkowych cech z `Features.json`.
- **5-etapowy konfigurator pakietu:**
  1. Wybór atrakcji i oferty głównej,
  2. Dobór dodatków do oferty głównej (z ostrzeżeniem przed dodaniem niekompatybilnego dodatku lub informacją, gdy udogodnienie dopasowuje trasę do profilu),
  3. Dołączanie rekomendowanych ofert powiązanych N:M wraz z ich dodatkami,
  4. Podgląd zagregowanych cech całego pakietu i raportu kompatybilności ze wskazaniem źródła każdego ograniczenia,
  5. Podział grupy na kategorie zniżkowe, wybór płatności (`BLIK`, `Karta`, `Przelew online`, `Kasa na miejscu`) i wygenerowanie potwierdzenia `WAW-RRRR-XXXX`.

### Panel Organizatora
- **Atrakcje i limity osób:** dodawanie, edycja pojemności `MaxParticipants`, przypisywanie cech ogólnych ze słownika `Features.json` oraz kaskadowe usuwanie.
- **Oferty i trasy grupowe:** tworzenie i edycja ofert (w tym ofert ściśle 4-osobowych lub dla większych grup), nadpisywanie domyślnych cech atrakcji, wycofywanie ofert (`Withdrawn`) z automatycznym czyszczeniem powiązań.
- **Dodatki i powiązania N:M:** zarządzanie dodatkami wpływającymi na cechy pakietu oraz dwukierunkowe łączenie i rozłączanie ofert.
- **Słownik cech (`Features.json`):** dodawanie nowych cech (`Boolean`, `Number`, `OptionList`, `Text`), edycja i usuwanie z kontrolą użycia (ostrzeżenie z listą powiązanych obiektów i możliwością kaskadowego odpięcia).
- **Zamówienia klientów:** podgląd wszystkich złożonych zamówień wraz z rozliczeniem rabatów.

---

## 5. Dynamiczny system cech JSON i weryfikacja kompatybilności

- Nowe cechy dodane przez organizatora w słowniku `Features.json` działają natychmiast w całym systemie **bez rekompilacji kodu C#**.
- **Dziedziczenie i nadpisywanie:** `Attraction` przechowuje ogólne cechy obiektu, natomiast `Offer` dziedziczy je i może je nadpisać lub rozszerzyć (np. *Kopalnia Soli Wieliczka* ma ogólną dostępność windową na *Trasie Turystycznej*, podczas gdy *Podziemna Trasa Górnicza* wymaga ukończenia `10 lat` i `Wysokiej` sprawności fizycznej).
- **Wpływ dodatków (`Addon`):** dodatek w trybie `ProvideAccommodation` może znieść barierę oferty (np. *Podjazd meleksem* na Kopiec Kościuszki lub *Zjazd windą górniczą*), a w trybie `TightenRequirement` zaostrzyć wymagania (np. *Bilet łączony na Wieżę Ratuszową* ze schodami).
- **Agregacja pakietowa (`OfferConfiguration.ValidateAgainst`):** minimalny wiek pakietu to maksimum ze wszystkich wybranych pozycji, wysiłek fizyczny odpowiada najbardziej wymagającemu elementowi, a udogodnienie (np. dostęp dla wózków) wymaga spełnienia przez wszystkie pozycje pakietu.

---

## 6. Grupy, pojemność atrakcji i system zniżek

- **Limity wielkości grup i pula miejsc:** system kontroluje zarówno wymagane widełki pojedynczej rezerwacji (np. oferty `TYLKO 4 os.`: *Królewska Gra Zamkowa*, *Prywatna czteroosobowa łódź flisacka*, *Zespół Ratowników Solnych*, *Pakiet Coaster Team 4-os.*), jak i dostępną pulę wolnych miejsc na ofercie oraz dzienną pojemność atrakcji.
- **Kategorie biletowe i ulgi indywidualne:**
  - `Dorosły (normalny)` – **0%** zniżki
  - `Dziecko (do 12 lat)` – **-50%** zniżki
  - `Młodzież / Student (13–26 lat)` – **-30%** zniżki
  - `Senior (65+ lat)` – **-25%** zniżki
  - `Osoba z niepełnosprawnością` – **-50%** zniżki
- **Dodatkowy rabat grupowy:** dla grup **4–7 osób** naliczane jest dodatkowe **-5%**, a od **8 osób** dodatkowe **-10%** od sumy po ulgach indywidualnych.

---

## 7. Dokumentacja endpointów REST API

| Metoda | Ścieżka | Opis |
| :--- | :--- | :--- |
| `GET` | `/api/features` | Pobiera słownik cech z `Features.json` wraz z liczbą użyć |
| `GET` | `/api/features/{id}` | Szczegóły użycia cechy w atrakcjach, ofertach i dodatkach |
| `POST` | `/api/features` | Dodaje nową cechę do `Features.json` |
| `PUT` | `/api/features/{id}` | Aktualizuje definicję cechy w `Features.json` |
| `DELETE` | `/api/features/{id}?force=false` | Usuwa cechę (zwraca `409 Conflict`, jeśli jest w użyciu) |
| `GET` | `/api/attractions` | Pobiera listę atrakcji wraz z pojemnością `maxParticipants` i cechami |
| `POST / PUT / DELETE` | `/api/attractions[/{id}]` | Zarządzanie atrakcjami turystycznymi |
| `POST` | `/api/attractions/{id}/validate` | Weryfikuje dostępność atrakcji względem `ParticipantProfile` |
| `GET` | `/api/offers?filter=...` | Pobiera oferty (`available`, `all`, `planned`, `today`, `withdrawn`, `group`, `group4`) |
| `POST` | `/api/offers/search` | Wyszukuje i weryfikuje oferty względem przekazanego profilu uczestnika |
| `POST / PUT / DELETE` | `/api/offers[/{id}]` | Tworzenie, edycja i usuwanie ofert |
| `POST` | `/api/offers/{id}/withdraw` | Wycofuje ofertę i usuwa jej aktywne powiązania |
| `GET / POST / PUT / DELETE` | `/api/offers/{offerId}/addons[/{addonId}]` | Zarządzanie dodatkami oferty i ich modyfikatorami cech |
| `POST / DELETE` | `/api/offers/relations` | Łączenie i rozłączanie dwukierunkowych powiązań ofert N:M |
| `POST` | `/api/packages/evaluate` | Wylicza ceny, zniżki grupowe, cechy pakietu i raport kompatybilności |
| `POST` | `/api/packages/preview-change` | Symuluje wpływ dodania dodatku lub oferty powiązanej na kompatybilność |
| `GET / POST` | `/api/orders` | Przeglądanie oraz składanie zamówień z pełną walidacją biznesową |
