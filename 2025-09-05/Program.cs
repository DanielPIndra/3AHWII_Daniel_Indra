
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Benutzung: dotnet run \"2 3/8\" \"1 5/6\"");
            return;
        }

        var frac1 = ParseMixedFraction(args[0]);
        var frac2 = ParseMixedFraction(args[1]);

        // Brüche addieren
        int numerator = frac1.Item1 * frac2.Item2 + frac2.Item1 * frac1.Item2;
        int denominator = frac1.Item2 * frac2.Item2;

        // Kürzen
        int gcd = GCD(numerator, denominator);
        numerator /= gcd;
        denominator /= gcd;

        // Gemischten Bruch darstellen
        int whole = numerator / denominator;
        int restNum = numerator % denominator;

        if (restNum == 0)
            Console.WriteLine(whole);
        else if (whole == 0)
            Console.WriteLine($"{restNum}/{denominator}");
        else
            Console.WriteLine($"{whole} {restNum}/{denominator}");
    }

    // Parser für gemischten Bruch wie "2 3/8" oder nur "3/4" oder nur "5"
    static Tuple<int, int> ParseMixedFraction(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int whole = 0, num = 0, den = 1;

        if (parts.Length == 2)
        {
            whole = int.Parse(parts[0]);
            var fracParts = parts[1].Split('/');
            num = int.Parse(fracParts[0]);
            den = int.Parse(fracParts[1]);
        }
        else if (parts.Length == 1)
        {
            if (parts[0].Contains('/'))
            {
                var fracParts = parts[0].Split('/');
                num = int.Parse(fracParts[0]);
                den = int.Parse(fracParts[1]);
            }
            else
            {
                whole = int.Parse(parts[0]);
            }
        }

        // Alles als unechten Bruch zurückgeben
        return Tuple.Create(whole * den + num, den);
    }

    static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return Math.Abs(a);
    }
}
