#r "nuget:System.Text.Json"
#r "nuget:System.Net.Http"
#r "nuget:System.Net.Http.Json"
#r "nuget:TeamCitySharp-forked-mavezeau"

/*
 * Args[0] - env.GITEA_TOKEN_COMMIT_STATUS%
 * Args[1] - teamcity.serverUrl
 * Args[2] - owner
 * Args[3] - repo
 * Args[4] - sha
 * Args[5] - env.TEAMCITY_PROJECT_NAME
 * Args[6] - env.TEAMCITY_BUILDCONF_NAME
 * Args[7] - teamcity.build.triggeredBy
 * Args[8] - system.teamcity.buildType.id
 * Args[9] - teamcity.build.id
 * Args[10] - system.teamcity.auth.userId
 * Args[11] - system.teamcity.auth.password 
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
        var client = new TeamCityClient("your teamcity url", true);
        client.Connect(Args[10], Args[11]);
        var buildField = BuildField.WithFields(status: true, statusText: true, startDate: true);
        var buildsFields = BuildsField.WithFields(buildField: buildField);
        return client.Builds.GetFields(buildsFields.ToString()).ByBuildLocator(BuildLocator.WithId(int.Parse(Args[9])))[0];
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

var token = Args[0];
var owner = Args[2];
var repo = Args[3];
var sha = Args[4];

var buildStatus = TeamcityApi.GetBuildStatus();

var client = new HttpClient();
client.BaseAddress = new Uri(new Uri(Args[1]).GetLeftPart(UriPartial.Authority));
client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
var endPoint = $"/gitea/api/v1/repos/{HttpUtility.UrlEncode(owner)}/{HttpUtility.UrlEncode(repo)}/statuses/{HttpUtility.UrlEncode(sha)}";

WriteLine($"Build status: {buildStatus.Status}, status text: {buildStatus.StatusText}");

var isOk = buildStatus.Status.ToLower() == "success";
var status = new CommitStatus
{
    Context = $"{Args[5]}.{Args[6]}",
    Description = isOk ? "" : buildStatus.StatusText,
    State = isOk ? "success" : "error",
    TargetUrl = $"{Args[1]}/buildConfiguration/{Args[8]}/{Args[9]}"
};
WriteLine($"Start post commit status");
var resp = await client.PostAsJsonAsync(endPoint, status);
WriteLine($"End {resp.StatusCode} to {resp.RequestMessage.RequestUri}");
if (!resp.IsSuccessStatusCode)
{
    var err = await resp.Content.ReadAsStringAsync();
    WriteLine(err);
}