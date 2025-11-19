using System.Collections.Specialized;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World als Klasse!");

        // Hilfe & Argumentprüfung
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            Console.WriteLine("Benutzung: dotnet run --project 2025-11-12_HÜ -- \"<bruch1>\" \"<bruch2>\"");
            Console.WriteLine("Formate: \"g a/b\" (gemischter Bruch), \"a/b\" oder \"n\" (ganze Zahl)");
            Console.WriteLine("Beispiel: dotnet run --project 2025-11-12_HÜ -- \"2 3/8\" \"1 5/6\"");
            return;
        }

        if (args.Length != 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Fehler: Bitte genau 2 Argumente (zwei Brüche) angeben. Für Hilfe: --help");
            Console.ResetColor();
            return;
        }

        try
        {
            // Setze hier einen Breakpoint (klicke links neben die Zeilennummer):
            // -> Breakpoint sinnvoll direkt vor der ersten Bruch-Erzeugung
            Bruch b1 = new Bruch(args[0]);
            Bruch b2 = new Bruch(args[1]);
            Bruch b3 = b1.addiere(b2);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ergebnis: " + b3.ToString());
            Console.ResetColor();
        }
        catch (FormatException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Fehler: Ungültiges Eingabeformat - " + ex.Message);
            Console.ResetColor();
            Environment.ExitCode = 2;
        }
        catch (ArgumentException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Fehler: " + ex.Message);
            Console.ResetColor();
            Environment.ExitCode = 3;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Unbekannter Fehler: " + ex.Message);
            Console.ResetColor();
            Environment.ExitCode = 1;
        }
    }
}