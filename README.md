# Attractions – System zarządzania atrakcjami turystycznymi, ofertami i zamówieniami

Konsolowa aplikacja napisana w języku **C# (.NET 10)** z wykorzystaniem programowania obiektowego (OOP), kolekcji `List<T>` oraz zapytań **LINQ**. Aplikacja działa wyłącznie lokalnie w konsoli i przechowuje wszystkie dane w pamięci operacyjnej RAM (bez baz danych, plików zewnętrznych ani warstwy webowej).

---

## Spis treści

1. [Wymagania systemowe](#1-wymagania-systemowe)
2. [Uruchomienie projektu](#2-uruchomienie-projektu)
3. [Architektura i struktura projektu](#3-architektura-i-struktura-projektu)
4. [Role i funkcjonalności](#4-role-i-funkcjonalności)
5. [Pełny proces zakupu i składania zamówienia](#5-pełny-proces-zakupu-i-składania-zamówienia)
6. [Relacje i reguły biznesowe](#6-relacje-i-reguły-biznesowe)
7. [Dane startowe (DataSeeder – Wawel, Kraków i Małopolska)](#7-dane-startowe-dataseeder--wawel-kraków-i-małopolska)

---

## 1. Wymagania systemowe

- **.NET 10 SDK** (np. `10.0.x`)
- System Windows, Linux lub macOS z terminalem obsługującym kodowanie UTF-8

Sprawdzenie zainstalowanej wersji SDK:

```powershell
dotnet --version
```

---

## 2. Uruchomienie projektu

Z poziomu głównego katalogu solucji uruchom w terminalu:

```powershell
dotnet build
dotnet run --project Attractions
```

Możesz również otworzyć plik solucji `Projekt Biznesowy.slnx` w programie **Visual Studio** lub **Cursor** i uruchomić projekt `Attractions` klawiszem `F5` / `Ctrl+F5`.

> **Uwaga:** Wszystkie dane (atrakcje, oferty, dodatki oraz złożone w trakcie sesji zamówienia) przechowywane są wyłącznie w pamięci RAM (`InMemoryData`). Zamknięcie aplikacji powoduje utratę zmian wprowadzonych podczas sesji.

---

## 3. Architektura i struktura projektu

```text
Attractions/
│
├── Models/
│   ├── Attraction.cs            # Model atrakcji turystycznej
│   ├── Offer.cs                 # Model oferty przypisanej do atrakcji
│   ├── Addon.cs                 # Model dodatku do oferty
│   ├── OfferStatus.cs           # Enum statusu oferty (Planned, Withdrawn)
│   ├── Order.cs                 # Model złożonego zamówienia klienta w pamięci RAM
│   ├── OrderOfferItem.cs        # Snapshot wybranej oferty (głównej lub powiązanej) w zamówieniu
│   ├── OrderAddonItem.cs        # Snapshot wybranego dodatku w zamówieniu
│   └── PaymentMethod.cs         # Enum sposobu płatności (BLIK, Karta, Przelew, Kasa na miejscu)
│
├── DTOs/
│   ├── OfferConfiguration.cs    # Konfiguracja rozszerzonej oferty (koszyk użytkownika)
│   ├── SelectedRelatedOffer.cs  # Wybrana oferta powiązana wraz z jej dodatkami
│   └── ServiceResult.cs         # Zunifikowany wynik operacji biznesowej
│
├── Data/
│   ├── InMemoryData.cs          # Kolekcje List<T> w pamięci RAM i generatory ID
│   └── DataSeeder.cs            # Rozbudowana baza atrakcji i ofert Wawelu, Krakowa i okolic
│
├── Services/
│   ├── AttractionService.cs     # Logika CRUD atrakcji i kaskadowego usuwania
│   ├── OfferService.cs          # Logika ofert, filtrowania dat, wycofywania i powiązań N:M
│   ├── AddonService.cs          # Logika zarządzania dodatkami ofert
│   ├── OfferExtensionService.cs # Logika rozszerzania oferty o dodatki i powiązane oferty
│   └── OrderService.cs          # Logika wyboru ofert wg atrakcji i składania zamówień
│
├── Menus/
│   ├── ConsoleHelper.cs         # Walidacja wejścia z konsoli i formatowanie widoków
│   ├── MainMenu.cs              # Menu główne wyboru roli
│   ├── OrganizerMenu.cs         # Menu główne organizatora (w tym podgląd zamówień klientów)
│   ├── AttractionMenu.cs        # Podmenu zarządzania atrakcjami
│   ├── OfferMenu.cs             # Podmenu zarządzania ofertami
│   ├── AddonMenu.cs             # Podmenu zarządzania dodatkami
│   ├── RelatedOfferMenu.cs      # Podmenu zarządzania powiązaniami ofert
│   └── UserMenu.cs              # Menu użytkownika i 5-etapowy kreator zakupu / zamówienia
│
├── Attractions.csproj
└── Program.cs                   # Punkt wejścia i inicjalizacja zależności
```

---

## 4. Role i funkcjonalności

### Organizator
- **Zarządzanie atrakcjami:** przeglądanie, podgląd szczegółów z przypisanymi ofertami, dodawanie, edycja i kaskadowe usuwanie atrakcji.
- **Zarządzanie ofertami:** tworzenie wielu ofert dla atrakcji, przeglądanie, szczegóły, edycja (z możliwością przywrócenia wycofanej oferty do statusu `Planned`), wycofywanie i usuwanie.
- **Filtrowanie ofert:** widoki ofert zaplanowanych (przyszłych), ofert na dzisiaj (`Date.Date == DateTime.Today`) oraz ofert wycofanych (`Withdrawn`).
- **Zarządzanie dodatkami i powiązaniami:** pełna obsługa dodatków oraz dwukierunkowych powiązań N:M między aktywnymi ofertami.
- **Podgląd zamówień klientów:** możliwość przeglądania zamówień złożonych przez użytkowników w bieżącej sesji.

### Użytkownik
- Przeglądanie dostępnych ofert, szczegółów, dodatków oraz powiązanych ofert.
- Przeglądanie i wybieranie ofert pogrupowanych według atrakcji (opcja `7`).
- Samodzielne rozszerzanie aktywnej konfiguracji o dodatki (opcja `4`) i oferty powiązane wraz z ich dodatkami (opcja `6`).
- **Pełny kreator zakupu / składania zamówienia krok po kroku (opcja `8`)** oraz przeglądanie potwierdzeń swoich złożonych zamówień (opcja `9`).

---

## 5. Pełny proces zakupu i składania zamówienia

Kreator zamówienia prowadzi użytkownika przez 5 czytelnych kroków:
1. **Krok 1/5 – Wybór atrakcji:** lista atrakcji posiadających dostępne oferty.
2. **Krok 2/5 – Wybór oferty:** prezentacja dostępnych ofert wybranej atrakcji i utworzenie konfiguracji (`OfferConfiguration`).
3. **Krok 3/5 – Wybór proponowanych dodatków:** interaktywne zaznaczanie/odznaczanie dodatków przypisanych do wybranej oferty.
4. **Krok 4/5 – Wybór proponowanych ofert powiązanych:** rekomendacje powiązanych tras zwiedzania wraz z możliwością dobrania dodatków do każdej dołączonej oferty powiązanej.
5. **Krok 5/5 – Podsumowanie i finalizacja zamówienia:** pełne zestawienie cen składowych, podanie liczby biletów, imienia i nazwiska, opcjonalnego adresu e-mail oraz formy płatności (`BLIK`, `Karta`, `Przelew online`, `Kasa biletowa na miejscu`), zakończone wygenerowaniem potwierdzenia zamówienia o unikalnym numerze `WAW-RRRR-XXXX`.

Złożenie zamówienia zapisuje niemutowalną migawkę pozycji w `InMemoryData.Orders` i **nie zmienia pierwotnej ceny zapisanej w obiektach `Offer`**.

---

## 6. Relacje i reguły biznesowe

- **Attraction → Offer (1:N):** każda oferta należy do istniejącej atrakcji; usunięcie atrakcji kaskadowo usuwa jej oferty, dodatki i powiązania.
- **Offer → Addon (1:N):** usunięcie oferty usuwa jej dodatki; wycofana oferta ma zablokowane dodawanie/edycję dodatków oraz rozszerzanie.
- **Offer ↔ Offer (N:M):** dwukierunkowe powiązania bez auto-powiązań, bez duplikatów i bez ofert wycofanych; wycofanie oferty usuwa jej powiązania.

---

## 7. Dane startowe (DataSeeder – Wawel, Kraków i Małopolska)

Przy uruchomieniu w pamięci RAM inicjalizowane są:
- **15 atrakcji**, w tym **5 stref na Wzgórzu Wawelskim i bezpośrednim Podzamczu** (*Zamek Królewski – Komnaty i Wystawy*, *Katedra Wawelska, Krypty i Dzwon Zygmunt*, *Ogrody Królewskie i Baszty Twierdzy Wawel*, *Smocza Jama, Podzamcze i Bulwary pod Wawelem*, *Ulica Kanonicza i Podwawelski Trakt Królewski*) oraz 10 atrakcji w Krakowie i Małopolsce (m.in. *Zamek Pieskowa Skała – oddział Zamku na Wawelu*, *Rynek Główny*, *Bazylika Mariacka*, *Kazimierz i Skałka*, *Tyniec*, *Wieliczka*, *Morskie Oko*),
- **44 oferty** (w tym aż **29 ofert wawelskich** – komnaty, prywatne apartamenty, skarbiec ze Szczerbcem, zbrojownia, Wawel Podziemny, Namioty Tureckie, Wawel Odzyskany, trasa rodzinna, Pakiet Królewski VIP, nocne zwiedzanie, katedra, Kaplica Zygmuntowska, Skarbiec Katedralny, trasa koronacyjna, ogrody królowej Bony, Baszta Sandomierska, fortyfikacje, wschód słońca na tarasach, Smocza Jama, rejsy statkiem i gondolą pod Wawelem, spacer z latarnikiem, ul. Kanonicza),
- **77 dodatków** tematycznych oraz **50 dwukierunkowych powiązań** tworzących gotowe pakiety zwiedzania.
