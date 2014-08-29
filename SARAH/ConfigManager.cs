using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Globalization;

using IniParser;
using IniParser.Model;

namespace net.encausse.sarah {
  public class ConfigManager {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static ConfigManager manager = null;
    private ConfigManager(){}

    public static ConfigManager GetInstance() {
      if (manager == null) {
        manager = new ConfigManager();
        manager.Init();
      }
      return manager;
    }

    public String PATH_PLUGIN = "";
    private void Init() {
      PATH_PLUGIN = Environment.GetEnvironmentVariable("PATH_PLUGINS");
    }

    // -------------------------------------------
    //  LOADING
    // -------------------------------------------

    protected IniData Config;
    public void Load(string path) { Load(path, false); }
    public void Load(string path, bool clean) {
      SARAH.GetInstance().Log("ConfigManager", "Loading: " + path);
      var parser = new FileIniDataParser();
      var conf   = parser.ReadFile(path);
      //SARAH.GetInstance().Log("ConfigManager", conf.ToString());
      if (clean) {
        foreach (var section in conf.Sections) {
          section.LeadingComments.Clear();
          section.TrailingComments.Clear();
          foreach (var keydata in section.Keys) {
            keydata.Comments.Clear();
          }
        }
      }
      if (Config == null) { Config = conf;  } else { Config.Merge(conf); }
    }
    
    public void Save(string path) {
      if (Config == null) { return; }
      var parser = new FileIniDataParser();
      parser.WriteFile(path, Config);
    }

    public void LoadAddOns(string path) {
      if (File.Exists(path + "/addon.ini")) {
        Load(path + "/addon.ini");
      }
      foreach (var directory in Directory.GetDirectories(path)){
        LoadAddOns(directory);
      }
    }

    // -------------------------------------------
    //  GETTER
    // -------------------------------------------

    CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

    public int Find(string query, int def) {
      string find = Find(query, null);
      try { return find != null ? int.Parse(find, culture) : def; } catch (Exception) { }
      return def;
    }

    public double Find(string query, double def) {
      string find = Find(query, null);
      try { return find != null ? double.Parse(find, culture) : def; } catch (Exception) { }
      return def;
    }

    public bool Find(string query, bool def) {
      string find = Find(query, null);
      try { return find != null ? bool.Parse(find) : def; } catch (Exception) { }
      return def;
    }

    public String Find(string query, string def) {
      var index = query.LastIndexOf(".");
      var section = query.Substring(0, index);
      var key = query.Substring(index+1);
      return Find(section, key, def);
    }

    protected String Find(string section, string key, string def) {
      try { return Config[section][key]; } catch (Exception) {
        if (!"enable".Equals(key)) {
          SARAH.GetInstance().Log("Missing config : " + section + "." + key);
        }
        return def; 
      }
    }
  }
}
