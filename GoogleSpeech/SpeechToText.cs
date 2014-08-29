using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Globalization;
using System.Web.Script.Serialization;

using CUETools.Codecs;
using CUETools.Codecs.FLAKE;


namespace net.encausse.sarah {
  public class SpeechToText {
    private string endpointAddress;

    public SpeechToText(string endpointAddress, CultureInfo culture) {
      this.endpointAddress = endpointAddress + "&lang=" + culture.Name;
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("SpeechToText", msg);
    }

    // -------------------------------------------
    //  RECOGNIZE
    // ------------------------------------------

    public String Recognize(Stream contentToRecognize) {
      var request = (HttpWebRequest) WebRequest.Create(this.endpointAddress + "&maxresults=6&pfilter=2");
      ConfigureRequest(request);
      var requestStream = request.GetRequestStream();
      ConvertToFlac(contentToRecognize, requestStream);

      using (var response = request.GetResponse()) {
        using (var responseStream = response.GetResponseStream()) {
          using (var zippedStream = new GZipStream(responseStream, CompressionMode.Decompress)) {
            using (var sr = new StreamReader(zippedStream)) {
              var results = sr.ReadToEnd().Split('\n');
              return toJSON(results);
            }
          }
        }
      }
    }

    private String toJSON(string[] response) {
      JavaScriptSerializer serializer = new JavaScriptSerializer();
      foreach (var str in response) {
        if (String.IsNullOrEmpty(str)) continue;

        var json = serializer.DeserializeObject(str);
        if (!(json is IDictionary<string, object>)) continue;

        IDictionary<string, object> dict = (IDictionary<string, object>) json;

        var results = (object[]) dict["result"];
        if (results == null || results.Length == 0) continue;
        var result  = (IDictionary<string, object>) results[0];

        var alts = (object[]) result["alternative"];
        if (alts == null || alts.Length == 0) continue;
        var alt = (IDictionary<string, object>) alts[0];

        Debug("toJSON: " + alt["confidence"] + " : " + alt["transcript"]);
        return (string) alt["transcript"];
      }
      return "";
    }

    private static void ConfigureRequest(HttpWebRequest request) {
      request.KeepAlive = true;
      request.SendChunked = true;
      request.ContentType = "audio/x-flac; rate=16000";
      request.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
      request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
      request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-GB,en-US;q=0.8,en;q=0.6");
      request.Headers.Set(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
      request.Method = "POST";
    }

    private void ConvertToFlac(Stream sourceStream, Stream destinationStream) {
      var audioSource = new WAVReader(null, sourceStream);
      try {
        if (audioSource.PCM.SampleRate != 16000) {
          throw new InvalidOperationException("Incorrect frequency - WAV file must be at 16 KHz.");
        }
        var buff = new AudioBuffer(audioSource, 0x10000);
        var flakeWriter = new FlakeWriter(null, destinationStream, audioSource.PCM);
        // flakeWriter.CompressionLevel = 8;
        while (audioSource.Read(buff, -1) != 0) {
          flakeWriter.Write(buff);
        }
        flakeWriter.Close();
      }
      finally {
        audioSource.Close();
      }
    }
  }
}
