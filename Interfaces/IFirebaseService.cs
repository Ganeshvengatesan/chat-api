namespace ChatApplicationAPI.Interfaces;

public interface IFirebaseService
{
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
    Task<int> SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
}
