using System;
using System.Windows.Forms;

namespace AudioSwitcher
{
    public class AppContext : ApplicationContext
    {
        private NotifyIcon notifyIcon;
        private Controller controller;
        public AppContext()
        {
            MenuItem configurationMenuItem = new MenuItem("Configuration", new EventHandler(Configuration));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Resources.sound;
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]
                {
                    configurationMenuItem,
                    exitMenuItem
                });
            notifyIcon.Visible = true;
            controller = new Controller();
            controller.SetupKeyboardHooks();
        }

        void Configuration(object sender, EventArgs e)
        {
            var configurationForm = new Configuration();
            configurationForm.Show();
        }

        void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            controller.Dispose();
            Application.Exit();
        }
    }
}