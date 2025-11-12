class Bruch
{
    // jetzt kommen die sog. "Attribute" der Klasse oder "Felder"
    private int ganz;
    private int zaehler;
    private int nenner;

    public Bruch(string bruchtext)
    {
        string[] teile1 = bruchtext.Split(' ');
        this.ganz = int.Parse(teile1[0]);
        string[] teile = teile1[1].Split('/');
        this.zaehler = int.Parse(teile[0]);
        this.nenner = int.Parse(teile[1]);
    }

    public Bruch(int ganz, int zaehler, int nenner)
    {
        this.ganz = ganz;
        this.zaehler = zaehler;
        this.nenner = nenner;
    }

    public Bruch addiere(Bruch b)
    {
        // Umwandeln in unechte Brüche
        int n1 = this.ganz * this.nenner + this.zaehler;
        int d1 = this.nenner;

        int n2 = b.ganz * b.nenner + b.zaehler;
        int d2 = b.nenner;

        // gemeinsamen Nenner bilden und Zähler addieren
        int numerator = n1 * d2 + n2 * d1;
        int denominator = d1 * d2;

        // kürzen
        int g = GCD(Math.Abs(numerator), Math.Abs(denominator));
        if (g != 0)
        {
            numerator /= g;
            denominator /= g;
        }

        // gemischten Bruch bilden
        int resultGanz = numerator / denominator;
        int resultZaehler = Math.Abs(numerator % denominator);
        int resultNenner = denominator;

        return new Bruch(resultGanz, resultZaehler, resultNenner);
    }

    private int GCD(int a, int b)
    {
        if (a == 0) return b;
        if (b == 0) return a;
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
    }

    public string toString()
    {
        return $"ich bin ein bruch: {this.zaehler}/{this.nenner}";
        // JS: return `ich bin ein bruch: ${this.zaehler}/${this.nenner}`;
    }

    // kompatibler ToString-override (C# Konvention) – ruft die vorhandene Methode auf
    public override string ToString()
    {
        return toString();
    }
}