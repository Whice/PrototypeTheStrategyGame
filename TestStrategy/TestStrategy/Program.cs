using System;
using System.Collections.Generic;
//using System.Linq;
using System.Windows.Forms;

namespace TestStrategy
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (WindowOfTheGame windowOfTheGame = new WindowOfTheGame())
            {
                // Show our form and initialize our graphics engine
                windowOfTheGame.Show();
                windowOfTheGame.InitializeGraphics();
                Application.Run(windowOfTheGame);
            }
        }
    }
}
