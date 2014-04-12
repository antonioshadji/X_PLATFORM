using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using XTAPI;
using LOG;

namespace X_Platform2
{
    /// <summary>
    /// Main Application form
    /// </summary>
    public partial class Form1 : Form
    {
        #region Variables
        /// <summary>
        /// Create two log file classes, log for application actions and prc for Time and sales
        /// </summary>
        private LogFiles log = null;
        private LogFiles prc = null;

        /// <summary>
        /// Instantiate XTAPI Objects
        /// </summary>
        private XTAPI.TTGateClass ttGate = null;
        private XTAPI.TTDropHandler ttDropHandler = null;

        private XTAPI.TTInstrNotifyClass ttInstrNotify = null;
        private XTAPI.TTInstrNotifyClass ttInstrNotifyLast = null;
        private XTAPI.TTInstrNotifyClass ttInstrNotifyOpen = null;

        private XTAPI.TTInstrObj ttInstrObj = null;
        private XTAPI.TTOrderSetClass ttOrderSet = null;

        private bool isOn = false;                  //Application on or off - true/false
        private bool isContractFound = false;       //Is the contract loaded?
        private bool isOrderServerUP = false;       //Is the TT Order Server connected?
        private bool isPriceServerUP = false;       //Is the TT Price Server connected?
        private bool isFillServerUP = false;        //Is the TT Fill Server connected?
        private bool isOpenDelivered = false;       //Has exchange open price been delivered?
        private bool isOpenComparedToLast = false;  //Has open been checked against first last print?
        private bool isOpenCaptured = false;        //Should the application capture open from last print at start time?
        internal bool isExchangeOpenUsed = true;    //By default, use official exchange open price.
        private bool isOrderCalculated = false;     //Have orders been calculated?
        private bool isOrderQueued = false;         //Has the next order been queued up?
        private bool isAllocationDataVerified = false;  //If alloc file is verified allocations are created.
        private bool isTradingLimitDisabled = false;    //debug purposes disable trading limits
        private bool isAutoSendAllocationsOn = false;
        private bool isRollContractDropped = false;
        private bool isLimitOrderSent = false;      //syntheticSL is order sent
        private bool isOrderHung = false;
        private bool isCustomAllocLoaded = false;
       
        /// <summary>
        /// If the application is started with command line parameter /auto bAlertOn is set to false
        /// bAlertOn: by default (true) All alerting functions are on.
        /// ALGO: nativeSL
        /// true: orders submit on hold
        /// false: orders submit active at the exchange
        /// ALGO: syntheticSL
        /// true: Alert sounds when order submitted
        /// false: no alert on order submission
        /// 
        /// false: no compare last/open alert
        /// </summary>
        private bool isAlertOn = true;

        //only needed if depth is enabled
        //private Array aBidDepth;
        //private Array aAskDepth;

        private string siteOrderKey = null;     //Variable to store TT Order key
        private string msgAlert = "ALERT: EXECUTION ALERT";

        private decimal oneTick = 0;    //value of 1 tick in decimals

        //Trading parameters
        private int ordNum = 1;

        private decimal tradePrice1 = 0;
        private decimal tradePrice2 = 0;
        private decimal last = 0;
        private int lstQty = 0;
        internal decimal open = 0;
        private decimal priorOpen = 0; //track changes in open price

        private int tradeQty = 0;
        private int fillQty = 0;
        private int fillCount = 0;
        private decimal stopPrice = 0;
        private string buySell = null;
        private decimal limitPrice = 0;

        private string ttGateway = null;
        private string ttProduct = null;
        private string ttProductType = "FUTURE";
        private string ttContract = null;
        private string ttCustomer = "XTAPI";
        private string contractDescription = null;

        //these variables are set from MDR data
        internal string longShort = null; //S or L
        private int startPosition = 0;
        private int mdrQty1 = 0;
        private int mdrQty2 = 0;

        internal decimal buystop = 0;
        internal decimal bV = 0;
        internal decimal sellstop = 0;
        private decimal dV = 0;

        private TimeSpan startTime;
        private TimeSpan stopTime;
        
        // ^ above is current MDR data
        private decimal StdAllocMultiplier = 1M;
        private decimal CustomAllocMultiplier = 1M;

        /// <summary>
        /// table for contract data
        /// </summary>
        public DataSet setMDRData = new DataSet("MDR_Data");
        private DataTable tblContract = null;

        /// <summary>
        /// allocation data setup
        /// </summary>
        private DataSet setAllocData = new DataSet("Allocation");
        private DataTable tblAccounts = null;
        private DataTable tblSetup = null;

        //email variables setup
        private DataSet setINI = new DataSet("INI");
        private DataTable tblAddress = null;
        private DataTable tblParameters = null;
        private DataTable tblStartup = null;
        private string msgBody = string.Empty; 

        //custom templar allocation
        private DataSet setCustom = new DataSet("CustomAllocation");
        private DataTable tblAddress1 = null;
        private DataTable tblTemplar = null;

        private SortedDictionary<decimal, int> fillList = new SortedDictionary<decimal, int>();
        private SortedDictionary<int, int> qty1Alloc = new SortedDictionary<int, int>();
        private SortedDictionary<int, int> qty2Alloc = new SortedDictionary<int, int>();
        private SortedDictionary<int, string> accountNumbers = new SortedDictionary<int, string>();

        private Color lstColor = Color.LightGray;

        private string algo = "syntheticSL";
        
        private enum Algorithm 
        { 
            syntheticSL, 
            nativeSL, 
            stopMarket 
        }

        private Algorithm currentAlgo = Algorithm.syntheticSL;
        
        #endregion
        
        /// <summary>
        /// Main Form
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load TT Objects , register events, run startup procedures
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            log = new LogFiles(this.listBox1, "X_PLATFORM");
            prc = new LogFiles(this.listBox1, "Prices");
  
            log.CleanLog();
            log.WriteLog("Form1_Load");
            log.WriteLog(Application.ExecutablePath);
            log.WriteLog(Application.ProductVersion);

            ttGate = new XTAPI.TTGateClass();
            ttInstrObj = new XTAPI.TTInstrObj();
            ttDropHandler = new XTAPI.TTDropHandlerClass();

            ttInstrNotify = new XTAPI.TTInstrNotifyClass();
            ttInstrNotifyLast = new TTInstrNotifyClass();
            ttInstrNotifyOpen = new TTInstrNotifyClass();

            ttOrderSet = new XTAPI.TTOrderSetClass();
           

            ttInstrObj.MergeImpliedsIntoDirect = 1;

            // Subscribe to the OnExchangeStateUpdate.
            ttGate.OnExchangeStateUpdate += 
                new XTAPI._ITTGateEvents_OnExchangeStateUpdateEventHandler(TTGate_OnExchangeStateUpdate);
            ttGate.OnStatusUpdate += 
                new XTAPI._ITTGateEvents_OnStatusUpdateEventHandler(TTGate_OnStatusUpdate);
            // Setup the instrument notification call back functions
            ttInstrNotify.OnNotifyFound += 
                new XTAPI._ITTInstrNotifyEvents_OnNotifyFoundEventHandler(this.TTInstrNotify_OnNotifyFound);
            ttInstrNotify.OnNotifyNotFound += 
                new _ITTInstrNotifyEvents_OnNotifyNotFoundEventHandler(TTInstrNotify_OnNotifyNotFound);
            //m_TTInstrNotify.OnNotifyUpdate += 
                //new XTAPI._ITTInstrNotifyEvents_OnNotifyUpdateEventHandler(m_TTInstrNotify_OnNotifyUpdate);
            //m_TTInstrNotify.OnNotifyDepthData +=
            //    new XTAPI._ITTInstrNotifyEvents_OnNotifyDepthDataEventHandler(m_TTInstrNotify_OnNotifyDepthData);
            // Subscribe to the fill events.
            ttDropHandler.OnNotifyDrop +=
                    new XTAPI._ITTDropHandlerEvents_OnNotifyDropEventHandler(this.TTDropHandler_OnNotifyDrop);

            ttOrderSet.OnOrderFillData += 
                new XTAPI._ITTOrderSetEvents_OnOrderFillDataEventHandler(TTOrderSet_OnOrderFillData);

            ttOrderSet.OnOrderRejected += 
                new XTAPI._ITTOrderSetEvents_OnOrderRejectedEventHandler(TTOrderSet_OnOrderRejected);
            ttOrderSet.OnOrderSetUpdate += 
                new XTAPI._ITTOrderSetEvents_OnOrderSetUpdateEventHandler(TTOrderSet_OnOrderSetUpdate);

            // Enable the TTOrderTracker.
            //ttOrderSet.EnableOrderUpdateData = 1;

            // Register the active form for drag and drop.
            ttDropHandler.RegisterDropWindow((int)this.Handle);

            // Enable the depth updates.
            //ttInstrNotify.EnableDepthUpdates = 1;

            ttInstrNotifyLast.EnablePriceUpdates = 1;
            ttInstrNotifyLast.UpdateFilter = "Last,LastQty";
            ttInstrNotifyLast.OnNotifyUpdate += 
                new _ITTInstrNotifyEvents_OnNotifyUpdateEventHandler(TTInstrNotifyLast_OnNotifyUpdate);

            ttInstrNotifyOpen.EnablePriceUpdates  = 1;
            ttInstrNotifyOpen.UpdateFilter = "OPEN";
            ttInstrNotifyOpen.OnNotifyUpdate += 
                new _ITTInstrNotifyEvents_OnNotifyUpdateEventHandler(TTInstrNotifyOpen_OnNotifyUpdate);
         
            _LoadMDR();
            _LoadAlloc();
            _LoadINI();
            _LoadCustomAlloc();

            _processCustomParameters();

            _displayParameters(false); //record parameters in logfile

            _connectExchange();
            //_ChkCustomers();

            button5.Enabled = true;
            lblOrder.Text = string.Empty;
            _verifyDirectoryStructure();
        }

        /// <summary>
        /// closing cleanup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            log.WriteLog("Form1_FormClosing");
            log.WriteLog(e.CloseReason.ToString());

            log.CloseLog();
            prc.CloseLog();
        }
        
        /// <summary>
        /// verify that the expected directory structure is present
        /// </summary>
        private void _verifyDirectoryStructure()
        {
                _VerifyDirectory(@"\..\_alerts");
                _VerifyDirectory(@"\..\_allocations");
                _VerifyDirectory(@"\..\_allocations\archive");
                _VerifyDirectory(@"\..\_allocCustom");
                _VerifyDirectory(@"\..\_allocCustom\archive");

                _ArchiveFiles(@"\..\_allocations", "MMddyy");
                _ArchiveFiles(@"\..\_allocCustom", "yyyyMMdd");
        }

        /// <summary>
        /// move previous day files to archive folder
        /// </summary>
        /// <param name="dir"></param> directory to scan
        /// <param name="DateFormat"></param> date format found in file to determine what trade date the file belongs to
        private void _ArchiveFiles(string dir, string DateFormat)
        {
            try
            {
                string[] files = Directory.GetFiles(Application.StartupPath.ToString() + dir);
                foreach (string fn in files)
                {
                    log.WriteLog(_tradeDate(DateFormat));
                    //move files to processed folder if trade date does not match
                    if (!fn.Contains(_tradeDate(DateFormat)))
                    {
                        log.WriteLog(fn);
                        int i = fn.LastIndexOf(@"\");
                        string tmp = fn.Substring(i + 1);
                        log.LogListBox(tmp);

                        try
                        {
                            if (File.Exists(fn.Insert(i + 1, @"archive\")))
                            { File.Delete(fn); }
                            else
                            { File.Move(fn, fn.Insert(i + 1, @"archive\")); }
                        }
                        catch (Exception ex)
                        { log.LogListBox(ex.ToString()); }
                    }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// verify a single directory exists, if not create it
        /// </summary>
        /// <param name="d1"></param>
        private void _VerifyDirectory(string d1)
        {
            try
            {
                bool blnDirectoryExists = Directory.Exists(Application.StartupPath.ToString() + d1);
                if (!blnDirectoryExists)
                {
                    Directory.CreateDirectory(Application.StartupPath.ToString() + d1);
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Custom Parameters
        /// /auto = auto mode
        /// ALGO: nativeSL - orders submit active at the exchange
        /// ALGO: stopMarket - sound alert when order activated
        /// 
        /// /NOLIMIT - DEBUG PURPOSES removes trading limits  
        /// 
        /// /x:## /y:##  if used both required
        /// specify desktop location of application form in pixels
        /// </summary>
        private void _processCustomParameters()
        {
            string[] args = Environment.GetCommandLineArgs();
            log.LogListBox("Command Line Args: " + args.Length.ToString());
            int x = -10000;
            int y = -10000;

            foreach (string arg in args)
            {
                log.LogListBox(arg.ToUpperInvariant());
                if (arg.ToUpperInvariant().Equals("/AUTO") || arg.ToUpperInvariant().Equals("AUTO"))
                {
                    isAlertOn = false;
                    log.LogListBox("AUTO MODE ENABLED");
                    //nativeSL submits orders live to the exchange instead of placing on hold
                    //no change for other algorithms
                }

                if (arg.ToUpperInvariant().Equals("/NOLIMIT"))
                { 
                    isTradingLimitDisabled = true;
                    log.LogListBox("TRADING LIMITS DISABLED");
                   
                }

                if (arg.ToUpperInvariant().Contains("/X:"))
                { x = Convert.ToInt32(arg.Substring(3)); }

                if (arg.ToUpperInvariant().Contains("/Y:"))
                { y = Convert.ToInt32(arg.Substring(3)); }
            }

            if (x != -10000 && y != -10000)
            {
                log.LogListBox(string.Format("Custom Desktop Location {0}x{1}", x, y));
                this.DesktopLocation = new Point(x, y); 
            }
            else
            { _setDesktopLocation(); }
        }

        /// <summary>
        /// hardcoded location for existing products
        /// </summary>
        private void _setDesktopLocation()
        { 
            if (SystemInformation.WorkingArea.Width == 1920 && SystemInformation.WorkingArea.Height >= 1170)
            {
                switch (ttProduct)
                {
                    case "6E":
                        this.DesktopLocation = new Point(0, 0);
                        break;
                    case "6J":
                        this.DesktopLocation = new Point(0, this.Height);
                        break;
                    case "Sugar No 11":
                        this.DesktopLocation = new Point(this.Width, 0);
                        break;
                    case "6C":
                        this.DesktopLocation = new Point(this.Width, this.Height);
                        break;
                    case "ZN":
                        this.DesktopLocation = new Point(this.Width * 2, 0);
                        break;
                    case "GC":
                        this.DesktopLocation = new Point(this.Width * 2, this.Height);
                        break;
                    case "SI":
                        this.DesktopLocation = new Point(this.Width * 3, 0);
                        break;
                    case "NQ":
                        this.DesktopLocation = new Point(this.Width * 3, this.Height);
                        break;
                    case "CL":
                        this.DesktopLocation = new Point(this.Width * 4, 0);
                        break;
                    case "NG":
                        this.DesktopLocation = new Point(this.Width * 4, this.Height);
                        break;
                    case "ZC":
                        this.DesktopLocation = new Point(this.Width * 5, 0);
                        break;
                    case "ZS":
                        this.DesktopLocation = new Point(this.Width * 5, this.Height);
                        break;
                    case "FGBL":
                        this.DesktopLocation = new Point(0, this.Height * 2);
                        break;
                    case "FESX":
                        this.DesktopLocation = new Point(this.Width, this.Height * 2);
                        break;
                    case "FDAX":
                        this.DesktopLocation = new Point(this.Width * 2, this.Height * 2);
                        break;
                    case "NK":
                        this.DesktopLocation = new Point(this.Width, 0);
                        break;
                    case "TSE JGB":
                        this.DesktopLocation = new Point(this.Width * 2, 0);
                        break;
                    case "HKFE HSI":
                        this.DesktopLocation = new Point(this.Width * 3, 0);
                        break;
                    default:
                        log.LogListBox("Desktop Location NOT customized for : "+ttProduct );
                        break;
                }
            }  
        }

        /// <summary>
        /// custom form setup for different algorithms
        /// </summary>
        private void _ALGOsetup()
        {
            log.LogListBox("ALGO: " + algo);

            switch (algo)
            {
                case "nativeSL":
                    currentAlgo = Algorithm.nativeSL;
                    break;
                case "syntheticSL":
                    currentAlgo = Algorithm.syntheticSL;
                    this.listBox1.BackColor = Color.Yellow;
                    break;
                case "stopMarket":
                    currentAlgo = Algorithm.stopMarket;
                    label1.Text = "Trigger";
                    break;
                default:
                    log.LogListBox(algo + " NOT SET UP CORRECTLY!!");
                    break;
            }

            groupBox3.Text = string.Format("Active Order: {0} - ALGO: {1}", ordNum, currentAlgo.ToString());
        }

        /// <summary>
        /// handles rejected orders differently for different algorithms
        /// </summary>
        /// <param name="sok"></param>
        /// <param name="msg"></param>
        private void _ALGO_OnOrderRejected(string sok, string msg)
        {
            if (sok == siteOrderKey)
            {
                switch (algo)
                {
                    case "syntheticSL":
                        if (sok == siteOrderKey)
                        { isLimitOrderSent = false; }
                        break;
                    case "stopMarket":
                    case "nativeSL":
                    default:
                        break;
                }

                siteOrderKey = null;
            }

            msgAlert = "ALERT: ORDER REJECTED " + msg;
            log.LogListBox(msgAlert);
            AlertTimer.Interval = 1000;
            AlertTimer.Start();
        }

        /// <summary>
        /// load contract data from CONTRACT.XML File.
        /// Application cannot run without a CONTRACT.XML File
        /// </summary>
        internal void _LoadMDR()
        {
            log.WriteLog("_LoadDT");
            try
            {
                if (File.Exists("CONTRACT.XML"))
                {
                    log.LogListBox("CONTRACT.XML file Exists");
                    
                    setMDRData.Clear();
                    setMDRData.ReadXml("CONTRACT.XML");

                    tblContract = setMDRData.Tables[0];
                    DataRow dr = tblContract.Rows[0];

                    try
                    {
                        ttGateway = dr["Gateway"].ToString();
                        ttProduct = dr["Product"].ToString();
                        ttProductType = dr["ProdType"].ToString();
                        ttContract = dr["Contract"].ToString();
                        startPosition = Convert.ToInt32(dr["CurPos"]);
                        longShort = dr["PosDir"].ToString();
                        mdrQty1 = Convert.ToInt32(dr["Q1"]);

                        if (longShort == "S")
                        { buystop = Convert.ToDecimal(dr["StopPrc"]); }
                        else
                        { sellstop = Convert.ToDecimal(dr["StopPrc"]); }

                        bV = Convert.ToDecimal(dr["bV"]);
                        dV = Convert.ToDecimal(dr["dV"]);
                        mdrQty2 = Convert.ToInt32(dr["Q2"]);

                        startTime = TimeSpan.Parse(dr["StartTime"].ToString());
                        stopTime = TimeSpan.Parse(dr["StopTime"].ToString());
                        
                        //only set bOpenCapture to true if the application is loaded before trading time
                        //bOpenCapture default value is false
                        if (!_isValidTradingTime()) { isOpenCaptured = Convert.ToBoolean(dr["bCaptureOpen"]); }

                        if (isOpenCaptured) { isExchangeOpenUsed = false; }
                        
                        algo = dr["ALGO"].ToString();
                        _ALGOsetup();

                        nudOffSet.Value = Math.Max(Convert.ToDecimal(dr["offset"]),nudOffSet.Minimum);

                        this.Text = string.Format("{0} not yet loaded", ttContract);
                        fillQty = mdrQty1;
   
                        contractDescription = dr["Description"].ToString();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("MDR Data not loaded!\r\nError in CONTRACT.XML data file\r\nMESSAGE: " + ex.Message,ttContract );
                        log.WriteLog("MDR Data not loaded!\r\nError in CONTRACT.XML data file");
                        log.WriteLog(ex.ToString());
                    }
                }
                else
                { log.LogListBox("CONTRACT.XML file does not exist"); }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Load ALLOC.XML containing allocation data fro trades 1 and 2 quantities
        /// application can run without ALLOC.XML file but no allocations will be sent
        /// </summary>
        private void _LoadAlloc()
        {
            log.WriteLog("_LoadAlloc");
            try
            {
                int qty1Tot = 0;
                int qty2Tot = 0;

                if (File.Exists("ALLOC.XML"))
                {
                    log.LogListBox("ALLOC.XML file Exists");

                    setAllocData.ReadXml("ALLOC.XML");

                    if (setAllocData.Tables.Contains("accounts"))
                    {
                        tblAccounts = setAllocData.Tables[0];

                        foreach (DataRow row in tblAccounts.Rows)
                        {
                            qty1Alloc[Convert.ToInt32(row[2])] = Convert.ToInt32(row[0]);
                            qty1Tot += Convert.ToInt32(row[0]);
                            qty2Alloc[Convert.ToInt32(row[2])] = Convert.ToInt32(row[1]);
                            qty2Tot += Convert.ToInt32(row[1]);
                            accountNumbers[Convert.ToInt32(row[2])] = row[3].ToString();
                        }
                    }
                    else
                    { log.LogListBox("No allocation data found"); }

                    if (setAllocData.Tables.Contains("setup"))
                    {
                        tblSetup = setAllocData.Tables[1];

                        foreach (DataColumn col in tblSetup.Columns)
                        {
                            log.WriteLog("setup Table Column: " + col.ColumnName);

                            if (string.Equals(col.ColumnName, "PriceMultiplier", StringComparison.OrdinalIgnoreCase))
                            { 
                                StdAllocMultiplier = Convert.ToDecimal(tblSetup.Rows[0].ItemArray[col.Ordinal]);

                                if (StdAllocMultiplier != 0)
                                { log.LogListBox("Allocation Price Multiplier: " + StdAllocMultiplier.ToString()); }
                                else
                                {
                                    StdAllocMultiplier = 1;
                                    log.LogListBox("Allocation Price Multiplier ERROR, using default multiplier of 1.00 ");
                                }
                            }

                            //start here
                            if (string.Equals(col.ColumnName, "Open", StringComparison.OrdinalIgnoreCase))
                            {
                                decimal savedOpen = 0;
                                savedOpen = Convert.ToDecimal(tblSetup.Rows[0].ItemArray[col.Ordinal]);

                                if (savedOpen != 0)
                                {
                                    open = savedOpen;
                                    nudOpen.Value = savedOpen;
                                    chkBxOpen.Checked = true;
                                    isOpenCaptured = false;
                                    isExchangeOpenUsed = false;
                                    isOpenDelivered = true;
                                    log.LogListBox("Saved Open Price: " + savedOpen.ToString());
                                }
                            }

                            if (string.Equals(col.ColumnName, "OrdNum", StringComparison.OrdinalIgnoreCase))
                            {
                                int lastOrdNum = 0;
                                lastOrdNum = Convert.ToInt32(tblSetup.Rows[0].ItemArray[col.Ordinal]);

                                if (lastOrdNum != 0)
                                {
                                    ordNum = lastOrdNum;
                                    log.LogListBox("Saved Order Number: " + lastOrdNum.ToString());
                                    if (lastOrdNum > 1) { fillQty = mdrQty2; }
                                }
                            }
                            //stop here
                        }
                    }
               
                    log.LogListBox(string.Format("Alloc Q1: {0} account #'s - Total Qty: {1}", qty1Alloc.Count, qty1Tot));
                    log.LogListBox(string.Format("Alloc Q2: {0} account #'s - Total Qty: {1}", qty2Alloc.Count, qty2Tot));

                    if (mdrQty1 != qty1Tot || mdrQty2 != qty2Tot)
                    {
                        string msg = "Error in Allocation data\r\n" +
                                        "Total Quantity does not match Trade Quantity\r\n" +
                                        "allocation files will be incorrect";

                        log.WriteLog(msg);
                        MessageBox.Show(msg,ttContract );
                    }
                    else
                    {
                        log.LogListBox("Allocation data matches trade data");
                        log.LogListBox("Allocation files will be automatically generated");
                    }
                }
                else
                { log.LogListBox("ALLOC.XML file does not exist"); }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Load email settings, and optionally the saved open and ordnum 
        /// if the application has been restarted during the day 
        /// </summary>
        private void _LoadINI()
        {
            log.WriteLog("Load Email Parameters");
           
            try
            {
                if (File.Exists("INI.XML"))
                {
                    log.LogListBox("INI.XML file Exists");
                    setINI.ReadXml("INI.XML");
                    log.LogListBox("INI.XML: Data Loaded");

                    //set up email address table 
                    //record email recipients in log file
                    if (setINI.Tables.Contains("address"))
                    {
                        tblAddress = setINI.Tables["address"];

                        log.WriteLog("Allocation Email Recipients:");
                        foreach (DataRow row in tblAddress.Rows)
                        {
                            log.WriteLog(row.ItemArray[0].ToString());
                        }
                    }

                    //setup email 
                    if (setINI.Tables.Contains("parameters"))
                    {
                        tblParameters = setINI.Tables["parameters"];

                        bool[] verifyColumns = new bool[6];

                        DataRow r = tblParameters.Rows[0];
                        log.WriteLog("Email Server Details:");
                        foreach (DataColumn col in tblParameters.Columns)
                        {
                            log.WriteLog(string.Format("{0} : {1}", col.ColumnName, r[col.ColumnName].ToString()));
  
                        }

                        if (tblParameters.Columns.Contains("FROM")) { verifyColumns[0] = true; }
                        if (tblParameters.Columns.Contains("SERVER")) { verifyColumns[1] = true; }
                        if (tblParameters.Columns.Contains("PORT")) { verifyColumns[2] = true; }
                        if (tblParameters.Columns.Contains("SSL")) { verifyColumns[3] = true; }
                        if (tblParameters.Columns.Contains("LOGIN")) { verifyColumns[4] = true; }
                        if (tblParameters.Columns.Contains("PWORD")) { verifyColumns[5] = true; }

                        bool IsEmailSetup = true;
                        int i =0;
                        foreach (bool item in verifyColumns)
                        {
                            if (!item)
                            {
                                log.LogListBox("MISSING EMAIL PARAMETER: " + tblParameters.Columns[i].ColumnName ); 
                                IsEmailSetup = false;
                            }
                            i++;
                        }

                        if (IsEmailSetup) { isAutoSendAllocationsOn = true; }
                    }

                    //load saved data for current trade date
                    bool IsCurrentDaySaved = false;
                    if (setINI.Tables.Contains("startup"))
                    {
                        tblStartup = setINI.Tables["startup"];

                        if (setINI.Tables["startup"].Columns.Contains("TRADEDATE"))
                        {
                            log.WriteLog("setup Table Column: " + "TRADEDATE");

                            int i = setINI.Tables["startup"].Columns["TRADEDATE"].Ordinal;

                            string savedDate = Convert.ToString(tblStartup.Rows[0].ItemArray[i]);

                            if (string.Equals(savedDate, _tradeDate("MMddyy")))
                            {
                                log.LogListBox(string.Format("INI.XML: SavedDate: {0} matches Tradedate: {1}", savedDate, _tradeDate("MMddyy")));
                                IsCurrentDaySaved = true;
                            }
                            else
                            { log.LogListBox("INI.XML: Saved Trade date does not match"); }
                        }

                        if (IsCurrentDaySaved)
                        {
                            if (setINI.Tables["startup"].Columns.Contains("OPEN"))
                            {
                                log.WriteLog("setup Table Column: " + "OPEN");

                                int i = setINI.Tables["startup"].Columns["OPEN"].Ordinal;

                                decimal savedOpen = Convert.ToDecimal(tblStartup.Rows[0].ItemArray[i]);

                                if (savedOpen != 0)
                                {
                                    open = savedOpen;
                                    nudOpen.Value = savedOpen;
                                    chkBxOpen.Checked = true;
                                    isOpenCaptured = false;
                                    isExchangeOpenUsed = false;
                                    isOpenDelivered = true;
                                    log.LogListBox("Saved Open Price: " + savedOpen.ToString());
                                }
                                else
                                { log.LogListBox("INI.XML: Saved open = 0"); }
                            }

                            if (setINI.Tables["startup"].Columns.Contains("ORDNUM"))
                            {
                                log.WriteLog("setup Table Column: " + "ORDNUM");

                                int i = setINI.Tables["startup"].Columns["ORDNUM"].Ordinal;

                                int savedOrdNum = Convert.ToInt32(tblStartup.Rows[0].ItemArray[i]);

                                if (savedOrdNum != 0)
                                {
                                    ordNum = savedOrdNum;
                                    log.LogListBox("Saved Order Number: " + savedOrdNum.ToString());
                                    if (savedOrdNum > 1) { fillQty = mdrQty2; }
                                }
                                else
                                { log.LogListBox("INI.XML: Saved OrdNum = 0"); }

                            }
                        }
                    }

                    if (setINI.Tables.Contains("location"))
                    { 
                        //not implemented - possible replacement for custom startup parameters
                    }

                }
                else
                { log.LogListBox("INI.XML file does not exist"); }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Load Templar allocation parameters
        /// </summary>
        private void _LoadCustomAlloc()
        {
            try
            {
                if (File.Exists("CUSTOMALLOC.XML"))
                {
                    log.LogListBox("CUSTOMALLOC.XML file Exists");
                    setCustom.ReadXml("CUSTOMALLOC.XML");
                    log.LogListBox("CUSTOMALLOC.XML: Data Loaded");

                    if (setCustom.Tables.Contains("address1"))
                    {
                        tblAddress1 = setCustom.Tables["address1"];

                        log.WriteLog("Allocation Email Recipients:");
                        foreach (DataRow row in tblAddress1.Rows)
                        {
                            log.LogListBox(row.ItemArray[0].ToString());
                        }
                    }

                    if (setCustom.Tables.Contains("templar"))
                    {
                        tblTemplar = setCustom.Tables["templar"];
                        DataRow r = tblTemplar.Rows[0];
                        log.WriteLog("Templar Details:");
                        foreach (DataColumn col in tblTemplar.Columns)
                        {
                            log.WriteLog(string.Format("{0} : {1}", col.ColumnName, r[col.ColumnName].ToString()));
                        }
                        log.WriteLog("finished Loading Templar Details");
                        isCustomAllocLoaded = true;
                    }

                    if (setCustom.Tables.Contains("multiplier"))
                    {
                        DataTable dt = setCustom.Tables["multiplier"];
                        if (dt.Rows.Count == 1)
                        {
                            CustomAllocMultiplier = Convert.ToDecimal(dt.Rows[0].ItemArray[0]);
                            log.WriteLog("CustomAllocMultiplier: " + CustomAllocMultiplier.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }


        //deprecated - using _saveCurrentDaySettingINI() & INI.XML file for OPEN and ORDNUM 
        private void _saveCurrentDaySetting(string col, object value)
        {
            try
            {
                if (setAllocData.Tables.Contains("setup"))
                {
                    if (!tblSetup.Columns.Contains(col)) { tblSetup.Columns.Add(col, value.GetType()); }

                    DataRow currentDay = tblSetup.Rows[0];
                    currentDay[col] = value;

                    setAllocData.WriteXml("ALLOC.XML");
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }


        /// <summary>
        /// persists Current tradedate, OrdNum and Open price in INI.XML file
        /// </summary>
        internal void _saveCurrentDaySettingINI()
        {
            try
            {
                if (!setINI.Tables.Contains("startup"))
                {
                    tblStartup = new DataTable("startup");
                    setINI.Tables.Add(tblStartup);
                    log.LogListBox("ADD TABLE");
                }

                if (!setINI.Tables["startup"].Columns.Contains("TRADEDATE"))
                { 
                    setINI.Tables["startup"].Columns.Add("TRADEDATE", typeof(string));
                    log.LogListBox("COLUMN ADDED");
                }

                if (!setINI.Tables["startup"].Columns.Contains("OPEN")) 
                { 
                    setINI.Tables["startup"].Columns.Add("OPEN", typeof(decimal));
                    log.LogListBox("COLUMN ADDED");
                }
                if (!setINI.Tables["startup"].Columns.Contains("ORDNUM")) 
                { 
                    setINI.Tables["startup"].Columns.Add("ORDNUM", typeof(int));
                    log.LogListBox("COLUMN ADDED");
                }

                DataRow currentDay = setINI.Tables["startup"].NewRow();

                currentDay["TRADEDATE"] = _tradeDate("MMddyy");
                currentDay["ORDNUM"] = ordNum;
                currentDay["OPEN"] = open;

                setINI.Tables["startup"].Rows.Clear();
                setINI.Tables["startup"].Rows.Add(currentDay);
                setINI.AcceptChanges();

                setINI.WriteXml("INI.XML");
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// msgbx = FALSE : Record all loaded data in log file
        /// msgbx = True : Display on form control
        /// </summary>
        /// <param name="msgbx"></param>
        private void _displayParameters(bool msgbx)
        {
            if (!msgbx)
            {
                log.LogListBox("sExchange: " + ttGateway);
                log.LogListBox("sProduct: " + ttProduct);
                log.LogListBox("sProductType: " + ttProductType);
                log.LogListBox("sContract: " + ttContract);

                log.LogListBox("sPos: " + longShort);
                log.LogListBox("iPos: " + startPosition.ToString());

                log.LogListBox("Q1: " + mdrQty1.ToString());
                log.LogListBox("Q2: " + mdrQty2.ToString());

                if (buystop != 0)
                { log.LogListBox("buystop: " + buystop.ToString()); }
                else
                { log.LogListBox("sellstop: " + sellstop.ToString()); }

                log.LogListBox("bV: " + bV.ToString());
                log.LogListBox("dV: " + dV.ToString());

                log.LogListBox("Start: " + startTime.ToString());
                log.LogListBox("Stop: " + stopTime.ToString());

                log.LogListBox("Tick: " + oneTick.ToString());
                log.LogListBox("ALGO: " + algo);
                log.LogListBox("nudOffSet.Value : " + nudOffSet.Value .ToString());

                log.LogListBox("Q1 Allocation:");
                foreach (KeyValuePair<int, int> kvp in qty1Alloc)
                { log.LogListBox(kvp.ToString()); }

                log.LogListBox("Q2 Allocation:");
                foreach (KeyValuePair<int, int> kvp in qty2Alloc)
                { log.LogListBox(kvp.ToString()); }

                log.LogListBox("Full Account Numbers:");
                foreach (KeyValuePair<int, string> kvp in accountNumbers)
                { log.LogListBox(kvp.ToString()); }
            }
            else
            {
                ParameterList frm = new ParameterList();

                frm.listBox1.Items.Add("X_Platform2.exe Version:" + Application.ProductVersion);
                frm.listBox1.Items.Add("sExchange: " + ttGateway);
                frm.listBox1.Items.Add("sProduct: " + ttProduct);
                frm.listBox1.Items.Add("sProductType: " + ttProductType);
                frm.listBox1.Items.Add("sContract: " + ttContract);

                frm.listBox1.Items.Add("sPos: " + longShort);
                frm.listBox1.Items.Add("iPos: " + startPosition.ToString());

                frm.listBox1.Items.Add("Q1: " + mdrQty1.ToString());
                frm.listBox1.Items.Add("Q2: " + mdrQty2.ToString());

                if (buystop != 0)
                { frm.listBox1.Items.Add("buystop: " + buystop.ToString()); }
                else
                { frm.listBox1.Items.Add("sellstop: " + sellstop.ToString()); }

                frm.listBox1.Items.Add("bV: " + bV.ToString());
                frm.listBox1.Items.Add("dV: " + dV.ToString());

                frm.listBox1.Items.Add("Start: " + startTime.ToString());
                frm.listBox1.Items.Add("Stop: " + stopTime.ToString());

                frm.listBox1.Items.Add("Tick: " + oneTick.ToString());
                frm.listBox1.Items.Add("ALGO: " + algo);
                frm.listBox1.Items.Add("nudOffSet.Value : " + nudOffSet.Value .ToString());

                frm.listBox1.Items.Add(string.Empty);
                frm.listBox1.Items.Add("Q1 Allocation:");
                foreach (KeyValuePair<int, int> kvp in qty1Alloc)
                { frm.listBox1.Items.Add(kvp.ToString()); }

                frm.listBox1.Items.Add(string.Empty);
                frm.listBox1.Items.Add("Q2 Allocation:");
                foreach (KeyValuePair<int, int> kvp in qty2Alloc)
                { frm.listBox1.Items.Add(kvp.ToString()); }

                frm.listBox1.Items.Add(string.Empty);
                frm.listBox1.Items.Add("Full Account Numbers:");
                foreach (KeyValuePair<int, string> kvp in accountNumbers)
                { frm.listBox1.Items.Add(kvp.ToString()); }

                frm.listBox1.Items.Add(string.Empty);
                try
                {
                    if (isContractFound)
                    {
                        frm.listBox1.Items.Add(string.Format(
                            "1st Order: {0} {1} at {2}", 
                            (longShort == "L" ? "Sell" : "Buy"), 
                            mdrQty1,
                            ttInstrObj.get_TickPrice(tradePrice1, 0, "$")));
                        frm.listBox1.Items.Add(string.Format(
                            "2nd Order: {0} {1} at {2}", 
                            (longShort == "L" ? "Buy" : "Sell"), 
                            mdrQty2,
                            ttInstrObj.get_TickPrice(tradePrice2, 0, "$")));

                        if (tradePrice1 == sellstop || tradePrice1 == buystop)
                        { frm.listBox1.Items.Add("Open does not affect trade prices"); }
                        else
                        { frm.listBox1.Items.Add("Open +/- bV is active trade price"); }

                        frm.listBox1.Items.Add("Currrent Order Number: " + ordNum.ToString());
                        frm.listBox1.Items.Add(lblOrder.Text);
                    }
                    else
                    { frm.listBox1.Items.Add("Contract not yet Loaded: No orders calculated"); }
                }
                catch (Exception ex)
                { log.LogListBox(ex.ToString()); }
                
                frm.Show();
            }
        }
        
        /// <summary>
        /// verify proper customer settings are available.
        /// TT Workstation must be setup with XTAPI and XTAPI-ICE customer settings
        /// Otherwise orders will be rejected 
        /// </summary>
        private void _ChkCustomers()
        {
            try
            {
                string[] cust = (string[])ttGate.Customers;
                bool found = false;
                bool foundICE = false;

                foreach (string item in cust)
                {
                    if (string.Equals(item, "XTAPI", StringComparison.OrdinalIgnoreCase))
                    { found = true; }

                    if (string.Equals(item, "XTAPI-ICE", StringComparison.OrdinalIgnoreCase))
                    { foundICE = true; }
                }

                if (found && foundICE)
                {
                    log.LogListBox("Customer Settings found");
                    if (ttGateway.IndexOf("ICE") == 0)
                    { ttCustomer = "XTAPI-ICE"; }
                    else
                    { ttCustomer = "XTAPI"; }
                    log.LogListBox("Active X_TRADER Customer Default: " + ttCustomer);
                }
                else
                {
                    log.LogListBox("Customer Settings NOT found");
                    MessageBox.Show("Customer Settings NOT found\r\n" +
                                   "Please add Customer Defaults 'XTAPI' & 'XTAPI-ICE' for use by this application\r\n" +
                                   "All orders will be rejected until this change is made",ttContract );
                }
            }
            catch (Exception ex)
            {
                log.LogListBox(ex.ToString());
            }
        }

        /// <summary>
        /// force exchange events to fire which in calls _conectInstr to load contract
        /// </summary>
        private void _connectExchange()
        {
            try
            {
                ttGate.OpenExchangeFills(ttGateway);
                ttGate.OpenExchangeOrders(ttGateway);
                ttGate.OpenExchangePrices(ttGateway);
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }
        
        /// <summary>
        /// load contract
        /// </summary>
        private void _connectInstr()
        {
            // Update the Status Bar text.
            //statusBar1.Text = "Connecting to Instrument...";
            log.WriteLog("_connectInstr");

            try
            {
                if (ttInstrObj != null)
                {
                    // Detach previously attached instrument.
                    ttInstrNotify.DetachInstrument(ttInstrObj);
                    ttInstrNotifyLast.DetachInstrument(ttInstrObj);
                    ttInstrNotifyOpen.DetachInstrument(ttInstrObj);

                    // Set the TTInstrObj to NULL.
                    ttInstrObj = null;
                }

                // Instantiate the instrument object.
                ttInstrObj = new XTAPI.TTInstrObjClass();

                // Obtain the Instrument information from the user input.
                ttInstrObj.Exchange = ttGateway;
                ttInstrObj.Product = ttProduct;
                ttInstrObj.ProdType = ttProductType;
                ttInstrObj.Contract = ttContract;

                // Attach the TTInstrObj to the TTInstrNotify for price update events.
                ttInstrNotify.AttachInstrument(ttInstrObj);
                ttInstrNotifyLast.AttachInstrument(ttInstrObj);
                ttInstrNotifyOpen.AttachInstrument(ttInstrObj);

                // Open the TTInstrObj.
                ttInstrObj.Open(1);	// enable Market Depth:  1 - true, 0 - false
            }
            catch (Exception ex)
            {
                // Display exception message.
                log.LogListBox(ex.ToString());
            }
        }

        /// <summary>
        /// calculate orders from MDR data and opern proce
        /// </summary>
        internal void _calcOrders()
        {
            log.WriteLog("_calcOrders");
            log.LogListBox(string.Format("SellStop: {0} BuyStop: {1} Open: {2} bV: {3} dV: {4}", sellstop, buystop, open, bV, dV));

            if (open >= 0)
            {
                switch (longShort)
                {
                    case "L":
                        tradePrice1 = Math.Min(sellstop, open - bV);
                        tradePrice2 = tradePrice1 + dV;
                        break;
                    case "S":
                        tradePrice1 = Math.Max(buystop, open + bV);
                        tradePrice2 = tradePrice1 - dV;
                        break;
                    default:
                        break;
                }
                isOrderCalculated = true;
                
                try
                {
                    log.LogListBox(string.Format(
                        "1st Order: {0} {1} at {2}", 
                        (longShort == "L" ? "Sell" : "Buy"), 
                        mdrQty1,
                        ttInstrObj.get_TickPrice(tradePrice1, 0, "$")));
                    log.LogListBox(string.Format(
                        "2nd Order: {0} {1} at {2}", 
                        (longShort == "L" ? "Buy" : "Sell"), 
                        mdrQty2,
                        ttInstrObj.get_TickPrice(tradePrice2, 0, "$")));
                }
                catch (Exception ex)
                { log.LogListBox(ex.ToString()); }

                if (tradePrice1 == sellstop || tradePrice1 == buystop)
                { log.LogListBox("Open does not affect trade prices"); }
                else
                { log.LogListBox("Open +/- bV is active trade price"); }
            }
            else
            { log.LogListBox(string.Format("_calcOrders ERROR: Open {0}", open)); }
        }

        /// <summary>
        /// Queue correct order based on OrdNum
        /// </summary>
        internal void _queueCurrentOrder()
        {
            try
            {
                log.WriteLog("_queueCurrentOrder Called");
                if (isOrderCalculated)
                {
                    if (ordNum == 1)
                    { tradeQty = mdrQty1; }
                    else
                    { tradeQty = mdrQty2; }

                    if (ordNum % 2 == 1)
                    {
                        stopPrice = Convert.ToDecimal(ttInstrObj.get_TickPriceEx(tradePrice1, enumRoundPriceType.ROUND_NEAREST, 0, "#"));
                        if (longShort == "L")
                        { buySell = "S"; }
                        else
                        { buySell = "B"; }
                    }
                    else
                    {
                        stopPrice = Convert.ToDecimal(ttInstrObj.get_TickPriceEx(tradePrice2, enumRoundPriceType.ROUND_NEAREST, 0, "#"));
                        if (longShort == "L")
                        { buySell = "B"; }
                        else
                        { buySell = "S"; }
                    }

                    _setLimitPrice();

                    isOrderQueued = true;

                    log.WriteLog(string.Format(
                        "{0} {1} {2} STOP:{3} LIMIT:{4}", 
                        buySell, 
                        tradeQty, 
                        ttProduct,
                        ttInstrObj.get_TickPrice(stopPrice, 0, "$"),
                        ttInstrObj.get_TickPrice(limitPrice, 0, "$")));

                    _updateOrderLabel();
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Display Current Order on Form
        /// </summary>
        private void _updateOrderLabel()
        {
            log.WriteLog("_updateOrderLabel");

            lblOrder.Text = string.Format(
                "{0} {1} STP:{2}", 
                (buySell == "B" ? "BUY" : "SELL"), 
                tradeQty,
                ttInstrObj.get_TickPrice(stopPrice, 0, "$"));

            switch (algo)
            {
                case "stopMarket":
                    //do not add lmt price
                    break;
                default:
                    lblOrder.Text += string.Format(" LMT:{0}", ttInstrObj.get_TickPrice(limitPrice, 0, "$"));
                    break;
            }
            
            log.WriteLog(lblOrder.Text);

            Graphics g = lblOrder.CreateGraphics();
            int w = (int)g.MeasureString(lblOrder.Text, lblOrder.Font).Width;
            //int h = (int)g.MeasureString(lblOrder.Text, lblOrder.Font).Height;
            //check lblOrder.Size.Height in addition to make certain label is resized
            while (w+3 > lblOrder.Size.Width)
            {
                lblOrder.Font = new Font(lblOrder.Font.FontFamily, lblOrder.Font.Size - 1);
                w = (int)g.MeasureString(lblOrder.Text, lblOrder.Font).Width;
            }

            if (buySell == "B")
            {
                groupBox3.BackColor = Color.Blue;
                groupBox3.ForeColor = Color.White;
                lblOrder.BackColor = Color.Blue;
                lblOrder.ForeColor = Color.White;
            }
            else
            {
                groupBox3.BackColor = Color.Red;
                groupBox3.ForeColor = Color.Black;
                lblOrder.BackColor = Color.Red;
                lblOrder.ForeColor = Color.Black;
            }
            groupBox3.Text = string.Format("Active Order: {0} - ALGO: {1}", ordNum, currentAlgo.ToString()); 
        }

        /// <summary>
        /// submit limit order when using syntheticSL algorithm
        /// </summary>
        private void _SynSL_Execute()
        {
            if (!isLimitOrderSent)
            {
                _Execute(1, buySell, tradeQty, limitPrice, ttCustomer, false);
                isLimitOrderSent = true;
            }
            else
            {
                _chkHungOrder(last, limitPrice);
            }
            
            if (this.WindowState != FormWindowState.Normal) { this.WindowState = FormWindowState.Normal; }
        }

        /// <summary>
        /// If there is already an order workign for that contract, retrieve it 
        /// and verify that it is the correct order
        /// If multiple 
        /// </summary>
        private void _retrieveExistingOrder()
        {
            try
            {
                if (ttOrderSet.Count == 1)
                {
                    XTAPI.TTOrderObj tmpOO = (XTAPI.TTOrderObj)ttOrderSet[1];

                    Array data = (Array)tmpOO.get_Get("SiteOrderKey,BuySell,Qty,Stop#,Limit#,OrderType,OrderRestr,OnHold");
                    //                                 0            1       2   3     4      5         6          7

                    string sok = data.GetValue(0).ToString();
                    string bs = data.GetValue(1).ToString();
                    int q = Convert.ToInt32(data.GetValue(2));
                    decimal stp = Convert.ToDecimal(data.GetValue(3));
                    decimal lmt = Convert.ToDecimal(data.GetValue(4));
                    string oType = data.GetValue(5).ToString();
                    string oRestr = data.GetValue(6).ToString();
                    bool isCaptured = false;
                    
                    TTOrderProfile tmpOP = tmpOO.CreateOrderProfile;
                    int onHold = (int)tmpOP.get_Get("OnHold");

                    if (string.Equals(bs, buySell))
                    {
                        if (q == tradeQty)
                        {
                            if (stp == stopPrice)
                            {
                                siteOrderKey = sok;
                                log.LogListBox("NO ORDER SENT; Existing Order in the market was captured");
                                isCaptured = true;
                                chkBxOpen.Checked = true;
                                if (string.Equals(algo, "nativeSL")) { button4.Enabled = true; }

                                switch (algo)
                                {
                                    case "syntheticSL":
                                        //do nothing
                                        break;
                                    case "nativeSL":
                                        _ordersetUpdate_HoldNoHold(true);
                                        break;
                                    case "stopMarket":
                                        _ordersetUpdate_HoldNoHold(false);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    if (!isCaptured) { log.LogListBox("NO ORDER SENT; Existing Order in the market does not match"); } 
                }
                else
                { 
                    DialogResult dr = MessageBox.Show("More than 1 order is working for this contract.\r\nNo Orders will be submitted!\r\nDelete all existing orders?",ttContract,MessageBoxButtons.OKCancel );
                    if (dr == DialogResult.OK)
                    { }

                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Calculate distance from stop price
        /// </summary>
        /// <returns></returns>
        private string _distance()
        {
            string dist = null;
            try
            {
                dist = Convert.ToString(ttInstrObj.get_TickPriceEx(
                        0,
                        enumRoundPriceType.ROUND_NEAREST,
                        Convert.ToInt32((buySell == "B" ? (stopPrice - last) : (last - stopPrice)) / oneTick),
                        "$"));

            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }

            //return dist;

            if (stopPrice != 0)
            { return dist; }
            else
            { return "Order Price not yet calculated"; }
        }

        /// <summary>
        /// Display Time and Sales; call algorithm to check order
        /// </summary>
        /// <param name="lstPrice"></param>
        private void _chkSubmitOrder(decimal lstPrice)
        {
            if (lstPrice <= 0)
            {
                log.LogListBox("ORDERS NOT CHECKED - ERROR: Last = " + lstPrice.ToString());
            }
            else
            {
                try
                {
                    prc.LogListBox(string.Format(
                                    "LAST: {0} QTY: {1} Distance: {2}",
                                    ttInstrObj.get_TickPrice(last, 0, "$"),
                                    lstQty, 
                                    _distance()));
                }
                catch (Exception ex)
                { log.LogListBox(ex.ToString()); }
           
                switch (algo)
                {
                    case "nativeSL":
                        _ALGO_nativeSL();
                        break;
                    case "stopMarket":
                        _ALGO_stopMarket(lstPrice);
                        break;
                    case "syntheticSL":
                        _ALGO_SynSL(lstPrice);
                        break;
                    default:
                        log.LogListBox(algo + " NOT SET UP CORRECTLY!!");
                        break;
                }
            }
        }

        /// <summary>
        /// Sound Alert if a limit order is hung and not completely filled within 15 seconds
        /// TODO: Time could be removed to INI.XML
        /// </summary>
        /// <param name="LastPrc"></param>
        /// <param name="TradePrc"></param>
        private void _chkHungOrder(decimal LastPrc, decimal TradePrc)
        {
            if ((buySell == "B" && LastPrc >= TradePrc) || (buySell == "S" && LastPrc <= TradePrc))
            {
                log.WriteLog(string.Format(" HUNG? Last: {0} Trade Price: {1}", LastPrc, TradePrc));
                msgAlert = "ALERT: Potential hung order";
                log.LogListBox(msgAlert);
                if (!isOrderHung)
                {
                    isOrderHung = true;
                    hungTimer.Interval = 15000;
                    hungTimer.Start();
                }
            }
        }

        /// <summary>
        /// syntheticSL Algorithm : Send limit order when last price = stop price
        /// </summary>
        /// <param name="lstPrice"></param>
        private void _ALGO_SynSL(decimal lstPrice)
        {
            log.WriteLog("_ALGO_SynSL");

            if (isOrderServerUP)
            {
                if (buySell == "S")
                {
                    if (lstPrice <= stopPrice)
                    {
                        _SynSL_Execute();
                    }
                }

                if (buySell == "B")
                {
                    if (lstPrice >= stopPrice)
                    {
                        _SynSL_Execute();
                    }
                }

            }
            else
            {
                log.LogListBox(string.Format("Order Server: {0} Price Server: {1} Fill Server: {2}", isOrderServerUP, isPriceServerUP, isFillServerUP));
                if ((buySell == "B" ? (stopPrice - lstPrice) : (lstPrice - stopPrice)) / oneTick <= 10)
                {
                    msgAlert = "ALERT: ORDER SERVER DOWN";
                    log.LogListBox(msgAlert);
                    AlertTimer.Interval = 100;
                    AlertTimer.Start();
                }
            }
        }

        /// <summary>
        /// nativSL Algorithm: Stop Limit Order submitted on first last print after turning on
        /// /auto command line switch submits order live to the exchange
        /// otherwise order is submit on hold
        /// </summary>
        private void _ALGO_nativeSL()
        {
            try
            {
                if (siteOrderKey == null && isOrderQueued)
                {
                    log.WriteLog("m_TTOrderSet.Count: " + ttOrderSet.Count.ToString());
                    log.WriteLog("fillList.Count: " + fillList.Count.ToString());

                    if (ttOrderSet.Count == 0 && fillList.Count == 0)
                    {
                        if (isAlertOn)
                        { _Execute(3, buySell, tradeQty, stopPrice, ttCustomer, true); }
                        else
                        { _Execute(3, buySell, tradeQty, stopPrice, ttCustomer, false); }
                        chkBxOpen.Checked = true;

                        log.WriteLog(" _ALGO_nativeSL: new order submitted");
                        button4.Enabled = true;
                        
                    }
                    else
                    { _retrieveExistingOrder(); }
                }
                else
                {
                    _chkHungOrder(last , limitPrice);
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }


        /// <summary>
        /// Submit stop market on hold.  Automatically activate order 
        /// based on a trigger value away from the stop price
        /// to manually activate order increase trigger offset value to max value
        /// </summary>
        /// <param name="lstPrice"></param>
        private void _ALGO_stopMarket(decimal lstPrice)
        {
            try
            {
                if (siteOrderKey == null && isOrderQueued)
                {
                    log.WriteLog("m_TTOrderSet.Count: " + ttOrderSet.Count.ToString());
                    log.WriteLog("fillList.Count: " + fillList.Count.ToString());

                    if (ttOrderSet.Count == 0 && fillList.Count == 0)
                    {
                        //submit order on hold
                        _Execute(2, buySell, tradeQty, stopPrice, ttCustomer, true);
                        chkBxOpen.Checked = true;

                        log.WriteLog(" _ALGO_stopMarket: new order submitted");
                        //button4.Enabled = true;
                    }
                    else
                    { _retrieveExistingOrder(); }
                }
                else
                {
                    //sSOK !=null
                    if (buySell == "S")
                    {
                        //activate order 
                        if ((lstPrice - stopPrice) <= (nudOffSet.Value ))
                        { _activateOrder(true); }
                        
                        if ((lstPrice - stopPrice) > (2 * nudOffSet.Value ))
                        { _activateOrder(false); }
                    }

                    if (buySell == "B")
                    {
                        if ((stopPrice - lstPrice) <= (nudOffSet.Value )) 
                        { _activateOrder(true); }
              
                        if ((stopPrice - lstPrice) > (2 * nudOffSet.Value )) 
                        { _activateOrder(false); }
                    }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }


        /// <summary>
        /// Activate a held order or place an active order on hold.
        /// </summary>
        /// <param name="activate"></param>
        private void _activateOrder(bool activate)
        {
            // TODO: Exception thrown when order chaneg is attempted after order chaneg has already been submitted
            // Exception is caught and does not affect operation of program.
            try
            {
                if (siteOrderKey != null)
                {
                    TTOrderObj tmpOO = (TTOrderObj)ttOrderSet.get_SiteKeyLookup(siteOrderKey);
                    TTOrderProfile tmpOP = tmpOO.CreateOrderProfile;

                    int onHold = (int)tmpOP.get_Get("OnHold");
                    int exType = (int)tmpOO.get_Get("ExecutionType");
                    
                    log.WriteLog("OnHold: " + onHold.ToString() + " ExecutionType: " + exType.ToString());

                    if (exType == 1)
                    {
                        if (activate)
                        {
                            if (onHold == 1)
                            {
                                ttOrderSet.HoldOrder(tmpOO, 0);
                                _MakeNoise(@"C:\tt\sounds\laser.wav");
                            }
                        }
                        else
                        {
                            if (onHold == 0)
                            {
                                ttOrderSet.HoldOrder(tmpOO, 1);
                            }
                        }
                    }
                    else
                    { log.LogListBox("No Change: Order is in-flight"); }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// Change Order price is limit offset or open price is changed
        /// </summary>
        private void _setLimitPrice()
        {
            log.WriteLog("_setLimitPrice");

            try
            {
                log.WriteLog("isContractFound " + isContractFound.ToString());
                if (isContractFound)
                {
                    if (buySell == "S")
                    { limitPrice = Convert.ToDecimal(ttInstrObj.get_TickPriceEx(stopPrice, XTAPI.enumRoundPriceType.ROUND_NEAREST, -(int)nudOffSet.Value , "#")); }
                    else
                    { limitPrice = Convert.ToDecimal(ttInstrObj.get_TickPriceEx(stopPrice, XTAPI.enumRoundPriceType.ROUND_NEAREST, (int)nudOffSet.Value , "#")); }

                    if (siteOrderKey != null)
                    {
                        log.WriteLog("SOK: " + siteOrderKey);
                        TTOrderObj tmpOO = (TTOrderObj)ttOrderSet.get_SiteKeyLookup(siteOrderKey);
                        // Obtain the TTOrderProfile from the last order.
                        XTAPI.TTOrderProfile tmpOP = tmpOO.CreateOrderProfile;

                        int exType = (int)tmpOO.get_Get("ExecutionType");
                        log.WriteLog("ExecutionType: " + exType.ToString());

                        if (exType == 1)
                        {
                            if (currentAlgo == Algorithm.nativeSL)
                            {
                                if (limitPrice != Convert.ToDecimal(tmpOP.get_Get("Limit#")) || stopPrice != Convert.ToDecimal(tmpOP.get_Get("Stop#")))
                                {
                                    tmpOP.Set("Limit#", limitPrice);
                                    tmpOP.Set("Stop#", stopPrice);
                                    // Update Order as change or cancel/replace (0 - change, 1 - cancel/replace).
                                    ttOrderSet.UpdateOrder(tmpOP, 1);

                                    log.WriteLog(string.Format("Price Updated {0}: {1} {2}: {3}", "Limit#", limitPrice, "Stop#", stopPrice));
                                }
                                else
                                { log.WriteLog("No Change in Order Prices"); }
                            }

                            if (currentAlgo == Algorithm.stopMarket)
                            {
                                if (stopPrice != Convert.ToDecimal(tmpOP.get_Get("Stop#")))
                                {
                                    tmpOP.Set("Stop#", stopPrice);
                                    // Update Order as change or cancel/replace (0 - change, 1 - cancel/replace).
                                    ttOrderSet.UpdateOrder(tmpOP, 1);
                                    log.WriteLog(string.Format("Price Updated {0}: {1}", "Stop#", stopPrice));
                                }
                                else
                                { log.WriteLog("No Change in Order Prices"); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }


        /// <summary>
        /// Submit an order
        /// </summary>
        /// <param name="orderType"></param> integer 0=market 1=limit 2=stop market 3=stop limit
        /// <param name="bs"></param> B=BUY S=SELL
        /// <param name="qty"></param> trade quantity
        /// <param name="price"></param> Stop Price
        /// <param name="ttCustomer"></param> X_TRADER Customer default XTAPI or XTAPI-ICE
        /// <param name="hold"></param> submit on hold or live
        private void _Execute(int orderType, string bs, int qty, decimal price, string ttCustomer, bool hold)
        {
            log.LogListBox("EXECUTE: type " + orderType.ToString() +
                                            " " + bs.ToString() + " " +
                                             " " + qty.ToString() + " " +
                                              " " + price.ToString() + " " +
                                               " " + ttCustomer.ToString());

            // Initialize the submittedQuantity value to zero.
            int submittedQuantity = 0;

            try
            {
                // Set the TTInstrObj to the TTOrderProfile.
                XTAPI.TTOrderProfileClass ttOrderProfile = new XTAPI.TTOrderProfileClass();
                ttOrderProfile.Instrument = ttInstrObj;

                log.LogListBox("EXECUTE: " + ttInstrObj.Contract.ToString());

                // Set the customer default property (e.g. "<Default>").
                ttOrderProfile.Customer = ttCustomer;

                // Set for Buy or Sell.
                ttOrderProfile.Set("BuySell", bs);

                // Set the quantity.
                ttOrderProfile.Set("Qty", qty);

                // Determine which Order Type is selected.
                if (orderType == 0)
                {   // Market Order
                    // Set the order type to "M" for a market order.
                    ttOrderProfile.Set("OrderType", "M");
                }
                else if (orderType == 1)
                {   // Limit Order
                    // Set the order type to "L" for a limit order.
                    ttOrderProfile.Set("OrderType", "L");
                    // Set the limit order price.
                    ttOrderProfile.Set("Limit#", price);

                }
                else if (orderType == 2)
                {   // Stop Market Order
                    // Set the order type to "M" for a market order.
                    ttOrderProfile.Set("OrderType", "M");
                    // Set the order restriction to "S" for a stop order.
                    ttOrderProfile.Set("OrderRestr", "S");
                    // Set the stop price.
                    ttOrderProfile.Set("Stop#", price);
                }
                else if (orderType == 3)
                {   // Stop Limit Order
                    // Set the order type to "L" for a limit order.
                    ttOrderProfile.Set("OrderType", "L");
                    // Set the order restriction to "S" for a stop order.
                    ttOrderProfile.Set("OrderRestr", "S");
                    // Set the limit price.
                    object tmp = ttInstrObj.get_TickPriceEx(
                        price,
                        enumRoundPriceType.ROUND_NEAREST,
                        (bs == "S" ? -(int)nudOffSet.Value  : (int)nudOffSet.Value ),
                        "#");
                    ttOrderProfile.Set("Limit#", Convert.ToDecimal(tmp));
                    // Set the stop price.
                    ttOrderProfile.Set("Stop#", price);
                }

                ttOrderProfile.Set("FFT2", "AUTOX");
                ttOrderProfile.Set("ColorPri", 65535);
                if (hold) { ttOrderProfile.Set("OnHold", hold); }

                // Send the order by submitting the TTOrderProfile through the TTOrderSet.
                submittedQuantity = ttOrderSet.get_SendOrder(ttOrderProfile);
                siteOrderKey = ttOrderProfile.get_GetLast("SiteKey").ToString();
                log.LogListBox("SOK: " + siteOrderKey);
            }
            catch (Exception e)
            {
                log.LogListBox(e.Message);
                log.LogListBox(e.ToString());
            }
        }

        /// <summary>
        /// play sound
        /// </summary>
        /// <param name="sndFile"></param>
        private void _MakeNoise(string sndFile)
        {
            {
                System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
                myPlayer.SoundLocation = sndFile;
                myPlayer.Play();
            }
        }

        /// <summary>
        /// email standard Allocation file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        private void _SendAllocationFile(string fileName, string fileNumber)
        {
            //create the mail message
            System.Net.Mail.MailMessage mail = new MailMessage();

            foreach (DataRow row in tblAddress.Rows)
            { mail.To.Add(row.ItemArray[0].ToString()); }

            Attachment data = new Attachment(fileName);
            mail.Attachments.Add(data);

            mail.Subject = string.Format("Allocation Parser File – {0} – Notification #{1}", _tradeDate("MMMM dd, yyyy"), fileNumber);
            log.WriteLog(mail.Subject);

            mail.Body = msgBody;

            try
            {
                DataRow email = tblParameters.Rows[0];
                
                //parameters.Rows[0].ItemArray[0].ToString()
                mail.From = new MailAddress(email["FROM"].ToString());
                
                //parameters.Rows[0].ItemArray[1].ToString(), 
                //Convert.ToInt32(parameters.Rows[0].ItemArray[2]));
                System.Net.Mail.SmtpClient smtp = new SmtpClient(email["SERVER"].ToString(), Convert.ToInt32(email["PORT"]));

                //parameters.Rows[0].ItemArray[4].ToString(),
                //parameters.Rows[0].ItemArray[5].ToString());
                smtp.Credentials = new NetworkCredential(email["LOGIN"].ToString(), email["PWORD"].ToString());

                //Convert.ToBoolean(parameters.Rows[0].ItemArray[3]);
                smtp.EnableSsl = Convert.ToBoolean(email["SSL"]);
                smtp.Send(mail);
                log.LogListBox("Allocation Email Sent!");
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// email Custom Templar allocation file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNumber"></param>
        private void _SendCustomAllocationFile(string fileName, string fileNumber)
        {
            //create the mail message
            System.Net.Mail.MailMessage mail = new MailMessage();

            foreach (DataRow row in tblAddress1.Rows)
            { mail.To.Add(row.ItemArray[0].ToString()); }

            Attachment data = new Attachment(fileName);
            mail.Attachments.Add(data);

            mail.Subject = string.Format("Trades today - {0} - Newedge 200 0P985 Templar - Notification #{1}", _tradeDate("MMM dd, yyyy"), fileNumber);

            log.WriteLog(mail.Subject);

            try
            {
                DataRow email = tblParameters.Rows[0];

                //parameters.Rows[0].ItemArray[0].ToString()
                mail.From = new MailAddress(email["FROM"].ToString());

                //parameters.Rows[0].ItemArray[1].ToString(), 
                //Convert.ToInt32(parameters.Rows[0].ItemArray[2]));
                System.Net.Mail.SmtpClient smtp = new SmtpClient(email["SERVER"].ToString(), Convert.ToInt32(email["PORT"]));

                //parameters.Rows[0].ItemArray[4].ToString(),
                //parameters.Rows[0].ItemArray[5].ToString());
                smtp.Credentials = new NetworkCredential(email["LOGIN"].ToString(), email["PWORD"].ToString());

                //Convert.ToBoolean(parameters.Rows[0].ItemArray[3]);
                smtp.EnableSsl = Convert.ToBoolean(email["SSL"]);
                smtp.Send(mail);
                log.LogListBox("CUSTOM Allocation Email Sent!");
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        //deprecated
        private void _SendAlert(string msg)
        {
            //create the mail message
            System.Net.Mail.MailMessage mail = new MailMessage();

            mail.To.Add("to@gmail.com");

            mail.Subject = msg;
            mail.Body = DateTime.Now.ToLongTimeString();

            try
            {
                mail.From = new MailAddress("from@gmail.com");
                System.Net.Mail.SmtpClient smtp = new SmtpClient("smtp.gmail.com",587);
                smtp.Credentials = new NetworkCredential("from@gmail.com","password");
                smtp.EnableSsl = true;
                smtp.Send(mail);
                Console.Beep();
            }
            catch (Exception ex)
            {
                StreamWriter sw = new StreamWriter("AlertMail.log", true, Encoding.ASCII, 100);
                sw.WriteLine(DateTime.Now.ToLongDateString());
                sw.WriteLine(DateTime.Now.ToLongTimeString());
                sw.WriteLine(ex.ToString());
                sw.Close();
            }
        }

        /// <summary>
        /// For markets with shortened trading hours, 
        /// attempt to capture last print at start time 
        /// </summary>
        private void _captureOpen()
        {
            if (isOpenCaptured)
            {
                log.WriteLog("_captureOpen");
                if (DateTime.Now.TimeOfDay >= startTime)
                {
                    try
                    {
                        open = Convert.ToDecimal(ttInstrObj.get_Get("Last#"));
                        nudOpen.Value = open;
                        chkBxOpen.Checked = true;
                        isOpenCaptured = false;
                    }
                    catch (Exception ex)
                    { log.LogListBox(ex.ToString()); }
                }
            }
        }

        /// <summary>
        /// Setup filllist for allocation
        /// </summary>
        /// <param name="bs"></param>
        private void _setupAllocation(string bs)
        {
            log.LogListBox("_setupAllocation");
            if (isAllocationDataVerified)
            {
                int q = 0;
                decimal p = 0;
                foreach (KeyValuePair<decimal, int> kvp in fillList)
                {
                    log.LogListBox(kvp.ToString());
                    p += kvp.Key * kvp.Value;
                    q += kvp.Value;
                }
                log.LogListBox(string.Format(
                    "Average Fill: {0}",
                    ttInstrObj.get_TickPriceEx((p / q), enumRoundPriceType.ROUND_NEAREST, 0, "$")));

                log.LogListBox("Total Quantity: " + q.ToString());

                if (q == mdrQty1)
                {
                    log.LogListBox("Q1 Allocation");
                    _writeAllocation(bs,  qty1Alloc);
                }
                else if (q == mdrQty2)
                {
                    log.LogListBox("Q2 Allocation");
                    _writeAllocation(bs,  qty2Alloc);
                }
                else
                {
                    log.LogListBox("Quantity does not match Q1 or Q2; Fill List Cleared");
                    fillList.Clear();
                }
            }
        }


        /// <summary>
        /// Write allocation file
        /// </summary>
        /// <param name="bs"></param> B = BUY S= SELL
        /// <param name="allocation"></param> Allocation data structure
        private void _writeAllocation(string bs, SortedDictionary<int, int> allocation)
        {
            log.WriteLog("_writeAllocation");
            ArrayList allocFile = new ArrayList();

            try
            {
                decimal[] fillListPrices = new decimal[fillList.Count];
                int[] fillListQty = new int[fillList.Count];

                fillList.Keys.CopyTo(fillListPrices, 0);
                fillList.Values.CopyTo(fillListQty, 0);
               
                int ctr = -1;

                //account handle, allocation quantity
                log.WriteLog("foreach Allocation loop");
                log.WriteLog("fillQty Max Index: " + fillListQty.GetUpperBound(0).ToString());
                
                //kvp Allocation order, allocation quantity
                foreach (KeyValuePair<int, int> a in allocation) 
                {
                    log.WriteLog(a.ToString());
                    ctr++;
                    log.WriteLog("CTR: " + ctr.ToString());

                    if (fillListQty[ctr] >= a.Value)
                    {
                        allocFile.Add(_allocRow(
                            accountNumbers[a.Key],
                            bs,
                            a.Value.ToString(),
                            Convert.ToString((Convert.ToDecimal(ttInstrObj.get_TickPrice(fillListPrices[ctr], 0, "#")) * StdAllocMultiplier))));

                        fillListQty[ctr] -= a.Value;
                        
                        if (fillListQty[ctr] > 0) 
                        {
                            
                            log.WriteLog(string.Format("remaining fillQty[{0}]: {1}", ctr, fillListQty[ctr].ToString()));
                            ctr--;
                            log.WriteLog("CTR: " + ctr.ToString());
                        }                        
                    }
                    else
                    {
                        int allocationValue = a.Value;

                        while (allocationValue > 0)
                        {
                            log.WriteLog("aVal: " + allocationValue.ToString());
                            if (fillListQty[ctr] < allocationValue)
                            {
                                allocFile.Add(_allocRow(
                                            accountNumbers[a.Key],
                                            bs,
                                            fillListQty[ctr].ToString(),
                                            Convert.ToString((Convert.ToDecimal(ttInstrObj.get_TickPrice(fillListPrices[ctr], 0, "#")) * StdAllocMultiplier))));

                                allocationValue -= fillListQty[ctr];
                                log.WriteLog("aVal: " + allocationValue.ToString());
                                ctr++;
                                log.WriteLog("CTR: " + ctr.ToString());
                            }
                            else
                            {
                                allocFile.Add(_allocRow(
                                    accountNumbers[a.Key],
                                    bs,
                                    allocationValue.ToString(),
                                    Convert.ToString((Convert.ToDecimal(ttInstrObj.get_TickPrice(fillListPrices[ctr], 0, "#")) * StdAllocMultiplier))));
                                
                                fillListQty[ctr] -= allocationValue;
                                allocationValue = 0;
                                
                                if (fillListQty[ctr] > 0) 
                                {
                                    log.WriteLog(string.Format("remaining fillQty[{0}]: {1}", ctr, fillListQty[ctr].ToString()));
                                    ctr--;
                                    log.WriteLog("CTR: " + ctr.ToString());
                                }
                            }
                        }
                    }
                } 
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }

            try
            {
                string fn = Application.StartupPath.ToString() + @"..\..\_allocations";

                string[] files = System.IO.Directory.GetFiles(fn);
                int fileNum = files.GetLength(0) + 1;

                fn += @"\" + string.Format("allocation_{0}_#{1}.csv", _tradeDate("MMddyy"), fileNum.ToString());

                StreamWriter sw = new StreamWriter(fn, true, Encoding.ASCII, 100);

                foreach (string[] row in allocFile)
                {
                    string allocationRow = null;
                    for (int i = 0; i <= row.GetUpperBound(0); i++)
                    {
                        allocationRow += row.GetValue(i).ToString();
                        if (i != row.GetUpperBound(0)) { allocationRow += ","; }
                    }
                    log.LogListBox(allocationRow);
                    sw.WriteLine(allocationRow);
                }
                sw.Close();

                if (isAutoSendAllocationsOn)
                {
                    _SendAllocationFile(fn, fileNum.ToString());
                }

                if (isCustomAllocLoaded) { _CustomAllocation(allocFile, "2000P985"); }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }

            fillList.Clear();
            log.WriteLog("fillList Cleared");
            fillQty = mdrQty2;      
        }

        /// <summary>
        /// standard allocation row
        /// </summary>
        /// <param name="acct"></param>
        /// <param name="bs"></param>
        /// <param name="qty"></param>
        /// <param name="prc"></param>
        /// <returns></returns> string array containing a row of allocation data
        private string[] _allocRow(string acct, string bs, string qty, string prc)
        {
            return new string[] { _tradeDate("MM/dd/yy"), bs, qty, _monthCode(), ttProduct, prc, acct };
        }

        /// <summary>
        /// Process custom allocation
        /// </summary>
        /// <param name="allocFile"></param>
        /// <param name="acct"></param>
        private void _CustomAllocation(ArrayList allocFile, string acct)
        {
            ArrayList tradelist = new ArrayList();
            ArrayList TemplarList = new ArrayList();

            foreach (string[] row in allocFile)
            {
                if (string.Equals(row[6], acct, StringComparison.OrdinalIgnoreCase))
                {
                    tradelist.Add(row);
                }
            }

             foreach (string[] row in tradelist)
             {
                 TemplarList.Add(_CustomAllocRow(row));
             }


            try
            {
                string fn = Application.StartupPath.ToString() + @"..\..\_allocCustom";

                string[] files = System.IO.Directory.GetFiles(fn);
                int fileNum = files.GetLength(0) + 1;

                fn += @"\" + string.Format("{0}_{1}{2}.csv",acct, _tradeDate("yyyyMMdd"),DateTime.Now.ToString("HHmm"));

                StreamWriter sw = new StreamWriter(fn, true, Encoding.ASCII, 100);

                string colHeadings = null;
                foreach (DataColumn col in setCustom.Tables["templar"].Columns )
                {
                    colHeadings += col.ColumnName;
                    if (col.Ordinal != setCustom.Tables["templar"].Columns.Count - 1)
                    { colHeadings += ","; }
                }
                sw.WriteLine(colHeadings);

                foreach (string[] row in TemplarList)
                {
                    string allocationRow = null;
                    for (int i = 0; i <= row.GetUpperBound(0); i++)
                    {
                        allocationRow += row.GetValue(i).ToString();
                        if (i != row.GetUpperBound(0)) { allocationRow += ","; }
                    }
                    log.LogListBox(allocationRow);
                    sw.WriteLine(allocationRow);
                }
                sw.Close();

                if (isAutoSendAllocationsOn)
                {
                    _SendCustomAllocationFile(fn, fileNum.ToString());
                }




            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
            
        }

        /// <summary>
        /// create custom allocation row data
        /// </summary>
        /// <param name="stdAllocdata"></param>
        /// <returns></returns>
        private string[] _CustomAllocRow(string[] stdAllocdata)
        {
            try
            {
                DataRow templarData = setCustom.Tables["templar"].Rows[0];

                string cMonth = _monthCode().Substring(0, 1);
                string cYear = _monthCode().Substring(1, 1);
                DateTime exp = Convert.ToDateTime(ttInstrObj.get_Get("ExpirationDate"));
                string maturityDate = exp.ToString("yyyyMMdd");
                string cur = ttInstrObj.get_Get("Currency").ToString();

                return new string[] { _tradeDate("yyyyMMdd"),
                _tradeDate("yyyyMMdd"),
                templarData["ContractName"].ToString(),
                stdAllocdata[1].ToString(),
                stdAllocdata[2].ToString(),
                Convert.ToString(Convert.ToDecimal(stdAllocdata[5]) * CustomAllocMultiplier),
                templarData["Security"].ToString(),
                templarData["BloombergTK"].ToString(),
                cMonth,cYear,
                maturityDate,
                string.Empty,
                string.Empty,
                cur,
                cur,
                templarData["BloombergTK"].ToString() + _monthCode() + " "+templarData["BloombergSymbol"].ToString(),
                templarData["ClearingAcct"].ToString(),
                templarData["Clearingbroker"].ToString(),
                templarData["ExecAcct"].ToString(),
                templarData["ExecBroker"].ToString(),
                templarData["Counterparty-Exchange"].ToString() };

            }
            catch (Exception ex)
            {
                log.LogListBox(ex.ToString());
                return new string[] { "error" };
            }

        }

        /// <summary>
        /// hold/activate button code
        /// </summary>
        private void _button_HoldNoHold()
        {
            try
            {
                if (ttOrderSet.Count <= 0)
                {
                    MessageBox.Show(this, "There are no orders in the TTOrderSet to modify!",ttContract );
                    return;
                }

                if (siteOrderKey != null)
                {
                    TTOrderObj tmpOO = (TTOrderObj)ttOrderSet.get_SiteKeyLookup(siteOrderKey);
                    TTOrderProfile tmpOP = tmpOO.CreateOrderProfile;

                    int onHold = (int)tmpOP.get_Get("OnHold");

                    if (onHold != 0)
                    { 
                        ttOrderSet.HoldOrder(tmpOO, 0);
                        _MakeNoise(@"C:\tt\sounds\laser.wav");
                    }
                    else
                    { ttOrderSet.HoldOrder(tmpOO, 1); }
                }
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        /// <summary>
        /// shutoff application
        /// </summary>
        private void _shutdown()
        {
            isOn = false;
            log.WriteLog("isON = " + isOn.ToString());

            btnOff.BackColor = System.Drawing.Color.Red;
            btnON.BackColor = System.Drawing.Color.FromKnownColor(KnownColor.Control);
            button4.Text = string.Empty;
            button4.BackColor = System.Drawing.Color.FromKnownColor(KnownColor.Control);
            button4.Enabled = false;

            if (AlertTimer.Enabled) { AlertTimer.Stop(); }
            if (ServerTimer.Enabled) { ServerTimer.Stop(); }

            switch (algo)
            {
                case "syntheticSL":
                    this.listBox1.BackColor = Color.Yellow;
                    break;
                case "nativeSL":
                    // do nothing
                case "stopMarket":
                    // do nothing
                default:
                    break;
            }

            _DeleteOrders();

            if (MarketTimer.Enabled) { MarketTimer.Stop(); }
            siteOrderKey = null;
            this.listBox1.BackColor = Color.LightGray;
        }

        /// <summary>
        /// Delete All Orders for contract
        /// </summary>
        private void _DeleteOrders()
        {
            try
            {
                log.WriteLog("Delete Orders");
                // Delete all of the Buy orders.
                ttOrderSet.get_DeleteOrders(1, null, null, 0, null);
                // Delete all of the Sell orders.
                ttOrderSet.get_DeleteOrders(0, null, null, 0, null);
            }
            catch (Exception ex)
            {
                log.LogListBox("ERROR: Orders may not have been deleted");
                log.LogListBox(ex.ToString());
            }
        }

        /// <summary>
        /// Annotate log file for open last comparison 
        /// </summary>
        /// <param name="open"></param>
        /// <param name="last"></param>
        private void _compareOpenLast(decimal open, decimal last)
        {
            if (isOpenDelivered && !isOpenComparedToLast)
            {
                log.LogListBox("COMPARE OPEN & First Print");
                log.LogListBox("OPEN: " + open.ToString());
                log.LogListBox("LAST: " + last.ToString());

                if ((Math.Abs(open - last) / oneTick) <= 10)
                {
                    log.LogListBox("Open within tolerance of first print");
                    isOpenComparedToLast = true;
                }
                else
                {
                    msgAlert = "ALERT: Open/Last Discrepancy greater then tolerance";
                    log.LogListBox(msgAlert);
                    //if (bAlertOn) { AlertTimer.Start(); }
                    isOpenComparedToLast = true;
                }
            }
        }
        
        /// <summary>
        /// verify current time against trding time frame
        /// </summary>
        /// <returns></returns> true if time is between start time and stop time
        private bool _isValidTradingTime()
        {
            // returns true during market trading hours
            if (startTime < stopTime)
            {
                if (DateTime.Now.TimeOfDay >= startTime && DateTime.Now.TimeOfDay < stopTime)
                { return true; }
                else
                { return false; }
            }
            else
            {
                if (DateTime.Now.TimeOfDay >= startTime || DateTime.Now.TimeOfDay < stopTime)
                { return true; }
                else
                { return false; }
            }
        }

        /// <summary>
        /// check if out time is post market
        /// </summary>
        /// <returns></returns> true if trading has ended for the day
        private bool _isPostMarket()
        {
            if (string.Equals(_tradeDate("MMddyy"), DateTime.Now.Date.ToString("MMddyy"), StringComparison.OrdinalIgnoreCase) &&
                DateTime.Now.TimeOfDay > stopTime)
            { return true; }
            else
            { return false; }
        }


        /// <summary>
        /// Trade date
        /// </summary>
        /// <param name="format">Representative date format</param>
        /// <returns></returns> returns trade date in custom string format
        private string _tradeDate(string format)
        {
            if (DateTime.UtcNow.TimeOfDay > TimeSpan.Parse("22:05:00"))
            { return DateTime.UtcNow.Date.AddDays(1).ToString(format); }            
            else
            { return DateTime.UtcNow.Date.ToString(format); }
        }

        /// <summary>
        /// Futures Month Code and one-digit year
        /// </summary>
        /// <returns></returns> one letter month and one digit year
        private string _monthCode()
        {
            string monthCode = null;
            string exp = ttInstrObj.get_Get("Contract").ToString();
            exp = exp.Substring(exp.Trim().Length - 5);

            DateTime parsed = DateTime.ParseExact(exp, "MMMyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));

            int m = parsed.Month;
            int y = parsed.Year % 10;

            switch (m)
            {
                case 1:
                    monthCode = "F";
                    break;
                case 2:
                    monthCode = "G";
                    break;
                case 3:
                    monthCode = "H";
                    break;
                case 4:
                    monthCode = "J";
                    break;
                case 5:
                    monthCode = "K";
                    break;
                case 6:
                    monthCode = "M";
                    break;
                case 7:
                    monthCode = "N";
                    break;
                case 8:
                    monthCode = "Q";
                    break;
                case 9:
                    monthCode = "U";
                    break;
                case 10:
                    monthCode = "V";
                    break;
                case 11:
                    monthCode = "X";
                    break;
                case 12:
                    monthCode = "Z";
                    break;
                default:
                    log.LogListBox("MonthCode Error: month is " + m.ToString());
                    break;
            }

            monthCode += y.ToString();

            return monthCode;
        }

        //not used
        private string _FutureExpirationCode(int index)
        {
            string value = string.Empty;
            string MonthCode = string.Empty;
            int yearValue = 0;
            int i = 0;

            yearValue = DateTime.Today.Year + Convert.ToInt32(index / 12);
            i = index % 12;
            switch (i)
            {
                case 1:
                    MonthCode = "JAN";
                    break;
                case 2:
                    MonthCode = "FEB";
                    break;
                case 3:
                    MonthCode = "MAR";
                    break;
                case 4:
                    MonthCode = "APR";
                    break;
                case 5:
                    MonthCode = "MAY";
                    break;
                case 6:
                    MonthCode = "JUN";
                    break;
                case 7:
                    MonthCode = "JUL";
                    break;
                case 8:
                    MonthCode = "AUG";
                    break;
                case 9:
                    MonthCode = "SEP";
                    break;
                case 10:
                    MonthCode = "OCT";
                    break;
                case 11:
                    MonthCode = "NOV";
                    break;
                case 0:
                    MonthCode = "DEC";
                    yearValue = yearValue - 1;
                        break;
             
                default:
                    break;
            }     
            return MonthCode + yearValue.ToString().Substring(2);
        }

        /// <summary>
        /// Launch Roll Contract form
        /// </summary>
        /// <param name="increment"></param>
        /// <param name="decimals"></param>
        private void _RollContract(decimal increment, int decimals)
        {
            try
            {
                DataRow rollUpdate = setMDRData.Tables[0].Rows[0];
                rollUpdate.BeginEdit();
                rollUpdate[3] = ttContract;
                rollUpdate.AcceptChanges();
            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }

            Form2_Roll frm = new Form2_Roll();

            frm.mdrUpdate = setMDRData;

            frm.inc = increment;
            frm.places = decimals;
            frm.x = this.DesktopLocation.X + 30;
            frm.y = this.DesktopLocation.Y + 100;
            frm.mainForm = this;
            frm.Show();

        }

        /// <summary>
        /// TTGate OnExchangeStateUpdate event handler
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="text"></param>
        /// <param name="openned"></param>
        /// <param name="serverUp"></param>
        private void _gatewayStatusUpdate(string exchange, string text, int openned, int serverUp)
        {
            //if (String.Equals(exchange, ttGateway, StringComparison.OrdinalIgnoreCase))
            if (String.Equals(exchange.Substring(0, ttGateway.Length), ttGateway, StringComparison.OrdinalIgnoreCase))
            {
                log.LogListBox("Process Gateway Status Update");

                if (text == "Price")
                {
                    if (openned == 0 && serverUp == 0)
                    {
                        isPriceServerUP = false;
                        log.LogListBox(ttGateway + " Price Server EXISTS");
                    }

                    if (openned == 1 && serverUp == 0)
                    {
                        isPriceServerUP = false;
                        log.LogListBox(ttGateway + " Price Server DOWN");
                    }

                    if (openned == 1 && serverUp == 1)
                    {
                        isPriceServerUP = true;
                        log.LogListBox(ttGateway + " Price Server UP");
                    }
                }
                else if (text == "Price (Downloading)")
                {
                    isPriceServerUP = false;
                    log.LogListBox("Price Server Downloading");
                }

                if (text == "Order")
                {
                    if (openned == 0 && serverUp == 0)
                    {
                        isOrderServerUP = false;
                        log.LogListBox(ttGateway + " Order Server EXISTS");
                    }

                    if (openned == 1 && serverUp == 0)
                    {
                        isOrderServerUP = false;
                        log.LogListBox(ttGateway + " Order Server DOWN");
                    }

                    if (openned == 1 && serverUp == 1)
                    {
                        isOrderServerUP = true;
                        log.LogListBox(ttGateway + " Order Server UP");
                    }
                }
                else if (text == "Order (Downloading)")
                {
                    isOrderServerUP = false;
                    log.LogListBox(ttGateway + " Order Server Downloading");
                }

                if (text == "Fill")
                {
                    if (openned == 0 && serverUp == 0)
                    {
                        isFillServerUP = false;
                        log.LogListBox(ttGateway + " Fill Server EXISTS");
                    }

                    if (openned == 1 && serverUp == 0)
                    {
                        isFillServerUP = false;
                        log.LogListBox(ttGateway + " Fill Server DOWN");
                    }

                    if (openned == 1 && serverUp == 1)
                    {
                        isFillServerUP = true;
                        log.LogListBox(ttGateway + " Fill Server UP");
                    }
                }
                else if (text == "Fill (Downloading)")
                {
                    isFillServerUP = false;
                    log.LogListBox(ttGateway + " Fill Server Downloading");
                }
            }

            string fn = Application.StartupPath.ToString() + @"\..\_alerts\SERVER";

            if (isFillServerUP && isOrderServerUP && isPriceServerUP)
            {
                log.LogListBox("All Servers Operational");
                if (!isContractFound) { _connectInstr(); }
                if (ServerTimer.Enabled)
                {
                    ServerTimer.Stop();

                    //Color tmp = this.listBox1.BackColor; 
                    this.listBox1.BackColor = lstColor;
                    //lstColor = tmp;

                    log.LogListBox("Server Alert Reset");
                }

                try
                {
                    if (File.Exists(fn))
                    {
                        File.Delete(fn);
                        log.LogListBox("server alert deleted");
                    }
                    else
                    { log.LogListBox("No server alerts found"); }
                }
                catch (Exception ex)
                { log.LogListBox(ex.ToString()); }
            }
            else
            {
                if (_isValidTradingTime())
                {
                    log.LogListBox("SERVER DOWN!!");
                    log.LogListBox("Fill Server: " + isFillServerUP.ToString());
                    log.LogListBox("Order Server: " + isOrderServerUP.ToString());
                    log.LogListBox("Price Server: " + isPriceServerUP.ToString());
                    msgAlert = "ALERT: SERVER DOWN";

                    try
                    {
                        if (!File.Exists(fn))
                        {
                            lstColor = this.listBox1.BackColor;
                            ServerTimer.Interval = 60000;
                            ServerTimer.Start();
                            log.LogListBox("ServerTimer.Interval: " + ServerTimer.Interval.ToString());
                            log.LogListBox(msgAlert);
                            log.LogListBox("Server Alert Timer started");
                            StreamWriter sw = new StreamWriter(fn);
                            sw.WriteLine(DateTime.Now.ToLongTimeString());
                            sw.Close();
                        }
                        else
                        { log.LogListBox("Alert already triggered"); }
                    }
                    catch (Exception ex)
                    { log.LogListBox(ex.ToString()); }
                }
            }
        }

        /// <summary>
        /// Add trade summary to allocation email
        /// </summary>
        private void _CreateEmailBody()
        {
            decimal p = 0;
            int q = 0;
            foreach (KeyValuePair<decimal, int> kvp in fillList)
            {
                q += kvp.Value;
                p += kvp.Key * kvp.Value;
            }

            msgBody = string.Format("{0} {1} {2} {3:F} \r\n", buySell, tradeQty, ttProduct, p / q);
            if (buySell == "S")
            {
                msgBody += string.Format("s: {0:F}", (p / q) - stopPrice);
            }
            else
            {
                msgBody += string.Format("s: {0:F}", stopPrice - (p / q));
            }
        }

        /// <summary>
        /// After full Fill, setup next trade
        /// </summary>
        private void _SetupNextOrder()
        {
            ordNum++;
            fillQty = mdrQty2;
            log.LogListBox(string.Format("Order Number {0} is active", ordNum));
            _queueCurrentOrder();
            //_saveCurrentDaySetting("OrdNum", ordNum);
            _saveCurrentDaySettingINI();
            isLimitOrderSent = false;
        }

        //SEE XTAPI DOCUMENTATION of Event Information 

        private void TTDropHandler_OnNotifyDrop()
        {
            // Update the Status Bar text.
            log.WriteLog("m_TTDropHandler_OnNotifyDrop");
            try
            {
                TTInstrObj dropInstr = (XTAPI.TTInstrObj)ttDropHandler[1];

                if (!string.Equals(dropInstr.Product, ttProduct))
                {
                    MessageBox.Show(string.Format("Dropped: {0}\r\nContract does not match existing product",dropInstr.Product),"Existing Product: "+ttProduct);
                    ttDropHandler.Reset();
                    return;
                }
               
                DialogResult dr = MessageBox.Show("New Contract Dropped!\r\nDo you want to roll this contract?", dropInstr.Product +"Contract dropped", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    ttDropHandler.Reset();
                    return; 
                }
                else
                { isRollContractDropped = true; }

                // Test if a TTInstrObj currently exists.
                if (ttInstrObj != null)
                {
                    // Detach previously attached instrument.
                    ttInstrNotify.DetachInstrument(ttInstrObj);
                    ttInstrNotifyLast.DetachInstrument(ttInstrObj);
                    ttInstrNotifyOpen.DetachInstrument(ttInstrObj);
                }

                // Obtain the TTInstrObj from the TTDropHandler object.
                ttInstrObj = (XTAPI.TTInstrObj)ttDropHandler[1];

                // Attach the TTInstrObj to the TTInstrNotify for price update events.
                ttInstrNotify.AttachInstrument(ttInstrObj);
                ttInstrNotifyLast.AttachInstrument(ttInstrObj);
                ttInstrNotifyOpen.AttachInstrument(ttInstrObj);

                // Open the TTInstrObj.
                ttInstrObj.Open(1);	// enable Market Depth:  1 - true, 0 - false

                // Clear drop handler list.
                ttDropHandler.Reset();
            }
            catch (Exception ex)
            {
                // Display exception message.
                log.LogListBox(ex.ToString());
            }
        }

        private void TTInstrNotify_OnNotifyFound(XTAPI.TTInstrNotify pNotify, XTAPI.TTInstrObj pInstr)
        {
            isContractFound = true;

            log.WriteLog("m_TTInstrNotify_OnNotifyFound");
            try
            {
                ttInstrObj = pInstr;

                ttGateway = ttInstrObj.Exchange;
                ttProduct = ttInstrObj.Product;
                ttProductType = ttInstrObj.ProdType;
                ttContract = ttInstrObj.Contract;
            }
            catch (Exception e)
            { log.LogListBox(e.ToString()); }

            this.Text = ttGateway + " " + ttContract + " - " + contractDescription;

            try
            {
                if (isExchangeOpenUsed)
                {
                    if (!isRollContractDropped)
                    {
                        open = Convert.ToDecimal(ttInstrObj.get_Get("Open#"));
                        //orders are calced and queued when nudOpen value is changed
                        nudOpen.Value = open;
                        log.LogListBox("OPEN: " + open.ToString());
                    }
                }

                oneTick = Convert.ToDecimal(ttInstrObj.get_TickPriceEx(0, XTAPI.enumRoundPriceType.ROUND_NONE, 1, "#"));
                nudOpen.Increment = oneTick;
                log.WriteLog("Increment: " + oneTick.ToString());

                string tick = oneTick.ToString();
                int period = tick.IndexOf(".");
                log.WriteLog("Decimal Point IndexOf: " + period.ToString());
                
                if (period == -1)
                { nudOpen.DecimalPlaces = 0; }
                else
                { 
                    tick = tick.Substring(period + 1);
                    nudOpen.DecimalPlaces = tick.Length;
                }
                log.WriteLog("Decimal Places: " + nudOpen.DecimalPlaces.ToString());
                log.WriteLog("trimmed tick: " + tick);

                if (currentAlgo == Algorithm.stopMarket)
                {
                    nudOffSet.Increment = nudOpen.Increment;
                    nudOffSet.DecimalPlaces = nudOpen.DecimalPlaces;
                    nudOffSet.Minimum = 5 * oneTick;
                }
                else if (currentAlgo == Algorithm.nativeSL )
                { 
                    nudOffSet.Minimum = 0;
                    nudOffSet.DecimalPlaces  = 0;
                    nudOffSet.Increment = 1;
                }
                else if (currentAlgo == Algorithm.syntheticSL)
                { 
                    nudOffSet.Minimum = -5;
                    nudOffSet.DecimalPlaces = 0;
                    nudOffSet.Increment = 1;
                }

            }
            catch (Exception e)
            { log.LogListBox(e.ToString()); }

            try
            {
                if (isTradingLimitDisabled)
                { 
                    ttOrderSet.Set("NetLimits", 0);
                    log.LogListBox("Limits Disabled");
                }
                else
                {
                    log.LogListBox("MaxPosition: " + Math.Max(mdrQty1, mdrQty2).ToString());

                    //set max position to more than a single order but less than 2 times the smaller order
                    //this allows for adjusting trades in the same direction as the first trade of the day
                    //but guards against more than one order being sent to the market in the same direction
                    ttOrderSet.Set("MaxPosition", Math.Max(Math.Max(mdrQty1, mdrQty2),Math.Min(mdrQty1 ,mdrQty2)*1.5));
                    ttOrderSet.Set("MaxOrderQty", Math.Max(mdrQty1, mdrQty2));
                    //TODO: set to 2 to enable submitting held orders, only one needed for application
                    //PCR Opened by devnet
                    ttOrderSet.Set("MaxOrders", 2);
                    ttOrderSet.Set("MaxWorking", Math.Max(mdrQty1, mdrQty2));
                    ttOrderSet.Set("ThrowLimitErrors", 1);
                }

                ttOrderSet.EnableOrderSend = 1;
                ttOrderSet.EnableOrderFillData = 1;
                ttOrderSet.EnableOrderSetUpdates = 1;
                ttOrderSet.EnableOrderRejectData = 1;

                //add filters
                //XTAPI.TTOrderSelector ttOrderSelector = new XTAPI.TTOrderSelectorClass();
                //ttOrderSelector.Reset();
                //ttOrderSelector.AllMatchesRequired = 1;
                //ttOrderSelector.AllowAnyMatches = 0;
                //ttOrderSelector.AddTest("Instr.Exchange", ttGateway);
                //ttOrderSelector.AddTest("Contract$", ttContract);
                //use FFT2 filter or assume no other trades were made?? NO
                //Filter not used to allow alocation of manual trades in teh event that a limt order is hung
                //ttOrderSelector.AddTest("FFT2", "AUTOX");

                ttOrderSet.OrderSelector = pInstr.CreateOrderSelector;
                //open orderset with send orders enabled
                ttOrderSet.Open(1);
            }
            catch (Exception e)
            {
                log.LogListBox(e.ToString());
            }

            log.LogListBox(string.Format("LOADED: {0} {1} TickSize: {2}", ttGateway, ttContract, oneTick));

            log.WriteLog("setup.Columns.Count: " + tblSetup.Columns.Count.ToString());

            //If application has been run already today open price must be saved and set - recalc orders on loading
            if (open != 0)
            {
                _calcOrders();
                _queueCurrentOrder();
            }

            if (isRollContractDropped)
            {
                _RollContract(nudOpen.Increment , nudOpen.DecimalPlaces); 
            }
        }

        private void TTInstrNotify_OnNotifyNotFound(TTInstrNotify pNotify, TTInstrObj pInstr)
        {
            log.WriteLog("m_TTInstrNotify_OnNotifyNotFound");
            log.LogListBox(pInstr.Contract + " NOT FOUND");
            msgAlert = "ALERT: " + pInstr.Contract + " NOT FOUND";
            log.LogListBox(msgAlert);
            AlertTimer.Interval = 100;
            AlertTimer.Start();
        }

        //m_TTInstrNotify_OnNotifyDepthData disabled
        //private void m_TTInstrNotify_OnNotifyDepthData(XTAPI.TTInstrNotify pNotify, XTAPI.TTInstrObj pInstr)
        //{
        //    log.logListBox("m_TTInstrNotify_OnNotifyDepthData called");

        //    // Obtain the bid depth array
        //    try
        //    {
        //        aBidDepth = (Array)pInstr.get_Get("BidDepth(0)");
        //        //r.BP1 = Convert.ToDecimal(aBidDepth.GetValue(0, 0)); // = BestBidPrice
        //        //r.BQ1 = Convert.ToDecimal(aBidDepth.GetValue(0, 1)); // = BestBidQty
        //        //r.BP2 = Convert.ToDecimal(aBidDepth.GetValue(1, 0)); // = 2nd Level BidPrice
        //        //r.BQ2 = Convert.ToDecimal(aBidDepth.GetValue(1, 1)); // = 2nd Level BidQty
        //        // Obtain the ask depth array
        //        aAskDepth = (Array)pInstr.get_Get("AskDepth(0)");
        //        //r.AP1 = Convert.ToDecimal(aAskDepth.GetValue(0, 0)); // = BestAskPrice
        //        //r.AQ1 = Convert.ToDecimal(aAskDepth.GetValue(0, 1)); // = BestAskQty
        //        //r.AP2 = Convert.ToDecimal(aAskDepth.GetValue(1, 0)); // = 2nd Level AskPrice
        //        //r.AQ2 = Convert.ToDecimal(aAskDepth.GetValue(1, 1)); // = 2nd Level AskQty

        //         depthRecords.writeLog(string.Format("Total Mkt Size, {0},x,{1}",
        //            _addDepth(aBidDepth).ToString(), _addDepth(aAskDepth).ToString()));

        //    }
        //    catch (Exception e)
        //    {
        //        log.logListBox("DATAFAIL: " + e.ToString());
        //    }

        //}

        // these two functions are used in conjunction with disabled OnDepthpdate
        //private int _addDepth(Array a)
        //{
        //    int sum = 0;

        //    if (a != null)
        //    {
        //        // Iterate through the depth array.
        //        for (int i = 0; i <= a.GetUpperBound(0); i++)
        //        {
        //            sum += (int)a.GetValue(i, 1);
        //        }
        //    }
        //    return sum;
        //}

        //private decimal _avgPrice(Array a)
        //{
        //    decimal sum = 0;
        //    int qty = 0;

        //    if (a != null)
        //    {
        //        // Iterate through the depth array.
        //        for (int i = 0; i <= a.GetUpperBound(0); i++)
        //        {
        //            sum += (decimal)a.GetValue(i, 0) * (int)a.GetValue(i, 1);
        //            qty += (int)a.GetValue(i, 1);
        //        }
        //    }
        //    return sum / qty;
        //}

        private void TTInstrNotifyOpen_OnNotifyUpdate(TTInstrNotify pNotify, TTInstrObj pInstr)
        {
            log.WriteLog("m_TTInstrNotifyOpen_OnNotifyUpdate");          
            try
            {
                if (!chkBxOpen.Checked)
                {
                    if (isPriceServerUP)
                    {
                        priorOpen = open;
                        open = Convert.ToDecimal(pInstr.get_Get("Open#"));

                        if (open > 0)
                        {
                            if (!isOpenDelivered)
                            {
                                log.LogListBox("First OPEN Notify: " + open.ToString());
                                isOpenDelivered = true;
                            }
                            else
                            {
                                if (open != priorOpen)
                                { log.LogListBox(string.Format("Prior Open: {0} Updated Open: {1}", priorOpen, open)); }
                            }
                            //changing nudOpen.Value calls calc & queue
                            nudOpen.Value = open;
                        }
                        else
                        { log.LogListBox("ERROR: Open <= 0"); }
                    }
                    else
                    { log.LogListBox("PRICE SERVER DOWN"); }
                }
                else
                { log.LogListBox("OPEN PRICE NOTIFY: NO CHANGE MADE 'Set Open' checked"); }
            }
            catch (Exception ex)
            {
                log.LogListBox(ex.ToString());
            }
        }

        private void TTInstrNotifyLast_OnNotifyUpdate(TTInstrNotify pNotify, TTInstrObj pInstr)
        {
            //log.WriteLog("m_TTInstrNotifyLast_OnNotifyUpdate");
            _captureOpen();
            
            try
            {
                if (isPriceServerUP && isOrderServerUP )
                {
                    Array get = (Array)pInstr.get_Get("Last#,LastQTY");
                    last = Convert.ToDecimal(get.GetValue(0));
                    lstQty = Convert.ToInt32(get.GetValue(1));

                    if (isOn)
                    {
                        if (isExchangeOpenUsed) { _compareOpenLast(open, last); }

                        if (_isValidTradingTime() && isOrderQueued) 
                        {
                            if (lstQty != 0) { _chkSubmitOrder(last); }
                        }
                    }
                    else
                    {
                        prc.LogListBox(string.Format(
                            "LAST: {0} QTY: {1} Distance: {2}",
                            ttInstrObj.get_TickPrice(last, 0, "$"),
                            lstQty, 
                            _distance()));
                    }
                }
            }                     
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        private void TTGate_OnExchangeStateUpdate(string exchange, string text, int openned, int serverUp)
        {
            //if (String.Equals(exchange, ttGateway, StringComparison.OrdinalIgnoreCase))
            if (exchange.Length >= ttGateway.Length && String.Equals(exchange.Substring(0, ttGateway.Length), ttGateway, StringComparison.OrdinalIgnoreCase))
            {
                log.WriteLog("TTGate_OnExchangeStateUpdate");
                _gatewayStatusUpdate(exchange, text, openned, serverUp);
            }
        }
 
        private void TTGate_OnStatusUpdate(int hintMask, string text)
        {
            switch (hintMask)
            {
                case 1:
                    log.LogListBox("STATUS: X_TRADER is connected");
                    break;
                case 32:
                    log.LogListBox("STATUS: X_TRADER PRO is Available");
                    break;
                case 8:
                    log.LogListBox("STATUS: Customer Defaults updated");
                    _ChkCustomers();
                    break;
                default:
                    log.WriteLog("STATUS: " + hintMask.ToString());
                    break;
            }
        }

        private void TTOrderSet_OnOrderFillData(XTAPI.TTFillObj pFillObj)
        {
            log.WriteLog("OnOrderFillData: " + isOn.ToString());
           
            if (isOn)
            try
            {
                if (this.WindowState != FormWindowState.Normal) { this.WindowState = FormWindowState.Normal; }
                // Retrieve the fill information using the TTFillObj Get Properties.
                Array afillData = (Array)pFillObj.get_Get("SiteOrderKey,Price#,Qty,IsStop,FillType,Partial,FFT2,BuySell");
                //                                   0            1     2   3      4        5        6    7        
                //log.logListBox("FillType: " + afillData.GetValue(4).ToString());
                //log.logListBox("PARTIAL: " + afillData.GetValue(5).ToString());

                //string sok = afillData.GetValue(0).ToString();
                //bool isStop = Convert.ToBoolean(afillData.GetValue(3));
                decimal prc = Convert.ToDecimal(afillData.GetValue(1));
                int qty = Convert.ToInt32(afillData.GetValue(2));
                string ftype = afillData.GetValue(4).ToString();
                string bs = afillData.GetValue(7).ToString();

                if (fillList.ContainsKey(prc))
                { fillList[prc] += qty; }
                else
                { fillList[prc] = qty;  }

                fillCount += qty;
                log.LogListBox("fillCount: " + fillCount.ToString());

                if (fillCount == fillQty)  
                {
                    log.LogListBox("FULL FILL");
                    _MakeNoise(@"C:\tt\sounds\cashreg.wav");

                    if (AlertTimer.Enabled)
                    {
                        this.listBox1.BackColor = lstColor;
                        AlertTimer.Stop();
                        log.WriteLog("AlertTimer stopped");
                    }

                    isOrderHung = false;
                    if (hungTimer.Enabled) 
                    { 
                        hungTimer.Stop();
                        log.WriteLog("hungTimer stopped");
                    }

                    fillCount = 0;
                    siteOrderKey = null;
                    _CreateEmailBody();
                    _SetupNextOrder();
                    _setupAllocation(bs);
                }

                if (fillCount > fillQty)
                {
                    log.LogListBox("Target quantity was overshot");
                    log.LogListBox("No Allocation file will be created");
                    msgAlert = "ALERT: Target quantity was overshot";
                    _MakeNoise(@"C:\tt\sounds\glass.wav"); 
                }


            }
            catch (Exception ex)
            { log.LogListBox(ex.ToString()); }
        }

        private void TTOrderSet_OnOrderRejected(XTAPI.TTOrderObj pOrderObj)
        {
            log.WriteLog("m_TTOrderSet_OnOrderRejected");

            try
            {
                Array data = (Array)pOrderObj.get_Get("SiteOrderKey,Msg");
                string sok = data.GetValue(0).ToString();
                string msg = data.GetValue(1).ToString();

                log.LogListBox("REJECTED SOK: " + (sok == null ? "null" : sok));
                log.LogListBox(msg);

                _ALGO_OnOrderRejected(sok, msg);
            }
            catch (Exception ex)
            { log.LogListBox("sok Exception" + ex.ToString()); }

        }

        private void TTOrderSet_OnOrderSetUpdate(XTAPI.TTOrderSet pOrderSet)
        {
            log.WriteLog("TTOrderSet_OnOrderSetUpdate");
            try
            {
                Array data = (Array)pOrderSet.get_Get("NetPos,NetCnt");
                                                        //0      1   2   

                int pos = Convert.ToInt32(data.GetValue(0)) + (longShort == "S" ? -startPosition : startPosition);

                log.WriteLog("POS: " + pos.ToString()+" WrkOrds: " + data.GetValue(1).ToString());

                _ALGO_OnOrderSetUpdate();

                //_verifyOrderAtExchange(pOrderSet);  //deprecated
            }
            catch (Exception ex)
            { 
                log.LogListBox(ex.ToString()); 
            }
        }


        //deprecated - TTOrderTracker, if needed for future algorithm.
        //private void _verifyOrderAtExchange(XTAPI.TTOrderSet pOrderSet)
        //{
        //    XTAPI.TTOrderObj tempOldOrder;
        //    XTAPI.TTOrderObj tempNewOrder;
        //    XTAPI.TTOrderTrackerObj tempOrderTrackerObj;
        //    Array tempOrderData;

        //    log.WriteLog("_verifyOrderAtExchange");

        //    // Obtain the Next TTOrderTrackerObj object from the TTOrderSet.
        //    tempOrderTrackerObj = pOrderSet.NextOrderTracker;

        //    // Iterate through the list of TTOrderTrackerObj objects.
        //    while( tempOrderTrackerObj != null )
        //    {
        //        log.WriteLog( "TTOrderTrackerObj");

        //        // Test if an Old Order (past state) exists.
        //        if( tempOrderTrackerObj.HasOldOrder != 0 )
        //        {
        //            log.LogListBox("Previous Order State: ");

        //            // Obtain the TTOrderObj from the TTOrderTrackerObj.
        //            tempOldOrder = tempOrderTrackerObj.OldOrder;
        //            // Retrieve TTOrderObj information.
        //            tempOrderData = (Array)tempOldOrder.get_Get("OrdStatus,OrdAction,SiteOrderKey,ExOrderID,OrderNo,ExecutionType");

        //            // Display the information on the interface.

        //            log.LogListBox(string.Format("Action: {0} Status: {1} SOK:{2} EX:{3}",
        //                tempOrderData.GetValue(1), tempOrderData.GetValue(0), tempOrderData.GetValue(2), tempOrderData.GetValue(3)));

        //        }
        //        else
        //        {
        //            log.LogListBox("Previous Order State: NULL");
        //        }

        //        // Test if an New Order (current state) exists.
        //        if( tempOrderTrackerObj.HasNewOrder != 0 )
        //        {
        //            log.LogListBox("New Order State: ");

        //            // Obtain the TTOrderObj from the TTOrderTrackerObj.
        //            tempNewOrder = tempOrderTrackerObj.NewOrder;
        //            // Retrieve TTOrderObj information.
        //            tempOrderData = (Array)tempNewOrder.get_Get("OrdStatus,OrdAction,SiteOrderKey,ExOrderID,OrderNo,ExecutionType");
        //            log.LogListBox(string.Format("Action: {0} Status: {1} SOK:{2} EX:{3}",
        //                tempOrderData.GetValue(1), tempOrderData.GetValue(0), tempOrderData.GetValue(2), tempOrderData.GetValue(3)));

        //        }
        //        else
        //        {
        //            log.LogListBox("New Order State: NULL");
        //        }
        //        log.LogListBox("--------------------");
        //        // Obtain the Next TTOrderTrackerObj object from the TTOrderSet.
        //        tempOrderTrackerObj = pOrderSet.NextOrderTracker;
        //    }
		
        //}

        /// <summary>
        /// Algorithm handler for OnOrderSetUpdate Event
        /// </summary>
        private void _ALGO_OnOrderSetUpdate()
        {
            switch (algo)
            {
                case "syntheticSL":
                   //do nothing
                    break;
                case "nativeSL":
                    _ordersetUpdate_HoldNoHold(true);
                    break;
                case "stopMarket":
                    _ordersetUpdate_HoldNoHold(false);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Monitor hold status of order adn display on interface
        /// </summary>
        /// <param name="AllowManualHold"></param>
        private void _ordersetUpdate_HoldNoHold(bool AllowManualHold)
        {
            if (siteOrderKey != null)
            {
                int onHold = -1;
                try
                {
                    TTOrderObj tmpOO = (TTOrderObj)ttOrderSet.get_SiteKeyLookup(siteOrderKey);
                    TTOrderProfile tmpOP = tmpOO.CreateOrderProfile;

                    onHold = (int)tmpOP.get_Get("OnHold");

                }
                catch (Exception ex)
                { log.LogListBox(ex.ToString()); }

                if (onHold != 0)
                {
                    if (AllowManualHold)
                    {
                        button4.Text = "Activate";
                        button4.BackColor = Color.Green;
                    }
                    log.LogListBox("Order on Hold");
                    //lstColor = this.listBox1.BackColor;
                    this.listBox1.BackColor = Color.Yellow;
                }
                else
                {
                    if (AllowManualHold)
                    {
                        button4.Text = "HOLD";
                        button4.BackColor = Color.Yellow;
                    }
                    log.LogListBox("Order Activated");
                    //lstColor = this.listBox1.BackColor;
                    this.listBox1.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
        }

        /// <summary>
        /// stop alert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            _saveCurrentDaySettingINI();

            if (AlertTimer.Enabled || ServerTimer.Enabled ||hungTimer.Enabled )
            {
                log.LogListBox("STOP ALERT");

                if (AlertTimer.Enabled) { AlertTimer.Stop(); }
                if (ServerTimer.Enabled) { ServerTimer.Stop(); }
                if (hungTimer.Enabled) { hungTimer.Stop(); }

                button2.BackColor = Color.FromKnownColor(KnownColor.Control);
                log.LogListBox("last Color was: " + lstColor.ToString());
                this.listBox1.BackColor = lstColor;
            }
            else
            { log.LogListBox("No Alerts to reset"); }
        }

        /// <summary>
        /// Activate / hold order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            switch (algo)
            {
                case "syntheticSL":
                    //do nothing button disabled
                    break;
                case "nativeSL":
                    _button_HoldNoHold();
                    break;
                case "stopMarket":
                    //do nothing button disabled
                    break;
                default:
                    log.LogListBox(algo + " NOT SET UP CORRECTLY!!");
                    break;
            }
        }

        /// <summary>
        /// display parameters form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            _displayParameters(true);
        }

        private void btnON_Click(object sender, EventArgs e)
        {
            if (isOn == false)
            {
                
                //prevent downloaded fills from being allocated and creating additional allocation files
                isAllocationDataVerified = true; 
                isOn = true;
                btnON.BackColor = System.Drawing.Color.Green;
                btnOff.BackColor = System.Drawing.Color.FromKnownColor(KnownColor.Control);
                log.WriteLog("isON = " + isOn.ToString());

                fillCount = 0;
                if (fillList.Count > 0) { fillList.Clear(); }

                switch (algo)
                {
                    case "syntheticSL":
                        this.listBox1.BackColor = Color.FromKnownColor(KnownColor.Window);
                        break;
                    case "nativeSL":
                        MarketTimer.Start();
                        break;
                    case "stopMarket":
                        MarketTimer.Start();
                        break;
                    default:
                        log.LogListBox(algo + " NOT SET UP CORRECTLY!!");
                        break;
                }
            }
        }

        private void btnOff_Click(object sender, EventArgs e)
        {
            if (isOn == true)
            { _shutdown(); }
        }

        /// <summary>
        /// Open Value saved when check box checked and price recalced if neccessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkBxOpen_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBxOpen.Checked)
            {
                nudOpen.Enabled = false;
                log.WriteLog("chkBxOpen.Checked: " + chkBxOpen.Checked.ToString());
                
                if (isContractFound)
                {
                    open = nudOpen.Value;

                    //if (open != 0) { _saveCurrentDaySetting("Open", open); }
                    _saveCurrentDaySettingINI();

                    try
                    {
                        log.LogListBox(string.Format(
                            "Open price set to {0}",
                                    ttInstrObj.get_TickPriceEx(open, XTAPI.enumRoundPriceType.ROUND_NEAREST, 0, "$")));

                        log.WriteLog(string.Format(
                            "{0} {1} {2} STOP:{3} LIMIT:{4}", 
                            buySell, 
                            tradeQty, 
                            ttProduct,
                            ttInstrObj.get_TickPrice(stopPrice, 0, "$"),
                            ttInstrObj.get_TickPrice(limitPrice, 0, "$")));
                    }
                    catch (Exception ex)
                    { log.LogListBox(ex.ToString()); }
                }
            }
            else
            {
                log.WriteLog("chkBxOpen.Checked: " + chkBxOpen.Checked.ToString());
                nudOpen.Enabled = true;
            }
        }

        private void nudOpen_ValueChanged(object sender, EventArgs e)
        {
            log.WriteLog("nudOpen_ValueChanged");
            if (isContractFound)
            {
                open = nudOpen.Value;
                _calcOrders();
                _queueCurrentOrder();
            }
        }
        
        /// <summary>
        /// modify price if neccessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nudOffSet_ValueChanged(object sender, EventArgs e)
        {
         
            log.LogListBox("nudOffSet.Value : " + nudOffSet.Value.ToString());
            _setLimitPrice();
            if (isContractFound) { _updateOrderLabel(); }
        }

        /// <summary>
        /// Display time if pre-market
        /// shutdown at stop time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketTimer_Tick(object sender, EventArgs e)
        {
            if (!_isValidTradingTime())
            {
                log.LogListBox(string.Format("Outside Trading Hours: Start: {0} Current: {1}", 
                    startTime.ToString(),
                    DateTime.Now.ToString("HH:mm:ss")));

                if (_isPostMarket())
                { _shutdown(); }
            }
            else
            {
                if (DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(3)) > stopTime &&
                    string.Equals(_tradeDate("MMddyy"), DateTime.Now.Date.ToString("MMddyy"), StringComparison.OrdinalIgnoreCase))
                {
                    if (MarketTimer.Interval != 100)
                    {
                        log.LogListBox("Accelerate Market Timer: Market Close Countdown");
                        MarketTimer.Interval = 100;
                    }
                }
            }
        }

        /// <summary>
        /// Alert timer to recyle alert until human intervenes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlertTimer_Tick(object sender, EventArgs e)
        {
            //Alerts set for:
            //Instrument not found
            //order rejected
            //execution
            if (AlertTimer.Interval != 3000) { AlertTimer.Interval = 3000; }

            log.LogListBox(msgAlert);
            if (_isValidTradingTime()) { _MakeNoise(@"C:\tt\sounds\klaxon.wav"); }

            if (this.WindowState != FormWindowState.Normal) { this.WindowState = FormWindowState.Normal; }

            if (this.listBox1.BackColor != Color.Red)
            {
                lstColor = this.listBox1.BackColor;
                this.listBox1.BackColor = Color.Red;
                this.button2.BackColor = Color.Red;
                //Thread t = new Thread(delegate() { _SendAlert(msgAlert); });
                //t.Start();
               
            }
        }


        /// <summary>
        /// Triggered if server goes down/connection lost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerTimer_Tick(object sender, EventArgs e)
        {
            if (ServerTimer.Interval != 3000) { ServerTimer.Interval = 3000; }

            log.LogListBox(msgAlert);
            if (_isValidTradingTime()) { _MakeNoise(@"C:\tt\sounds\klaxon.wav"); }

            if (this.WindowState != FormWindowState.Normal) { this.WindowState = FormWindowState.Normal; }

            if (this.listBox1.BackColor != Color.Red)
            {
                lstColor = this.listBox1.BackColor;
                this.listBox1.BackColor = Color.Red;
                this.button2.BackColor = Color.Red;
                //Thread t = new Thread(delegate() { _SendAlert(msgAlert); });
                //t.Start();
            }
        }

        /// <summary>
        /// Sound alarm if limit order is hung
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hungTimer_Tick(object sender, EventArgs e)
        {
            if (hungTimer.Interval != 3000) { hungTimer.Interval = 3000; }
            log.LogListBox(msgAlert);
            if (_isValidTradingTime()) { _MakeNoise(@"C:\tt\sounds\klaxon.wav"); }
            if (this.WindowState != FormWindowState.Normal) { this.WindowState = FormWindowState.Normal; }

            if (this.listBox1.BackColor != Color.Red)
            {
                lstColor = this.listBox1.BackColor;
                this.listBox1.BackColor = Color.Red;
                this.button2.BackColor = Color.Red;
            }
        }
    }
}
