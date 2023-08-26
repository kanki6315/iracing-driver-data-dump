class IRacingUser
{
    public readonly string Id;
    public readonly string Name;
    public readonly List<IRacingLicense> Licenses;

    public IRacingUser(string id, string name, List<IRacingLicense> licenses)
    {
        Id = id;
        Name = name;
        Licenses = licenses;
    }
}

class IRacingLicense
{
    public readonly string Type;
    public readonly string Irating;
    public readonly string SafetyGroup;
    public readonly string SafetyRating;

    public IRacingLicense(string type, string irating, string safetyGroup, string safetyRating)
    {
        Type = type;
        Irating = irating;
        SafetyGroup = safetyGroup;
        SafetyRating = safetyRating;
    }
}