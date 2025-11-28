using System.Net;

namespace FLaun.Scripts
{
    internal class InternetChecker
    {
        public static bool OK()
        {
            try
            {
                Dns.GetHostEntry("genesis-cs.space");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
