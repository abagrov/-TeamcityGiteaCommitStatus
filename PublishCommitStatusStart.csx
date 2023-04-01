#r "nuget:System.Text.Json"
#r "nuget:System.Net.Http"
#r "nuget:System.Net.Http.Json"

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
var client = new HttpClient();
client.BaseAddress = new Uri(new Uri(Args[1]).GetLeftPart(UriPartial.Authority));
//WriteLine(client.BaseAddress);
client.DefaultRequestHeaders.Add("Authorization", $"token {token}");

var owner = Args[2];
var repo = Args[3];
var sha = Args[4];

var endPoint = $"/gitea/api/v1/repos/{HttpUtility.UrlEncode(owner)}/{HttpUtility.UrlEncode(repo)}/statuses/{HttpUtility.UrlEncode(sha)}";
var status = new CommitStatus
{
    Context = $"{Args[5]}.{Args[6]}",
    Description = $"\n\nStarted by {Args[7]} at {DateTime.Now.ToString("G")}",
    State = "pending",
    TargetUrl = $"{Args[1]}/buildConfiguration/{Args[8]}/{Args[9]}"
};
WriteLine($"Start post commit status");
//WriteLine(status.ToString());
var resp = await client.PostAsJsonAsync(endPoint, status);
WriteLine($"End {resp.StatusCode} to ${resp.RequestMessage.RequestUri}");
if (!resp.IsSuccessStatusCode)
{
    var err = await resp.Content.ReadAsStringAsync();
    WriteLine($"Error: {err}");
}