using CsvHelper.Configuration.Attributes;

namespace iRacingDriverVerifier;

class CsvUser {
    [Index(0)]
    public string Id { get; set; }
}