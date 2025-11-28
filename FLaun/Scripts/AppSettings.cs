using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLaun.Scripts
{
    public class AppSettings
    {
        public bool IsFirstRun { get; set; } = true;
        public int RamSize { get; set; } = 0;
        public int RamInSystem { get; set; } = 0;
        public string PathForGame { get; set; } = string.Empty;
        public string DefaultPathForGame { get; set; } = string.Empty;
        public string OfflineNickname { get; set; } = string.Empty;
        public string AssemblyMods { get; set;} = string.Empty;
        public string CurrentVersionOfAssembly { get; set; } = string.Empty;
    }
}
