using BookEasy.ConsoleApp;
using BookEasy.Application;
using BookEasy.Infrastructure;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BookEasy", "reservations.json");
var app = new BookingService(new JsonReservationRepository(dataPath));
var ui = new TerminalUi(app);
ui.Run();
