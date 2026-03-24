class Program
{
    static void Main()
    {
        Einkaufsliste liste = new Einkaufsliste();

        string[] startArtikel =
        {
            "Milch", "Brot", "Ei", "Butter", "Salz", "Reis", "Nudeln", "Apfel", "Tee", "Honig", "Kaese"
        };

        Console.WriteLine("=== Hinzufuegen mit out ===");
        foreach (string artikel in startArtikel)
        {
            bool erfolg = liste.VersucheHinzufuegen(artikel, out string meldung);
            Console.WriteLine($"{erfolg}: {meldung}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Suchen mit break ===");
        Console.WriteLine($"Enthaelt 'Brot': {liste.Enthaelt("Brot")}");
        Console.WriteLine($"Enthaelt 'Wasser': {liste.Enthaelt("Wasser")}");

        Console.WriteLine();
        Console.WriteLine("=== Ausgabe mit continue ===");
        liste.GibKurzeNamenAus(6);

        Console.WriteLine();
        Console.WriteLine("=== Bonus: String-Vergleich ===");
        string a = "Milch";
        string b = "Mil" + "ch";

        var vergleich = liste.VergleicheStrings(a, b);
        Console.WriteLine($"a == b: {vergleich.GleichMitOperator}");
        Console.WriteLine($"a.Equals(b): {vergleich.GleichMitEquals}");
        Console.WriteLine($"Gleiches Ergebnis: {vergleich.GleichesErgebnis}");

        Console.WriteLine();
        Console.WriteLine($"Anzahl in der Liste: {liste.Anzahl}");
    }
}
