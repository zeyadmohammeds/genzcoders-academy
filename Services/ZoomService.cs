using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace GenZCoders.Services;

public interface IZoomService
{
    Task<ZoomMeetingResponse> CreateMeetingAsync(string topic, int durationMinutes, CancellationToken cancellationToken = default);
}

public class ZoomMeetingResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("join_url")]
    public string JoinUrl { get; set; } = string.Empty;

    [JsonPropertyName("start_url")]
    public string StartUrl { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class ZoomService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IZoomService
{
    public async Task<ZoomMeetingResponse> CreateMeetingAsync(string topic, int durationMinutes, CancellationToken cancellationToken = default)
    {
        var accountId = configuration["Zoom:AccountId"];
        var clientId = configuration["Zoom:ClientId"];
        var clientSecret = configuration["Zoom:ClientSecret"];

        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Zoom Server-to-Server OAuth credentials are not fully configured in appsettings.json.");
        }

        using var client = httpClientFactory.CreateClient();
        
        // 1. Get Access Token
        var tokenUrl = $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}";
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        
        var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to acquire Zoom access token: {tokenResponse.StatusCode} - {errContent}");
        }
        
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Access token was not returned in the Zoom response.");

        // 2. Create Meeting
        var createMeetingUrl = "https://api.zoom.us/v2/users/me/meetings";
        var meetingRequest = new HttpRequestMessage(HttpMethod.Post, createMeetingUrl);
        meetingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var body = new
        {
            topic = topic,
            type = 2, // Scheduled Meeting
            duration = durationMinutes,
            settings = new
            {
                host_video = true,
                participant_video = true,
                join_before_host = true,
                jbh_time = 0,
                mute_upon_entry = true,
                waiting_room = false,
                approval_type = 2 // No registration required
            }
        };

        meetingRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        
        var meetingResponse = await client.SendAsync(meetingRequest, cancellationToken);
        if (!meetingResponse.IsSuccessStatusCode)
        {
            var errContent = await meetingResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create Zoom meeting: {meetingResponse.StatusCode} - {errContent}");
        }
        
        var meetingJson = await meetingResponse.Content.ReadAsStringAsync(cancellationToken);
        var zoomResponse = JsonSerializer.Deserialize<ZoomMeetingResponse>(meetingJson);
        
        return zoomResponse ?? throw new InvalidOperationException("Failed to deserialize Zoom meeting response.");
    }
}
