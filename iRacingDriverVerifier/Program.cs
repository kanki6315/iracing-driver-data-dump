// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Config.Net;
using ConsoleTables;
using CsvHelper;
using CsvHelper.Configuration;
using iRacingDriverVerifier;

var iRacingService = new IRacingService();
string filePath;

var settings = new ConfigurationBuilder<IAppSettings>()
    .UseJsonFile("appsettings.json")
    .Build();

GetIracingCredentials(settings);
await iRacingService.IRacingLogin(settings);

var licenseType = GetLicenseTypeFromInput();
var iRacingIds = GetFilePathFromInput();
var users = new List<IRacingUser>();
foreach (var driverChunks in iRacingIds.Chunk(10))
{
    users.AddRange(await iRacingService.GetDriverDetails(driverChunks.ToList()));
}

var table = new ConsoleTable("Id", "Name", "iRating", "License", "SR");
var csvRecords = new List<object>();
foreach (var user in users)
{
    var license = user.Licenses.First(x => x.Type == licenseType);
    table.AddRow(user.Id, user.Name, license.Irating, license.SafetyGroup, license.SafetyRating);
    csvRecords.Add(new { Id = user.Id, Name = user.Name, Irating = license.Irating, License = license.SafetyGroup, SR = license.SafetyRating});
}
table.Write();
var outputFilePath = $"{filePath.Substring(0, filePath.Length - 4)}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.csv";

using (var writer = new StreamWriter(outputFilePath))
using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
{
    csv.WriteRecords(csvRecords);
}

void GetIracingCredentials(IAppSettings settings)
{
    if (String.IsNullOrEmpty(settings.IracingEmail))
    {
        Console.WriteLine("Enter iRacing Email:");
        var emailInput = Console.ReadLine();
        settings.IracingEmail = emailInput!;
        Console.WriteLine("Enter iRacing Password:");
        var passwordInput = Console.ReadLine();
        settings.IracingPassword = passwordInput!;
    }
    else
    {
        Console.WriteLine($"Enter iRacing Email (to continue using: {settings.IracingEmail}, hit enter):");
        var emailInput = Console.ReadLine();
        if (emailInput == "")
        {
            return;
        }
        settings.IracingEmail = emailInput!;
        Console.WriteLine("Enter iRacing Password:");
        var passwordInput = Console.ReadLine();
        settings.IracingPassword = passwordInput!;
    }
}

string GetLicenseTypeFromInput()
{
    var acceptableInput = new List<string>() {"road", "oval", "dirt_oval", "dirt_road"};
    do
    {
        Console.WriteLine("What type of license do you want to look at - road, oval, dirt_oval or dirt_road: (Hit enter to use road)");
        var licenseTypeInput = Console.ReadLine();
        var licenseTypeStr = licenseTypeInput != "" ?  licenseTypeInput : "road";

        if (acceptableInput.Contains(licenseTypeStr))
        {
            return licenseTypeStr;
        }
        Console.WriteLine($"{licenseTypeInput} is not a valid iRacing License Type");
    } while (true);
}

List<string> GetFilePathFromInput()
{
    do
    {
        Console.WriteLine("Enter the file name of the iRacing IDs in a csv format: (Hit enter to use drivers.csv)");
        var filePathInput = Console.ReadLine();
        
        var fileName = filePathInput != "" ?  filePathInput : "drivers.csv";

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using (var reader = new StreamReader(fileName))
            using (var csv = new CsvReader(reader, config))
            {
                var iRacingIds = csv.GetRecords<CsvUser>().Select(x => x.Id).ToList();
                filePath = fileName;
                return iRacingIds;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to read csv: " + ex.Message);
        }
    } while (true);
}


