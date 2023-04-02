#r "nuget:System.Text.Json"
#r "nuget:System.Net.Http"
#r "nuget:System.Net.Http.Json"

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
 */

using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;

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
//    WriteLine($"{i}: {Args[i]}");
//}

var token = Args[0];
var teamcityUrl = Args[1];
var giteaUrl = Args[2];

if (!giteaUrl.EndsWith("/")) giteaUrl += "/";
var client = new HttpClient();
client.BaseAddress = new Uri(giteaUrl);
WriteLine(client.BaseAddress);
client.DefaultRequestHeaders.Add("Authorization", $"token {token}");

var owner = Args[3];
var repo = Args[4];
var sha = Args[5];
var teamcityProjectName = Args[6];
var teamcityBuildName = Args[7];
var teamcityTriggerBy = Args[8];
var teamcityBuildTypeId = Args[9];
var teamcityBuildId = Args[10];

var endPoint = $"api/v1/repos/{HttpUtility.UrlEncode(owner)}/{HttpUtility.UrlEncode(repo)}/statuses/{HttpUtility.UrlEncode(sha)}";
var status = new CommitStatus
{
    Context = $"{teamcityProjectName}.{teamcityBuildName}",
    Description = $"\n\nStarted by {teamcityTriggerBy} at {DateTime.Now.ToString("G")}",
    State = "pending",
    TargetUrl = $"{teamcityUrl}/buildConfiguration/{teamcityBuildTypeId}/{teamcityBuildId}"
};
WriteLine($"Start post commit status");
//WriteLine(status.ToString());
var log = "";
try
{
    var resp = await client.PostAsJsonAsync(endPoint, status);
    log = $"End {resp.StatusCode} to {resp.RequestMessage.RequestUri}";
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
