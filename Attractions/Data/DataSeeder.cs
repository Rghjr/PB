using Attractions.Models;

namespace Attractions.Data;

public static class DataSeeder
{
    public static void Seed()
    {
        if (InMemoryData.Attractions.Count > 0 || InMemoryData.Offers.Count > 0)
        {
            return;
        }

        DateTime today = DateTime.Today;
        int nextAddonId = 1;

        var attractions = new List<Attraction>
        {
            new()
            {
                Id = 1,
                Name = "Zamek Królewski na Wawelu – Komnaty i Wystawy",
                Location = "Kraków, Wawel 5",
                Description = "Historyczna rezydencja królów Polski z dziedzińcem arkadowym, komnatami, apartamentami, skarbcem, zbrojownią i podziemiami."
            },
            new()
            {
                Id = 2,
                Name = "Katedra Wawelska, Krypty i Dzwon Zygmunt",
                Location = "Kraków, Wawel 3",
                Description = "Świątynia koronacyjna i nekropolia królów Polski z Kaplicą Zygmuntowską, Grobami Królewskimi, Skarbcem Katedralnym i Wieżą Zygmuntowską."
            },
            new()
            {
                Id = 3,
                Name = "Ogrody Królewskie i Baszty Twierdzy Wawel",
                Location = "Kraków, Wawel 5 (tarasy i mury obronne)",
                Description = "Odtworzone renesansowe ogrody królowej Bony, Baszta Sandomierska, Baszta Złodziejska oraz południowe fortyfikacje wzgórza."
            },
            new()
            {
                Id = 4,
                Name = "Smocza Jama, Podzamcze i Bulwary pod Wawelem",
                Location = "Kraków, Wzgórze Wawelskie / Bulwar Czerwieński",
                Description = "Wapienna jaskinia krasowa w zboczu Wawelu z rzeźbą Smoka Wawelskiego oraz przystanią statków i gondoli na Wiśle."
            },
            new()
            {
                Id = 5,
                Name = "Ulica Kanonicza i Podwawelski Trakt Królewski",
                Location = "Kraków, ul. Kanonicza 1–25 (u stóp Wawelu)",
                Description = "Najstarsza i najpiękniejsza renesansowa ulica pod samym Wawelem z Domem Długosza i Pałacem Biskupa Erazma Ciołka."
            },
            new()
            {
                Id = 6,
                Name = "Rynek Główny, Sukiennice i Podziemia Rynku",
                Location = "Kraków, Rynek Główny 1–3",
                Description = "Centralny plac średniowiecznego Krakowa z multimedialnym muzeum pod płytą rynku, Sukiennicami i Wieżą Ratuszową."
            },
            new()
            {
                Id = 7,
                Name = "Bazylika Mariacka i Wieża Hejnalica",
                Location = "Kraków, pl. Mariacki 5",
                Description = "Gotycka bazylika słynąca z monumentalnego ołtarza Wita Stwosza, polichromii Jana Matejki oraz hejnału mariackiego."
            },
            new()
            {
                Id = 8,
                Name = "Dzielnica Kazimierz, Skałka i Stara Synagoga",
                Location = "Kraków, ul. Skałeczna / ul. Szeroka 24",
                Description = "Sąsiadująca z Wawelem historyczna dzielnica łącząca Panteon Narodowy na Skałce z zabytkowymi synagogami Kazimierza."
            },
            new()
            {
                Id = 9,
                Name = "Fabryka Emalia Oskara Schindlera i Podgórze",
                Location = "Kraków, ul. Lipowa 4",
                Description = "Oddział Muzeum Krakowa prezentujący dzieje miasta w latach 1939–1945 w sąsiedztwie Rynku Podgórskiego i Kopca Krakusa."
            },
            new()
            {
                Id = 10,
                Name = "Kopiec Kościuszki i Wzgórze św. Bronisławy",
                Location = "Kraków, al. Waszyngtona 1",
                Description = "Zabytkowy kopiec widokowy otoczony XIX-wiecznym fortem z panoramą Wzgórza Wawelskiego, Wisły i Tatr."
            },
            new()
            {
                Id = 11,
                Name = "Opactwo Benedyktynów w Tyńcu",
                Location = "Kraków-Tyniec, ul. Benedyktyńska 37",
                Description = "Najstarszy istniejący klasztor w Polsce z XI wieku, wznoszący się na wapiennym wzgórzu nad przełomem Wisły."
            },
            new()
            {
                Id = 12,
                Name = "Zamek Pieskowa Skała i Ojcowski Park Narodowy",
                Location = "Sułoszowa / Ojców (oddział Zamku na Wawelu)",
                Description = "Renesansowa rezydencja obronna będąca oficjalnym oddziałem Zamku Królewskiego na Wawelu w sercu jurajskiej Doliny Prądnika."
            },
            new()
            {
                Id = 13,
                Name = "Kopalnia Soli \"Wieliczka\"",
                Location = "Wieliczka, ul. Daniłowicza 10",
                Description = "Zabytkowa podziemna kopalnia soli tuż pod Krakowem, wpisana wraz z Wawelem na pierwszą listę dziedzictwa UNESCO."
            },
            new()
            {
                Id = 14,
                Name = "Park Rozrywki Energylandia",
                Location = "Zator, al. 3 Maja 2",
                Description = "Największy rodzinny park rozrywki w Małopolsce ze strefami ekstremalnymi, wodnymi i familijnymi."
            },
            new()
            {
                Id = 15,
                Name = "Morskie Oko i Dolina Rybiego Potoku",
                Location = "Zakopane, Tatrzański Park Narodowy",
                Description = "Całodniowy szlak górski z wyjazdem spod Wawelu nad największe polodowcowe jezioro w Tatrach."
            }
        };

        InMemoryData.Attractions.AddRange(attractions);

        var offers = new List<Offer>
        {
            // =========================================================================
            // 1. ZAMEK KRÓLEWSKI NA WAWELU – KOMNATY I WYSTAWY (10 ofert)
            // =========================================================================
            new()
            {
                Id = 1,
                AttractionId = 1,
                Name = "Reprezentacyjne Komnaty Królewskie (Zamek II)",
                Description = "Zwiedzanie sal II piętra zamku: Sala Poselska z rzeźbionymi głowami wawelskimi, Sala Senatorska i kolekcja arrasów Zygmunta Augusta.",
                Price = 89.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Audioprzewodnik wielojęzyczny Wawel", "Zestaw słuchawkowy z narracją o komnatach i arrasach królewskich.", 18.00m),
                    CreateAddon(ref nextAddonId, "Oprowadzanie z licencjonowanym kustoszem", "Kameralny spacer w grupie do 12 osób z historykiem sztuki.", 45.00m),
                    CreateAddon(ref nextAddonId, "Album ilustrowany \"Zamek Królewski na Wawelu\"", "Wydawnictwo pamiątkowe w twardej oprawie odbierane w punkcie muzealnym.", 59.00m)
                }
            },
            new()
            {
                Id = 2,
                AttractionId = 1,
                Name = "Prywatne Apartamenty Królewskie z przewodnikiem (Zamek I)",
                Description = "Wizyta w apartamentach mieszkalnych królów na I piętrze, sypialniach królewskich, gabinetach kolekcjonerskich i pokojach gościnnych.",
                Price = 92.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wejście do Renesansowej Wieży Sobieskiego (Kurzej Stopki)", "Dodatkowy wstęp do słynnych gabinetów alchemicznych Zygmunta III Wazy.", 28.00m),
                    CreateAddon(ref nextAddonId, "Zestaw pocztówek z reprodukcjami portretów Jagiellonów", "Kolekcjonerska teka 10 reprodukcji malarstwa dworskiego.", 22.00m)
                }
            },
            new()
            {
                Id = 3,
                AttractionId = 1,
                Name = "Skarbiec Koronny w nowej odsłonie i Zbrojownia ze Szczerbcem",
                Description = "Klejnoty koronne, pamiątki po Jagiellonach i Wazach, miecz koronacyjny Szczerbiec, płaszcz kawalera Orderu Ducha Świętego i husarskie zbroje.",
                Price = 85.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Zejście do piwnic Zbrojowni z kolekcją luf armatnich", "Rozszerzona trasa po gotyckich sklepionych podziemiach z artylerią.", 20.00m),
                    CreateAddon(ref nextAddonId, "Wybijanie pamiątkowego dukata królewskiego", "Własnoręcznie wybita replika renesansowej monety zygmuntowskiej.", 25.00m)
                }
            },
            new()
            {
                Id = 4,
                AttractionId = 1,
                Name = "Wawel Podziemny: Lapidarium, Międzymurze i Wawel Zaginiony",
                Description = "Podziemny rezerwat archeologiczny prezentujący przedromańską Rotundę NMP z X wieku, relikty kuchni królewskich i multimedialne lapidarium.",
                Price = 55.00m,
                Date = today.AddDays(1),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Pokaz rekonstrukcji 3D wczesnośredniowiecznego grodu", "Projekcja VR/3D ukazująca przemiany zabudowy Wawelu od X do XVI wieku.", 19.00m),
                    CreateAddon(ref nextAddonId, "Komentarz archeologa wawelskiego", "30-minutowe wprowadzenie przy reliktach przedromańskiej rotundy.", 30.00m)
                }
            },
            new()
            {
                Id = 5,
                AttractionId = 1,
                Name = "Sztuka Wschodu: Namioty Tureckie i trofea spod Wiednia",
                Description = "Unikatowa na świecie kolekcja monumentalnych XVII-wiecznych namiotów osmańskich, chorągwi, szabel, kobierców perskich i porcelany wschodniej.",
                Price = 62.00m,
                Date = today.AddDays(2),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Warsztat kaligrafii i ornamentyki orientalnej", "45-minutowe spotkanie edukacyjne w pracowni muzealnej.", 35.00m),
                    CreateAddon(ref nextAddonId, "Kawa po turecku i deser chałwowy w kawiarni zamkowej", "Tradycyjna kawa parzona w tygielku na dziedzińcu zewnętrznym.", 29.00m)
                }
            },
            new()
            {
                Id = 6,
                AttractionId = 1,
                Name = "Wawel Odzyskany oraz Gabinet Porcelany i Sreber Królewskich",
                Description = "Opowieść o przywracaniu świetności zamkowi na początku XX wieku połączona z ekspozycją królewskiej porcelany miśnieńskiej.",
                Price = 48.00m,
                Date = today.AddDays(3),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Filiżanka czekolady pitnej wg XVIII-wiecznej receptury", "Degustacja gorącej czekolady w kawiarni na Wzgórzu Wawelskim.", 26.00m)
                }
            },
            new()
            {
                Id = 7,
                AttractionId = 1,
                Name = "Wawel dla rodzin: \"Tajemnice Króla Zygmunta i Królowej Bony\"",
                Description = "Interaktywne zwiedzanie zamku z edukatorem w stroju renesansowym, zagadkami dworskimi, przymierzaniem replik koron i dyplomem.",
                Price = 75.00m,
                Date = today.AddDays(4),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Ilustrowany zeszyt zadań i naklejek \"Młody Dworzanin\"", "Książeczka edukacyjna z pieczęcią Zamku Królewskiego.", 19.00m),
                    CreateAddon(ref nextAddonId, "Warsztat tworzenia renesansowych pieczęci lakowych", "Zajęcia plastyczne w sali edukacyjnej po zwiedzaniu.", 28.00m)
                }
            },
            new()
            {
                Id = 8,
                AttractionId = 1,
                Name = "Pakiet Królewski VIP: Wawel Pełne Zwiedzanie w 1 Dzień",
                Description = "Całodniowa przepustka premium obejmująca Komnaty, Prywatne Apartamenty, Skarbiec, Zbrojownię oraz Wawel Podziemny bez kolejek.",
                Price = 219.00m,
                Date = today.AddDays(5),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Indywidualny przewodnik VIP na wyłączność (4 godziny)", "Dedykowany historyk sztuki dostosowujący tempo trasy do gości.", 190.00m),
                    CreateAddon(ref nextAddonId, "Dwudaniowy obiad staropolski w restauracji na Wzgórzu Wawelskim", "Przerwa obiadowa w trakcie zwiedzania z widokiem na katedrę.", 89.00m)
                }
            },
            new()
            {
                Id = 9,
                AttractionId = 1,
                Name = "Nocne zwiedzanie Zamku Królewskiego i krużganków z kustoszem",
                Description = "Wieczorne oprowadzanie po zamknięciu bram wzgórza dla ruchu dziennego – iluminowany dziedziniec arkadowy i wybrane sale królewskie.",
                Price = 165.00m,
                Date = today.AddDays(10),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Degustacja staropolskich miodów i przekąsek dworskich", "Wieczorny poczęstunek inspirowany kuchnią królowej Bony.", 58.00m),
                    CreateAddon(ref nextAddonId, "Kameralny recital muzyki renesansowej na lutni", "20-minutowy koncert na krużgankach zamkowych.", 45.00m)
                }
            },
            new()
            {
                Id = 10,
                AttractionId = 1,
                Name = "Pokaz specjalny chorągwi królewskich (oferta wycofana)",
                Description = "Czasowa ekspozycja konserwatorska zabytkowych tkanin – oferta wycofana na czas prac renowacyjnych.",
                Price = 70.00m,
                Date = today.AddDays(12),
                Status = OfferStatus.Withdrawn,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Katalog wystawy konserwatorskiej", "Broszura opisująca proces renowacji chorągwi.", 24.00m)
                }
            },

            // =========================================================================
            // 2. KATEDRA WAWELSKA, KRYPTY I DZWON ZYGMUNT (6 ofert)
            // =========================================================================
            new()
            {
                Id = 11,
                AttractionId = 2,
                Name = "Katedra Wawelska, Groby Królewskie i Dzwon Zygmunt",
                Description = "Pełna trasa katedralna: nawa główna z konfesją św. Stanisława, zejście do krypt królewskich i wspinaczka na Wieżę Zygmuntowską.",
                Price = 58.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Zestaw słuchawkowy Audio-Guide Katedra Wawelska", "Szczegółowy przewodnik głosowy po wszystkich 18 kaplicach bocznych.", 16.00m),
                    CreateAddon(ref nextAddonId, "Pamiątkowy medalion z wizerunkiem Dzwonu Zygmunt", "Mosiężny medalion wybijany w mennicy katedralnej.", 20.00m)
                }
            },
            new()
            {
                Id = 12,
                AttractionId = 2,
                Name = "Kaplica Zygmuntowska i Krypta Wieszczów z historykiem sztuki",
                Description = "Tematyczne oprowadzanie poświęcone „perle renesansu z tej strony Alp” oraz sarkofagom Adama Mickiewicza i Juliusza Słowackiego.",
                Price = 79.00m,
                Date = today.AddDays(1),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Przewodnik książkowy po kaplicach i nagrobkach katedralnych", "Bogato ilustrowana publikacja z planem krypt królewskich.", 28.00m)
                }
            },
            new()
            {
                Id = 13,
                AttractionId = 2,
                Name = "Krypta pod Wieżą Srebrnych Dzwonów i Muzeum Katedralne",
                Description = "Zwiedzanie krypty z sarkofagiem Marszałka Józefa Piłsudskiego oraz Muzeum Katedralnego im. Jana Pawła II u wejścia na wzgórze.",
                Price = 54.00m,
                Date = today.AddDays(2),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Komentarz przewodnika o pamiątkach papieskich i królewskich", "45-minutowe oprowadzanie po salach Muzeum Katedralnego.", 25.00m)
                }
            },
            new()
            {
                Id = 14,
                AttractionId = 2,
                Name = "Śladami koronacji królów Polski – trasa tematyczna w Katedrze",
                Description = "Opowieść o ceremoniale koronacyjnym od Władysława Łokietka po Stanisława Augusta przy Ołtarzu Koronacyjnym i nagrobkach Jagiellonów.",
                Price = 84.00m,
                Date = today.AddDays(6),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Reprint historycznego drzewa genealogicznego królów Polski", "Ozdobna plansza na papierze czerpanym w tubie.", 32.00m)
                }
            },
            new()
            {
                Id = 15,
                AttractionId = 2,
                Name = "Skarbiec Katedralny: Włócznia św. Maurycego i relikwiarze",
                Description = "Specjalne wejście z kustoszem do Skarbca Katedralnego kryjącego najstarsze polskie regalia z roku 1000 i gotyckie złotnictwo.",
                Price = 68.00m,
                Date = today.AddDays(8),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Lupa jubilerska i karta detali złotniczych", "Zestaw do podziwiania średniowiecznych filigranów i gemm.", 15.00m)
                }
            },
            new()
            {
                Id = 16,
                AttractionId = 2,
                Name = "Wieczorny koncert organowy w Katedrze Wawelskiej (termin miniony)",
                Description = "Recital muzyki sakralnej Jana Sebastiana Bacha w historycznych wnętrzach katedry.",
                Price = 90.00m,
                Date = today.AddDays(-3),
                Status = OfferStatus.Planned
            },

            // =========================================================================
            // 3. OGRODY KRÓLEWSKIE I BASZTY TWIERDZY WAWEL (5 ofert)
            // =========================================================================
            new()
            {
                Id = 17,
                AttractionId = 3,
                Name = "Ogrody Królewskie Królowej Bony i górny taras renesansowy",
                Description = "Spacer po zrekonstruowanych dwupoziomowych ogrodach tarasowych ze szpalerami grabowymi, winnicą i ziołami dworskimi.",
                Price = 38.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Mini-warsztat zielarski i saszetka ziół z ogrodów wawelskich", "Prezentacja renesansowych roślin leczniczych i aromatyczna pamiątka.", 26.00m),
                    CreateAddon(ref nextAddonId, "Lemoniada różana i kruche ciasteczka na tarasie widokowym", "Poczęstunek w letnim pawilonie przy ogrodach.", 22.00m)
                }
            },
            new()
            {
                Id = 18,
                AttractionId = 3,
                Name = "Baszta Sandomierska i spacer gankiem obronnym murów Wawelu",
                Description = "Wejście na 7 kondygnacji XV-wiecznej baszty ogniowej i przejście historycznym gankiem straży z panoramą zakola Wisły i Kazimierza.",
                Price = 42.00m,
                Date = today.AddDays(1),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wejście rozszerzone na Basztę Senatorską", "Dostęp do drugiej potężnej baszty w południowym ciągu murów.", 18.00m)
                }
            },
            new()
            {
                Id = 19,
                AttractionId = 3,
                Name = "Fortyfikacje Twierdzy Wawel: Baszta Złodziejska i Bastion Władysława IV",
                Description = "Militarna historia Wawelu od średniowiecznych baszt przez nowożytne basteje po XIX-wieczne kaponiery austriackiej cytadeli.",
                Price = 59.00m,
                Date = today.AddDays(4),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Historyczna mapa fortyfikacji Twierdzy Kraków i Wawelu", "Rozkładany plan militarny z omówieniem systemów obronnych.", 21.00m)
                }
            },
            new()
            {
                Id = 20,
                AttractionId = 3,
                Name = "Wschód słońca na tarasach widokowych Wawelu – spacer fotograficzny",
                Description = "Poranne wejście przed otwarciem dla turystów z fotografem krajobrazu – Zamek, Wisła, Stare Miasto i Tatry w porannym świetle.",
                Price = 110.00m,
                Date = today.AddDays(7),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wypożyczenie lekkiego statywu podróżnego", "Statyw fotograficzny na czas porannego pleneru na wzgórzu.", 25.00m),
                    CreateAddon(ref nextAddonId, "Śniadanie poranne z kawą speciality u stóp Wawelu", "Ciepły rogal maślany, jajecznica i kawa po zakończeniu sesji.", 39.00m)
                }
            },
            new()
            {
                Id = 21,
                AttractionId = 3,
                Name = "Plener malarski w Ogrodach Królewskich (oferta wycofana)",
                Description = "Sezonowe warsztaty akwareli na górnym tarasie ogrodowym – edycja zakończona na sezon jesienno-zimowy.",
                Price = 95.00m,
                Date = today.AddDays(15),
                Status = OfferStatus.Withdrawn
            },

            // =========================================================================
            // 4. SMOCZA JAMA, PODZAMCZE I BULWARY POD WAWELEM (5 ofert)
            // =========================================================================
            new()
            {
                Id = 22,
                AttractionId = 4,
                Name = "Zejście do Smoczej Jamy i spacer legend Wzgórza Wawelskiego",
                Description = "Przejście krętymi schodami przez 81-metrową krasową jaskinię w skale wawelskiej prosto pod ziejącego ogniem Smoka Wawelskiego.",
                Price = 39.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Pakiet rodzinny: gra terenowa \"Tropem Smoka Wawelskiego\"", "Zestaw zagadek z mapą wzgórza i pluszowym smokiem dla dzieci.", 29.00m),
                    CreateAddon(ref nextAddonId, "Gorąca czekolada i krakowski obwarzanek na Bulwarze Czerwieńskim", "Kupon do kawiarni widokowej naprzeciwko pomnika smoka.", 21.00m)
                }
            },
            new()
            {
                Id = 23,
                AttractionId = 4,
                Name = "Panoramiczny rejs statkiem po Wiśle spod Smoczej Jamy",
                Description = "Godzinny rejs z przystani pod Wawelem wzdłuż murów zamkowych, kościoła na Skałce, Kazimierza, Kładki Bernatka i Cricoteki.",
                Price = 69.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Miejsce na górnym pokładzie widokowym VIP", "Gwarantowane miejsce na otwartym tarasie z ciepłym kocem.", 20.00m),
                    CreateAddon(ref nextAddonId, "Zestaw kawowy z domową szarlotką na pokładzie", "Świeżo parzona kawa lub herbata oraz ciasto podczas rejsu.", 27.00m)
                }
            },
            new()
            {
                Id = 24,
                AttractionId = 4,
                Name = "Rejs drewnianą gondolą wokół zakola Wawelu o zachodzie słońca",
                Description = "Kameralny 45-minutowy rejs tradycyjną drewnianą łodzią flisacką u podnóża oświetlonych murów Zamku i Katedry Wawelskiej.",
                Price = 119.00m,
                Date = today.AddDays(2),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Kieliszek bezalkoholowego wina musującego i truskawki", "Poczęstunek serwowany podczas postoju widokowego pod Wawelem.", 34.00m),
                    CreateAddon(ref nextAddonId, "Pamiątkowa sesja zdjęciowa z pokładu na tle Wawelu", "5 profesjonalnych ujęć w wersji cyfrowej wykonanych przez sternika.", 35.00m)
                }
            },
            new()
            {
                Id = 25,
                AttractionId = 4,
                Name = "Rejs wycieczkowy spod Wawelu przełomem Wisły do Opactwa w Tyńcu",
                Description = "Malownicza wyprawa wodna z Bulwaru Czerwieńskiego obok Klasztoru Norbertanek i Lasu Wolskiego pod samo wzgórze klasztorne w Tyńcu.",
                Price = 115.00m,
                Date = today.AddDays(6),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Przewóz roweru na pokładzie (powrót Wiślaną Trasą Rowerową)", "Możliwość zabrania roweru i powrotu asfaltową trasą po wale wiślanym.", 25.00m),
                    CreateAddon(ref nextAddonId, "Kosz piknikowy ze specjałami małopolskimi", "Sery regionalne, pieczywo rzemieślnicze i tłoczony sok z sadów podkrakowskich.", 54.00m)
                }
            },
            new()
            {
                Id = 26,
                AttractionId = 4,
                Name = "Wieczorny spacer z latarnikiem: Legendy Podzamcza i zakola Wisły",
                Description = "Klimatyczna opowieść o dawnych rybakach, flisakach, alchemiku Twardowskim i tajemnicach murów obronnych u stóp Wawelu.",
                Price = 56.00m,
                Date = today.AddDays(3),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Rozgrzewająca herbata korzenna z miodem i pomarańczą", "Serwowana w kubku termicznym na zakończenie spaceru.", 18.00m)
                }
            },

            // =========================================================================
            // 5. ULICA KANONICZA I PODWAWELSKI TRAKT KRÓLEWSKI (3 oferty)
            // =========================================================================
            new()
            {
                Id = 27,
                AttractionId = 5,
                Name = "Ulica Kanonicza pod Wawelem: Dom Długosza i renesansowe dziedzińce",
                Description = "Spacer najstarszą ulicą pod samym wejściem na Wawel: kamienice kanoników kapituły katedralnej, sgraffita, portale i Plac św. Marii Magdaleny.",
                Price = 55.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wstęp do Muzeum Archidiecezjalnego Kardynała Karola Wojtyły", "Zwiedzanie dawnego domu kanoniczego przy ul. Kanoniczej 19–21.", 24.00m)
                }
            },
            new()
            {
                Id = 28,
                AttractionId = 5,
                Name = "Spacer Drogą Królewską: Barbakan – Rynek – Kanonicza – Wawel",
                Description = "Piesza wycieczka historycznym szlakiem wjazdów koronacyjnych od Bramy Floriańskiej przez Rynek Główny i ul. Grodzką aż na dziedziniec Wawelu.",
                Price = 65.00m,
                Date = today.AddDays(1),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wstęp na dziedziniec i do sal Collegium Maius UJ", "Wizyta w XV-wiecznym sercu Uniwersytetu Jagiellońskiego po drodze.", 25.00m),
                    CreateAddon(ref nextAddonId, "Degustacja wypieków krakowskich na trasie", "Certyfikowany obwarzanek krakowski oraz sernik po krakowsku.", 32.00m)
                }
            },
            new()
            {
                Id = 29,
                AttractionId = 5,
                Name = "Pałac Biskupa Erazma Ciołka – sztuka dawnej Polski pod Wawelem",
                Description = "Zwiedzanie oddziału Muzeum Narodowego przy ul. Kanoniczej 17 ze skarbami rzeźby gotyckiej (Madonna z Krużlowej) i malarstwa sakralnego.",
                Price = 46.00m,
                Date = today.AddDays(5),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Oprowadzanie kuratorskie po kolekcji sztuki średniowiecznej", "60 minut ze specjalistą sztuki małopolskiej.", 30.00m)
                }
            },

            // =========================================================================
            // 6–15. SĄSIEDNIE MIEJSCA W KRAKOWIE ORAZ OKOLICACH MAŁOPOLSKI (15 ofert)
            // =========================================================================
            new()
            {
                Id = 30,
                AttractionId = 6,
                Name = "Podziemia Rynku: Śladem europejskiej tożsamości Krakowa",
                Description = "Zejście 4 metry pod płytę Rynku Głównego do średniowiecznych kramów kupieckich, warsztatów kowalskich i brukowanych traktów.",
                Price = 48.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Oprowadzanie z przewodnikiem miejskim po Podziemiach", "90-minutowa opowieść o handlu i życiu dawnego Krakowa.", 35.00m),
                    CreateAddon(ref nextAddonId, "Bilet łączony na Wieżę Ratuszową", "Wejście na gotycką wieżę dawnego ratusza na Rynku Głównym.", 18.00m)
                }
            },
            new()
            {
                Id = 31,
                AttractionId = 6,
                Name = "Galeria Sztuki Polskiej XIX wieku w Sukiennicach",
                Description = "Słynna galeria Muzeum Narodowego z monumentalnymi płótnami Jana Matejki (\"Hołd pruski\"), Siemiradzkiego, Chełmońskiego i Malczewskiego.",
                Price = 44.00m,
                Date = today.AddDays(2),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Kawa i deser na tarasie Sukiennic z widokiem na Rynek", "Rezerwacja stolika w zabytkowych wnętrzach Kawiarni Noworolski.", 34.00m)
                }
            },
            new()
            {
                Id = 32,
                AttractionId = 7,
                Name = "Bazylika Mariacka i uroczyste otwarcie Ołtarza Wita Stwosza",
                Description = "Zwiedzanie prezbiterium i naw kościoła Mariackiego połączone z codzienną ceremonią odsłonięcia gotyckiego poliptyku Wita Stwosza.",
                Price = 36.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Lornetka muzealna do obserwacji detali rzeźbiarskich", "Wypożyczenie optyki pozwalającej dostrzec kunszt polichromii ołtarza.", 12.00m),
                    CreateAddon(ref nextAddonId, "Prelekcja o polichromii Matejki i witrażach Wyspiańskiego", "30-minutowy komentarz historyka sztuki.", 25.00m)
                }
            },
            new()
            {
                Id = 33,
                AttractionId = 7,
                Name = "Wejście na Wieżę Mariacką (Hejnalicę)",
                Description = "Pokonanie 271 stopni na wysokość 54 metrów do izby hejnalisty z bliskim widokiem na Rynek Główny i Wzgórze Wawelskie.",
                Price = 32.00m,
                Date = today.AddDays(4),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Pamiątkowa rycina panoramy Krakowa z Hejnalicy", "Ręcznie tłoczona grafika z pieczęcią.", 19.00m)
                }
            },
            new()
            {
                Id = 34,
                AttractionId = 8,
                Name = "Od Wawelu przez Skałkę po Kazimierz i Starą Synagogę",
                Description = "Spacer łączący podnóże Wawelu z Klasztorem Paulinów na Skałce (Krypta Zasłużonych), Placem Wolnica, ul. Szeroką i Placem Nowym.",
                Price = 72.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Bilety do Krypty Zasłużonych na Skałce oraz Starej Synagogi", "Wstęp do obu obiektów muzealnych na trasie spaceru.", 34.00m),
                    CreateAddon(ref nextAddonId, "Kultowa zapiekanka z Okrąglaka na Placu Nowym + lemoniada", "Przystanek kulinarny w sercu Kazimierza.", 26.00m)
                }
            },
            new()
            {
                Id = 35,
                AttractionId = 8,
                Name = "Wieczorny Kazimierz: koncert muzyki klezmerskiej na żywo",
                Description = "Nastrojowy spacer oświetlonymi zaułkami Kazimierza zakończony koncertem tradycyjnego tria klezmerskiego przy ul. Szerokiej.",
                Price = 149.00m,
                Date = today.AddDays(5),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Kolacja degustacyjna kuchni galicyjskiej i żydowskiej", "Przystawki, danie główne oraz chałka własnego wypieku.", 69.00m)
                }
            },
            new()
            {
                Id = 36,
                AttractionId = 9,
                Name = "Fabryka Schindlera – wystawa stała z licencjonowanym przewodnikiem",
                Description = "Narracyjna ekspozycja „Kraków – czas okupacji 1939–1945” w dawnym budynku administracyjnym fabryki Emalia na Zabłociu.",
                Price = 78.00m,
                Date = today.AddDays(3),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Spacer po Podgórzu, Placu Bohaterów Getta i Aptece pod Orłem", "Rozszerzenie trasy o 60 minut z przewodnikiem.", 35.00m),
                    CreateAddon(ref nextAddonId, "Bilet sąsiedzki do Muzeum Sztuki Współczesnej MOCAK", "Wstęp na wystawy współczesne tuż obok Fabryki Schindlera.", 24.00m)
                }
            },
            new()
            {
                Id = 37,
                AttractionId = 9,
                Name = "Zachód słońca na Kopcu Krakusa z panoramą Wawelu (termin miniony)",
                Description = "Popołudniowa wycieczka przez Kładkę Bernatka i Rynek Podgórski na prehistoryczny Kopiec Krakusa.",
                Price = 49.00m,
                Date = today.AddDays(-2),
                Status = OfferStatus.Planned
            },
            new()
            {
                Id = 38,
                AttractionId = 10,
                Name = "Kopiec Kościuszki, Muzeum Kościuszkowskie i panorama na Wawel i Tatry",
                Description = "Wejście na szczyt Kopca Kościuszki (326 m n.p.m.) ze wspaniałym widokiem na zakole Wisły, Wzgórze Wawelskie i Stare Miasto.",
                Price = 38.00m,
                Date = today.AddDays(2),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Podjazd meleksen widokowym przez Aleję Waszyngtona", "Wygodny przejazd spod Błoń pod sam fort na Wzgórzu św. Bronisławy.", 25.00m),
                    CreateAddon(ref nextAddonId, "Wystawa w Bastionie V Twierdzy Kraków", "Dodatkowa ekspozycja forteczna u podnóża kopca.", 16.00m)
                }
            },
            new()
            {
                Id = 39,
                AttractionId = 11,
                Name = "Zwiedzanie Opactwa w Tyńcu: krużganki, kościół i muzeum klasztorne",
                Description = "Wizyta w XI-wiecznym opactwie benedyktynów nad Wisłą: romańskie detale, barokowy kościół i dziedziniec ze studnią.",
                Price = 52.00m,
                Date = today.AddDays(6),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Degustacja produktów benedyktyńskich w kawiarni klasztornej", "Pierniki korzenne, konfitury, syropy ziołowe i kwas chlebowy.", 34.00m),
                    CreateAddon(ref nextAddonId, "Warsztat średniowiecznego skryptorium i kaligrafii", "Zajęcia pisania gęsim piórem prowadzone w muzeum opactwa.", 38.00m)
                }
            },
            new()
            {
                Id = 40,
                AttractionId = 12,
                Name = "Zamek Pieskowa Skała (oddział Wawelu) i Ojcowski Park Narodowy",
                Description = "Wycieczka spod Wawelu do jurajskiej rezydencji w Pieskowej Skale pod Maczugą Herkulesa, Bramy Krakowskiej i Jaskini Łokietka.",
                Price = 155.00m,
                Date = today.AddDays(8),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Komnaty Zamku w Pieskowej Skale – kolekcja wawelska", "Wstęp na wystawy stałe oddziału Zamku Królewskiego na Wawelu.", 35.00m),
                    CreateAddon(ref nextAddonId, "Tradycyjny wędzony pstrąg ojcowski nad potokiem", "Świeżo wędzony pstrąg z lokalnej hodowli w Dolinie Prądnika.", 46.00m)
                }
            },
            new()
            {
                Id = 41,
                AttractionId = 13,
                Name = "Kopalnia Soli Wieliczka – Trasa Turystyczna z transferem spod Wawelu",
                Description = "Klasyczny spacer podziemnymi komorami solnymi wraz z monumentalną Kaplicą św. Kingi i solankowymi jeziorami.",
                Price = 139.00m,
                Date = today,
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wejście do naziemnej Tężni Solankowej", "45-minutowa inhalacja mikroklimatem solankowym po wyjściu z podziemi.", 19.00m),
                    CreateAddon(ref nextAddonId, "Zjazd zabytkową windą górniczą", "Zjazd oryginalną szolą górniczą zamiast pokonywania 380 schodów.", 35.00m)
                }
            },
            new()
            {
                Id = 42,
                AttractionId = 13,
                Name = "Podziemna Trasa Górnicza w Wieliczce (aktywna ekspedycja)",
                Description = "Wyprawa w kaskach z lampami górniczymi do surowych wyrobisk solnych z pomiarem metanu i zadaniami górniczymi.",
                Price = 155.00m,
                Date = today.AddDays(5),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Obiad w Karczmie Górniczej 125 m pod ziemią", "Dwudaniowy tradycyjny posiłek serwowany w podziemnej komorze.", 65.00m),
                    CreateAddon(ref nextAddonId, "Imienny certyfikat adepta sztuki górniczej", "Pamiątkowy dyplom potwierdzający ukończenie ekspedycji.", 15.00m)
                }
            },
            new()
            {
                Id = 43,
                AttractionId = 14,
                Name = "Energylandia – bilet całodniowy Fast Pass z transferem z Krakowa",
                Description = "Całodzienny wstęp do wszystkich stref parku rozrywki w Zatorze z szybkim wejściem na rollercoastery i transportem.",
                Price = 239.00m,
                Date = today.AddDays(7),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Szafka depozytowa całodniowa", "Nielimitowany dostęp do zamykanej szafki na bagaż podręczny.", 20.00m),
                    CreateAddon(ref nextAddonId, "Voucher gastronomiczny All-Day", "Zestaw obiadowy, deser oraz napoje w strefach gastronomicznych.", 79.00m)
                }
            },
            new()
            {
                Id = 44,
                AttractionId = 15,
                Name = "Wyprawa spod Wawelu nad Morskie Oko o świcie z przewodnikiem",
                Description = "Poranny wyjazd z Krakowa na Palenicę Białczańską i wędrówka z licencjonowanym przewodnikiem tatrzańskim nad Morskie Oko.",
                Price = 195.00m,
                Date = today.AddDays(9),
                Status = OfferStatus.Planned,
                Addons = new List<Addon>
                {
                    CreateAddon(ref nextAddonId, "Wypożyczenie kijków trekkingowych", "Para lekkich regulowanych kijków na czas całej wycieczki.", 25.00m),
                    CreateAddon(ref nextAddonId, "Ciepły posiłek w schronisku PTTK nad Morskim Okiem", "Kwaśnica góralska lub szarlotka z gorącą herbatą.", 49.00m)
                }
            }
        };

        // =========================================================================
        // POWIĄZANIA DWUKIERUNKOWE – GĘSTA SIEĆ TRAS WAWELSKICH I OKOLICZNYCH
        // =========================================================================

        // Wewnątrz Zamku Królewskiego na Wawelu:
        ConnectById(offers, 1, 2);   // Komnaty Królewskie <-> Prywatne Apartamenty
        ConnectById(offers, 1, 3);   // Komnaty Królewskie <-> Skarbiec Koronny i Zbrojownia
        ConnectById(offers, 1, 4);   // Komnaty Królewskie <-> Wawel Podziemny
        ConnectById(offers, 1, 5);   // Komnaty Królewskie <-> Sztuka Wschodu (Namioty Tureckie)
        ConnectById(offers, 2, 6);   // Prywatne Apartamenty <-> Wawel Odzyskany i Gabinet Porcelany
        ConnectById(offers, 3, 4);   // Skarbiec i Zbrojownia <-> Wawel Podziemny
        ConnectById(offers, 3, 5);   // Skarbiec i Zbrojownia <-> Sztuka Wschodu
        ConnectById(offers, 5, 9);   // Sztuka Wschodu <-> Nocne zwiedzanie Zamku

        // Zamek Królewski <-> Katedra Wawelska:
        ConnectById(offers, 1, 11);  // Komnaty Królewskie <-> Katedra, Groby Królewskie i Dzwon Zygmunt
        ConnectById(offers, 2, 12);  // Prywatne Apartamenty <-> Kaplica Zygmuntowska z historykiem sztuki
        ConnectById(offers, 3, 14);  // Skarbiec ze Szczerbcem <-> Śladami koronacji królów w Katedrze
        ConnectById(offers, 3, 15);  // Skarbiec Koronny <-> Skarbiec Katedralny (Włócznia św. Maurycego)
        ConnectById(offers, 8, 11);  // Pakiet Królewski VIP <-> Katedra Wawelska pełna trasa
        ConnectById(offers, 9, 12);  // Nocne zwiedzanie Zamku <-> Kaplica Zygmuntowska
        ConnectById(offers, 11, 12); // Katedra pełna trasa <-> Kaplica Zygmuntowska
        ConnectById(offers, 11, 13); // Katedra pełna trasa <-> Krypta pod Wieżą Srebrnych Dzwonów i Muzeum Katedralne
        ConnectById(offers, 14, 15); // Śladami koronacji <-> Skarbiec Katedralny

        // Zamek i Katedra <-> Ogrody Królewskie, Baszty, Smocza Jama i Bulwary:
        ConnectById(offers, 1, 17);  // Komnaty Królewskie <-> Ogrody Królewskie Królowej Bony
        ConnectById(offers, 1, 22);  // Komnaty Królewskie <-> Smocza Jama
        ConnectById(offers, 2, 17);  // Prywatne Apartamenty <-> Ogrody Królewskie
        ConnectById(offers, 4, 18);  // Wawel Podziemny <-> Baszta Sandomierska i ganek obronny
        ConnectById(offers, 4, 19);  // Wawel Podziemny <-> Fortyfikacje Twierdzy Wawel
        ConnectById(offers, 7, 22);  // Wawel dla rodzin <-> Smocza Jama (idealny pakiet rodzinny!)
        ConnectById(offers, 11, 22); // Katedra Wawelska <-> Smocza Jama
        ConnectById(offers, 17, 18); // Ogrody Królewskie <-> Baszta Sandomierska
        ConnectById(offers, 18, 19); // Baszta Sandomierska <-> Fortyfikacje Twierdzy Wawel
        ConnectById(offers, 18, 20); // Baszta Sandomierska <-> Wschód słońca na tarasach Wawelu
        ConnectById(offers, 22, 23); // Smocza Jama <-> Panoramiczny rejs statkiem spod Wawelu
        ConnectById(offers, 23, 24); // Rejs statkiem <-> Rejs drewnianą gondolą o zachodzie słońca
        ConnectById(offers, 24, 26); // Rejs gondolą o zachodzie <-> Wieczorny spacer z latarnikiem po Podzamczu
        ConnectById(offers, 9, 26);  // Nocne zwiedzanie Wawelu <-> Wieczorny spacer z latarnikiem

        // Wawel <-> Ulica Kanonicza, Droga Królewska, Rynek, Kazimierz i okolice:
        ConnectById(offers, 1, 27);  // Komnaty Wawelskie <-> Ulica Kanonicza pod Wawelem
        ConnectById(offers, 1, 28);  // Komnaty Wawelskie <-> Spacer Drogą Królewską
        ConnectById(offers, 13, 27); // Muzeum Katedralne <-> Ulica Kanonicza (Muzeum Archidiecezjalne)
        ConnectById(offers, 27, 29); // Ulica Kanonicza <-> Pałac Biskupa Erazma Ciołka
        ConnectById(offers, 28, 30); // Spacer Drogą Królewską <-> Podziemia Rynku
        ConnectById(offers, 28, 32); // Spacer Drogą Królewską <-> Bazylika Mariacka i Ołtarz Wita Stwosza
        ConnectById(offers, 30, 31); // Podziemia Rynku <-> Galeria w Sukiennicach
        ConnectById(offers, 32, 33); // Bazylika Mariacka <-> Wieża Mariacka (Hejnalica)
        ConnectById(offers, 23, 34); // Rejs spod Wawelu <-> Spacer od Wawelu przez Skałkę na Kazimierz
        ConnectById(offers, 34, 35); // Spacer po Kazimierzu <-> Wieczorny koncert klezmerski
        ConnectById(offers, 34, 36); // Spacer po Kazimierzu <-> Fabryka Schindlera
        ConnectById(offers, 20, 38); // Wschód słońca na Wawelu <-> Kopiec Kościuszki (punkty widokowe)
        ConnectById(offers, 25, 39); // Rejs spod Wawelu do Tyńca <-> Zwiedzanie Opactwa w Tyńcu

        // Wawel <-> Zamek Pieskowa Skała (oddział Wawelu), Wieliczka i Tatry:
        ConnectById(offers, 1, 40);  // Zamek na Wawelu <-> Zamek Pieskowa Skała i Ojców (rezydencje królewskie)
        ConnectById(offers, 8, 40);  // Pakiet Królewski VIP <-> Zamek Pieskowa Skała
        ConnectById(offers, 1, 41);  // Zamek na Wawelu <-> Kopalnia Soli Wieliczka (duet UNESCO)
        ConnectById(offers, 41, 42); // Wieliczka Trasa Turystyczna <-> Wieliczka Trasa Górnicza
        ConnectById(offers, 40, 44); // Ojcowski Park Narodowy <-> Wyprawa nad Morskie Oko
        ConnectById(offers, 43, 44); // Energylandia <-> Wyprawa nad Morskie Oko

        InMemoryData.Offers.AddRange(offers);
    }

    private static Addon CreateAddon(ref int nextAddonId, string name, string description, decimal price)
    {
        return new Addon
        {
            Id = nextAddonId++,
            Name = name,
            Description = description,
            Price = price
        };
    }

    private static void ConnectById(List<Offer> offers, int firstId, int secondId)
    {
        if (firstId == secondId)
        {
            return;
        }

        var first = offers.FirstOrDefault(o => o.Id == firstId);
        var second = offers.FirstOrDefault(o => o.Id == secondId);

        if (first is null || second is null)
        {
            return;
        }

        if (first.Status == OfferStatus.Withdrawn || second.Status == OfferStatus.Withdrawn)
        {
            return;
        }

        if (!first.RelatedOfferIds.Contains(second.Id))
        {
            first.RelatedOfferIds.Add(second.Id);
        }

        if (!second.RelatedOfferIds.Contains(first.Id))
        {
            second.RelatedOfferIds.Add(first.Id);
        }
    }
}
