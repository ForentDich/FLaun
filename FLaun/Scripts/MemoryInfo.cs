using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FLaun.Scripts
{
    public class MemoryInfo
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        private static long GetMemoryInKilobytes()
        {
            if (GetPhysicallyInstalledSystemMemory(out long memKb))
                return memKb;
            else
                throw new InvalidOperationException("Не удалось получить информацию о физической памяти.");
        }

        public static int TotalRamInSystem
        {
            get
            {
                long memKb = GetMemoryInKilobytes();
                return (int)(memKb / 1024); // Возвращает в мегабайтах
            }
        }

        public static int RecommendedRamSize
        {
            get
            {
                long memKb = GetMemoryInKilobytes();
                return (int)(((memKb / 1024) * 3) / 4); // 75% от общей памяти
            }
        }
    }
}
