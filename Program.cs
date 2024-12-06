using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using XtremeLogoDumper;
using System.Text.Json.Serialization;

ColorConsole.WriteLine("~|Yellow|XtremeLogoDumper~|Reset| v1.0 - ~|Green|Luka Kusulja");
ColorConsole.WriteLine("~|Cyan|https://github.com/luka-kusulja");
Console.WriteLine();

#region Login and account info
Console.Write("Server host: ");
var serverHost = Console.ReadLine();
Console.Write("Username: ");
var username = Console.ReadLine();
Console.Write("Password: ");
var password = Console.ReadLine();
Console.Write("Custom agent (ex. ThrPlayer/1.1.1): ");
var agentHeader = Console.ReadLine();

Console.WriteLine();

if (String.IsNullOrEmpty(agentHeader) == false)
{
    XtremeClient.AgentHeader = agentHeader;
}

var accountInfoUrl = new Uri($"{serverHost?.TrimEnd('/')}/player_api.php?username={username}&password={password}");
var liveStreamsUrl = new Uri($"{accountInfoUrl.AbsoluteUri}&action=get_live_streams");

var workingDirectoryName = $"{liveStreamsUrl.Host}-{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}";

var jsonParseOptions = new JsonSerializerOptions
{
    AllowOutOfOrderMetadataProperties = true,
    AllowTrailingCommas = true,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    ReadCommentHandling = JsonCommentHandling.Skip
};

ColorConsole.WriteLine($"Creating working directory ~|Yellow|{workingDirectoryName}");
Directory.CreateDirectory(workingDirectoryName);

ColorConsole.WriteLine($"Getting account info for ~|Green|{username}");
string? rawAccountJson;
try
{
    rawAccountJson = XtremeClient.GetString(accountInfoUrl).Result;
}
catch (Exception ex)
{
    ColorConsole.WriteLine($"~|Red|{ex.Message}");
    return 1;
}

if(String.IsNullOrEmpty(rawAccountJson))
{
    ColorConsole.WriteLine($"~|Red|Auth failed");
    return 2;
}

Console.WriteLine("Got server response");
var accountJsonFilename = $"{workingDirectoryName}/account_info.json";
ColorConsole.WriteLine($"Writing response ~|Magenta|{ASCIIEncoding.ASCII.GetByteCount(rawAccountJson)}~|Reset| bytes to ~|Yellow|{accountJsonFilename}");
File.WriteAllText(accountJsonFilename, rawAccountJson);
Console.WriteLine("Response writen to file");

var accountInfo = JsonSerializer.Deserialize<StreamAccountItem>(rawAccountJson, jsonParseOptions);

if(accountInfo?.user_info.auth == 1)
{
    ColorConsole.WriteLine($"~|Green|Successful auth");
}

Console.WriteLine("Account JSON info");
Console.WriteLine(JsonSerializer.Serialize(accountInfo, new JsonSerializerOptions { WriteIndented = true }));
#endregion

#region Dump logo
Console.WriteLine("Getting live stream list");
var rawStreamsJson = XtremeClient.GetString(liveStreamsUrl).Result;

Console.WriteLine("Got server response");
var streamsJsonFilename = $"{workingDirectoryName}/get_live_streams.json";
ColorConsole.WriteLine($"Writing response ~|Magenta|{ASCIIEncoding.ASCII.GetByteCount(rawStreamsJson)}~|Reset| bytes to ~|Yellow|{streamsJsonFilename}");
File.WriteAllText(streamsJsonFilename, rawStreamsJson);
Console.WriteLine("Response writen to file");

var streamsJson = JsonSerializer.Deserialize<List<StreamItem>>(rawStreamsJson, jsonParseOptions);

ColorConsole.WriteLine($"~|Cyan|{streamsJson.Count} ~|Reset|streams found");

var uniqueLogos = streamsJson.Where(x => String.IsNullOrWhiteSpace(x.stream_icon) == false).DistinctBy(x => x.stream_icon).ToList();
ColorConsole.WriteLine($"~|Cyan|{uniqueLogos.Count} ~|Reset|unique logos found (~|Cyan|{((100.0 / streamsJson.Count) * uniqueLogos.Count).ToString("00.00")}%~|Reset|)");

var logoUrlFilename = $"{workingDirectoryName}/urls.txt";
var allLogoUrls = uniqueLogos.Select(x => x.stream_icon).ToArray();
File.WriteAllLines(logoUrlFilename, allLogoUrls);
ColorConsole.WriteLine($"Extracted all URLs into ~|Yellow|{logoUrlFilename}");

Console.WriteLine("Do you want to dump all logos ? (y / n)");
if(Console.ReadKey(true).Key != ConsoleKey.Y)
{
    return 3;
}

var logosDirectory = $"{workingDirectoryName}/logos";
ColorConsole.WriteLine($"Creating logos directory ~|Yellow|{logosDirectory}");
Directory.CreateDirectory(logosDirectory);

ColorConsole.WriteLine("Starting logo dump ~|Red|press X to stop");
Console.WriteLine();
Console.WriteLine();

var i = 0;
var failed = 0;
var stopwatch = Stopwatch.StartNew();
var consoleTopPosition = Console.GetCursorPosition().Top;
Console.CursorVisible = false;
foreach (var currentLogo in uniqueLogos)
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.X)
    {
        Console.WriteLine();
        ColorConsole.WriteLine("~|Red|Interupted by user");
        break;
    }

    i++;

    byte[] file = [];
    try
    {
        file = XtremeClient.GetByte(new Uri(currentLogo.stream_icon)).Result;
    }
    catch
    {
        failed++;
        File.AppendAllText($"{workingDirectoryName}/failed.txt", currentLogo.stream_icon + Environment.NewLine);
    }

    if (file.Length > 16)
    {
        var filename = $"{currentLogo.stream_id}-{currentLogo.stream_icon.Substring(currentLogo.stream_icon.LastIndexOf("/") + 1)}";
        File.WriteAllBytes($"{logosDirectory}/{filename}", file);
    }

    Console.SetCursorPosition(0, consoleTopPosition - 1);
    var eta = (stopwatch.Elapsed.TotalSeconds / i) * (uniqueLogos.Count - i);
    ColorConsole.Write($"~|Cyan|{((100.0 / uniqueLogos.Count) * i).ToString("00.00")}%~|Reset| | ~|Cyan|{i} ~|Reset|processed | ~|Cyan|{failed} ~|Reset|failed | ETA ~|Cyan|{TimeSpan.FromSeconds(eta).ToString(@"hh\:mm\:ss")}");

    if(i % 100 == 0)
    {
        Task.Delay(50).Wait();
    }
}

Console.CursorVisible = true;
Console.WriteLine();
ColorConsole.WriteLine($"Logos dumped in ~|Cyan|{TimeSpan.FromSeconds(stopwatch.Elapsed.TotalMinutes).ToString(@"hh\:mm\:ss")} ~|Reset|minutes");
#endregion

Console.WriteLine();
Console.WriteLine("Report any bugs or feature requests to");
ColorConsole.WriteLine("~|Cyan|https://github.com/luka-kusulja");
Console.WriteLine();

return 0;