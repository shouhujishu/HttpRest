using System.Text;

/// <summary>
/// 守护封装制作
/// </summary>
namespace ShouHu.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    /// <summary>
    /// Rest 请求客户端 线程安全可以同时进行不同请求  .Create() 创建客户端;
    /// </summary>
    public class RestClient
    {
        private HttpClient client = null;

        /// <summary>
        /// 是否允许重定向 默认False
        /// </summary>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// 允许重定向最大次数 如为0默认为:7 允许重定向才有效.
        /// </summary>
        public int MaxAutomaticRedirections { get; set; }

        /// <summary>
        /// 设置代理服务器
        /// </summary>
        public string Proxy { get; set; }

        /// <summary>
        /// 设置超时毫秒 0默认设置
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 创建客户端
        /// </summary>
        /// <returns></returns>
        public RestClient Create()
        {
            HttpClientHandler Handler = new HttpClientHandler()
            {
                UseCookies = false,
                AllowAutoRedirect = this.AllowAutoRedirect,
                Proxy = (!String.IsNullOrEmpty(this.Proxy) ? new WebProxy(this.Proxy, false) { UseDefaultCredentials = false, } : null),
            };
            if (this.AllowAutoRedirect)
            {
                Handler.MaxAutomaticRedirections = this.MaxAutomaticRedirections > 0 ? this.MaxAutomaticRedirections : 7;
            }
            client = new HttpClient(Handler);
            if (this.Timeout > 0)
            {
                client.Timeout = TimeSpan.FromMilliseconds(this.Timeout);
            }
            return this;
        }

        /// <summary>
        /// 发送请求数据
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public RestRes Send(RestItem Request)
        {
            if (client == null) throw new ArgumentException("RestClient未创建!");
            using (Task<HttpResponseMessage> ReqTask = client.SendAsync(Request.GetMessage()))
            {
                RestRes Res = new RestRes();
                try
                {
                    ReqTask.Wait();
                    using (HttpResponseMessage Response = ReqTask.Result)
                    {
                        Res.StatusCode = (int)Response.StatusCode;//HTTP响应状态码
                        Res.Headers = Response.Headers;
                        Res.Bytes = Response.Content.ReadAsByteArrayAsync().Result;//HTTP响应内容
                        Res.Cookies = CookieEx(Res.Headers, Request.Cookies, Request.AutoCookies);
                        Res.IsSuccessStatusCode = Response.IsSuccessStatusCode;
                        if (Request.AutoCookies) Request.Cookies = Res.Cookies;
                    }
                }
                catch (Exception ex)
                {
                    Res.Exception = ex.Message;
                }
                return Res;
            }
        }

        /// <summary>
        /// 内部cookie 更新处理
        /// </summary>
        /// <param name="Headers"></param>
        /// <param name="UpCookie"></param>
        /// <param name="Update"></param>
        /// <returns></returns>
        private string CookieEx(HttpResponseHeaders Headers, string UpCookie, bool Update)
        {
            string _Cookies = "", Name, Value;
            IEnumerable<string> cookies;
            Dictionary<string, string> CookieY = new Dictionary<string, string>();
            int index = -1;

            if (Headers.TryGetValues("set-cookie", out cookies))
            {
                if (Update && !string.IsNullOrEmpty(UpCookie))
                {
                    string[] Cookie = UpCookie.Split(';');
                    foreach (var item in Cookie)
                    {
                        index = item.IndexOf("=");
                        if (index > -1)
                        {
                            Name = item.Substring(0, index);
                            Value = item.Substring(Name.Length + 1).Trim();
                            Name = Name.Trim();
                            if (CookieY.ContainsKey(Name))
                            {
                                CookieY[Name] = Value;
                            }
                            else
                            {
                                CookieY.Add(Name, Value);
                            }
                        }
                    }
                }

                foreach (var item in cookies)
                {
                    index = item.IndexOf(";");
                    if (index > -1)
                    {
                        string Cookie = item.Substring(0, index).Trim();
                        index = Cookie.IndexOf("=");
                        if (index > -1)
                        {
                            Name = Cookie.Substring(0, index);
                            Value = Cookie.Substring(Name.Length + 1).Trim();
                            Name = Name.Trim();
                            if (CookieY.ContainsKey(Name))
                            {
                                CookieY[Name] = Value;
                            }
                            else
                            {
                                CookieY.Add(Name, Value);
                            }
                        }
                    }
                }

                if (CookieY.Count > 0)
                {
                    foreach (var item in CookieY)
                    {
                        Name = item.Key;
                        Value = item.Value;
                        if (Value == "deleted" || Value == "DELETED" || Value == "Deleted")
                        {
                        }
                        else
                        {
                            _Cookies += Name + "=" + Value + "; ";
                        }
                    }
                }
                _Cookies = _Cookies.Trim().TrimEnd(';');
            }
            else
            {
                if (Update) _Cookies = UpCookie;
            }
            return _Cookies;
        }
    }

    /// <summary>
    /// Rest 请求项目配置
    /// </summary>
    public class RestItem
    {
        private string _Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        private string _ContentType = "application/x-www-form-urlencoded";
        private string _Method = "GET";
        private string _UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.167 Safari/537.36";

        public RestItem()
        {
            this._initialize();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Url">地址</param>
        public RestItem(string Url)
        {
            this.Url = Url;
            this._initialize();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="method">请求方式</param>
        /// <param name="Url">地址</param>
        public RestItem(string method, string Url)
        {
            this.Method = method;
            this.Url = Url;
            this._initialize();
        }

        /// <summary>
        /// 接收数据类型 默认:text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8
        /// </summary>
        public string Accept
        {
            get { return this._Accept; }
            set { this._Accept = value; }
        }

        /// <summary>
        /// 自动更新处理Cookies 默认:False
        /// </summary>
        public bool AutoCookies { get; set; }

        /// <summary>
        /// 提交数据类型 默认:application/x-www-form-urlencoded
        /// </summary>
        public string ContentType
        {
            get { return _ContentType; }
            set { _ContentType = value; }
        }

        /// <summary>
        /// Cookies 例:name1=xxx; name2=xxx 本Cookie无视域限制.
        /// </summary>
        public string Cookies { get; set; }

        /// <summary>
        /// 设置协议头 TryAdd
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
        /// <summary>
        /// 快捷添加协议头重复键名默认覆盖
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="Val">值</param>
        public void HeadersAdd(string key,string Val) {
            if (this.Headers.ContainsKey(key))
            {
                this.Headers[key] = Val;
            }
            else
            {
                this.Headers.Add(key, Val);
            }
            
        }
        /// <summary>
        /// 请求方式 默认GET
        /// </summary>
        public string Method { get { return _Method; } set { _Method = value; } }

        /// <summary>
        /// 发送字节 (String/Byte)同不为NULL 默认String优先
        /// </summary>
        public byte[] PostdataByte { get; set; }

        /// <summary>
        /// 发送字符串 (String/Byte)同不为NULL 默认String优先
        /// </summary>
        public string PostString { set; get; }

        /// <summary>
        /// 设置来源地址
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        ///请求地址
        /// </summary>
        public string Url { set; get; }

        /// <summary>
        /// 用户代理 默认:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.167 Safari/537.36
        /// </summary>
        public string UserAgent
        {
            get { return _UserAgent; }
            set { _UserAgent = value; }
        }

        /// <summary>
        /// 生成Send 请求包
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage GetMessage()
        {
            HttpRequestMessage Request = new HttpRequestMessage()
            {
                RequestUri = new Uri(this.Url),
                Method = new HttpMethod(this.Method),
            };
                Request.Headers.Add("Accept", this.Accept);
                if (this.Referer != null) Request.Headers.Referrer = new Uri(this.Referer);
                if (Request.Method != HttpMethod.Get)
                {
                    if (this.PostString != null)
                    {
                        Request.Content = new StringContent(this.PostString);
                        Request.Content.Headers.Remove("Content-Type");
                        Request.Content.Headers.Add("Content-Type", this.ContentType);
                    }
                    else if (this.PostdataByte != null)
                    {
                        Request.Content = new ByteArrayContent(this.PostdataByte);
                        Request.Content.Headers.Remove("Content-Type");
                        Request.Content.Headers.Add("Content-Type", this.ContentType);
                    }
                }
                Request.Headers.Add("User-Agent", this.UserAgent);
                if (Headers.Count > 0)
                {
                    foreach (var item in Headers)
                    {
                        Request.Headers.TryAddWithoutValidation(item.Key, item.Value);
                    }
                }
                if (!string.IsNullOrEmpty(this.Cookies)) Request.Headers.Add("Cookie", this.Cookies);

                return Request;
           
        }

        private void _initialize()
        {
            this.Headers = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Rest 返回结果
    /// </summary>
    public class RestRes
    {
        /// <summary>
        /// 取响应原始字节
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// 取Cookies 默认AutoCookies为False时只返回当前Cookies
        /// </summary>
        public string Cookies { get; set; }

        /// <summary>
        /// 异常错误提示 默认NULL
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// 成功状态 状态码范围(200-299)
        /// </summary>
        public bool IsSuccessStatusCode { get; set; }

        /// <summary>
        /// 取响应标头集合
        /// </summary>
        public HttpResponseHeaders Headers { get; set; }

        /// <summary>
        /// 重定向URL地址
        /// </summary>
        public Uri Location { get { return this.Headers.Location; } }

        /// <summary>
        ///  取响应状态码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 取内容文本 编码:UTF8
        /// </summary>
        public string Text { get { return Encoding.UTF8.GetString(this.Bytes); } }

        /// <summary>
        /// 取指定编码内容文本
        /// </summary>
        /// <param name="en">指定编码</param>
        /// <returns></returns>
        public string TextEx(Encoding en)
        {
            return en.GetString(this.Bytes);
        }
    }
}

namespace ShouHu.RestExtended
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

  
    /// <summary>
    /// HttpEasy 扩展类
    /// </summary>
    public static class RestEx
    {
        /// <summary>
    /// 取现行时间戳10位
    /// </summary>
    /// <returns></returns>
        public static string TimeStamp10()
        {
            long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp.ToString();
        }

        /// <summary>
        /// 取现行时间戳13位
        /// </summary>
        /// <returns></returns>
        public static string TimeStamp13()
        {
            long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            return unixTimestamp.ToString();
        }
        /// <summary>
        /// 时间戳转时间
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            if (unixTimeStamp.Length == 10)
            {
                dtDateTime = dtDateTime.AddSeconds(long.Parse(unixTimeStamp)).ToLocalTime();
            } else if (unixTimeStamp.Length == 13) {
                dtDateTime = dtDateTime.AddMilliseconds(long.Parse(unixTimeStamp)).ToLocalTime();
            }
            else
            {
                throw new Exception("时间戳长度错误！");
            }
            return dtDateTime;
        }
        /// <summary>
        /// 去除HTML标签提取正文内容
        /// </summary>
        /// <param name="htmlString">HTML源码</param>
        /// <returns></returns>
        public static string GetPlainTextFromHtml(string htmlString)
        {
            string htmlTagPattern = "<.*?>";
            var regexCss = new Regex("(\\<script(.+?)\\</script\\>)|(\\<style(.+?)\\</style\\>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            htmlString = regexCss.Replace(htmlString, string.Empty);
            htmlString = Regex.Replace(htmlString, htmlTagPattern, string.Empty);
            htmlString = Regex.Replace(htmlString, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
            htmlString = htmlString.Replace("&nbsp;", string.Empty);

            return htmlString;
        }

        /// <summary>
        /// 取文本左边
        /// </summary>
        /// <param name="Text">原始文本</param>
        /// <param name="str">截止文本</param>
        /// <param name="index">起始位置</param>
        /// <returns></returns>
        public static string LeftEx(string Text, string str, int index = 0)
        {
            if (Text.Length == 0) return Text;
            int x = Text.IndexOf(str, index);
            if (x == -1) return "";
            return Text.Substring(0, x);
        }

        /// <summary>
        /// 取文本中间
        /// </summary>
        /// <param name="Text">原始文本</param>
        /// <param name="Start">起始文本</param>
        /// <param name="End">截止文本</param>
        /// <param name="index">起始位置</param>
        /// <returns></returns>
        public static string MidEx(string Text, string Start, string End, int index = 0)
        {
            if (Text.Length == 0) return Text;
            int x1 = Text.IndexOf(Start, index);
            if (x1 == -1) return "";
            x1 += Start.Length;
            int x2 = Text.IndexOf(End, x1+1);
            if (x2 == -1) return "";
            return Text.Substring(x1, x2 - x1);
        }

        /// <summary>
        /// 取文本右边
        /// </summary>
        /// <param name="Text">原始文本</param>
        /// <param name="str">截止文本</param>
        /// <param name="index">起始位置</param>
        /// <returns></returns>
        public static string RightEx(string Text, string str, int index = -1)
        {
            if (Text.Length == 0) return Text;
            if (index == -1) index = Text.Length;
            int x = Text.LastIndexOf(str, index);
            if (x == -1) return "";
            x = Text.Length - str.Length - x;
            return Text.Substring(Text.Length - x, x);
        }

        /// <summary>
        /// 字符串转Unicode文本 例:你好->\u4f60\u597d;
        /// </summary>
        /// <param name="text">待转换文本</param>
        /// <returns></returns>
        public static string StringToUnicode(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c > 127)
                {
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Unicode文本转字符串 例:\u4f60\u597d->你好
        /// </summary>
        /// <param name="text">转换文本</param>
        /// <returns></returns>
        public static string UnicodeToString(string text)
        {
            return Regex.Replace(
            text,
            @"\\u(?<Value>[a-zA-Z0-9]{4})",
            m =>
            {
                return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
            });
        }
        /// <summary>
        /// 取Cookies值
        /// </summary>
        /// <param name="cookie">全部Cookies</param>
        /// <param name="Name">键名</param>
        /// <returns></returns>
        public static string GetCookiesVal(string cookie, string Name) {
         int L=  cookie.IndexOf(Name+"=");
            if (L==-1) return "";
         int x= cookie.IndexOf(";", L+1);
            if (x > -1)
            {
                L = L + Name.Length + 1;
             return  cookie.Substring(L, x-L).Trim();
            }
            else {
                return cookie.Substring(L + Name.Length + 1).Trim();
            }
        }

    }
}