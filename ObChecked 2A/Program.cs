using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSM = Tekla.Structures.Model;

namespace ObChecked
{

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Try to connect to Tekla model
            var model = new TSM.Model();

            if (!model.GetConnectionStatus())
            {
                MessageBox.Show("Tekla Structures model is not open or connection failed.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit without starting the app
            }

            Application.Run(new FormMain(model));
        }

    }
}
