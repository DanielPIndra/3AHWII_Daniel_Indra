using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static readonly Dictionary<int, string> artikelVerzeichnis = new Dictionary<int, string>();

    static void Main()
    {
        Console.WriteLine("=== Uebung: Lagerverwaltung ===");
        Console.WriteLine();

        // Schritt 1: Array (Das feste Regal)
        Console.WriteLine("--- Schritt 1: Array (Das feste Regal) ---");
        string[] regalPlatze =
        {
            "Kupplung",
            "Zahnriemen",
            "Bremsscheibe",
            "Lichtmaschine",
            "Stoßdaempfer"
        };

        Array.Sort(regalPlatze);

        foreach (string teil in regalPlatze)
        {
            Console.WriteLine(teil);
        }

        Console.WriteLine();

        // Schritt 2: List (Die dynamische Einlagerung)
        Console.WriteLine("--- Schritt 2: List (Die dynamische Einlagerung) ---");
        List<string> eingangskorb = new List<string>();

        eingangskorb.Add("Schraube");
        eingangskorb.Add("Mutter");
        eingangskorb.Add("Bolzen");
        eingangskorb.Add("Feder");

        // Entfernt das zweite Element (Index 1)
        eingangskorb.RemoveAt(1);

        if (eingangskorb.Contains("Schraube"))
        {
            Console.WriteLine("Schraube ist noch im Eingangskorb enthalten.");
        }
        else
        {
            Console.WriteLine("Schraube ist nicht mehr im Eingangskorb enthalten.");
        }

        Console.WriteLine($"Anzahl verbleibender Teile: {eingangskorb.Count}");
        Console.WriteLine();

        // Schritt 3: Dictionary (Das Such-System)
        Console.WriteLine("--- Schritt 3: Dictionary (Das Such-System) ---");
        artikelVerzeichnis.Add(101, "Motor");
        artikelVerzeichnis.Add(102, "Getriebe");
        artikelVerzeichnis.Add(103, "Reifen");

        FindArtikel(102);
        FindArtikel(999);

        Console.WriteLine();
        Console.WriteLine("Alle Eintraege im Dictionary:");

        foreach (KeyValuePair<int, string> eintrag in artikelVerzeichnis)
        {
            Console.WriteLine($"ID: {eintrag.Key}, Teil: {eintrag.Value}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Ende der Uebung ===");
    }

    static void FindArtikel(int id)
    {
        if (artikelVerzeichnis.TryGetValue(id, out string? teilname))
        {
            Console.WriteLine($"ID {id} gefunden: {teilname}");
        }
        else
        {
            Console.WriteLine($"Fehler: Unbekannte Artikel-ID {id}.");
        }
    }
}
