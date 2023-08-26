using System.Net;
using System.Net.Http.Json;
using System.Web;
using iRacingDriverVerifier;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class IRacingService
{
    private HttpClientHandler _handler;
    private HttpClient _httpClient;
    private const string IracingBaseUrl = "members-ng.iracing.com";

    public IRacingService()
    {
        _handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };
        _httpClient = new HttpClient(_handler);
    }

    public async Task IRacingLogin(IAppSettings settings)
    {
        var sha256 = System.Security.Cryptography.SHA256.Create();

        var passwordAndEmail = settings.IracingPassword + (settings.IracingEmail.ToLowerInvariant());
        var hashedPasswordAndEmailBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(passwordAndEmail));
        var encodedHash = Convert.ToBase64String(hashedPasswordAndEmailBytes);
        
        var body = new
        {
            email = $"{settings.IracingEmail}",
            password = $"{encodedHash}",
        };

        var url = ConstructRequestUrl("auth");

        var response = await _httpClient.PostAsJsonAsync(url, body);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new Exception("iRacing Servers are down for maintenance");
        } else if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Unknown Error while logging in to iRacing");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
    }

    public async Task<List<IRacingUser>> GetDriverDetails(List<string> iRacingIDs)
    {
        var queryParams = new Dictionary<string, object>()
        {
            ["cust_ids"] = string.Join(",", iRacingIDs),
            ["include_licenses"] = true
        };
        var url = ConstructRequestUrl("data/member/get", queryParams);
        var dataResponse = await MakeLinkRequestAndGetData(url);

        var users = new List<IRacingUser>();
        
        var jsonData = (JObject)JsonConvert.DeserializeObject(dataResponse)!;
        if (!jsonData.ContainsKey("success") || jsonData["success"].ToString() == "false")
        {
            throw new Exception("Error retrieving member details from iRacing");
        }
        foreach (var member in jsonData["members"])
        {
            var licenseList = new List<IRacingLicense>();
            foreach (var licenseToken in member["licenses"])
            {
                var license = (JObject) licenseToken;
                licenseList.Add(new IRacingLicense(
                    license["category"].ToString(), 
                    license.ContainsKey("irating") ? license["irating"].ToString() : "0", 
                    license["group_name"].ToString(), 
                    license["safety_rating"].ToString()));
            }

            var newUser = new IRacingUser(
                member["cust_id"].ToString(),
                member["display_name"].ToString(), 
                licenseList);
            users.Add(newUser);
        }

        return users;
    }

    private async Task<string> MakeLinkRequestAndGetData(string url)
    {
        var linkResponse = await _httpClient.GetAsync(url);
        if (linkResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new Exception("iRacing Servers are down for maintenance");
        } else if (!linkResponse.IsSuccessStatusCode)
        {
            throw new Exception("Unknown Error while retrieving driver details");
        }

        return await ProcessLinkResponseAndRequestData(await linkResponse.Content.ReadAsStringAsync());
    }

    private async Task<string> ProcessLinkResponseAndRequestData(string responseBody)
    {
        var jsonData = (JObject)JsonConvert.DeserializeObject(responseBody)!;

        if (!jsonData.ContainsKey("link"))
        {
            throw new Exception($"Unable to get link response from iRacing: {responseBody}");
        }

        return await _httpClient.GetStringAsync(jsonData["link"]!.ToString());
    }
    
    private static string ConstructRequestUrl(string path)
    {
        var builder = new UriBuilder("https", IracingBaseUrl) { Path = path };
        return builder.ToString();
    }
    
    private static string ConstructRequestUrl(string path, Dictionary<string, object> queryParams)
    {
        var builder = new UriBuilder("https", IracingBaseUrl) { Path = path };
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var (key, value) in queryParams)
        {
            query[key] = value.ToString();
        }

        builder.Query = HttpUtility.UrlDecode(query.ToString());

        return builder.ToString();
    }
}