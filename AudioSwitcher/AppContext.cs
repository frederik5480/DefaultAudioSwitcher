using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioSwitcher
{
    public class AppContext : ApplicationContext
    {
        private NotifyIcon notifyIcon;
        private Controller controller;
        public AppContext()
        {
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Resource1.sound;
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]
                { 
                    exitMenuItem
                });
            notifyIcon.Visible = true;
            controller = new Controller();
            controller.SetupKeyboardHooks();
        }
        void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            controller.Dispose();
            Application.Exit();
        }
    }
}