using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

using NHttp;

namespace net.encausse.sarah.http {
  public class HttpManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static HttpManager manager = null;
    private HttpManager() { }

    public static HttpManager GetInstance() {
      if (manager == null) {
        manager = new HttpManager();
      }
      return manager;
    }

    public void Dispose() {
      if (http != null) {
        http.Stop();
        http.Dispose();
      }

      if (local != null) {
        local.Stop();
        local.Dispose();
      }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("HttpManager", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("HttpManager", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("HttpManager", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("HttpManager", ex);
    }

    // -------------------------------------------
    //  HTTP
    // -------------------------------------------

    protected String CleanURL(string url) {
      var prefix = "http://127.0.0.1:8080";
      return url.Replace(prefix, ConfigManager.GetInstance().Find("http.remote.server", prefix));
    }

    // -------------------------------------------
    //  GET
    // -------------------------------------------

    public void SendGET(string url) {
      SendGET(null, url, null, null);
    }
    public void SendGET(string device, string url, string token, IDictionary<string, string> parameters) {
      if (url == null) { return; }

      // Clean URL
      url = CleanURL(url);

      // Append ClientId
      url = AppendURL(url, "client", ConfigManager.GetInstance().Find("bot.id","SARAH"));

      // Append Parameters
      AddOnManager.GetInstance().BeforeGET(device, url, token, parameters);
      if (parameters != null) {
        foreach (var param in parameters) {
          url = AppendURL(url, param.Key, param.Value);
        }
      }

      try {
        Log("Build HttpRequest: " + url);
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "GET";

        Log("Send HttpRequest: " + req.Address);
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        using (StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8)) {
          var body = sr.ReadToEnd();
          Log("Handle BODY Request: " + body + " for " + token);
          AddOnManager.GetInstance().HandleBODY(device, body, token);
        }
      }
      catch (WebException ex) {
        Error(ex);
      }
    }

    protected String AppendURL(string url, string param, string value) {
      value = HttpUtility.UrlEncode(value);
      if (url.IndexOf('?') < 0)
        return url + "?" + param + "=" + value;
      return url + (url.EndsWith("&") ? "" : "&") + param + "=" + value;
    }

    // -------------------------------------------
    //  POST
    // -------------------------------------------

    public void SendPOST(string device, string url, string token, string[] keys, string[] values) {
      if (url == null) { return; }
      if (keys == null || values == null) { return; }

      // Clean URL
      url = CleanURL(url);

      // POST Data
      StringBuilder postData = new StringBuilder();
      for (int i = 0; i < keys.Length; i++) {
        postData.Append(keys[i] + "=" + HttpUtility.UrlEncode(values[i]) + "&");
      }
      ASCIIEncoding ascii = new ASCIIEncoding();
      byte[] postBytes = ascii.GetBytes(postData.ToString());

      // Build request
      Log("Build POSTRequest: " + url);
      HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
      req.Method = "POST";
      req.ContentType = "application/x-www-form-urlencoded";
      req.ContentLength = postBytes.Length;

      // Send POST data
      Log("Send POSTRequest: " + req.Address);
      try {
        Stream postStream = req.GetRequestStream();
        postStream.Write(postBytes, 0, postBytes.Length);
        postStream.Flush();
        postStream.Close();
      }
      catch (WebException ex) {
        Error(ex);
      }
    }

    // -------------------------------------------
    //  UPLOAD
    // -------------------------------------------

    public void SendFILE(string device, string url, string token, string path) {
      if (url == null) { return; }
      if (path == null) { SendGET(device, url, token, null); return; }

      url = CleanURL(url);
      Log("Build UploadRequest: " + url);

      WebClient client = new WebClient();
      client.Headers.Add("user-agent", "S.A.R.A.H. (Self Actuated Residential Automated Habitat)");

      try {
        byte[] responseArray = client.UploadFile(url, path);
        String response = System.Text.Encoding.ASCII.GetString(responseArray);
        AddOnManager.GetInstance().HandleBODY(device, response, token);
      }
      catch (Exception ex) {
        Error("Exception: " + ex.Message);
      }
    }

    // -------------------------------------------
    //  HTTP SERVER
    // -------------------------------------------

    HttpServer http = null;
    HttpServer local = null;

    public void StartHttpServer() {

      int port = ConfigManager.GetInstance().Find("http.local.port", 8888);
      IPAddress address = GetIpAddress();

      // 192.168.0.x
      if (address != null) {
        try {
          http = new HttpServer();
          http.EndPoint = new IPEndPoint(address, port);
          http.Start();
          http.RequestReceived += this.http_RequestReceived;
          Log("Starting Server: http://" + http.EndPoint + "/");
        } catch (Exception ex) {
          http = null;
          Log("Exception: " + ex.Message);
        }
      }

      // Localhost
      try {
        local = new HttpServer();
        local.EndPoint = new IPEndPoint(IPAddress.Loopback, port);
        local.RequestReceived += this.http_RequestReceived;
        local.Start();
        Log("Starting Server: http://" + local.EndPoint + "/");
      } catch (Exception ex) {
        local = null;
        Log("Exception: " + ex.Message);
      }
    }

    protected IPAddress GetIpAddress() {
      IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
      foreach (IPAddress ip in host.AddressList) {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
          return ip;
        }
      }
      return null;
    }

    protected void http_RequestReceived(object sender, HttpRequestEventArgs e) {
      Log("Request received: " + e.Request.Url.AbsoluteUri);

      var qs = e.Request.Url.Query;
      var parameters = e.Request.Params;
      var files = new Dictionary<string, string>();
      var temp = ConfigManager.GetInstance().Find("http.local.temp", "AddOns/http/temp/");

      // Dump all files in a temporary directory
      foreach (string key in e.Request.Files.Keys) {
        var file = e.Request.Files.Get(key);
        if (file == null) continue;

        using (var reader = new BinaryReader(file.InputStream)) {
          var data = reader.ReadBytes(file.ContentLength);
          var path = temp + file.FileName;
          if (File.Exists(path)) { File.Delete(path); }
          File.WriteAllBytes(path, data);
          files.Add(key, path);
        }
      }

      // Fake response
      using (var writer = new StreamWriter(e.Response.OutputStream)) {

        // Handle custom request
        AddOnManager.GetInstance().BeforeHTTPRequest(qs, parameters, files, writer);

        // Write to stream
        writer.Write(" ");
        writer.Flush();
        writer.Close();
      }
      AddOnManager.GetInstance().AfterHTTPRequest(qs, parameters, files);
    }


  }
}
