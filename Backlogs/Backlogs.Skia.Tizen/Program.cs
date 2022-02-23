using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace Backlogs.Skia.Tizen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new TizenHost(() => new Backlogs.App(), args);
            host.Run();
        }
    }
}
