using CsvHelper.Configuration;
using CSV;

public class PersonMap : ClassMap<Person>
{
    public PersonMap()
    {
        Map(m => m.Fullname).Name("Fullname");
        Map(m => m.Email).Name("Email");
        Map(m => m.Telefon).Name("Telefon");
        Map(m => m.Adresse).Name("Adresse");
        Map(m => m.Unicode).Name("unicode").Optional();
    }
}
