using System;
using SuchByte.MacroDeck.Plugins;

namespace StreamerbotPlugin
{
    public class Configuration
    {
        private int? _port;
        private string _address;
        private string _endpoint;
        private Uri _uri;
        private static readonly object _lock = new object();
        private static Configuration _instance;
        public static Configuration Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new Configuration();
                }
            }
        }
        public int Port
        {
            get { return _port ?? int.Parse(PluginConfiguration.GetValue(PluginInstance.Main, "Port") ?? "8080"); }
            set { _port = value; PluginConfiguration.SetValue(PluginInstance.Main, "Port", value.ToString()); }
        }
        public string Address
        {
            get { return _address ?? PluginConfiguration.GetValue(PluginInstance.Main, "Address") ?? "127.0.0.1"; }
            set { _address = value; PluginConfiguration.SetValue(PluginInstance.Main, "Address", value); }
        }
        public string Endpoint
        {
            get { return _endpoint ?? PluginConfiguration.GetValue(PluginInstance.Main, "Endpoint") ?? "/"; }
            set { _endpoint = value; PluginConfiguration.SetValue(PluginInstance.Main, "Endpoint", value); }
        }
        public Uri uri
        {
            get { return _uri ?? new UriBuilder("ws", Address, Port, Endpoint).Uri; }
            set { _uri = value; }
        }
       

    }
}