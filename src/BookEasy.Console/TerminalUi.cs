
using System.Globalization;
using System.Text;
using BookEasy.Application;
using BookEasy.Core;

namespace BookEasy.ConsoleApp;

public sealed class TerminalUi
{
    private readonly BookingService _app;

    private const ConsoleColor Accent = ConsoleColor.Cyan;
    private const ConsoleColor Good = ConsoleColor.Green;
    private const ConsoleColor Bad = ConsoleColor.Red;
    private const ConsoleColor Warn = ConsoleColor.Yellow;
    private const ConsoleColor Dim = ConsoleColor.DarkGray;

    public TerminalUi(BookingService app)
    {
        _app = app;
    }

    // ==========================================
    // URUCHOMIENIE APLIKACJI
    // ==========================================

    public void Run()
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        try
        {
            System.Console.CursorVisible = false;

            while (true)
            {
                var bookings = _app.GetReservations();
                var today = DateTime.Today;

                var upcoming = bookings
                    .Where(x => x.Start >= DateTime.Now)
                    .OrderBy(x => x.Start)
                    .FirstOrDefault();

                string summary =
                    $"Wszystkie: {bookings.Count}  |  " +
                    $"Dzisiaj: {bookings.Count(x => x.Start.Date == today)}";

                if (upcoming != null)
                {
                    summary +=
                        $"  |  Najbliższa: {upcoming.Start:dd.MM HH:mm}";
                }

                int choice = Menu(
                    "CENTRUM REZERWACJI",
                    [
                        "NOWA REZERWACJA",
                        "PRZEGLĄD REZERWACJI",
                        "WYSZUKAJ KLIENTA",
                        "DOSTĘPNE TERMINY",
                        "ANULUJ WIZYTĘ",
                        "POMOC I SKRÓTY",
                        "ZAKOŃCZ PROGRAM"
                    ],
                    summary
                );

                switch (choice)
                {
                    case 0:
                        CreateBooking();
                        break;

                    case 1:
                        BrowseBookings();
                        break;

                    case 2:
                        SearchBookings();
                        break;

                    case 3:
                        AvailableSlots();
                        break;

                    case 4:
                        CancelBooking();
                        break;

                    case 5:
                        Help();
                        break;

                    case 6:
                        Goodbye();
                        return;

                    case -1:
                        // Esc lub Q w menu głównym:
                        // pozostajemy w aplikacji.
                        break;
                }
            }
        }
        finally
        {
            System.Console.ResetColor();

            try
            {
                System.Console.CursorVisible = true;
            }
            catch
            {
                // Niektóre terminale nie obsługują kursora.
            }
        }
    }

    // ==========================================
    // WYGLĄD I KOLORY
    // ==========================================

    private static int W =>
        Math.Max(1, Math.Min(66, System.Console.WindowWidth - 1));

    private static string Short(string text, int width)
    {
        if (width <= 0)
            return "";

        return text.Length <= width
            ? text
            : width > 3
                ? text[..(width - 3)] + "..."
                : text[..width];
    }

    private static void Line(
        string text = "",
        ConsoleColor color = ConsoleColor.Gray)
    {
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(Short(text, W));
        System.Console.ResetColor();
    }

    private static void Rule()
    {
        Line(new string('─', W), Dim);
    }

    private static void Header(
        string title,
        string? subtitle = null)
    {
        System.Console.ResetColor();
        System.Console.Clear();

        int inner = Math.Max(1, W - 2);

        Line("╔" + new string('═', inner) + "╗", Accent);

        Line(
            "║" +
            Short("  ◈ BOOKEASY  /  REZERWACJE", inner)
                .PadRight(inner) +
            "║",
            Accent
        );

        Line("╚" + new string('═', inner) + "╝", Accent);

        Line();
        Line("  " + title.ToUpperInvariant(), ConsoleColor.White);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            Line("  " + subtitle, Dim);
        }

        Line();
        Rule();
        Line();
    }

    private static void Section(string name)
    {
        Line();
        Line("  " + name.ToUpperInvariant(), Accent);
        Rule();
    }

    private static string Ask(string label)
    {
        System.Console.CursorVisible = true;
        System.Console.ForegroundColor = ConsoleColor.White;

        System.Console.Write("  " + label + ": ");

        System.Console.ResetColor();

        string result =
            System.Console.ReadLine()?.Trim() ?? "";

        System.Console.CursorVisible = false;

        return result;
    }

    private static void Pause()
    {
        Line();
        Line("  [Dowolny klawisz] Powrót do menu", Dim);

        System.Console.ReadKey(true);
    }

    private static void Notice(
        string message,
        ConsoleColor color = Good)
    {
        Line();
        Line("  " + message, color);

        Pause();
    }

    private static bool Confirm(string question)
    {
        Line();
        Line("  " + question, Warn);
        Line("  [T] Potwierdź     [N/Esc] Wróć", Dim);

        while (true)
        {
            var key = System.Console.ReadKey(true).Key;

            if (key == ConsoleKey.T)
                return true;

            if (key is ConsoleKey.N or ConsoleKey.Escape)
                return false;
        }
    }

    // ==========================================
    // MENU Z PRZEWIJANIEM
    // ==========================================

    private static int Menu(
        string title,
        IReadOnlyList<string> items,
        string? subtitle = null)
    {
        if (items.Count == 0)
            return -1;

        int selected = 0;
        int offset = 0;

        int top = 0;
        int visible = 0;

        int oldWidth = 0;
        int oldHeight = 0;

        void Paint(int row)
        {
            int index = offset + row;

            if (index < 0 ||
                index >= items.Count ||
                row < 0 ||
                row >= visible)
            {
                return;
            }

            System.Console.SetCursorPosition(
                0,
                top + row
            );

            bool active = index == selected;

            System.Console.ForegroundColor =
                active ? ConsoleColor.Black : ConsoleColor.Gray;

            System.Console.BackgroundColor =
                active ? Accent : ConsoleColor.Black;

            string text =
                (active ? "  > " : "    ") + items[index];

            int width = Math.Max(
                1,
                System.Console.WindowWidth - 2
            );

            System.Console.Write(
                Short(text, width).PadRight(width)
            );

            System.Console.ResetColor();
        }

        void Render()
        {
            Header(title, subtitle);

            top = System.Console.CursorTop;

            visible = Math.Max(
                1,
                Math.Min(
                    items.Count,
                    System.Console.WindowHeight - top - 4
                )
            );

            offset = Math.Clamp(
                offset,
                0,
                Math.Max(0, items.Count - visible)
            );

            for (int i = 0; i < visible; i++)
            {
                Paint(i);
                System.Console.WriteLine();
            }

            Line();

            Line(
                $"  {selected + 1}/{items.Count}  " +
                "↑↓ Wybór  PgUp/PgDn Przewijanie",
                Dim
            );

            Line(
                "  Enter Wybierz   Esc/Q Wstecz   Home/End",
                Dim
            );

            oldWidth = System.Console.WindowWidth;
            oldHeight = System.Console.WindowHeight;
        }

        void SmallScreen()
        {
            System.Console.Clear();

            Line("  BOOKEASY", Accent);
            Line("  Powiększ terminal do minimum 40x16.", Warn);
            Line("  Naciśnij klawisz po zmianie rozmiaru.", Dim);
        }

        void SafeRender()
        {
            if (System.Console.WindowWidth < 40 ||
                System.Console.WindowHeight < 16)
            {
                SmallScreen();
                return;
            }

            try
            {
                Render();
            }
            catch (ArgumentOutOfRangeException)
            {
                SmallScreen();
            }
            catch (IOException)
            {
                SmallScreen();
            }
        }

        SafeRender();

        while (true)
        {
            var key = System.Console.ReadKey(true).Key;

            // Esc i Q cofają z menu.
            if (key is ConsoleKey.Escape or ConsoleKey.Q)
                return -1;

            if (System.Console.WindowWidth < 40 ||
                System.Console.WindowHeight < 16)
            {
                SafeRender();
                continue;
            }

            if (key == ConsoleKey.Enter)
                return selected;

            int previous = selected;

            selected = key switch
            {
                ConsoleKey.UpArrow =>
                    (selected - 1 + items.Count) % items.Count,

                ConsoleKey.DownArrow =>
                    (selected + 1) % items.Count,

                ConsoleKey.Home => 0,

                ConsoleKey.End => items.Count - 1,

                ConsoleKey.PageUp =>
                    Math.Max(
                        0,
                        selected - Math.Max(1, visible)
                    ),

                ConsoleKey.PageDown =>
                    Math.Min(
                        items.Count - 1,
                        selected + Math.Max(1, visible)
                    ),

                _ => selected
            };

            if (selected == previous &&
                oldWidth == System.Console.WindowWidth &&
                oldHeight == System.Console.WindowHeight)
            {
                continue;
            }

            int previousOffset = offset;

            if (selected < offset)
            {
                offset = selected;
            }
            else if (selected >= offset + visible)
            {
                offset = selected - visible + 1;
            }

            if (previousOffset != offset ||
                oldWidth != System.Console.WindowWidth ||
                oldHeight != System.Console.WindowHeight)
            {
                SafeRender();
                continue;
            }

            try
            {
                Paint(previous - offset);
                Paint(selected - offset);

                System.Console.SetCursorPosition(
                    0,
                    top + visible + 1
                );

                Line(
                    $"  {selected + 1}/{items.Count}  " +
                    "↑↓ Wybór  PgUp/PgDn Przewijanie",
                    Dim
                );
            }
            catch (ArgumentOutOfRangeException)
            {
                SafeRender();
            }
            catch (IOException)
            {
                SafeRender();
            }
        }
    }

    // ==========================================
    // WALIDACJA DATY
    // ==========================================

    private static DateOnly? AskDate()
    {
        Line("  Format: RRRR-MM-DD, np. 2026-11-12", Dim);
        Line("  Puste pole oznacza powrót.", Dim);
        Line();

        while (true)
        {
            string value = Ask("Data");

            if (string.IsNullOrWhiteSpace(value))
                return null;

            bool valid = DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date
            );

            if (!valid)
            {
                Line(
                    "  [X] Niepoprawny format daty.",
                    Bad
                );

                Line(
                    "  Przykład: 2026-11-12",
                    Dim
                );

                continue;
            }

            if (date < DateOnly.FromDateTime(DateTime.Today))
            {
                Line(
                    "  [X] Data nie może być w przeszłości.",
                    Bad
                );

                continue;
            }

            return date;
        }
    }

    // ==========================================
    // WALIDACJA NUMERU TELEFONU
    // ==========================================

    private static string AskPhone()
    {
        Line();
        Line("  NUMER TELEFONU", Accent);
        Line("  Wymagane dokładnie 9 cyfr.", Dim);
        Line("  Przykład: 501234567", Dim);
        Line("  Nie wpisuj spacji ani prefiksu +48.", Dim);
        Line("  Puste pole = anulowanie.", Dim);
        Line();

        while (true)
        {
            string phone = Ask("Numer telefonu");

            if (string.IsNullOrWhiteSpace(phone))
            {
                return "";
            }

            if (phone.Length != 9)
            {
                Line(
                    $"  [X] Wpisano {phone.Length} znaków, wymagane 9.",
                    Bad
                );

                Line(
                    "  Przykład poprawnego numeru: 501234567",
                    Dim
                );

                continue;
            }

            if (!phone.All(char.IsAsciiDigit))
            {
                Line(
                    "  [X] Numer może zawierać tylko cyfry 0-9.",
                    Bad
                );

                Line(
                    "  Usuń litery, spacje i znaki specjalne.",
                    Dim
                );

                continue;
            }

            Line(
                "  [OK] Poprawny format numeru telefonu.",
                Good
            );

            return phone;
        }
    }

    // ==========================================
    // TWORZENIE REZERWACJI
    // ==========================================

    private void CreateBooking()
    {
        int serviceIndex = Menu(
            "NOWA REZERWACJA",
            _app.Services.Select(s =>
                $"{s.Name}  /  {s.DurationMinutes} min  /  {s.Price} zł"
            ).ToList(),
            "ETAP 1 Z 3 - WYBÓR USŁUGI"
        );

        if (serviceIndex < 0)
            return;

        var service = _app.Services[serviceIndex];

        Header(
            "NOWA REZERWACJA",
            "ETAP 2 Z 3 - TERMIN"
        );

        var date = AskDate();

        if (date == null)
            return;

        var slots = _app.GetAvailableSlots(
            date.Value,
            service.Id
        );

        if (slots.Count == 0)
        {
            Header("BRAK TERMINÓW");

            Notice(
                "Nie znaleziono wolnych godzin.",
                Warn
            );

            return;
        }

        int slotIndex = Menu(
            "WYBIERZ GODZINĘ",
            slots.Select(t =>
                t.ToString("HH:mm")
            ).ToList(),
            $"{date:dd.MM.yyyy}  /  {service.Name}"
        );

        if (slotIndex < 0)
            return;

        var start = date.Value.ToDateTime(
            slots[slotIndex]
        );

        Header(
            "DANE KLIENTA",
            "ETAP 3 Z 3 - FORMULARZ"
        );

        string name = Ask("Imię i nazwisko");

        if (string.IsNullOrWhiteSpace(name))
        {
            Notice(
                "Imię i nazwisko są wymagane.",
                Bad
            );

            return;
        }

        // Walidacja numeru telefonu.
        string phone = AskPhone();

        if (string.IsNullOrWhiteSpace(phone))
        {
            Notice(
                "Tworzenie rezerwacji zostało przerwane.",
                Warn
            );

            return;
        }

        Header("PODSUMOWANIE");

        Section("KLIENT");
        Line($"  {name}");
        Line($"  Telefon: {phone}");

        Section("WIZYTA");
        Line($"  Usługa:    {service.Name}");
        Line($"  Data:      {start:dd.MM.yyyy}");
        Line($"  Godzina:   {start:HH:mm}");
        Line($"  Czas:      {service.DurationMinutes} minut");
        Line($"  Cena:      {service.Price} zł", Good);

        if (!Confirm("Czy potwierdzasz rezerwację?"))
            return;

        try
        {
            _app.Create(
                name,
                phone,
                service.Id,
                start
            );

            Header("GOTOWE");

            Notice(
                "[OK] Rezerwacja została zapisana!",
                Good
            );
        }
        catch (ArgumentException ex)
        {
            Header("NIE UDAŁO SIĘ");

            Notice(
                ex.Message,
                Bad
            );
        }
    }

    // ==========================================
    // PRZEGLĄD REZERWACJI
    // ==========================================

    private void BrowseBookings()
    {
        int filter = Menu(
            "PRZEGLĄD REZERWACJI",
            [
                "WSZYSTKIE REZERWACJE",
                "TYLKO DZISIAJ",
                "NADCHODZĄCE WIZYTY",
                "MINIONE WIZYTY"
            ],
            "Wybierz sposób filtrowania"
        );

        if (filter < 0)
            return;

        var reservations = _app.GetReservations()
            .OrderBy(x => x.Start)
            .ToList();

        reservations = filter switch
        {
            1 => reservations
                .Where(x =>
                    x.Start.Date == DateTime.Today)
                .ToList(),

            2 => reservations
                .Where(x =>
                    x.Start >= DateTime.Now)
                .ToList(),

            3 => reservations
                .Where(x =>
                    x.Start < DateTime.Now)
                .ToList(),

            _ => reservations
        };

        SelectReservation(
            reservations,
            "LISTA REZERWACJI"
        );
    }

    private void SelectReservation(
        IReadOnlyList<Reservation> reservations,
        string heading)
    {
        if (reservations.Count == 0)
        {
            Header(heading);

            Notice(
                "Brak rezerwacji spełniających kryteria.",
                Warn
            );

            return;
        }

        int index = Menu(
            heading,
            reservations.Select(r =>
                $"{r.Start:dd.MM HH:mm}  |  {r.ClientName}"
            ).ToList(),
            $"Znaleziono: {reservations.Count}"
        );

        if (index < 0)
            return;

        ShowDetails(reservations[index]);
    }

    private void ShowDetails(Reservation reservation)
    {
        var service = _app.GetService(
            reservation.ServiceId
        );

        Header("SZCZEGÓŁY REZERWACJI");

        bool upcoming =
            reservation.Start >= DateTime.Now;

        Line(
            upcoming
                ? "  STATUS: NADCHODZĄCA"
                : "  STATUS: MINIONA",
            upcoming ? Good : Dim
        );

        Section("DANE KLIENTA");

        Line(
            $"  Imię i nazwisko: {reservation.ClientName}"
        );

        Line(
            $"  Telefon:         {reservation.Phone}"
        );

        Section("INFORMACJE O WIZYCIE");

        Line($"  Usługa:          {service.Name}");
        Line($"  Data:            {reservation.Start:dd.MM.yyyy}");
        Line($"  Godzina:         {reservation.Start:HH:mm}");
        Line($"  Czas trwania:    {service.DurationMinutes} min");
        Line($"  Cena:            {service.Price} zł", Good);

        Pause();
    }

    // ==========================================
    // WYSZUKIWANIE REZERWACJI
    // ==========================================

    private void SearchBookings()
    {
        Header("WYSZUKIWARKA REZERWACJI");

        Line(
            "  Wpisz imię, nazwisko lub numer telefonu.",
            Dim
        );

        Line(
            "  Wielkość liter nie ma znaczenia.",
            Dim
        );

        Line();

        string query = Ask("Szukaj");

        if (string.IsNullOrWhiteSpace(query))
            return;

        var results = _app.GetReservations()
            .Where(r =>
                r.ClientName.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                r.Phone.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .OrderBy(r => r.Start)
            .ToList();

        SelectReservation(
            results,
            "WYNIKI WYSZUKIWANIA"
        );
    }

    // ==========================================
    // DOSTĘPNE TERMINY
    // ==========================================

    private void AvailableSlots()
    {
        int index = Menu(
            "DOSTĘPNOŚĆ WIZYT",
            _app.Services.Select(s =>
                $"{s.Name} / {s.DurationMinutes} min"
            ).ToList(),
            "Wybierz usługę"
        );

        if (index < 0)
            return;

        var service = _app.Services[index];

        Header("SPRAWDŹ TERMIN");

        var date = AskDate();

        if (date == null)
            return;

        var slots = _app.GetAvailableSlots(
            date.Value,
            service.Id
        );

        if (slots.Count == 0)
        {
            Header("DOSTĘPNOŚĆ");

            Notice(
                "Brak wolnych terminów tego dnia.",
                Warn
            );

            return;
        }

        int chosen = Menu(
            "WOLNE GODZINY",
            slots.Select(s =>
                s.ToString("HH:mm")
            ).ToList(),
            $"{date:dd.MM.yyyy} | Wolne: {slots.Count}"
        );

        if (chosen < 0)
            return;

        Header("TERMIN DOSTĘPNY");

        Line(
            $"  Data:    {date:dd.MM.yyyy}",
            Good
        );

        Line(
            $"  Godzina: {slots[chosen]:HH:mm}",
            Good
        );

        Line(
            $"  Usługa:  {service.Name}"
        );

        Notice(
            "Termin jest obecnie wolny.",
            Good
        );
    }

    // ==========================================
    // ANULOWANIE REZERWACJI
    // ==========================================

    private void CancelBooking()
    {
        var bookings = _app.GetReservations()
            .Where(r =>
                r.Start >= DateTime.Now)
            .OrderBy(r => r.Start)
            .ToList();

        if (bookings.Count == 0)
        {
            Header("ANULOWANIE");

            Notice(
                "Brak nadchodzących rezerwacji.",
                Warn
            );

            return;
        }

        int index = Menu(
            "ANULUJ REZERWACJĘ",
            bookings.Select(r =>
                $"{r.Start:dd.MM HH:mm}  |  {r.ClientName}"
            ).ToList(),
            "Wybierz wizytę do anulowania"
        );

        if (index < 0)
            return;

        var booking = bookings[index];

        var service = _app.GetService(
            booking.ServiceId
        );

        Header("POTWIERDZENIE ANULOWANIA");

        Line(
            $"  Klient:  {booking.ClientName}"
        );

        Line(
            $"  Termin:  {booking.Start:dd.MM.yyyy HH:mm}"
        );

        Line(
            $"  Usługa:  {service.Name}"
        );

        if (!Confirm("Czy na pewno anulować tę wizytę?"))
        {
            Notice(
                "Operacja została przerwana.",
                Warn
            );

            return;
        }

        bool success = _app.Cancel(
            booking.Id
        );

        Header("WYNIK OPERACJI");

        Notice(
            success
                ? "[OK] Wizyta została anulowana."
                : "[X] Nie znaleziono rezerwacji.",
            success ? Good : Bad
        );
    }

    // ==========================================
    // POMOC I SKRÓTY
    // ==========================================

    private static void Help()
    {
        Header("POMOC I SKRÓTY");

        Section("NAWIGACJA");

        Line("  Strzałka w górę    Poprzednia opcja");
        Line("  Strzałka w dół     Następna opcja");
        Line("  Enter              Wybierz opcję");
        Line("  Esc / Q            Powrót z menu");
        Line("  Home / End         Początek / koniec");
        Line("  PgUp / PgDn        Przewiń listę");

        Section("KOLORY");

        Line("  TURKUSOWY          Aktywny element", Accent);
        Line("  ZIELONY            Sukces / dostępność", Good);
        Line("  ŻÓŁTY              Ostrzeżenie", Warn);
        Line("  CZERWONY           Błąd", Bad);

        Section("DANE KLIENTA");

        Line("  Numer telefonu musi mieć dokładnie 9 cyfr.");
        Line("  Przykład: 501234567", Good);
        Line("  Bez spacji, liter i prefiksu +48.");

        Section("INFORMACJE");

        Line("  Godziny pracy: 09:00 - 17:00");
        Line("  Rezerwacje zapisywane są automatycznie.");
        Line("  System zapobiega nakładaniu się wizyt.");

        Pause();
    }

    // ==========================================
    // ZAMKNIĘCIE
    // ==========================================

    private static void Goodbye()
    {
        Header("DO ZOBACZENIA");

        Line(
            "  Dziękujemy za korzystanie z BookEasy.",
            Good
        );

        Line(
            "  Wszystkie zapisane dane pozostają w systemie."
        );

        Line();
    }
}


