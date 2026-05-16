using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GenZCoders.Services;

/// <summary>
/// Generates a JWT for the Zoom Meeting SDK (web) using the SDK key/secret.
/// Configure <c>Zoom:SdkKey</c> and <c>Zoom:SdkSecret</c> from the Zoom Marketplace SDK app.
/// </summary>
public static class ZoomMeetingSdkSignature
{
    public static string Generate(string sdkKey, string sdkSecret, string meetingNumber, int role)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = iat + 7200; // 2 hours
        var payload = new Dictionary<string, object>
        {
            ["appKey"] = sdkKey,
            ["sdkKey"] = sdkKey,
            ["mn"] = meetingNumber,
            ["role"] = role,
            ["iat"] = iat,
            ["exp"] = exp,
            ["tokenExp"] = exp,
        };

        var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" }));
        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var header = Base64UrlEncode(headerBytes);
        var payloadSegment = Base64UrlEncode(payloadBytes);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sdkSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payloadSegment}"));
        var sig = Base64UrlEncode(hash);
        return $"{header}.{payloadSegment}.{sig}";
    }

    static string Base64UrlEncode(byte[] data)
    {
        var s = Convert.ToBase64String(data);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
