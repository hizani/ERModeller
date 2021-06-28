using System;

namespace ERMODELLER
{
    public static class EntryPoint
    {
        /// <summary>
        /// Функция входа в программу
        /// </summary>
        /// <param name="args">Имя файла</param>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                var app = new App();
                app.InitializeComponent();
                MainWindow.projPath = args[0];
                app.Run();
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}