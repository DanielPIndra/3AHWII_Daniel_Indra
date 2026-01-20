using System.Globalization;
using CsvHelper;
using CSV;

var filePath = @"..\person_csv\persons.csv";

using (var reader = new StreamReader(filePath))
using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
{
    csv.Context.RegisterClassMap<PersonMap>();
    var records = csv.GetRecords<Person>();

    foreach (var person in records)
    {
        Console.WriteLine(person);
    }
}
