using System;
using System.Drawing;
using System.Windows.Forms;

namespace X_Platform2
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ParameterList : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public ParameterList()
        {
            InitializeComponent();
        }

        private void ParameterList_Load(object sender, EventArgs e)
        {
            this.DesktopLocation = new Point(Form1.ActiveForm.DesktopLocation.X, Form1.ActiveForm.DesktopLocation.Y);
        }
    }
}
