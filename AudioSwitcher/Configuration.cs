using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            device1Name.Text = ControllerBase.Device1Name;
            device1Information.Text = ControllerBase.Device1Information; 
            device2Name.Text = ControllerBase.Device2Name;
            device2Information.Text = ControllerBase.Device2Information;
        }

        private void Save_Click(object sender, EventArgs e)
        {

        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
