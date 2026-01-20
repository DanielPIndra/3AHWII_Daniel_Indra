class Auto
{
    // Auto-Property
    public string Marke { get; set; }

    // Property mit Logik
    private int baujahr;
    public int Baujahr
    {
        get { return baujahr; }
        set
        {
            if (value < 1900)
            {
                baujahr = 1900;
            }
            else
            {
                baujahr = value;
            }
        }
    }

    // Constructor mit this
    public Auto(string marke, int baujahr)
    {
        this.Marke = marke;   // Property
        this.Baujahr = baujahr; // geht durch set → Validierung
    }
}
