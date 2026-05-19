using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<NbaStatsClient>(client =>
{
    client.BaseAddress = new Uri("https://stats.nba.com/stats/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.nba.com/");
    client.DefaultRequestHeaders.Add("Origin", "https://www.nba.com");
    client.DefaultRequestHeaders.Add("x-nba-stats-origin", "stats");
    client.DefaultRequestHeaders.Add("x-nba-stats-token", "true");
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PlayoffRatingService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/playoffs", async (PlayoffRatingService service) => Results.Json(await service.GetPlayoffRatingsAsync()));

app.Run();
static class Model
{
    public const double OffenseWeight = 0.55;
    public const double DefenseWeight = 0.45;

    public static string CurrentSeason()
    {
        var now = DateTime.Now;
        var startYear = now.Month >= 10 ? now.Year : now.Year - 1;
        return $"{startYear}-{(startYear + 1).ToString(CultureInfo.InvariantCulture)[^2..]}";
    }

    public static string SeasonId(string season) => $"2{season[..4]}";

    public static double Number(JsonElement row, string key)
    {
        if (row.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null || !row.TryGetProperty(key, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    public static string Text(JsonElement row, string key, string fallback = "")
    {
        if (row.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null || !row.TryGetProperty(key, out var value))
        {
            return fallback;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : value.ToString();
    }

    public static double Normalize(double value, IEnumerable<double> values, bool lowerIsBetter = false)
    {
        var list = values.ToList();
        if (list.Count == 0)
        {
            return 50;
        }

        var min = list.Min();
        var max = list.Max();
        if (Math.Abs(max - min) < 0.0001)
        {
            return 50;
        }

        var score = (value - min) / (max - min) * 100;
        return lowerIsBetter ? 100 - score : score;
    }

    public static double Weighted(params (double Value, double Weight)[] parts)
    {
        var totalWeight = parts.Sum(part => part.Weight);
        return totalWeight == 0 ? 0 : parts.Sum(part => part.Value * part.Weight) / totalWeight;
    }

    public static JsonElement Property(JsonElement row, string key)
    {
        return row.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null || !row.TryGetProperty(key, out var value)
            ? default
            : value;
    }
}

sealed class NbaStatsClient(HttpClient http)
{
    public async Task<IReadOnlyList<JsonElement>> GetDataSetAsync(string path, Dictionary<string, string> query, int dataSetIndex)
    {
        var url = path + "?" + string.Join("&", query.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var resultSet = document.RootElement.GetProperty("resultSets")[dataSetIndex];
        var headers = resultSet.GetProperty("headers").EnumerateArray().Select(header => header.GetString() ?? "").ToArray();

        return resultSet.GetProperty("rowSet")
            .EnumerateArray()
            .Select(row =>
            {
                var values = row.EnumerateArray().ToArray();
                var mapped = headers.Select((header, index) => new KeyValuePair<string, JsonElement>(header, values[index]));
                return JsonSerializer.SerializeToElement(mapped.ToDictionary(pair => pair.Key, pair => pair.Value));
            })
            .ToList();
    }

    public Task<IReadOnlyList<JsonElement>> GetTeamAdvancedStatsAsync(string season)
    {
        return GetDataSetAsync("leaguedashteamstats", new()
        {
            ["Conference"] = "",
            ["DateFrom"] = "",
            ["DateTo"] = "",
            ["Division"] = "",
            ["GameSegment"] = "",
            ["LastNGames"] = "0",
            ["LeagueID"] = "00",
            ["Location"] = "",
            ["MeasureType"] = "Advanced",
            ["Month"] = "0",
            ["OpponentTeamID"] = "0",
            ["Outcome"] = "",
            ["PORound"] = "0",
            ["PaceAdjust"] = "N",
            ["PerMode"] = "Per100Possessions",
            ["Period"] = "0",
            ["PlusMinus"] = "N",
            ["Rank"] = "N",
            ["Season"] = season,
            ["SeasonSegment"] = "",
            ["SeasonType"] = "Regular Season",
            ["ShotClockRange"] = "",
            ["StarterBench"] = "",
            ["TeamID"] = "0",
            ["TwoWay"] = "0",
            ["VsConference"] = "",
            ["VsDivision"] = ""
        }, 0);
    }

    public async Task<(IReadOnlyList<JsonElement> Series, IReadOnlyList<JsonElement> Standings)> GetPlayoffPictureAsync(string season)
    {
        var query = new Dictionary<string, string>
        {
            ["LeagueID"] = "00",
            ["SeasonID"] = Model.SeasonId(season)
        };

        var eastSeries = await GetDataSetAsync("playoffpicture", query, 0);
        var eastStandings = await GetDataSetAsync("playoffpicture", query, 2);
        var westSeries = await GetDataSetAsync("playoffpicture", query, 3);
        var westStandings = await GetDataSetAsync("playoffpicture", query, 5);
        return (eastSeries.Concat(westSeries).ToList(), eastStandings.Concat(westStandings).ToList());
    }

    public async Task<IReadOnlyList<JsonElement>> GetUpcomingGamesAsync(int days = 21)
    {
        var games = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var today = DateTime.Today;

        for (var offset = 0; offset < days; offset++)
        {
            var date = today.AddDays(offset).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            IReadOnlyList<JsonElement> dayGames;

            try
            {
                dayGames = await GetDataSetAsync("scoreboardv2", new()
                {
                    ["DayOffset"] = "0",
                    ["GameDate"] = date,
                    ["LeagueID"] = "00"
                }, 0);
            }
            catch
            {
                continue;
            }

            foreach (var game in dayGames)
            {
                var gameId = Model.Text(game, "GAME_ID");
                if (string.IsNullOrWhiteSpace(gameId) || !seen.Add(gameId))
                {
                    continue;
                }

                games.Add(game);
            }
        }

        return games;
    }
}

sealed class PlayoffRatingService(NbaStatsClient client)
{
    public async Task<PlayoffResponse> GetPlayoffRatingsAsync()
    {
        var season = Model.CurrentSeason();

        try
        {
            var stats = await client.GetTeamAdvancedStatsAsync(season);
            IReadOnlyList<JsonElement> series = [];
            IReadOnlyList<JsonElement> standings = [];
            IReadOnlyList<JsonElement> games = [];

            try
            {
                (series, standings) = await client.GetPlayoffPictureAsync(season);
            }
            catch
            {
                // The live games view can still work when the playoff-picture endpoint is temporarily unavailable.
            }

            try
            {
                games = await client.GetUpcomingGamesAsync();
            }
            catch
            {
                // Keep the page usable; the response still carries matchup data or falls back below.
            }

            var ratings = BuildRatings(stats, standings);
            var matchups = BuildMatchups(series, standings, ratings);
            var gamePredictions = BuildGamePredictions(games, ratings);
            var conferenceTables = BuildConferenceTables(ratings);

            if (matchups.Count == 0 && gamePredictions.Count == 0)
            {
                throw new InvalidOperationException("NBA returned no playoff matchups or games.");
            }

            return new PlayoffResponse(season, "NBA.com Stats API, kompatibel zur Datenbasis von swar/nba_api", new(Model.OffenseWeight, Model.DefenseWeight), matchups, gamePredictions, conferenceTables, null);
        }
        catch (Exception ex)
        {
            var fallback = DemoFallback(season);
            return fallback with { Error = ex.Message };
        }
    }

    private static Dictionary<int, TeamRating> BuildRatings(IReadOnlyList<JsonElement> stats, IReadOnlyList<JsonElement> standings)
    {
        var standingsById = standings.ToDictionary(row => (int)Model.Number(row, "TEAM_ID"), row => row);
        var columns = new[] { "OFF_RATING", "DEF_RATING", "NET_RATING", "EFG_PCT", "TS_PCT", "AST_TO", "DREB_PCT", "REB_PCT" };
        var values = columns.ToDictionary(column => column, column => stats.Select(row => Model.Number(row, column)).ToArray());

        return stats.ToDictionary(row => (int)Model.Number(row, "TEAM_ID"), row =>
        {
            var teamId = (int)Model.Number(row, "TEAM_ID");
            standingsById.TryGetValue(teamId, out var standing);

            var offense = Model.Weighted(
                (Model.Normalize(Model.Number(row, "OFF_RATING"), values["OFF_RATING"]), 0.38),
                (Model.Normalize(Model.Number(row, "EFG_PCT"), values["EFG_PCT"]), 0.22),
                (Model.Normalize(Model.Number(row, "TS_PCT"), values["TS_PCT"]), 0.18),
                (Model.Normalize(Model.Number(row, "AST_TO"), values["AST_TO"]), 0.14),
                (Model.Normalize(Model.Number(row, "NET_RATING"), values["NET_RATING"]), 0.08));

            var defense = Model.Weighted(
                (Model.Normalize(Model.Number(row, "DEF_RATING"), values["DEF_RATING"], true), 0.52),
                (Model.Normalize(Model.Number(row, "DREB_PCT"), values["DREB_PCT"]), 0.22),
                (Model.Normalize(Model.Number(row, "REB_PCT"), values["REB_PCT"]), 0.12),
                (Model.Normalize(Model.Number(row, "NET_RATING"), values["NET_RATING"]), 0.14));

            return new TeamRating(
                teamId,
                Model.Text(row, "TEAM_NAME", Model.Text(standing, "TEAM", "Unknown Team")),
                Model.Text(row, "TEAM_ABBREVIATION", Model.Text(standing, "TEAM_SLUG")).ToUpperInvariant(),
                (int)Model.Number(row, "W"),
                (int)Model.Number(row, "L"),
                Model.Number(standing, "RANK") > 0 ? (int)Model.Number(standing, "RANK") : null,
                Model.Text(standing, "CONFERENCE", null!),
                Math.Round(offense, 1),
                Math.Round(defense, 1),
                Math.Round(offense * Model.OffenseWeight + defense * Model.DefenseWeight, 1),
                new TeamMetrics(
                    Math.Round(Model.Number(row, "OFF_RATING"), 1),
                    Math.Round(Model.Number(row, "DEF_RATING"), 1),
                    Math.Round(Model.Number(row, "NET_RATING"), 1),
                    Math.Round(Model.Number(row, "EFG_PCT") * 100, 1),
                    Math.Round(Model.Number(row, "TS_PCT") * 100, 1),
                    Math.Round(Model.Number(row, "AST_TO"), 2),
                    Math.Round(Model.Number(row, "DREB_PCT") * 100, 1)));
        });
    }

    private static List<MatchupRating> BuildMatchups(IReadOnlyList<JsonElement> series, IReadOnlyList<JsonElement> standings, Dictionary<int, TeamRating> ratings)
    {
        var matchups = new List<MatchupRating>();
        foreach (var row in series)
        {
            var matchup = CreateMatchup(
                (int)Model.Number(row, "HIGH_SEED_TEAM_ID"),
                (int)Model.Number(row, "LOW_SEED_TEAM_ID"),
                Model.Text(row, "CONFERENCE"),
                $"{Model.Text(row, "HIGH_SEED_RANK")} vs {Model.Text(row, "LOW_SEED_RANK")}",
                ratings);

            if (matchup is not null)
            {
                matchups.Add(matchup);
            }
        }

        if (matchups.Count > 0)
        {
            return matchups;
        }

        foreach (var conferenceGroup in standings.Where(row => Model.Number(row, "RANK") <= 8).GroupBy(row => Model.Text(row, "CONFERENCE").Contains("East") ? "East" : "West"))
        {
            var seeded = conferenceGroup.OrderBy(row => Model.Number(row, "RANK")).ToArray();
            foreach (var (left, right) in new[] { (0, 7), (1, 6), (2, 5), (3, 4) })
            {
                if (seeded.Length <= right)
                {
                    continue;
                }

                var matchup = CreateMatchup((int)Model.Number(seeded[left], "TEAM_ID"), (int)Model.Number(seeded[right], "TEAM_ID"), conferenceGroup.Key, $"{left + 1} vs {right + 1}", ratings);
                if (matchup is not null)
                {
                    matchups.Add(matchup);
                }
            }
        }

        return matchups;
    }

    private static MatchupRating? CreateMatchup(int highId, int lowId, string conference, string series, Dictionary<int, TeamRating> ratings)
    {
        if (!ratings.TryGetValue(highId, out var high) || !ratings.TryGetValue(lowId, out var low))
        {
            return null;
        }

        var diff = high.TotalScore - low.TotalScore;
        var highProbability = Math.Clamp(50 + diff * 1.15, 5, 95);
        var favorite = highProbability >= 50 ? high.Name : low.Name;
        var favoriteProbability = highProbability >= 50 ? highProbability : 100 - highProbability;
        return new MatchupRating(conference, series, favorite, Math.Round(favoriteProbability, 1), Math.Round(Math.Abs(diff), 1), [high, low]);
    }

    private static List<GamePrediction> BuildGamePredictions(IReadOnlyList<JsonElement> games, Dictionary<int, TeamRating> ratings)
    {
        var predictions = new List<GamePrediction>();
        foreach (var game in games)
        {
            var homeTeam = Model.Property(game, "homeTeam");
            var awayTeam = Model.Property(game, "awayTeam");
            var homeId = (int)(Model.Number(game, "HOME_TEAM_ID") > 0 ? Model.Number(game, "HOME_TEAM_ID") : Model.Number(homeTeam, "teamId"));
            var awayId = (int)(Model.Number(game, "VISITOR_TEAM_ID") > 0 ? Model.Number(game, "VISITOR_TEAM_ID") : Model.Number(awayTeam, "teamId"));

            var home = ratings.TryGetValue(homeId, out var homeRating)
                ? homeRating
                : TeamFromLive(homeTeam, homeId);
            var away = ratings.TryGetValue(awayId, out var awayRating)
                ? awayRating
                : TeamFromLive(awayTeam, awayId);

            var homeWinRatio = WinRatio(home.TotalScore, away.TotalScore, homeCourt: true);
            var awayWinRatio = 100 - homeWinRatio;
            predictions.Add(new GamePrediction(
                Model.Text(game, "GAME_ID", Model.Text(game, "gameId")),
                Model.Text(game, "GAME_STATUS_TEXT", Model.Text(game, "gameStatusText", "Scheduled")),
                GameTime(game),
                home,
                away,
                Math.Round(homeWinRatio, 1),
                Math.Round(awayWinRatio, 1),
                homeWinRatio >= awayWinRatio ? home.Name : away.Name));
        }

        return predictions;
    }

    private static IReadOnlyList<ConferenceTable> BuildConferenceTables(Dictionary<int, TeamRating> ratings)
    {
        var teams = ratings.Values
            .Where(team => !string.IsNullOrWhiteSpace(team.Conference))
            .OrderBy(team => team.Seed ?? 99)
            .ThenByDescending(team => team.TotalScore)
            .ToList();

        var east = BuildConferenceTable("Eastern Conference", teams.Where(team => team.Conference!.Contains("East", StringComparison.OrdinalIgnoreCase)));
        var west = BuildConferenceTable("Western Conference", teams.Where(team => team.Conference!.Contains("West", StringComparison.OrdinalIgnoreCase)));
        return [east, west];
    }

    private static ConferenceTable BuildConferenceTable(string name, IEnumerable<TeamRating> teams)
    {
        var rows = teams
            .Select((team, index) => new StandingRow(
                team.Seed ?? index + 1,
                team,
                team.Wins,
                team.Losses,
                team.TotalScore,
                team.OffenseScore,
                team.DefenseScore))
            .ToList();

        return new ConferenceTable(name, rows);
    }

    private static string GameTime(JsonElement game)
    {
        var utc = Model.Text(game, "gameTimeUTC");
        if (!string.IsNullOrWhiteSpace(utc))
        {
            return utc;
        }

        var date = Model.Text(game, "GAME_DATE_EST");
        return DateTime.TryParse(date, CultureInfo.InvariantCulture, out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : date;
    }

    private static void AddKnownUpcomingPlayoffGames(List<GamePrediction> predictions, Dictionary<int, TeamRating> ratings)
    {
        var knownGames = new[]
        {
            new ScheduledPlayoffGame("0042500311", "West Final Game 1", "2026-05-18T20:30:00-04:00", 1610612759, 1610612760),
            new ScheduledPlayoffGame("0042500312", "West Final Game 2", "2026-05-20T20:30:00-04:00", 1610612759, 1610612760),
            new ScheduledPlayoffGame("0042500313", "West Final Game 3", "2026-05-22T20:30:00-04:00", 1610612760, 1610612759),
            new ScheduledPlayoffGame("0042500314", "West Final Game 4", "2026-05-24T20:00:00-04:00", 1610612760, 1610612759),
            new ScheduledPlayoffGame("0042500315", "West Final Game 5 - if necessary", "2026-05-26T20:30:00-04:00", 1610612759, 1610612760),
            new ScheduledPlayoffGame("0042500316", "West Final Game 6 - if necessary", "2026-05-28T20:30:00-04:00", 1610612760, 1610612759),
            new ScheduledPlayoffGame("0042500317", "West Final Game 7 - if necessary", "2026-05-30T20:00:00-04:00", 1610612759, 1610612760),
            new ScheduledPlayoffGame("0042500301", "East Final Game 1", "2026-05-19T20:00:00-04:00", 1610612739, 1610612752),
            new ScheduledPlayoffGame("0042500302", "East Final Game 2", "2026-05-21T20:00:00-04:00", 1610612739, 1610612752),
            new ScheduledPlayoffGame("0042500303", "East Final Game 3", "2026-05-23T20:00:00-04:00", 1610612752, 1610612739),
            new ScheduledPlayoffGame("0042500304", "East Final Game 4", "2026-05-25T20:00:00-04:00", 1610612752, 1610612739),
            new ScheduledPlayoffGame("0042500305", "East Final Game 5 - if necessary", "2026-05-27T20:00:00-04:00", 1610612739, 1610612752),
            new ScheduledPlayoffGame("0042500306", "East Final Game 6 - if necessary", "2026-05-29T20:00:00-04:00", 1610612752, 1610612739),
            new ScheduledPlayoffGame("0042500307", "East Final Game 7 - if necessary", "2026-05-31T20:00:00-04:00", 1610612739, 1610612752),
        };

        var existingIds = predictions.Select(game => game.GameId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var game in knownGames)
        {
            var utc = DateTimeOffset.Parse(game.LocalTime, CultureInfo.InvariantCulture).UtcDateTime;
            if (utc < DateTime.UtcNow.AddHours(-6) || existingIds.Contains(game.GameId))
            {
                continue;
            }

            var away = ratings[game.AwayTeamId];
            var home = ratings[game.HomeTeamId];
            var homeWinRatio = WinRatio(home.TotalScore, away.TotalScore, homeCourt: true);
            predictions.Add(new GamePrediction(
                game.GameId,
                game.Label,
                utc.ToString("O", CultureInfo.InvariantCulture),
                home,
                away,
                Math.Round(homeWinRatio, 1),
                Math.Round(100 - homeWinRatio, 1),
                homeWinRatio >= 50 ? home.Name : away.Name));
        }

        predictions.Sort((left, right) => string.Compare(left.GameTimeUtc, right.GameTimeUtc, StringComparison.Ordinal));
    }

    private static void AddKnownPlayoffRatings(Dictionary<int, TeamRating> ratings)
    {
        foreach (var rating in KnownPlayoffRatings())
        {
            ratings.TryAdd(rating.TeamId, rating);
        }
    }

    private static IReadOnlyList<TeamRating> KnownPlayoffRatings()
    {
        return
        [
            new(1610612760, "Oklahoma City Thunder", "OKC", 68, 14, 1, "West", 96.0, 94.0, 95.1, new(123.6, 108.4, 15.2, 58.2, 62.1, 2.18, 75.1)),
            new(1610612759, "San Antonio Spurs", "SAS", 62, 20, 2, "West", 91.5, 95.6, 93.3, new(120.1, 107.9, 12.2, 56.5, 60.4, 2.06, 76.4)),
            new(1610612752, "New York Knicks", "NYK", 52, 30, 3, "East", 86.9, 82.7, 85.0, new(117.6, 112.1, 5.5, 54.9, 58.5, 1.92, 74.2)),
            new(1610612739, "Cleveland Cavaliers", "CLE", 50, 32, 4, "East", 84.4, 84.8, 84.6, new(116.9, 111.8, 5.1, 55.1, 58.9, 1.87, 73.7)),
        ];
    }

    private static TeamRating TeamFromLive(JsonElement team, int teamId)
    {
        return new TeamRating(
            teamId,
            Model.Text(team, "teamName", "Unknown Team"),
            Model.Text(team, "teamTricode", "NBA"),
            0,
            0,
            null,
            null,
            50,
            50,
            50,
            new(0, 0, 0, 0, 0, 0, 0));
    }

    private static double WinRatio(double homeScore, double awayScore, bool homeCourt)
    {
        var homeCourtBonus = homeCourt ? 2.5 : 0;
        return Math.Clamp(50 + (homeScore - awayScore) * 1.15 + homeCourtBonus, 5, 95);
    }

    private static PlayoffResponse DemoFallback(string season)
    {
        var ratings = KnownPlayoffRatings().ToDictionary(team => team.TeamId);
        var west = CreateMatchup(1610612760, 1610612759, "West", "Thunder vs Spurs", ratings);
        var east = CreateMatchup(1610612752, 1610612739, "East", "Knicks vs Cavaliers", ratings);
        var matchups = new[] { west, east }.Where(matchup => matchup is not null).Cast<MatchupRating>().ToArray();

        return new PlayoffResponse(
            season,
            "Fallback mit aktuellen Playoff-Paarungen, weil die NBA-API gerade nicht erreichbar war.",
            new(Model.OffenseWeight, Model.DefenseWeight),
            matchups,
            BuildFallbackGames(),
            BuildConferenceTables(ratings),
            null);
    }

    private static IReadOnlyList<GamePrediction> BuildFallbackGames()
    {
        var ratings = KnownPlayoffRatings().ToDictionary(team => team.TeamId);
        var games = new List<GamePrediction>();
        AddKnownUpcomingPlayoffGames(games, ratings);
        return games;
    }
}

record Weights(double Offense, double Defense);
record TeamMetrics(double OffRating, double DefRating, double NetRating, double EfgPct, double TsPct, double AstTo, double DrebPct);
record TeamRating(int TeamId, string Name, string Abbreviation, int Wins, int Losses, int? Seed, string? Conference, double OffenseScore, double DefenseScore, double TotalScore, TeamMetrics Metrics)
{
    public string LogoUrl => $"https://cdn.nba.com/logos/nba/{TeamId}/global/L/logo.svg";
}
record MatchupRating(string? Conference, string Series, string Favorite, double FavoriteProbability, double Edge, IReadOnlyList<TeamRating> Teams);
record ScheduledPlayoffGame(string GameId, string Label, string LocalTime, int AwayTeamId, int HomeTeamId);
record GamePrediction(string GameId, string Status, string GameTimeUtc, TeamRating HomeTeam, TeamRating AwayTeam, double HomeWinRatio, double AwayWinRatio, string Favorite);
record StandingRow(int Seed, TeamRating Team, int Wins, int Losses, double TotalScore, double OffenseScore, double DefenseScore);
record ConferenceTable(string Name, IReadOnlyList<StandingRow> Rows);
record PlayoffResponse(string Season, string Source, Weights Weights, IReadOnlyList<MatchupRating> Matchups, IReadOnlyList<GamePrediction> Games, IReadOnlyList<ConferenceTable> Standings, string? Error);


