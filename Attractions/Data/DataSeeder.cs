using Attractions.Models;

namespace Attractions.Data;

public static class DataSeeder
{
    public static void Seed()
    {
        JsonFeatureStore.LoadFeaturesOrInitializeDefaults();

        if (InMemoryData.Attractions.Count > 0 || InMemoryData.Offers.Count > 0)
        {
            return;
        }

        DateTime today = DateTime.Today;
        int nextAddonId = 1;

        Addon MakeAddon(string name, string description, decimal price) =>
            new(nextAddonId++, name, description, price);

        var attractions = new List<Attraction>
        {
            new(1, "Zamek Królewski na Wawelu – Komnaty i Wystawy",
                "Historyczna rezydencja królów Polski z dziedzińcem arkadowym, komnatami, apartamentami, skarbcem, zbrojownią i podziemiami.",
                "Kraków, Wawel 5", maxParticipants: 30),
            new(2, "Katedra Wawelska, Krypty i Dzwon Zygmunt",
                "Świątynia koronacyjna i nekropolia królów Polski z Kaplicą Zygmuntowską, Grobami Królewskimi, Skarbcem Katedralnym i Wieżą Zygmuntowską.",
                "Kraków, Wawel 3", maxParticipants: 25),
            new(3, "Ogrody Królewskie i Baszty Twierdzy Wawel",
                "Odtworzone renesansowe ogrody królowej Bony, Baszta Sandomierska, Baszta Złodziejska oraz południowe fortyfikacje wzgórza.",
                "Kraków, Wawel 5 (tarasy i mury obronne)", maxParticipants: 30),
            new(4, "Smocza Jama, Podzamcze i Bulwary pod Wawelem",
                "Wapienna jaskinia krasowa w zboczu Wawelu z rzeźbą Smoka Wawelskiego oraz przystanią statków i gondoli na Wiśle.",
                "Kraków, Wzgórze Wawelskie / Bulwar Czerwieński", maxParticipants: 40),
            new(5, "Ulica Kanonicza i Podwawelski Trakt Królewski",
                "Najstarsza i najpiękniejsza renesansowa ulica pod samym Wawelem z Domem Długosza i Pałacem Biskupa Erazma Ciołka.",
                "Kraków, ul. Kanonicza 1–25 (u stóp Wawelu)", maxParticipants: 25),
            new(6, "Rynek Główny, Sukiennice i Podziemia Rynku",
                "Centralny plac średniowiecznego Krakowa z multimedialnym muzeum pod płytą rynku, Sukiennicami i Wieżą Ratuszową.",
                "Kraków, Rynek Główny 1–3", maxParticipants: 35),
            new(7, "Bazylika Mariacka i Wieża Hejnalica",
                "Gotycka bazylika słynąca z monumentalnego ołtarza Wita Stwosza, polichromii Jana Matejki oraz hejnału mariackiego.",
                "Kraków, pl. Mariacki 5", maxParticipants: 30),
            new(8, "Dzielnica Kazimierz, Skałka i Stara Synagoga",
                "Sąsiadująca z Wawelem historyczna dzielnica łącząca Panteon Narodowy na Skałce z zabytkowymi synagogami Kazimierza.",
                "Kraków, ul. Skałeczna / ul. Szeroka 24", maxParticipants: 25),
            new(9, "Fabryka Emalia Oskara Schindlera i Podgórze",
                "Oddział Muzeum Krakowa prezentujący dzieje miasta w latach 1939–1945 w sąsiedztwie Rynku Podgórskiego i Kopca Krakusa.",
                "Kraków, ul. Lipowa 4", maxParticipants: 25),
            new(10, "Kopiec Kościuszki i Wzgórze św. Bronisławy",
                "Zabytkowy kopiec widokowy otoczony XIX-wiecznym fortem z panoramą Wzgórza Wawelskiego, Wisły i Tatr.",
                "Kraków, al. Waszyngtona 1", maxParticipants: 35),
            new(11, "Opactwo Benedyktynów w Tyńcu",
                "Najstarszy istniejący klasztor w Polsce z XI wieku, wznoszący się na wapiennym wzgórzu nad przełomem Wisły.",
                "Kraków-Tyniec, ul. Benedyktyńska 37", maxParticipants: 30),
            new(12, "Zamek Pieskowa Skała i Ojcowski Park Narodowy",
                "Renesansowa rezydencja obronna będąca oficjalnym oddziałem Zamku Królewskiego na Wawelu w sercu jurajskiej Doliny Prądnika.",
                "Sułoszowa / Ojców (oddział Zamku na Wawelu)", maxParticipants: 35),
            new(13, "Kopalnia Soli \"Wieliczka\"",
                "Zabytkowa podziemna kopalnia soli tuż pod Krakowem, wpisana wraz z Wawelem na pierwszą listę dziedzictwa UNESCO.",
                "Wieliczka, ul. Daniłowicza 10", maxParticipants: 35),
            new(14, "Park Rozrywki Energylandia",
                "Największy rodzinny park rozrywki w Małopolsce ze strefami ekstremalnymi, wodnymi i familijnymi.",
                "Zator, al. 3 Maja 2", maxParticipants: 60),
            new(15, "Morskie Oko i Dolina Rybiego Potoku",
                "Całodniowy szlak górski z wyjazdem spod Wawelu nad największe polodowcowe jezioro w Tatrach.",
                "Zakopane, Tatrzański Park Narodowy", maxParticipants: 20)
        };

        InMemoryData.Attractions.AddRange(attractions);

        var offers = new List<Offer>
        {
            // 1. ZAMEK KRÓLEWSKI NA WAWELU – KOMNATY I WYSTAWY
            new(1, 1, "Reprezentacyjne Komnaty Królewskie (Zamek II)",
                "Zwiedzanie sal II piętra zamku: Sala Poselska z rzeźbionymi głowami wawelskimi, Sala Senatorska i kolekcja arrasów Zygmunta Augusta.",
                89.00m, today, addons: new[]
                {
                    MakeAddon("Audioprzewodnik wielojęzyczny Wawel", "Zestaw słuchawkowy z narracją o komnatach i arrasach królewskich.", 18.00m),
                    MakeAddon("Oprowadzanie z licencjonowanym kustoszem", "Kameralny spacer w grupie do 12 osób z historykiem sztuki.", 45.00m),
                    MakeAddon("Album ilustrowany \"Zamek Królewski na Wawelu\"", "Wydawnictwo pamiątkowe w twardej oprawie odbierane w punkcie muzealnym.", 59.00m)
                }),

            new(2, 1, "Prywatne Apartamenty Królewskie z przewodnikiem (Zamek I)",
                "Wizyta w apartamentach mieszkalnych królów na I piętrze, sypialniach królewskich, gabinetach kolekcjonerskich i pokojach gościnnych.",
                92.00m, today, addons: new[]
                {
                    MakeAddon("Wejście do Renesansowej Wieży Sobieskiego (Kurzej Stopki)", "Dodatkowy wstęp do słynnych gabinetów alchemicznych Zygmunta III Wazy.", 28.00m),
                    MakeAddon("Zestaw pocztówek z reprodukcjami portretów Jagiellonów", "Kolekcjonerska teka 10 reprodukcji malarstwa dworskiego.", 22.00m)
                }),

            new(3, 1, "Skarbiec Koronny w nowej odsłonie i Zbrojownia ze Szczerbcem",
                "Klejnoty koronne, pamiątki po Jagiellonach i Wazach, miecz koronacyjny Szczerbiec, płaszcz kawalera Orderu Ducha Świętego i husarskie zbroje.",
                85.00m, today, addons: new[]
                {
                    MakeAddon("Zejście do piwnic Zbrojowni z kolekcją luf armatnich", "Rozszerzona trasa po gotyckich sklepionych podziemiach z artylerią.", 20.00m),
                    MakeAddon("Wybijanie pamiątkowego dukata królewskiego", "Własnoręcznie wybita replika renesansowej monety zygmuntowskiej.", 25.00m)
                }),

            new(4, 1, "Wawel Podziemny: Lapidarium, Międzymurze i Wawel Zaginiony",
                "Podziemny rezerwat archeologiczny prezentujący przedromańską Rotundę NMP z X wieku, relikty kuchni królewskich i multimedialne lapidarium.",
                55.00m, today.AddDays(1), addons: new[]
                {
                    MakeAddon("Pokaz rekonstrukcji 3D wczesnośredniowiecznego grodu", "Projekcja VR/3D ukazująca przemiany zabudowy Wawelu od X do XVI wieku.", 19.00m),
                    MakeAddon("Komentarz archeologa wawelskiego", "30-minutowe wprowadzenie przy reliktach przedromańskiej rotundy.", 30.00m)
                }),

            new(5, 1, "Sztuka Wschodu: Namioty Tureckie i trofea spod Wiednia",
                "Unikatowa na świecie kolekcja monumentalnych XVII-wiecznych namiotów osmańskich, chorągwi, szabel, kobierców perskich i porcelany wschodniej.",
                62.00m, today.AddDays(2), addons: new[]
                {
                    MakeAddon("Warsztat kaligrafii i ornamentyki orientalnej", "45-minutowe spotkanie edukacyjne w pracowni muzealnej.", 35.00m),
                    MakeAddon("Kawa po turecku i deser chałwowy w kawiarni zamkowej", "Tradycyjna kawa parzona w tygielku na dziedzińcu zewnętrznym.", 29.00m)
                }),

            new(6, 1, "Wawel Odzyskany oraz Gabinet Porcelany i Sreber Królewskich",
                "Opowieść o przywracaniu świetności zamkowi na początku XX wieku połączona z ekspozycją królewskiej porcelany miśnieńskiej.",
                48.00m, today.AddDays(3), addons: new[]
                {
                    MakeAddon("Filiżanka czekolady pitnej wg XVIII-wiecznej receptury", "Degustacja gorącej czekolady w kawiarni na Wzgórzu Wawelskim.", 26.00m)
                }),

            new(7, 1, "Wawel dla rodzin: \"Tajemnice Króla Zygmunta i Królowej Bony\"",
                "Interaktywne zwiedzanie zamku z edukatorem w stroju renesansowym, zagadkami dworskimi, przymierzaniem replik koron i dyplomem.",
                75.00m, today.AddDays(4), addons: new[]
                {
                    MakeAddon("Ilustrowany zeszyt zadań i naklejek \"Młody Dworzanin\"", "Książeczka edukacyjna z pieczęcią Zamku Królewskiego.", 19.00m),
                    MakeAddon("Warsztat tworzenia renesansowych pieczęci lakowych", "Zajęcia plastyczne w sali edukacyjnej po zwiedzaniu.", 28.00m)
                }),

            new(8, 1, "Pakiet Królewski VIP: Wawel Pełne Zwiedzanie w 1 Dzień",
                "Całodniowa przepustka premium obejmująca Komnaty, Prywatne Apartamenty, Skarbiec, Zbrojownię oraz Wawel Podziemny bez kolejek.",
                219.00m, today.AddDays(5), addons: new[]
                {
                    MakeAddon("Indywidualny przewodnik VIP na wyłączność (4 godziny)", "Dedykowany historyk sztuki dostosowujący tempo trasy do gości.", 190.00m),
                    MakeAddon("Dwudaniowy obiad staropolski w restauracji na Wzgórzu Wawelskim", "Przerwa obiadowa w trakcie zwiedzania z widokiem na katedrę.", 89.00m)
                }),

            new(9, 1, "Nocne zwiedzanie Zamku Królewskiego i krużganków z kustoszem",
                "Wieczorne oprowadzanie po zamknięciu bram wzgórza dla ruchu dziennego – iluminowany dziedziniec arkadowy i wybrane sale królewskie.",
                165.00m, today.AddDays(10), maxParticipants: 16, addons: new[]
                {
                    MakeAddon("Degustacja staropolskich miodów i przekąsek dworskich", "Wieczorny poczęstunek inspirowany kuchnią królowej Bony.", 58.00m),
                    MakeAddon("Kameralny recital muzyki renesansowej na lutni", "20-minutowy koncert na krużgankach zamkowych.", 45.00m)
                }),

            new(10, 1, "Pokaz specjalny chorągwi królewskich (oferta wycofana)",
                "Czasowa ekspozycja konserwatorska zabytkowych tkanin – oferta wycofana na czas prac renowacyjnych.",
                70.00m, today.AddDays(12), status: OfferStatus.Withdrawn, addons: new[]
                {
                    MakeAddon("Katalog wystawy konserwatorskiej", "Broszura opisująca proces renowacji chorągwi.", 24.00m)
                }),

            // 2. KATEDRA WAWELSKA, KRYPTY I DZWON ZYGMUNT
            new(11, 2, "Katedra Wawelska, Groby Królewskie i Dzwon Zygmunt",
                "Pełna trasa katedralna: nawa główna z konfesją św. Stanisława, zejście do krypt królewskich i wspinaczka na Wieżę Zygmuntowską.",
                58.00m, today, addons: new[]
                {
                    MakeAddon("Zestaw słuchawkowy Audio-Guide Katedra Wawelska", "Szczegółowy przewodnik głosowy po wszystkich 18 kaplicach bocznych.", 16.00m),
                    MakeAddon("Pamiątkowy medalion z wizerunkiem Dzwonu Zygmunt", "Mosiężny medalion wybijany w mennicy katedralnej.", 20.00m)
                }),

            new(12, 2, "Kaplica Zygmuntowska i Krypta Wieszczów z historykiem sztuki",
                "Tematyczne oprowadzanie poświęcone „perle renesansu z tej strony Alp” oraz sarkofagom Adama Mickiewicza i Juliusza Słowackiego.",
                79.00m, today.AddDays(1), addons: new[]
                {
                    MakeAddon("Przewodnik książkowy po kaplicach i nagrobkach katedralnych", "Bogato ilustrowana publikacja z planem krypt królewskich.", 28.00m)
                }),

            new(13, 2, "Krypta pod Wieżą Srebrnych Dzwonów i Muzeum Katedralne",
                "Zwiedzanie krypty z sarkofagiem Marszałka Józefa Piłsudskiego oraz Muzeum Katedralnego im. Jana Pawła II u wejścia na wzgórze.",
                54.00m, today.AddDays(2), addons: new[]
                {
                    MakeAddon("Komentarz przewodnika o pamiątkach papieskich i królewskich", "45-minutowe oprowadzanie po salach Muzeum Katedralnego.", 25.00m)
                }),

            new(14, 2, "Śladami koronacji królów Polski – trasa tematyczna w Katedrze",
                "Opowieść o ceremoniale koronacyjnym od Władysława Łokietka po Stanisława Augusta przy Ołtarzu Koronacyjnym i nagrobkach Jagiellonów.",
                84.00m, today.AddDays(6), addons: new[]
                {
                    MakeAddon("Reprint historycznego drzewa genealogicznego królów Polski", "Ozdobna plansza na papierze czerpanym w tubie.", 32.00m)
                }),

            new(15, 2, "Skarbiec Katedralny: Włócznia św. Maurycego i relikwiarze",
                "Specjalne wejście z kustoszem do Skarbca Katedralnego kryjącego najstarsze polskie regalia z roku 1000 i gotyckie złotnictwo.",
                68.00m, today.AddDays(8), maxParticipants: 15, addons: new[]
                {
                    MakeAddon("Lupa jubilerska i karta detali złotniczych", "Zestaw do podziwiania średniowiecznych filigranów i gemm.", 15.00m)
                }),

            new(16, 2, "Wieczorny koncert organowy w Katedrze Wawelskiej (termin miniony)",
                "Recital muzyki sakralnej Jana Sebastiana Bacha w historycznych wnętrzach katedry.",
                90.00m, today.AddDays(-3)),

            // 3. OGRODY KRÓLEWSKIE I BASZTY TWIERDZY WAWEL
            new(17, 3, "Ogrody Królewskie Królowej Bony i górny taras renesansowy",
                "Spacer po zrekonstruowanych dwupoziomowych ogrodach tarasowych ze szpalerami grabowymi, winnicą i ziołami dworskimi.",
                38.00m, today, addons: new[]
                {
                    MakeAddon("Mini-warsztat zielarski i saszetka ziół z ogrodów wawelskich", "Prezentacja renesansowych roślin leczniczych i aromatyczna pamiątka.", 26.00m),
                    MakeAddon("Lemoniada różana i kruche ciasteczka na tarasie widokowym", "Poczęstunek w letnim pawilonie przy ogrodach.", 22.00m)
                }),

            new(18, 3, "Baszta Sandomierska i spacer gankiem obronnym murów Wawelu",
                "Wejście na 7 kondygnacji XV-wiecznej baszty ogniowej i przejście historycznym gankiem straży z panoramą zakola Wisły i Kazimierza.",
                42.00m, today.AddDays(1), addons: new[]
                {
                    MakeAddon("Wejście rozszerzone na Basztę Senatorską", "Dostęp do drugiej potężnej baszty w południowym ciągu murów.", 18.00m)
                }),

            new(19, 3, "Fortyfikacje Twierdzy Wawel: Baszta Złodziejska i Bastion Władysława IV",
                "Militarna historia Wawelu od średniowiecznych baszt przez nowożytne basteje po XIX-wieczne kaponiery austriackiej cytadeli.",
                59.00m, today.AddDays(4), addons: new[]
                {
                    MakeAddon("Historyczna mapa fortyfikacji Twierdzy Kraków i Wawelu", "Rozkładany plan militarny z omówieniem systemów obronnych.", 21.00m)
                }),

            new(20, 3, "Wschód słońca na tarasach widokowych Wawelu – spacer fotograficzny",
                "Poranne wejście przed otwarciem dla turystów z fotografem krajobrazu – Zamek, Wisła, Stare Miasto i Tatry w porannym świetle.",
                110.00m, today.AddDays(7), maxParticipants: 12, addons: new[]
                {
                    MakeAddon("Wypożyczenie lekkiego statywu podróżnego", "Statyw fotograficzny na czas porannego pleneru na wzgórzu.", 25.00m),
                    MakeAddon("Śniadanie poranne z kawą speciality u stóp Wawelu", "Ciepły rogal maślany, jajecznica i kawa po zakończeniu sesji.", 39.00m)
                }),

            new(21, 3, "Plener malarski w Ogrodach Królewskich (oferta wycofana)",
                "Sezonowe warsztaty akwareli na górnym tarasie ogrodowym – edycja zakończona na sezon jesienno-zimowy.",
                95.00m, today.AddDays(15), status: OfferStatus.Withdrawn),

            // 4. SMOCZA JAMA, PODZAMCZE I BULWARY POD WAWELEM
            new(22, 4, "Zejście do Smoczej Jamy i spacer legend Wzgórza Wawelskiego",
                "Przejście krętymi schodami przez 81-metrową krasową jaskinię w skale wawelskiej prosto pod ziejącego ogniem Smoka Wawelskiego.",
                39.00m, today, addons: new[]
                {
                    MakeAddon("Pakiet rodzinny: gra terenowa \"Tropem Smoka Wawelskiego\"", "Zestaw zagadek z mapą wzgórza i pluszowym smokiem dla dzieci.", 29.00m),
                    MakeAddon("Gorąca czekolada i krakowski obwarzanek na Bulwarze Czerwieńskim", "Kupon do kawiarni widokowej naprzeciwko pomnika smoka.", 21.00m)
                }),

            new(23, 4, "Panoramiczny rejs statkiem po Wiśle spod Smoczej Jamy",
                "Godzinny rejs z przystani pod Wawelem wzdłuż murów zamkowych, kościoła na Skałce, Kazimierza, Kładki Bernatka i Cricoteki.",
                69.00m, today, addons: new[]
                {
                    MakeAddon("Miejsce na górnym pokładzie widokowym VIP", "Gwarantowane miejsce na otwartym tarasie z ciepłym kocem.", 20.00m),
                    MakeAddon("Zestaw kawowy z domową szarlotką na pokładzie", "Świeżo parzona kawa lub herbata oraz ciasto podczas rejsu.", 27.00m)
                }),

            new(24, 4, "Rejs drewnianą gondolą wokół zakola Wawelu o zachodzie słońca",
                "Kameralny 45-minutowy rejs tradycyjną drewnianą łodzią flisacką u podnóża oświetlonych murów Zamku i Katedry Wawelskiej.",
                119.00m, today.AddDays(2), maxParticipants: 12, addons: new[]
                {
                    MakeAddon("Kieliszek bezalkoholowego wina musującego i truskawki", "Poczęstunek serwowany podczas postoju widokowego pod Wawelem.", 34.00m),
                    MakeAddon("Pamiątkowa sesja zdjęciowa z pokładu na tle Wawelu", "5 profesjonalnych ujęć w wersji cyfrowej wykonanych przez sternika.", 35.00m)
                }),

            new(25, 4, "Rejs wycieczkowy spod Wawelu przełomem Wisły do Opactwa w Tyńcu",
                "Malownicza wyprawa wodna z Bulwaru Czerwieńskiego obok Klasztoru Norbertanek i Lasu Wolskiego pod samo wzgórze klasztorne w Tyńcu.",
                115.00m, today.AddDays(6), addons: new[]
                {
                    MakeAddon("Przewóz roweru na pokładzie (powrót Wiślaną Trasą Rowerową)", "Możliwość zabrania roweru i powrotu asfaltową trasą po wale wiślanym.", 25.00m),
                    MakeAddon("Kosz piknikowy ze specjałami małopolskimi", "Sery regionalne, pieczywo rzemieślnicze i tłoczony sok z sadów podkrakowskich.", 54.00m)
                }),

            new(26, 4, "Wieczorny spacer z latarnikiem: Legendy Podzamcza i zakola Wisły",
                "Klimatyczna opowieść o dawnych rybakach, flisakach, alchemiku Twardowskim i tajemnicach murów obronnych u stóp Wawelu.",
                56.00m, today.AddDays(3), addons: new[]
                {
                    MakeAddon("Rozgrzewająca herbata korzenna z miodem i pomarańczą", "Serwowana w kubku termicznym na zakończenie spaceru.", 18.00m)
                }),

            // 5. ULICA KANONICZA I PODWAWELSKI TRAKT KRÓLEWSKI
            new(27, 5, "Ulica Kanonicza pod Wawelem: Dom Długosza i renesansowe dziedzińce",
                "Spacer najstarszą ulicą pod samym wejściem na Wawel: kamienice kanoników kapituły katedralnej, sgraffita, portale i Plac św. Marii Magdaleny.",
                55.00m, today, addons: new[]
                {
                    MakeAddon("Wstęp do Muzeum Archidiecezjalnego Kardynała Karola Wojtyły", "Zwiedzanie dawnego domu kanoniczego przy ul. Kanoniczej 19–21.", 24.00m)
                }),

            new(28, 5, "Spacer Drogą Królewską: Barbakan – Rynek – Kanonicza – Wawel",
                "Piesza wycieczka historycznym szlakiem wjazdów koronacyjnych od Bramy Floriańskiej przez Rynek Główny i ul. Grodzką aż na dziedziniec Wawelu.",
                65.00m, today.AddDays(1), addons: new[]
                {
                    MakeAddon("Wstęp na dziedziniec i do sal Collegium Maius UJ", "Wizyta w XV-wiecznym sercu Uniwersytetu Jagiellońskiego po drodze.", 25.00m),
                    MakeAddon("Degustacja wypieków krakowskich na trasie", "Certyfikowany obwarzanek krakowski oraz sernik po krakowsku.", 32.00m)
                }),

            new(29, 5, "Pałac Biskupa Erazma Ciołka – sztuka dawnej Polski pod Wawelem",
                "Zwiedzanie oddziału Muzeum Narodowego przy ul. Kanoniczej 17 ze skarbami rzeźby gotyckiej (Madonna z Krużlowej) i malarstwa sakralnego.",
                46.00m, today.AddDays(5), addons: new[]
                {
                    MakeAddon("Oprowadzanie kuratorskie po kolekcji sztuki średniowiecznej", "60 minut ze specjalistą sztuki małopolskiej.", 30.00m)
                }),

            // 6–15. SĄSIEDNIE MIEJSCA W KRAKOWIE ORAZ OKOLICACH MAŁOPOLSKI
            new(30, 6, "Podziemia Rynku: Śladem europejskiej tożsamości Krakowa",
                "Zejście 4 metry pod płytę Rynku Głównego do średniowiecznych kramów kupieckich, warsztatów kowalskich i brukowanych traktów.",
                48.00m, today, addons: new[]
                {
                    MakeAddon("Oprowadzanie z przewodnikiem miejskim po Podziemiach", "90-minutowa opowieść o handlu i życiu dawnego Krakowa.", 35.00m),
                    MakeAddon("Bilet łączony na Wieżę Ratuszową", "Wejście na gotycką wieżę dawnego ratusza na Rynku Głównym.", 18.00m)
                }),

            new(31, 6, "Galeria Sztuki Polskiej XIX wieku w Sukiennicach",
                "Słynna galeria Muzeum Narodowego z monumentalnymi płótnami Jana Matejki (\"Hołd pruski\"), Siemiradzkiego, Chełmońskiego i Malczewskiego.",
                44.00m, today.AddDays(2), addons: new[]
                {
                    MakeAddon("Kawa i deser na tarasie Sukiennic z widokiem na Rynek", "Rezerwacja stolika w zabytkowych wnętrzach Kawiarni Noworolski.", 34.00m)
                }),

            new(32, 7, "Bazylika Mariacka i uroczyste otwarcie Ołtarza Wita Stwosza",
                "Zwiedzanie prezbiterium i naw kościoła Mariackiego połączone z codzienną ceremonią odsłonięcia gotyckiego poliptyku Wita Stwosza.",
                36.00m, today, addons: new[]
                {
                    MakeAddon("Lornetka muzealna do obserwacji detali rzeźbiarskich", "Wypożyczenie optyki pozwalającej dostrzec kunszt polichromii ołtarza.", 12.00m),
                    MakeAddon("Prelekcja o polichromii Matejki i witrażach Wyspiańskiego", "30-minutowy komentarz historyka sztuki.", 25.00m)
                }),

            new(33, 7, "Wejście na Wieżę Mariacką (Hejnalicę)",
                "Pokonanie 271 stopni na wysokość 54 metrów do izby hejnalisty z bliskim widokiem na Rynek Główny i Wzgórze Wawelskie.",
                32.00m, today.AddDays(4), addons: new[]
                {
                    MakeAddon("Pamiątkowa rycina panoramy Krakowa z Hejnalicy", "Ręcznie tłoczona grafika z pieczęcią.", 19.00m)
                }),

            new(34, 8, "Od Wawelu przez Skałkę po Kazimierz i Starą Synagogę",
                "Spacer łączący podnóże Wawelu z Klasztorem Paulinów na Skałce (Krypta Zasłużonych), Placem Wolnica, ul. Szeroką i Placem Nowym.",
                72.00m, today, addons: new[]
                {
                    MakeAddon("Bilety do Krypty Zasłużonych na Skałce oraz Starej Synagogi", "Wstęp do obu obiektów muzealnych na trasie spaceru.", 34.00m),
                    MakeAddon("Kultowa zapiekanka z Okrąglaka na Placu Nowym + lemoniada", "Przystanek kulinarny w sercu Kazimierza.", 26.00m)
                }),

            new(35, 8, "Wieczorny Kazimierz: koncert muzyki klezmerskiej na żywo",
                "Nastrojowy spacer oświetlonymi zaułkami Kazimierza zakończony koncertem tradycyjnego tria klezmerskiego przy ul. Szerokiej.",
                149.00m, today.AddDays(5), addons: new[]
                {
                    MakeAddon("Kolacja degustacyjna kuchni galicyjskiej i żydowskiej", "Przystawki, danie główne oraz chałka własnego wypieku.", 69.00m)
                }),

            new(36, 9, "Fabryka Schindlera – wystawa stała z licencjonowanym przewodnikiem",
                "Narracyjna ekspozycja „Kraków – czas okupacji 1939–1945” w dawnym budynku administracyjnym fabryki Emalia na Zabłociu.",
                78.00m, today.AddDays(3), addons: new[]
                {
                    MakeAddon("Spacer po Podgórzu, Placu Bohaterów Getta i Aptece pod Orłem", "Rozszerzenie trasy o 60 minut z przewodnikiem.", 35.00m),
                    MakeAddon("Bilet sąsiedzki do Muzeum Sztuki Współczesnej MOCAK", "Wstęp na wystawy współczesne tuż obok Fabryki Schindlera.", 24.00m)
                }),

            new(37, 9, "Zachód słońca na Kopcu Krakusa z panoramą Wawelu (termin miniony)",
                "Popołudniowa wycieczka przez Kładkę Bernatka i Rynek Podgórski na prehistoryczny Kopiec Krakusa.",
                49.00m, today.AddDays(-2)),

            new(38, 10, "Kopiec Kościuszki, Muzeum Kościuszkowskie i panorama na Wawel i Tatry",
                "Wejście na szczyt Kopca Kościuszki (326 m n.p.m.) ze wspaniałym widokiem na zakole Wisły, Wzgórze Wawelskie i Stare Miasto.",
                38.00m, today.AddDays(2), addons: new[]
                {
                    MakeAddon("Podjazd meleksen widokowym przez Aleję Waszyngtona", "Wygodny przejazd spod Błoń pod sam fort na Wzgórzu św. Bronisławy.", 25.00m),
                    MakeAddon("Wystawa w Bastionie V Twierdzy Kraków", "Dodatkowa ekspozycja forteczna u podnóża kopca.", 16.00m)
                }),

            new(39, 11, "Zwiedzanie Opactwa w Tyńcu: krużganki, kościół i muzeum klasztorne",
                "Wizyta w XI-wiecznym opactwie benedyktynów nad Wisłą: romańskie detale, barokowy kościół i dziedziniec ze studnią.",
                52.00m, today.AddDays(6), addons: new[]
                {
                    MakeAddon("Degustacja produktów benedyktyńskich w kawiarni klasztornej", "Pierniki korzenne, konfitury, syropy ziołowe i kwas chlebowy.", 34.00m),
                    MakeAddon("Warsztat średniowiecznego skryptorium i kaligrafii", "Zajęcia pisania gęsim piórem prowadzone w muzeum opactwa.", 38.00m)
                }),

            new(40, 12, "Zamek Pieskowa Skała (oddział Wawelu) i Ojcowski Park Narodowy",
                "Wycieczka spod Wawelu do jurajskiej rezydencji w Pieskowej Skale pod Maczugą Herkulesa, Bramy Krakowskiej i Jaskini Łokietka.",
                155.00m, today.AddDays(8), addons: new[]
                {
                    MakeAddon("Komnaty Zamku w Pieskowej Skale – kolekcja wawelska", "Wstęp na wystawy stałe oddziału Zamku Królewskiego na Wawelu.", 35.00m),
                    MakeAddon("Tradycyjny wędzony pstrąg ojcowski nad potokiem", "Świeżo wędzony pstrąg z lokalnej hodowli w Dolinie Prądnika.", 46.00m)
                }),

            new(41, 13, "Kopalnia Soli Wieliczka – Trasa Turystyczna z transferem spod Wawelu",
                "Klasyczny spacer podziemnymi komorami solnymi wraz z monumentalną Kaplicą św. Kingi i solankowymi jeziorami.",
                139.00m, today, addons: new[]
                {
                    MakeAddon("Wejście do naziemnej Tężni Solankowej", "45-minutowa inhalacja miklimatem solankowym po wyjściu z podziemi.", 19.00m),
                    MakeAddon("Zjazd zabytkową windą górniczą", "Zjazd oryginalną szolą górniczą zamiast pokonywania 380 schodów.", 35.00m)
                }),

            new(42, 13, "Podziemna Trasa Górnicza w Wieliczce (aktywna ekspedycja)",
                "Wyprawa w kaskach z lampami górniczymi do surowych wyrobisk solnych z pomiarem metanu i zadaniami górniczymi.",
                155.00m, today.AddDays(5), addons: new[]
                {
                    MakeAddon("Obiad w Karczmie Górniczej 125 m pod ziemią", "Dwudaniowy tradycyjny posiłek serwowany w podziemnej komorze.", 65.00m),
                    MakeAddon("Imienny certyfikat adepta sztuki górniczej", "Pamiątkowy dyplom potwierdzający ukończenie ekspedycji.", 15.00m)
                }),

            new(43, 14, "Energylandia – bilet całodniowy Fast Pass z transferem z Krakowa",
                "Całodzienny wstęp do wszystkich stref parku rozrywki w Zatorze z szybkim wejściem na rollercoastery i transportem.",
                239.00m, today.AddDays(7), addons: new[]
                {
                    MakeAddon("Szafka depozytowa całodniowa", "Nielimitowany dostęp do zamykanej szafki na bagaż podręczny.", 20.00m),
                    MakeAddon("Voucher gastronomiczny All-Day", "Zestaw obiadowy, deser oraz napoje w strefach gastronomicznych.", 79.00m)
                }),

            new(44, 15, "Wyprawa spod Wawelu nad Morskie Oko o świcie z przewodnikiem",
                "Poranny wyjazd z Krakowa na Palenicę Białczańską i wędrówka z licencjonowanym przewodnikiem tatrzańskim nad Morskie Oko.",
                195.00m, today.AddDays(9), addons: new[]
                {
                    MakeAddon("Wypożyczenie kijków trekkingowych", "Para lekkich regulowanych kijków na czas całej wycieczki.", 25.00m),
                    MakeAddon("Ciepły posiłek w schronisku PTTK nad Morskim Okiem", "Kwaśnica góralska lub szarlotka z gorącą herbatą.", 49.00m)
                }),

            // OFERTY DEDYKOWANE WYŁĄCZNIE DLA GRUP (NP. 4 OSÓB ORAZ WIĘKSZYCH ZESPOŁÓW)
            new(45, 1, "Królewska Gra Zamkowa: Tajemnica Alchemika (tylko grupa 4 os.)",
                "Zespołowa gra detektywistyczna w komnatach Wawelu i wokół gabinetów Zygmunta III Wazy, wymagająca ścisłej współpracy 4-osobowej drużyny.",
                89.00m, today.AddDays(1), minParticipants: 4, maxParticipants: 4, addons: new[]
                {
                    MakeAddon("Drewniana skrzynia alchemika z szyframi i mapą na własność", "Pamiątkowy zestaw rekwizytów dla 4-osobowej drużyny.", 32.00m),
                    MakeAddon("Dyplom Królewskiego Sekretarza z pieczęcią lakową", "Imienny certyfikat ukończenia misji na papierze czerpanym.", 15.00m)
                }),

            new(46, 4, "Prywatna czteroosobowa łódź flisacka pod Wawelem (tylko grupa 4 os.)",
                "Kameralny rejs na wyłączność tradycyjną 4-osobową łodzią ze sternikiem wzdłuż murów Wawelu, Skałki i Kazimierza.",
                95.00m, today, minParticipants: 4, maxParticipants: 4, addons: new[]
                {
                    MakeAddon("Patera serów i owoców małopolskich na pokładzie", "Zestaw degustacyjny przygotowany dla czteroosobowej załogi.", 38.00m)
                }),

            new(47, 13, "Zespół Ratowników Solnych w Wieliczce (tylko grupa 4 os.)",
                "Specjalna podziemna misja zadaniowa dokładnie dla 4-osobowego zastępu (nawigator, mierniczy, kronikarz i dowódca) ze zjazdem windą szybową.",
                145.00m, today.AddDays(3), minParticipants: 4, maxParticipants: 4, addons: new[]
                {
                    MakeAddon("Zestaw pamiątkowych lampionów solnych", "Ręcznie rzeźbiona bryła soli wielickiej dla każdego członka zastępu.", 29.00m)
                }),

            new(48, 14, "Pakiet Coaster Team 4-os. z prywatnym vanem spod Wawelu (tylko 4 os.)",
                "Oferta specjalna dla 4-osobowej ekipy: bezpośredni transfer prywatnym vanem spod Wawelu do Energylandii i wspólny pakiet wejść.",
                215.00m, today.AddDays(4), minParticipants: 4, maxParticipants: 4, addons: new[]
                {
                    MakeAddon("Pakiet zdjęć grupowych z fotoradarów na rollercoasterach", "Nielimitowane pobrania cyfrowych zdjęć całej czwórki.", 25.00m)
                }),

            new(49, 3, "Piknik Renesansowy Królowej Bony w altanie ogrodowej (grupa 4–6 os.)",
                "Kameralne spotkanie rodzinne lub towarzyskie dla grup od 4 do 6 osób w altanie Ogrodów Królewskich z opowieścią o kuchni dworskiej.",
                78.00m, today.AddDays(2), minParticipants: 4, maxParticipants: 6, addons: new[]
                {
                    MakeAddon("Kosz włoskich przysmaków i konfitur dworskich", "Focaccia ziołowa, sery dojrzewające, owoce i lemoniada kwiatowa.", 42.00m)
                }),

            new(50, 6, "Warsztaty dawnego rzemiosła i mennicy krakowskiej (grupa 6–15 os.)",
                "Zamknięte zajęcia warsztatowe dla grup zorganizowanych i wielopokoleniowych (6–15 osób) w Podziemiach Rynku z wybijaniem grosza.",
                64.00m, today.AddDays(5), minParticipants: 6, maxParticipants: 15, addons: new[]
                {
                    MakeAddon("Sakiewka skórzana z wybitym groszem krakowskim", "Własnoręcznie wybita moneta w rzemieślniczej sakiewce.", 22.00m)
                })
        };

        var pairs = new (int First, int Second)[]
        {
            (1, 2), (1, 3), (1, 4), (1, 5), (2, 6), (3, 4), (3, 5), (5, 9),
            (1, 11), (2, 12), (3, 14), (3, 15), (8, 11), (9, 12), (11, 12), (11, 13), (14, 15),
            (1, 17), (1, 22), (2, 17), (4, 18), (4, 19), (7, 22), (11, 22), (17, 18),
            (18, 19), (18, 20), (22, 23), (23, 24), (24, 26), (9, 26),
            (1, 27), (1, 28), (13, 27), (27, 29), (28, 30), (28, 32), (30, 31),
            (32, 33), (23, 34), (34, 35), (34, 36), (20, 38), (25, 39),
            (1, 40), (8, 40), (1, 41), (41, 42), (40, 44), (43, 44),
            (45, 46), (45, 49), (46, 49), (47, 48), (1, 45), (30, 50)
        };

        foreach (var (first, second) in pairs)
        {
            ConnectById(offers, first, second);
        }

        InMemoryData.Offers.AddRange(offers);

        SeedDefaultFeatureAssignments(attractions, offers);
        JsonFeatureStore.LoadOrSaveInitialAssignments();
    }

    private static void SeedDefaultFeatureAssignments(List<Attraction> attractions, List<Offer> offers)
    {
        int? fWheelchair = FindFeatureId("wheelchair", fallbackId: 1);
        int? fMinAge = FindFeatureId("min_age", fallbackId: 2);
        int? fMaxAge = FindFeatureId("max_age", fallbackId: 3);
        int? fGuardian = FindFeatureId("guardian_required", fallbackId: 4);
        int? fPhysical = FindFeatureId("physical_demand", fallbackId: 5);
        int? fMinHeight = FindFeatureId("min_height", fallbackId: 6);
        int? fChildFriendly = FindFeatureId("child_friendly", fallbackId: 7);
        int? fStroller = FindFeatureId("stroller_access", fallbackId: 8);
        int? fElevator = FindFeatureId("elevator", fallbackId: 9);
        int? fLanguage = InMemoryData.Features.FirstOrDefault(f => f.Id == 10 && f.Type == FeatureValueType.Text)?.Id;

        void SetAttr(int attrId, int? featureId, string val)
        {
            if (!featureId.HasValue) return;
            var a = attractions.FirstOrDefault(x => x.Id == attrId);
            var def = InMemoryData.Features.FirstOrDefault(f => f.Id == featureId.Value);
            if (a is not null && def is not null && def.TryNormalizeValue(val, out string norm, out _))
            {
                a.SetFeatureValue(def.Id, norm);
            }
        }

        void SetOff(int offerId, int? featureId, string val)
        {
            if (!featureId.HasValue) return;
            var o = offers.FirstOrDefault(x => x.Id == offerId);
            var def = InMemoryData.Features.FirstOrDefault(f => f.Id == featureId.Value);
            if (o is not null && def is not null && def.TryNormalizeValue(val, out string norm, out _))
            {
                o.SetFeatureValue(def.Id, norm);
            }
        }

        void SetAddonMod(int offerId, string addonNameFragment, int? featureId, string val, AddonEffectMode mode)
        {
            if (!featureId.HasValue) return;
            var o = offers.FirstOrDefault(x => x.Id == offerId);
            var addon = o?.Addons.FirstOrDefault(a => a.Name.Contains(addonNameFragment, StringComparison.OrdinalIgnoreCase));
            var def = InMemoryData.Features.FirstOrDefault(f => f.Id == featureId.Value);
            if (addon is not null && def is not null && def.TryNormalizeValue(val, out string norm, out _))
            {
                addon.SetFeatureModifier(def.Id, norm, mode);
            }
        }

        void ConfigureAttractionDefaults(
            int id,
            bool wheelchair,
            bool elevator,
            bool childFriendly,
            bool stroller,
            string physical,
            string languages)
        {
            SetAttr(id, fWheelchair, wheelchair ? "true" : "false");
            SetAttr(id, fElevator, elevator ? "true" : "false");
            SetAttr(id, fChildFriendly, childFriendly ? "true" : "false");
            SetAttr(id, fStroller, stroller ? "true" : "false");
            SetAttr(id, fPhysical, physical);
            SetAttr(id, fLanguage, languages);
        }

        ConfigureAttractionDefaults(1, true, true, true, false, "Niski", "Polski, Angielski, Niemiecki, Francuski");
        ConfigureAttractionDefaults(2, true, false, true, false, "Niski", "Polski, Angielski, Niemiecki");
        ConfigureAttractionDefaults(3, true, false, true, true, "Niski", "Polski, Angielski");
        ConfigureAttractionDefaults(4, true, false, true, true, "Niski", "Polski, Angielski");
        ConfigureAttractionDefaults(5, true, false, true, true, "Niski", "Polski, Angielski");
        ConfigureAttractionDefaults(6, true, true, true, true, "Niski", "Polski, Angielski, Niemiecki, Hiszpański");
        ConfigureAttractionDefaults(7, true, false, true, true, "Niski", "Polski, Angielski, Niemiecki");
        ConfigureAttractionDefaults(8, true, false, true, true, "Niski", "Polski, Angielski, Hebrajski");
        ConfigureAttractionDefaults(9, true, true, false, false, "Niski", "Polski, Angielski, Niemiecki");
        ConfigureAttractionDefaults(10, false, false, true, false, "Średni", "Polski, Angielski");
        ConfigureAttractionDefaults(11, true, true, true, true, "Niski", "Polski, Angielski");
        ConfigureAttractionDefaults(12, false, false, true, false, "Średni", "Polski, Angielski");
        ConfigureAttractionDefaults(13, true, true, true, false, "Średni", "Polski, Angielski, Niemiecki, Włoski");
        ConfigureAttractionDefaults(14, true, false, true, true, "Średni", "Polski, Angielski");
        ConfigureAttractionDefaults(15, false, false, false, false, "Wysoki", "Polski, Angielski");

        SetOff(3, fMinAge, "6");
        SetOff(4, fPhysical, "Średni");
        SetOff(7, fMinAge, "4");
        SetOff(7, fGuardian, "true");
        SetOff(7, fChildFriendly, "true");
        SetOff(8, fMinAge, "7");
        SetOff(8, fPhysical, "Średni");

        SetOff(9, fMinAge, "12");
        SetOff(9, fChildFriendly, "false");
        SetOff(9, fWheelchair, "false");
        SetOff(9, fElevator, "false");

        SetOff(11, fWheelchair, "false");
        SetOff(11, fPhysical, "Wysoki");
        SetOff(11, fMinAge, "7");
        SetOff(11, fMinHeight, "110");
        SetOff(11, fGuardian, "true");

        SetOff(15, fMinAge, "8");

        SetOff(18, fWheelchair, "false");
        SetOff(18, fStroller, "false");
        SetOff(18, fPhysical, "Wysoki");
        SetOff(18, fMinAge, "6");
        SetOff(18, fMaxAge, "80");
        SetOff(18, fGuardian, "true");

        SetOff(19, fWheelchair, "false");
        SetOff(19, fStroller, "false");
        SetOff(19, fPhysical, "Średni");
        SetOff(19, fMinAge, "8");

        SetOff(20, fMinAge, "10");
        SetOff(20, fStroller, "false");

        SetOff(22, fWheelchair, "false");
        SetOff(22, fStroller, "false");
        SetOff(22, fPhysical, "Średni");
        SetOff(22, fGuardian, "true");

        SetOff(24, fWheelchair, "false");
        SetOff(24, fStroller, "false");
        SetOff(24, fGuardian, "true");

        SetOff(26, fMinAge, "6");
        SetOff(28, fPhysical, "Średni");

        SetOff(33, fWheelchair, "false");
        SetOff(33, fStroller, "false");
        SetOff(33, fElevator, "false");
        SetOff(33, fPhysical, "Wysoki");
        SetOff(33, fMinAge, "8");
        SetOff(33, fMaxAge, "75");
        SetOff(33, fGuardian, "true");

        SetOff(35, fMinAge, "10");
        SetOff(35, fChildFriendly, "false");

        SetOff(36, fMinAge, "12");
        SetOff(36, fChildFriendly, "false");

        SetOff(40, fWheelchair, "false");
        SetOff(40, fStroller, "false");
        SetOff(40, fPhysical, "Średni");
        SetOff(40, fMinAge, "5");
        SetOff(40, fGuardian, "true");

        SetOff(41, fWheelchair, "true");
        SetOff(41, fElevator, "true");
        SetOff(41, fChildFriendly, "true");
        SetOff(41, fPhysical, "Średni");
        SetOff(41, fGuardian, "true");

        SetOff(42, fWheelchair, "false");
        SetOff(42, fElevator, "false");
        SetOff(42, fChildFriendly, "false");
        SetOff(42, fStroller, "false");
        SetOff(42, fMinAge, "10");
        SetOff(42, fMaxAge, "70");
        SetOff(42, fPhysical, "Wysoki");
        SetOff(42, fGuardian, "true");

        SetOff(43, fWheelchair, "false");
        SetOff(43, fStroller, "false");
        SetOff(43, fMinAge, "12");
        SetOff(43, fMaxAge, "70");
        SetOff(43, fMinHeight, "140");
        SetOff(43, fPhysical, "Wysoki");
        SetOff(43, fGuardian, "true");

        SetOff(44, fWheelchair, "false");
        SetOff(44, fStroller, "false");
        SetOff(44, fChildFriendly, "false");
        SetOff(44, fMinAge, "10");
        SetOff(44, fMaxAge, "75");
        SetOff(44, fPhysical, "Wysoki");
        SetOff(44, fGuardian, "true");

        SetOff(45, fMinAge, "8");
        SetOff(45, fGuardian, "true");
        SetOff(45, fChildFriendly, "true");

        SetOff(46, fWheelchair, "false");
        SetOff(46, fStroller, "false");
        SetOff(46, fGuardian, "true");

        SetOff(47, fWheelchair, "false");
        SetOff(47, fMinAge, "10");
        SetOff(47, fPhysical, "Średni");
        SetOff(47, fGuardian, "true");

        SetOff(48, fWheelchair, "false");
        SetOff(48, fStroller, "false");
        SetOff(48, fMinAge, "12");
        SetOff(48, fMinHeight, "140");
        SetOff(48, fPhysical, "Wysoki");
        SetOff(48, fGuardian, "true");

        SetOff(49, fWheelchair, "true");
        SetOff(49, fChildFriendly, "true");
        SetOff(49, fStroller, "true");
        SetOff(49, fPhysical, "Niski");

        SetOff(50, fWheelchair, "true");
        SetOff(50, fElevator, "true");
        SetOff(50, fChildFriendly, "true");
        SetOff(50, fMinAge, "6");
        SetOff(50, fGuardian, "true");

        SetAddonMod(2, "Wieży Sobieskiego", fWheelchair, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(2, "Wieży Sobieskiego", fElevator, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(2, "Wieży Sobieskiego", fPhysical, "Średni", AddonEffectMode.TightenRequirement);

        SetAddonMod(3, "piwnic Zbrojowni", fWheelchair, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(3, "piwnic Zbrojowni", fPhysical, "Średni", AddonEffectMode.TightenRequirement);

        SetAddonMod(9, "Degustacja staropolskich miodów", fMinAge, "18", AddonEffectMode.TightenRequirement);

        SetAddonMod(18, "Basztę Senatorską", fMinAge, "8", AddonEffectMode.TightenRequirement);
        SetAddonMod(18, "Basztę Senatorską", fPhysical, "Wysoki", AddonEffectMode.TightenRequirement);

        SetAddonMod(25, "Przewóz roweru", fMinAge, "10", AddonEffectMode.TightenRequirement);
        SetAddonMod(25, "Przewóz roweru", fPhysical, "Średni", AddonEffectMode.TightenRequirement);

        SetAddonMod(30, "Wieżę Ratuszową", fWheelchair, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(30, "Wieżę Ratuszową", fElevator, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(30, "Wieżę Ratuszową", fStroller, "false", AddonEffectMode.TightenRequirement);
        SetAddonMod(30, "Wieżę Ratuszową", fPhysical, "Wysoki", AddonEffectMode.TightenRequirement);
        SetAddonMod(30, "Wieżę Ratuszową", fMinAge, "6", AddonEffectMode.TightenRequirement);

        SetAddonMod(36, "Spacer po Podgórzu", fPhysical, "Średni", AddonEffectMode.TightenRequirement);

        SetAddonMod(38, "Podjazd meleksen", fWheelchair, "true", AddonEffectMode.ProvideAccommodation);
        SetAddonMod(38, "Podjazd meleksen", fPhysical, "Niski", AddonEffectMode.ProvideAccommodation);

        SetAddonMod(41, "windą górniczą", fElevator, "true", AddonEffectMode.ProvideAccommodation);
        SetAddonMod(41, "windą górniczą", fWheelchair, "true", AddonEffectMode.ProvideAccommodation);
        SetAddonMod(41, "windą górniczą", fPhysical, "Niski", AddonEffectMode.ProvideAccommodation);
    }

    private static int? FindFeatureId(string bindingKey, int fallbackId)
    {
        var byKey = InMemoryData.Features.FirstOrDefault(f =>
            string.Equals(f.ProfileBindingKey, bindingKey, StringComparison.OrdinalIgnoreCase));

        return byKey?.Id ?? InMemoryData.Features.FirstOrDefault(f => f.Id == fallbackId)?.Id;
    }

    private static void ConnectById(List<Offer> offers, int firstId, int secondId)
    {
        if (firstId == secondId)
        {
            return;
        }

        var first = offers.FirstOrDefault(o => o.Id == firstId);
        var second = offers.FirstOrDefault(o => o.Id == secondId);

        if (first is null || second is null ||
            first.Status == OfferStatus.Withdrawn || second.Status == OfferStatus.Withdrawn)
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
