using System;
using System.Windows.Forms;
using EcoRouteAI.Views;

namespace EcoRouteAI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Nécessaire pour WebView2 (BNF08)
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}