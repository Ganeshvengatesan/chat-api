using ChatApplicationAPI.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace ChatApplicationAPI.Services.Firebase;

public class FirebaseService : IFirebaseService
{
    private readonly ILogger<FirebaseService> _logger;
    private readonly bool _isInitialized = false;

    public FirebaseService(IConfiguration configuration, ILogger<FirebaseService> logger)
    {
        _logger = logger;
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = configuration["Firebase:CredentialFilePath"] 
                    ?? "linkup-e5614-firebase-adminsdk-fbsvc-eb387dbf74.json";

                if (File.Exists(credentialPath))
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                    _isInitialized = true;
                    _logger.LogInformation("Firebase Admin SDK successfully initialized with {Path}", credentialPath);
                }
                else
                {
                    _logger.LogWarning("Firebase credential file not found at {Path}. FCM push notifications disabled until file is present.", credentialPath);
                }
            }
            else
            {
                _isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
        }
    }

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning("Skipping push notification: Firebase not initialized or empty device token.");
            return false;
        }

        try
        {
            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent message to token {Token}: {Response}", deviceToken, response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Firebase push notification to token {Token}", deviceToken);
            return false;
        }
    }

    public async Task<int> SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isInitialized || deviceTokens == null || !deviceTokens.Any())
            return 0;

        try
        {
            var message = new MulticastMessage()
            {
                Tokens = deviceTokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
            _logger.LogInformation("Multicast push notification sent. Success count: {SuccessCount}", response.SuccessCount);
            return response.SuccessCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending multicast Firebase push notifications");
            return 0;
        }
    }
}
