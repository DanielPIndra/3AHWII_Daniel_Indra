namespace CSV;

public class Person
{
    public string Fullname { get; set; }
    public string Email { get; set; }
    public string Telefon { get; set; }
    public string Adresse { get; set; }
    public string Unicode { get; set; }

    public override string ToString()
    {
        return $"Name: {Fullname}, Email: {Email}, Tel: {Telefon}, Adresse: {Adresse}";
    }
}
