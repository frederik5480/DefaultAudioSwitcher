using AudioSwitcher.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace AudioSwitcher
{
    public partial class Configuration : Form
    {
        public Configuration()
        {
            InitializeComponent();
        }

        private void Configuration_Load(object sender, EventArgs e)
        {
            device1Name.Text = Settings.Device1Name;
            device1Information.Text = Settings.Device1Information;
            device2Name.Text = Settings.Device2Name;
            device2Information.Text = Settings.Device2Information;
            UpdateHotkeys();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            ControllerBase.Recording = false;
            Settings.Device1Name = device1Name.Text;
            Settings.Device1Information = device1Information.Text;
            Settings.Device2Name = device2Name.Text;
            Settings.Device2Information = device2Information.Text;
            Settings.Save();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            ControllerBase.Recording = true;
            Settings.Hotkeys.Clear();
            UpdateHotkeys();
        }

        public void UpdateHotkeys()
        {
            var sb = new StringBuilder();
            var keys = new List<VirtualCode>(Settings.Hotkeys.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append(keys[i]);
                if (i < keys.Count - 1)
                    sb.Append(" + ");
            }
            hotkeys.Text = sb.ToString();
        }
    }
}
