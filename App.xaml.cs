using System.Windows;
using MiniIDEv04.Data;

namespace MiniIDEv04
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ProjectDatabase.Initialize();
        }
    }
}
