public class Einkaufsliste
{
    private readonly string[] _artikel = new string[10];
    private int _anzahl;

    public int Anzahl => _anzahl;

    public bool VersucheHinzufuegen(string artikel, out string meldung)
    {
        if (string.IsNullOrWhiteSpace(artikel))
        {
            meldung = "Artikelname darf nicht leer sein.";
            return false;
        }

        if (_anzahl >= _artikel.Length)
        {
            meldung = "Kein Platz mehr: Die Einkaufsliste ist voll.";
            return false;
        }

        _artikel[_anzahl] = artikel.Trim();
        _anzahl++;
        meldung = $"'{artikel.Trim()}' wurde hinzugefuegt.";
        return true;
    }

    public bool Enthaelt(string gesuchterArtikel)
    {
        if (string.IsNullOrWhiteSpace(gesuchterArtikel))
        {
            return false;
        }

        bool gefunden = false;

        for (int i = 0; i < _anzahl; i++)
        {
            if (_artikel[i].Equals(gesuchterArtikel, StringComparison.OrdinalIgnoreCase))
            {
                gefunden = true;
                break;
            }
        }

        return gefunden;
    }

    public void GibKurzeNamenAus(int minLaenge)
    {
        Console.WriteLine($"Artikel kuerzer als {minLaenge} Zeichen:");

        for (int i = 0; i < _anzahl; i++)
        {
            string artikel = _artikel[i];

            if (artikel.Length >= minLaenge)
            {
                continue;
            }

            Console.WriteLine($"- {artikel}");
        }
    }

    public (bool GleichMitOperator, bool GleichMitEquals, bool GleichesErgebnis) VergleicheStrings(string a, string b)
    {
        bool mitOperator = a == b;
        bool mitEquals = a.Equals(b);
        bool gleichesErgebnis = mitOperator == mitEquals;

        return (mitOperator, mitEquals, gleichesErgebnis);
    }
}
