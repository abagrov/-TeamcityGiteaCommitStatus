#r "nuget:System.Text.Json"
#r "nuget:System.Net.Http"
#r "nuget:System.Net.Http.Json"
#r "nuget:TeamCitySharp-forked-mavezeau"

/*
 * Args[0] - env.GITEA_TOKEN_COMMIT_STATUS
 * Args[1] - teamcity.serverUrl
 * Args[2] - Gitea server url
 * Args[3] - owner of repo where commit status will be posted
 * Args[4] - repo where commit status will be posted
 * Args[5] - sha of commit
 * Args[6] - env.TEAMCITY_PROJECT_NAME
 * Args[7] - env.TEAMCITY_BUILDCONF_NAME
 * Args[8] - teamcity.build.triggeredBy
 * Args[9] - system.teamcity.buildType.id
 * Args[10] - teamcity.build.id
 * Args[11] - system.teamcity.auth.userId
 * Args[12] - system.teamcity.auth.password 
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Text.Json.Serialization;
using TeamCitySharp;
using TeamCitySharp.Fields;
using TeamCitySharp.Locators;
using TeamCitySharp.DomainEntities;
using System;

static class TeamcityApi
{
    public static Build GetBuildStatus()
    {
        var teamcityUrl = Args[1];

        var scheme = new Uri(teamcityUrl).GetLeftPart(UriPartial.Scheme);
        var teamcityUrlWoScheme = Args[1].Replace(scheme, "");
        //WriteLine(teamcityUrlWoScheme);
        var client = new TeamCityClient(teamcityUrlWoScheme, true);

        var teamcityUser = Args[11];
        var teamcityPass = Args[12];

        client.Connect(teamcityUser, teamcityPass);
        var buildField = BuildField.WithFields(status: true, statusText: true, startDate: true);
        var buildsFields = BuildsField.WithFields(buildField: buildField);

        if (!int.TryParse(Args[10], out var buildId))
            throw new ArgumentException($"Cant parse buildId parameter (Args[10]) - {Args[10]}.");

        return client.Builds.GetFields(buildsFields.ToString()).ByBuildLocator(BuildLocator.WithId(buildId))[0];
    }
}

public class CommitStatus
{
    [JsonPropertyName("context")]
    public string Context { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("target_url")]
    public string TargetUrl { get; set; }

    public override string ToString()
    {
        return $"Context: {Context} | Description: {Description} | State: {State} | TargetUrl: {TargetUrl}";
    }
}

//for (int i = 1; i < Args.Count; i++)
//{
//    WriteLine($"Args[{i}]: {Args[i]}");
//}

var token = Args[0];
var teamcityUrl = Args[1];
var giteaUrl = Args[2];
var owner = Args[3];
var repo = Args[4];
var sha = Args[5];

var buildStatus = TeamcityApi.GetBuildStatus();

if (!giteaUrl.EndsWith("/")) giteaUrl += "/";
var client = new HttpClient();
client.BaseAddress = new Uri(giteaUrl);
//WriteLine(client.BaseAddress);
client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
var endPoint = $"api/v1/repos/{HttpUtility.UrlEncode(owner)}/{HttpUtility.UrlEncode(repo)}/statuses/{HttpUtility.UrlEncode(sha)}";

WriteLine($"Build status: {buildStatus.Status}, status text: {buildStatus.StatusText}");

var isOk = buildStatus.Status.ToLower() == "success";
var teamcityProjectName = Args[6];
var teamcityBuildName = Args[7];
var teamcityBuildTypeId = Args[9];
var teamcityBuildId = Args[10];

var status = new CommitStatus
{
    Context = $"{teamcityProjectName}.{teamcityBuildName}",
    Description = isOk ? "" : buildStatus.StatusText,
    State = isOk ? "success" : "error",
    TargetUrl = $"{teamcityUrl}/buildConfiguration/{teamcityBuildTypeId}/{teamcityBuildId}"
};
WriteLine($"Start post commit status");
//WriteLine(status.ToString());
var log = "";
try
{
    var resp = await client.PostAsJsonAsync(endPoint, status);
    log = $"Got status code {resp.StatusCode}. Target URL: {resp.RequestMessage.RequestUri}";
    if (!resp.IsSuccessStatusCode)
    {
        var err = await resp.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{log}\n{err}");
    }
}
finally
{
    client.Dispose();
}
WriteLine(log);
