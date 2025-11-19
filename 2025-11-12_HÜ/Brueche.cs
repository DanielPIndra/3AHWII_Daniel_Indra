public class Bruch
{
    private int ganz, zaehler, nenner;

    public Bruch(string bruchtext)
    {
        if (string.IsNullOrWhiteSpace(bruchtext)) throw new FormatException("Leerer Bruch-String");

        string[] parts = bruchtext.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && parts[1].Contains('/'))
        {
            int g = int.Parse(parts[0]);
            string[] frac = parts[1].Split('/');
            FromImproper(g * int.Parse(frac[1]) + Math.Sign(g) * int.Parse(frac[0]), int.Parse(frac[1]));
        }
        else if (parts.Length == 1 && parts[0].Contains('/'))
        {
            string[] frac = parts[0].Split('/');
            FromImproper(int.Parse(frac[0]), int.Parse(frac[1]));
        }
        else if (parts.Length == 1)
        {
            ganz = int.Parse(parts[0]);
            zaehler = 0;
            nenner = 1;
        }
        else throw new FormatException("Ungültiges Eingabeformat für Bruch");
    }

    public Bruch(int ganz, int zaehler, int nenner)
    {
        if (nenner == 0) throw new ArgumentException("Nenner darf nicht 0 sein.");
        this.ganz = ganz;
        this.zaehler = Math.Abs(zaehler);
        this.nenner = nenner;
        Normalize();
    }

    // Konstruktor für unechten Bruch (Zähler/Nenner)
    public Bruch(int numerator, int denominator, bool fromImproper)
    {
        if (denominator == 0) throw new ArgumentException("Nenner darf nicht 0 sein.");
        FromImproper(numerator, denominator);
    }

    public Bruch addiere(Bruch b)
    {
        int n1 = ToImproperNumerator(), n2 = b.ToImproperNumerator();
        int d1 = nenner, d2 = b.nenner;
        return new Bruch(n1 * d2 + n2 * d1, d1 * d2, true);
    }

    private int GCD(int a, int b) => (a == 0) ? b : (b == 0) ? a : GCD(b, a % b);

    public override string ToString() => toString();

    private int ToImproperNumerator() => ganz * nenner + (ganz < 0 ? -zaehler : zaehler);

    private void FromImproper(int num, int den)
    {
        int g = GCD(Math.Abs(num), Math.Abs(den));
        num /= g; den /= g;
        if (den < 0) { den = -den; num = -num; }
        ganz = num / den;
        zaehler = Math.Abs(num % den);
        nenner = den;
    }

    private void Normalize()
    {
        if (nenner == 0) throw new ArgumentException("Nenner darf nicht 0 sein.");
        int num = ToImproperNumerator(), g = GCD(Math.Abs(num), Math.Abs(nenner));
        num /= g;
        nenner = nenner < 0 ? -nenner : nenner;
        ganz = num / nenner;
        zaehler = Math.Abs(num % nenner);
    }

    public string toString()
    {
        if (zaehler == 0) return ganz.ToString();
        if (ganz == 0) return $"{zaehler}/{nenner}";
        return $"{ganz} {zaehler}/{nenner}";
    }
}
