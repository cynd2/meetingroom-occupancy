using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace MeetingRoomSystem.Services;

public static class GoogleCalendarAuth
{
    public static async Task<CalendarService> CreateAsync(
        string credentialsPath = "credentials.json",
        string tokenPath       = "token.json")
    {
        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException(
                $"credentials.json not found at '{credentialsPath}'. " +
                "Download it from Google Cloud Console → Credentials.");

        UserCredential credential;

        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { CalendarService.Scope.CalendarReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore(tokenPath, true)
            );
        }

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "MeetingRoomSystem"
        });
    }
}