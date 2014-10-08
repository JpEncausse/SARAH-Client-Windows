using System;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using System.Xml.XPath;
using System.Collections.Generic;

namespace net.encausse.sarah.os {
  public class AddOn : AbstractAddOn {

    public AddOn() : base(){
      Name = "OS";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    public override void Setup() {
      base.Setup();
      OSManager.GetInstance();
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Run given path
      var run = parameters.Get("run");
      var param = parameters.Get("runp");
      if (run != null) {
        OSManager.GetInstance().RunApp(run, param);
      }
      
      // Activate application to foreground
      var activate = parameters.Get("activate");
      if (activate != null) {
        OSManager.GetInstance().ActivateApp(activate);
      }

      // Key modifier for other keyActions
      var keyMod = parameters.Get("keyMod");

      // Text to type
      var keyText = parameters.Get("keyText");
      if (keyText != null) {
        OSManager.GetInstance().SimulateTextEntry(keyText);
      }

      // Key press event
      var key = parameters.Get("keyUp");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key, 2, keyMod);
      }

      // Key press event
      key = parameters.Get("keyDown");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key, 1, keyMod);
      }

      // Key press event
      key = parameters.Get("keyPress");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key, 0, keyMod);
      }

      // Recognize file path
      var recognize = parameters.Get("recognize");
      if (recognize != null) {
        OSManager.GetInstance().Recognize(recognize);
      }
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.AfterSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      // Run given path
      var run = xnav.SelectSingleNode("/SML/action/@run");
      var param = xnav.SelectSingleNode("/SML/action/@runp");
      if (run != null) {
        OSManager.GetInstance().RunApp(run.Value, param.Value);
      }

      // Activate application to foreground
      var activate = xnav.SelectSingleNode("/SML/action/@activate");
      if (activate != null) {
        OSManager.GetInstance().ActivateApp(activate.Value);
      }

      // Key modifier for other keyActions
      var keyMod = xnav.SelectSingleNode("/SML/action/@keyMod");

      // Text to type
      var keyText = xnav.SelectSingleNode("/SML/action/@keyText");
      if (keyText != null) {
        OSManager.GetInstance().SimulateTextEntry(keyText.Value);
      }

      // Key press event
      var key = xnav.SelectSingleNode("/SML/action/@keyUp");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key.Value, 2, keyMod.Value);
      }

      // Key press event
      key = xnav.SelectSingleNode("/SML/action/@keyDown");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key.Value, 1, keyMod.Value);
      }

      // Key press event
      key = xnav.SelectSingleNode("/SML/action/@keyPress");
      if (key != null) {
        OSManager.GetInstance().SimulateKey(key.Value, 0, keyMod.Value);
      }

      // Recognize file path
      var recognize = xnav.SelectSingleNode("/SML/action/@recognize");
      if (recognize != null) {
        OSManager.GetInstance().Recognize(recognize.Value);
      }

    }

  }
}