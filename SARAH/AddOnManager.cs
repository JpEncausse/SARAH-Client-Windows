using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.XPath;

namespace net.encausse.sarah {
  public class AddOnManager : IAddOnHost, IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static AddOnManager manager = null;
    private AddOnManager() { }

    public static AddOnManager GetInstance() {
      if (manager == null) {
        manager = new AddOnManager();
      }
      return manager;
    }

    // -------------------------------------------
    //  IPLUGIN HOST (called by addon)
    // -------------------------------------------

    public void Log(IAddOn addon, string msg) {
      SARAH.GetInstance().Log("AddOnManager]["+addon.Name, msg);
    }

    // -------------------------------------------
    //  IPLUGIN (speech)
    // ------------------------------------------

    public void AddAudioSource(string device, Stream stream, string format, string language, double confidence) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleAudioSource(device, stream, format, language, confidence);
      }
    }

    public void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      foreach (IAddOn addon in AddOns) {
        addon.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);
      }
    }

    public void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      foreach (IAddOn addon in AddOns) {
        addon.AfterSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);
      }
    }

    public void BeforeSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) {
      foreach (IAddOn addon in AddOns) {
        addon.BeforeSpeechRejected(device, text, confidence, xnav, stream, options);
      }
    }

    public void AfterSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) {
      foreach (IAddOn addon in AddOns) {
        addon.AfterSpeechRejected(device, text, confidence, xnav, stream, options);
      }
    }

    // -------------------------------------------
    //  IPLUGIN (voice)
    // ------------------------------------------

    public void BeforeHandleVoice(String tts, bool sync) {
      foreach (IAddOn addon in AddOns) {
        addon.BeforeHandleVoice(tts, sync);
      }
    }

    public void AfterHandleVoice(String tts, bool sync, Stream stream) {
      foreach (IAddOn addon in AddOns) {
        addon.AfterHandleVoice(tts, sync, stream);
      }
    }
    // -------------------------------------------
    //  IPLUGIN (http)
    // -------------------------------------------

    public void SendGET(string device, string url, string token, IDictionary<string, string> parameters) {
      foreach (IAddOn addon in AddOns) {
        addon.SendGET(device, url, token, parameters);
      }
    }

    public void SendPOST(string device, string url, string token, string[] keys, string[] values) {
      foreach (IAddOn addon in AddOns) {
        addon.SendPOST(device, url, token, keys, values);
      }
    }

    public void SendFILE(string device, string url, string token, string path) {
      foreach (IAddOn addon in AddOns) {
        addon.SendFILE(device, url, token, path);
      }
    }

    public virtual void BeforeGET(string device, string url, string token, IDictionary<string, string> parameters) {
      foreach (IAddOn addon in AddOns) {
        addon.BeforeGET(device, url, token, parameters);
      }
    }

    public void HandleBODY(string device, string body, string token) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleBODY(device, body, token);
      }
    }

    public void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      foreach (IAddOn addon in AddOns) {
        addon.BeforeHTTPRequest(qs, parameters, files, writer);
      }
    }

    public void AfterHTTPRequest(string qs, NameValueCollection parameters, IDictionary files) {
      foreach (IAddOn addon in AddOns) {
        addon.AfterHTTPRequest(qs, parameters, files);
      }
    }

    // -------------------------------------------
    //  IPLUGIN (camera management)
    // -------------------------------------------

    public virtual void InitColorFrame(string device, byte[] data, Timestamp state, int width, int height, int fps) {
      foreach (IAddOn addon in AddOns) {
        addon.InitColorFrame(device, data, state, width, height, fps);
      }
    }

    public virtual void InitBodyFrame(string device, ICollection<NBody> data, Timestamp state, int width, int height) {
      foreach (IAddOn addon in AddOns) {
        addon.InitBodyFrame(device, data, state, width, height);
      }
    }

    public virtual void MotionDetected(string device, bool status) {
      foreach (IAddOn addon in AddOns) {
        addon.MotionDetected(device, status);
      }
    }

    // -------------------------------------------
    //  IPLUGIN (profile management)
    // -------------------------------------------

    public virtual void HandleProfile(string device, string key, object value) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleProfile(device, key, value);
      }
    }

    public virtual bool IsEngaged(string device) {
      foreach (IAddOn addon in AddOns) {
        if (addon.IsEngaged(device)) {
          return true;
        }
      }
      return false;
    }

    // -------------------------------------------
    //  IPLUGIN (logs management)
    // -------------------------------------------

    public void Log(string msg) {
      foreach (IAddOn addon in AddOns) {
        addon.Log(msg);
      }
    }

    public void Log(string ctxt, string msg) {
      foreach (IAddOn addon in AddOns) {
        addon.Log(ctxt, msg);
      }
    }

    public void Debug(string ctxt, string msg) {
      foreach (IAddOn addon in AddOns) {
        addon.Debug(ctxt, msg);
      }
    }

    public void Error(string ctxt, string msg) {
      foreach (IAddOn addon in AddOns) {
        addon.Error(ctxt, msg);
      }
    }

    public void Error(string ctxt, Exception ex) {
      foreach (IAddOn addon in AddOns) {
        addon.Error(ctxt, ex);
      }
    }

    // -------------------------------------------
    //  IPLUGIN (life cycle)
    // -------------------------------------------

    public void Start() {
      foreach (IAddOn addon in AddOns) {
        addon.Start();
      }
    }

    public void Setup() {
      foreach (IAddOn addon in AddOns) {
        addon.Setup();
      }
    }

    public void Dispose() {
      foreach (IAddOn addon in AddOns) {
        addon.Dispose();
      }
    }

    public bool Ready() {
      foreach (IAddOn addon in AddOns) {
        if (!addon.Ready()) return false;
      }
      return true;
    }

    // -------------------------------------------
    //  IPLUGIN (gui)
    // -------------------------------------------

    public void HandleMenuItem(ContextMenuStrip menu) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleMenuItem(menu);
      }
    }

    public void HandleSidebar(string device, StackPanel sidebar) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleSidebar(device, sidebar);
      }
    }

    public void HandleSelection(string device, Rectangle rect) {
      foreach (IAddOn addon in AddOns) {
        addon.HandleSelection(device, rect);
      }
    }

    public void RepaintColorFrame(string device, byte[] bgra, int width, int height) {
      foreach (IAddOn addon in AddOns) {
        addon.RepaintColorFrame(device, bgra, width, height);
      }
    }

    // -------------------------------------------
    //  LOADER
    // -------------------------------------------

    public ICollection<IAddOn> AddOns = new List<IAddOn>(); 
    public void LoadAddOns(string path) {
      Load(path, true);
    }

    public void Load(string path) {
      Load(path, false);
    }

    private void Load(string path, bool IsAddOn){

      if (File.Exists(path)) { return; }
      SARAH.GetInstance().Log("AddOnManager", "Searching: " + path);

      // Recursive with folders
      string[] folders = Directory.GetDirectories(path);
      foreach (string folder in folders) {
        Load(folder, IsAddOn);
      }

      // Retrieve all .dll files
		  string[] dll = Directory.GetFiles(path, "*.dll");

      // Load Assemblies from files
      ICollection<Assembly> assemblies = new List<Assembly>(dll.Length);
      foreach (string file in dll) {
        var name = Path.GetFileNameWithoutExtension(file);
        if ( IsAddOn && !ConfigManager.GetInstance().Find(name+".enable",false)){ continue; }
        if (!IsAddOn && !name.StartsWith("Emgu")) { continue; } // Hack

        try {
          AssemblyName an = AssemblyName.GetAssemblyName(file);
          Assembly assembly = Assembly.Load(an);
          if (IsAddOn) { assemblies.Add(assembly); }
        } catch(Exception ex) {
          SARAH.GetInstance().Error("AddOnManager", ex.Message);
        }
			}

			Type addonType = typeof(IAddOn);
			foreach(Assembly assembly in assemblies){

        if(assembly == null){ continue; }
        ICollection<Type> addonTypes = new List<Type>();

        // Retrieve AddOn's type
        Type[] types = assembly.GetTypes();
        foreach (Type type in types) {
          if (type.IsInterface || type.IsAbstract) { continue; }
          if (type.GetInterface(addonType.FullName) == null) { continue; }
          addonTypes.Add(type);
        }

        // Instanciate AddOns
				foreach(Type type in addonTypes){
					var addon = (IAddOn) Activator.CreateInstance(type);
          addon.Host = this;
					AddOns.Add(addon);
          SARAH.GetInstance().Log("AddOnManager", "Loading addon \"" + addon.Name + "\" v"+addon.Version);
				}

			}
    }
  }
}