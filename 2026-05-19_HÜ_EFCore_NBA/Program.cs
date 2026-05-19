using Microsoft.EntityFrameworkCore;

using var db = new NbaDbContext();
db.Database.EnsureCreated();
SeedData(db);

while (true)
{
    ClearScreen();
    Console.WriteLine("NBA Team Manager - EF Core CRUD");
    Console.WriteLine("================================");
    Console.WriteLine("1. Teams anzeigen");
    Console.WriteLine("2. Team anlegen");
    Console.WriteLine("3. Team bearbeiten");
    Console.WriteLine("4. Team loeschen");
    Console.WriteLine("5. Spieler anzeigen");
    Console.WriteLine("6. Spieler anlegen");
    Console.WriteLine("7. Spieler bearbeiten");
    Console.WriteLine("8. Spieler loeschen");
    Console.WriteLine("0. Beenden");
    Console.Write("Auswahl: ");

    switch (Console.ReadLine())
    {
        case "1":
            ShowTeams(db);
            break;
        case "2":
            CreateTeam(db);
            break;
        case "3":
            UpdateTeam(db);
            break;
        case "4":
            DeleteTeam(db);
            break;
        case "5":
            ShowPlayers(db);
            break;
        case "6":
            CreatePlayer(db);
            break;
        case "7":
            UpdatePlayer(db);
            break;
        case "8":
            DeletePlayer(db);
            break;
        case "0":
            return;
        default:
            Pause("Ungueltige Eingabe.");
            break;
    }
}

static void ShowTeams(NbaDbContext db)
{
    ClearScreen();
    Console.WriteLine("Teams");
    Console.WriteLine("-----");

    var teams = db.Teams
        .Include(team => team.Players)
        .OrderBy(team => team.Name)
        .ToList();

    foreach (var team in teams)
    {
        Console.WriteLine($"{team.Id}: {team.Name} ({team.City}) - {team.Conference}, Spieler: {team.Players.Count}");
    }

    Pause();
}

static void CreateTeam(NbaDbContext db)
{
    ClearScreen();
    Console.WriteLine("Team anlegen");
    Console.WriteLine("------------");

    var team = new Team
    {
        Name = ReadRequired("Name: "),
        City = ReadRequired("Stadt: "),
        Conference = ReadRequired("Conference (East/West): ")
    };

    db.Teams.Add(team);
    db.SaveChanges();
    Pause("Team wurde gespeichert.");
}

static void UpdateTeam(NbaDbContext db)
{
    ClearScreen();
    ShowTeamList(db);
    var team = db.Teams.Find(ReadInt("Team-ID bearbeiten: "));

    if (team is null)
    {
        Pause("Team wurde nicht gefunden.");
        return;
    }

    team.Name = ReadOptional("Name", team.Name);
    team.City = ReadOptional("Stadt", team.City);
    team.Conference = ReadOptional("Conference", team.Conference);

    db.SaveChanges();
    Pause("Team wurde aktualisiert.");
}

static void DeleteTeam(NbaDbContext db)
{
    ClearScreen();
    ShowTeamList(db);
    var teamId = ReadInt("Team-ID loeschen: ");
    var team = db.Teams
        .Include(item => item.Players)
        .FirstOrDefault(item => item.Id == teamId);

    if (team is null)
    {
        Pause("Team wurde nicht gefunden.");
        return;
    }

    db.Teams.Remove(team);
    db.SaveChanges();
    Pause("Team und zugehoerige Spieler wurden geloescht.");
}

static void ShowPlayers(NbaDbContext db)
{
    ClearScreen();
    Console.WriteLine("Spieler");
    Console.WriteLine("-------");

    var players = db.Players
        .Include(player => player.Team)
        .OrderBy(player => player.Team.Name)
        .ThenBy(player => player.LastName)
        .ToList();

    foreach (var player in players)
    {
        Console.WriteLine($"{player.Id}: {player.FirstName} {player.LastName}, #{player.JerseyNumber}, {player.Position}, Team: {player.Team.Name}");
    }

    Pause();
}

static void CreatePlayer(NbaDbContext db)
{
    ClearScreen();
    Console.WriteLine("Spieler anlegen");
    Console.WriteLine("---------------");
    ShowTeamList(db);

    var team = db.Teams.Find(ReadInt("Team-ID: "));
    if (team is null)
    {
        Pause("Team wurde nicht gefunden.");
        return;
    }

    var player = new Player
    {
        FirstName = ReadRequired("Vorname: "),
        LastName = ReadRequired("Nachname: "),
        Position = ReadRequired("Position: "),
        JerseyNumber = ReadInt("Trikotnummer: "),
        TeamId = team.Id
    };

    db.Players.Add(player);
    db.SaveChanges();
    Pause("Spieler wurde gespeichert.");
}

static void UpdatePlayer(NbaDbContext db)
{
    ClearScreen();
    ShowPlayerList(db);
    var player = db.Players.Find(ReadInt("Spieler-ID bearbeiten: "));

    if (player is null)
    {
        Pause("Spieler wurde nicht gefunden.");
        return;
    }

    ShowTeamList(db);
    var teamId = ReadOptionalInt("Neue Team-ID", player.TeamId);

    if (!db.Teams.Any(team => team.Id == teamId))
    {
        Pause("Team wurde nicht gefunden.");
        return;
    }

    player.FirstName = ReadOptional("Vorname", player.FirstName);
    player.LastName = ReadOptional("Nachname", player.LastName);
    player.Position = ReadOptional("Position", player.Position);
    player.JerseyNumber = ReadOptionalInt("Trikotnummer", player.JerseyNumber);
    player.TeamId = teamId;

    db.SaveChanges();
    Pause("Spieler wurde aktualisiert.");
}

static void DeletePlayer(NbaDbContext db)
{
    ClearScreen();
    ShowPlayerList(db);
    var player = db.Players.Find(ReadInt("Spieler-ID loeschen: "));

    if (player is null)
    {
        Pause("Spieler wurde nicht gefunden.");
        return;
    }

    db.Players.Remove(player);
    db.SaveChanges();
    Pause("Spieler wurde geloescht.");
}

static void ShowTeamList(NbaDbContext db)
{
    foreach (var team in db.Teams.OrderBy(team => team.Name))
    {
        Console.WriteLine($"{team.Id}: {team.Name} ({team.City})");
    }
}

static void ShowPlayerList(NbaDbContext db)
{
    foreach (var player in db.Players.Include(player => player.Team).OrderBy(player => player.LastName))
    {
        Console.WriteLine($"{player.Id}: {player.FirstName} {player.LastName} - {player.Team.Name}");
    }
}

static string ReadRequired(string label)
{
    while (true)
    {
        Console.Write(label);
        var value = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        Console.WriteLine("Bitte etwas eingeben.");
    }
}

static string ReadOptional(string label, string currentValue)
{
    Console.Write($"{label} ({currentValue}): ");
    var value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value) ? currentValue : value.Trim();
}

static int ReadInt(string label)
{
    while (true)
    {
        Console.Write(label);
        if (int.TryParse(Console.ReadLine(), out var value))
        {
            return value;
        }

        Console.WriteLine("Bitte eine Zahl eingeben.");
    }
}

static int ReadOptionalInt(string label, int currentValue)
{
    Console.Write($"{label} ({currentValue}): ");
    var value = Console.ReadLine();
    return int.TryParse(value, out var parsed) ? parsed : currentValue;
}

static void Pause(string message = "")
{
    if (!string.IsNullOrWhiteSpace(message))
    {
        Console.WriteLine(message);
    }

    Console.WriteLine();
    Console.Write("Weiter mit Enter...");
    Console.ReadLine();
}

static void ClearScreen()
{
    if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
    {
        Console.Clear();
    }
}

static void SeedData(NbaDbContext db)
{
    if (db.Teams.Any())
    {
        return;
    }

    var thunder = new Team
    {
        Name = "Thunder",
        City = "Oklahoma City",
        Conference = "West",
        Players =
        [
            new Player { FirstName = "Shai", LastName = "Gilgeous-Alexander", Position = "Guard", JerseyNumber = 2 },
            new Player { FirstName = "Chet", LastName = "Holmgren", Position = "Center", JerseyNumber = 7 }
        ]
    };

    var knicks = new Team
    {
        Name = "Knicks",
        City = "New York",
        Conference = "East",
        Players =
        [
            new Player { FirstName = "Jalen", LastName = "Brunson", Position = "Guard", JerseyNumber = 11 },
            new Player { FirstName = "Karl-Anthony", LastName = "Towns", Position = "Center", JerseyNumber = 32 }
        ]
    };

    db.Teams.AddRange(thunder, knicks);
    db.SaveChanges();
}

public class NbaDbContext : DbContext
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=nba-manager.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>()
            .HasMany(team => team.Players)
            .WithOne(player => player.Team)
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Conference { get; set; } = "";
    public List<Player> Players { get; set; } = [];
}

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public int JerseyNumber { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
}
