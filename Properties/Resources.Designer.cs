﻿namespace StreamerbotPlugin.Properties {
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
  
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("StreamerbotPlugin.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
      
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
     
        internal static System.Drawing.Bitmap streamerbot_logo_Connected {
            get {
                object obj = ResourceManager.GetObject("streamerbot-logo-Connected", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
     
        internal static System.Drawing.Bitmap streamerbot_logo_Disconnected {
            get {
                object obj = ResourceManager.GetObject("streamerbot-logo-Disconnected", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
    
        internal static System.Drawing.Bitmap streamerbot_logo_text {
            get {
                object obj = ResourceManager.GetObject("streamerbot-logo-text", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
       
        internal static System.Drawing.Bitmap streamerbot_logo_transparent {
            get {
                object obj = ResourceManager.GetObject("streamerbot-logo-transparent", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
    }
}
