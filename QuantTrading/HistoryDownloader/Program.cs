using System.Globalization;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using KiteConnect;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
var india = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, india).Date;
string? Option(string name)
{
    var i = Array.IndexOf(args, name);
    if (i < 0) return null;
    if (i + 1 == args.Length) throw new ArgumentException("Missing option value");
    return args[i + 1];
}
try
{
    var from = DateTime.SpecifyKind(DateTime.ParseExact(Option("--from") ?? today.AddMonths(-1).ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
    var through = DateTime.SpecifyKind(DateTime.ParseExact(Option("--to") ?? today.AddDays(-1).ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
    if (through < from || through >= today) throw new ArgumentException("Use a completed date range ending before today in India.");
    var until = through.AddDays(1);
    var key = Environment.GetEnvironmentVariable("KITE_API_KEY");
    var token = Environment.GetEnvironmentVariable("KITE_ACCESS_TOKEN");
    var credentialFile = Option("--credentials");
    if (credentialFile != null)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(credentialFile), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        string? Field(string a, string b) =>
            doc.RootElement.TryGetProperty(a, out var x) ? x.GetString() :
            doc.RootElement.TryGetProperty(b, out x) ? x.GetString() : null;
        key = string.IsNullOrWhiteSpace(key) ? Field("zerodha-api-key", "api_key") : key;
        token = string.IsNullOrWhiteSpace(token) ? Field("zerodha-access-token", "access_token") : token;
    }
    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("Kite credentials missing. Set KITE_API_KEY and KITE_ACCESS_TOKEN or use --credentials PATH.");
        return 2;
    }
    var kite = new Kite(APIKey: key, AccessToken: token, Debug: false, Timeout: 20000);
    var instruments = kite.GetInstruments("NSE")
        .Where(i => i.TradingSymbol == "IDEA" && i.Exchange == "NSE" && i.InstrumentType == "EQ")
        .ToArray();
    if (instruments.Length != 1) throw new InvalidOperationException("Expected exactly one NSE:IDEA equity instrument.");
    var instrument = instruments[0];
    Console.WriteLine($"Resolved NSE:IDEA token={instrument.InstrumentToken}; requesting {from:yyyy-MM-dd} through {through:yyyy-MM-dd} IST.");
    var candles = new SortedDictionary<DateTimeOffset, Candle>();
    for (var start = from; start < until; start = start.AddDays(7))
    {
        var end = start.AddDays(7) < until ? start.AddDays(7) : until;
        await Task.Delay(400);
        var batch = kite.GetHistoricalData(
            InstrumentToken: instrument.InstrumentToken.ToString(CultureInfo.InvariantCulture),
            FromDate: start, ToDate: end.AddSeconds(-1),
            Interval: "minute", Continuous: false, OI: false);
        foreach (var h in batch)
        {
            // SDK Convert.ToDateTime may return local machine time for offset-bearing API timestamps.
            var stamp = h.TimeStamp.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(h.TimeStamp, TimeSpan.FromMinutes(330))
                : TimeZoneInfo.ConvertTime(new DateTimeOffset(h.TimeStamp), india);
            var c = new Candle(stamp, h.Open, h.High, h.Low, h.Close, h.Volume);
            if (stamp.DateTime < from || stamp.DateTime >= until ||
                stamp.Second != 0 || stamp.Millisecond != 0 ||
                c.Low <= 0 || c.High < c.Low ||
                c.Open < c.Low || c.Open > c.High ||
                c.Close < c.Low || c.Close > c.High)
                throw new InvalidOperationException("Invalid candle returned; output not finalized.");
            if (candles.TryGetValue(stamp, out var previous) && previous != c)
                throw new InvalidOperationException("Conflicting duplicate candle; output not finalized.");
            candles[stamp] = c;
        }
        Console.WriteLine($"{start:yyyy-MM-dd} to {end.AddDays(-1):yyyy-MM-dd}: {batch.Count} candles");
    }
    if (candles.Count == 0)
    {
        Console.Error.WriteLine("Zerodha returned no candles; no successful dataset was written.");
        return 3;
    }
    var parent = Path.GetFullPath(Option("--output") ?? "downloads");
    var runName = $"IDEA-minute-{from:yyyyMMdd}-{through:yyyyMMdd}-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}";
    var final = Path.Combine(parent, runName);
    var stage = final + ".partial";
    Directory.CreateDirectory(stage);
    var csv = Path.Combine(stage, "IDEA-minute.csv");
    using (var writer = new StreamWriter(csv, false, new UTF8Encoding(false)))
    {
        writer.WriteLine("timestamp_ist,open,high,low,close,volume");
        foreach (var c in candles.Values)
            writer.WriteLine($"{c.Timestamp:yyyy-MM-ddTHH:mm:sszzz},{c.Open},{c.High},{c.Low},{c.Close},{c.Volume}");
    }
    var lean = Path.Combine(stage, "lean", "equity", "india", "minute", "idea");
    Directory.CreateDirectory(lean);
    foreach (var day in candles.Values.GroupBy(c => c.Timestamp.Date))
    {
        var date = day.Key.ToString("yyyyMMdd");
        using var archive = ZipFile.Open(Path.Combine(lean, date + "_trade.zip"), ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry(date + "_idea_minute_trade.csv").Open(), new UTF8Encoding(false));
        foreach (var c in day)
        {
            decimal Scale(decimal price)
            {
                var scaled = price * 10000m;
                if (scaled != decimal.Truncate(scaled)) throw new InvalidOperationException("Price exceeds LEAN equity precision.");
                return scaled;
            }
            writer.WriteLine($"{(long)c.Timestamp.TimeOfDay.TotalMilliseconds},{Scale(c.Open)},{Scale(c.High)},{Scale(c.Low)},{Scale(c.Close)},{c.Volume}");
        }
    }
    var perDay = candles.Values.GroupBy(c => c.Timestamp.Date)
        .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());
    var manifest = new {
        provider = "Zerodha Kite Connect", sdk = "Tech.Zerodha.KiteConnect 5.2.1",
        symbol = "NSE:IDEA", instrumentToken = instrument.InstrumentToken,
        interval = "minute", timeZone = "Asia/Kolkata", timestamps = "bar start",
        from = from.ToString("yyyy-MM-dd"), through = through.ToString("yyyy-MM-dd"),
        retrievedAtUtc = DateTimeOffset.UtcNow, count = candles.Count,
        first = candles.Keys.First(), last = candles.Keys.Last(), candlesPerDate = perDay,
        datesWithoutCandles = Enumerable.Range(0, (until - from).Days)
            .Select(d => from.AddDays(d).ToString("yyyy-MM-dd")).Where(d => !perDay.ContainsKey(d)).ToArray(),
        csvSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(csv))),
        note = "No synthetic bars or forward filling. Dates without candles may be weekends, holidays or missing provider data; exchange-calendar completeness has not been certified. Corporate-action adjustment provenance is not supplied by this exporter."
    };
    File.WriteAllText(Path.Combine(stage, "manifest.json"),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    Directory.Move(stage, final);
    Console.WriteLine($"SUCCESS: {candles.Count} candles, {perDay.Count} dates. Output: {final}");
    return 0;
}
catch (Exception e)
{
    // Never print SDK request/response payloads, credential values or full exception text.
    var kind = e.GetType().Name;
    Console.Error.WriteLine($"Download failed ({kind}).");
    if (kind.Contains("Token", StringComparison.OrdinalIgnoreCase))
        Console.Error.WriteLine("The Kite session is invalid or expired. Renew the access token on the server and rerun.");
    else if (kind.Contains("Permission", StringComparison.OrdinalIgnoreCase))
        Console.Error.WriteLine("Check that this Kite app/account has historical-data access.");
    else Console.Error.WriteLine("Check date options, connectivity, historical-data access and server file permissions.");
    return 1;
}

internal sealed record Candle(DateTimeOffset Timestamp, decimal Open, decimal High, decimal Low, decimal Close, ulong Volume);
