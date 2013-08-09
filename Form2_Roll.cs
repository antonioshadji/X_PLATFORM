using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using LOG;

namespace X_Platform2
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Form2_Roll : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public Form2_Roll()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal inc = 0;
        /// <summary>
        /// 
        /// </summary>
        public int places = 0;
        /// <summary>
        /// 
        /// </summary>
        public int x = 0;
        
        /// <summary>
        /// 
        /// </summary>
        public int y = 0;
        /// <summary>
        /// 
        /// </summary>
        public DataSet mdrUpdate = null;
        /// <summary>
        /// 
        /// </summary>
        public Form1 mainForm = null;

        LogFiles log = null;


        private void Form2_Roll_Load(object sender, EventArgs e)
        {
            try
            {
                this.DesktopLocation = new Point(x, y);
                this.numericUpDown1.Increment = inc;
                this.numericUpDown1.DecimalPlaces = places;
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }

            log = new LogFiles("ROLL");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked == false && radioButton2.Checked == false)
            {
                MessageBox.Show("You must choose one of the contract specifications!");
                return;
            }
            else
            {
                DataRow rollUpdate = mdrUpdate.Tables[0].Rows[0];
               
                for (int i = 0; i < rollUpdate.ItemArray.GetUpperBound(0) +1; i++)
			    {
                    if (rollUpdate.ItemArray[i] != null)
                    {
                        log.WriteLog(rollUpdate.ItemArray[i].ToString());
                    }
                    else
                        log.WriteLog("NULL VALUE");
			    }


                decimal stp = Convert.ToDecimal(rollUpdate[7]);
                //decimal bv = Convert.ToDecimal(rollUpdate[8]);

                log.WriteLog(stp.ToString());

                if (radioButton1.Checked == true)
                {
                    stp -= this.numericUpDown1.Value;
                    if (mainForm.longShort == "S")
                    { mainForm.buystop = stp; }
                    else
                    { mainForm.sellstop = stp; }
                    mainForm.nudOpen.Value -= this.numericUpDown1.Value; 
                }

                if (radioButton2.Checked == true)
                {
                    stp += this.numericUpDown1.Value;
                    if (mainForm.longShort == "S")
                    { mainForm.buystop = stp; }
                    else
                    { mainForm.sellstop = stp; }
                    mainForm.nudOpen.Value += this.numericUpDown1.Value; ;
                }

                //if (mainForm.longShort == "S")
                //{ bv += this.numericUpDown1.Value; }
                //else
                //{ bv -= this.numericUpDown1.Value; }

                log.WriteLog(stp.ToString());

                rollUpdate.BeginEdit();
                rollUpdate[7] = stp;
                //rollUpdate[8] = bv;
                rollUpdate.AcceptChanges();

                mdrUpdate.WriteXml("CONTRACT.XML");
                mainForm._saveCurrentDaySettingINI();
                
                //mainForm.bV = bv; 
            }

            //mainForm._LoadMDR();
           // mainForm._calcOrders();
           // mainForm._queueCurrentOrder();

            this.Hide();

//            if (!mainForm.isExchangeOpenUsed)
            MessageBox.Show("Manually Verify Order Price!");
        }

        private void Form2_Roll_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            { 
                MessageBox.Show("Spread data not entered.\r\nContract and order info do not match\r\nApplication will exit");
                Application.Exit();
            }
        }
    }
}
