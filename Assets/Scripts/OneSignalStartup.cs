using OneSignalSDK;
using UnityEngine;

public class OneSignalStartup : MonoBehaviour
{
    [SerializeField] private string appId;

    public string UserId => OneSignal.User?.PushSubscription?.Id;
    public string PushToken => OneSignal.User?.PushSubscription?.Token;

    public void Initialize()
    {
        OneSignal.Initialize(appId);
    }

    public void SetExternalId(string _id)
    {
        OneSignal.Login(_id);
    }

    public void Unsubscribe()
    {
        OneSignal.Notifications?.ClearAllNotifications();
        OneSignal.Logout();
    }
}
