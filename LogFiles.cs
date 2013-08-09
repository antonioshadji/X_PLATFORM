using System;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace LOG
{
    /// <summary>
    /// 
    /// </summary>
    public class LogFiles
    {
        private string logFileName = "LOG";
        private string logFileExt = "log";
        private ListBox lb = null;
        private long maxSize = 3500000;
        private TextWriter logTextWriter = null;
        private string logFile = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="box"></param>
        /// <param name="fileName"></param>
        public LogFiles(ListBox box, string fileName)
        {
            this.lb = box;
            if (fileName.Contains("."))
            {
                this.logFileName = fileName.Substring(0, fileName.IndexOf("."));
                this.logFileExt = fileName.Substring(fileName.IndexOf(".")+1, fileName.Length - (fileName.IndexOf(".")+1));
            }
            else
            { this.logFileName = fileName; }
            this.CreateLog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public LogFiles(string fileName)
        {
            this.logFileName = fileName;
            this.CreateLog();
        }

        /// <summary>
        /// constructor - no initialization
        /// </summary>
        public LogFiles() { this.CreateLog(); }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public void WriteLog(string msg)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(logFile);

                if (fileInfo.Length > maxSize)
                {
                    logTextWriter.WriteLine("CREATELOG");
                    logTextWriter.Flush();
                    logTextWriter.Close();
                    CreateLog();
                }

                logTextWriter.WriteLine(DateTime.Now.ToString("yyyyMMMdd_hh.mm.ss.ffftt") + " " + msg);
                logTextWriter.Flush();
            }
            catch (Exception e)
            { MessageBox.Show("WRITELOG:" + e.ToString()); }

        }

        /// <summary>
        /// 
        /// </summary>
        public void CloseLog()
        {
            try
            {
                if (logTextWriter != null)
                { logTextWriter.Close(); }
            }
            catch (Exception e)
            { MessageBox.Show("WRITELOG:" + e.ToString()); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public void LogListBox(string msg)
        {
            if (msg == null) { msg = "NULL MESSAGE"; }

            int maxLines = (lb.Size.Height / lb.ItemHeight);

            if (lb.Items.Count >= maxLines)
            {
                lb.Items.RemoveAt(0);
                lb.Items.Add(msg);
            }
            else
            {
                lb.Items.Add(msg);
            }

            WriteLog(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        public void CleanLog()
        {
            string logpath = Application.StartupPath.ToString() + @"\LOG";

            if (System.IO.Directory.Exists(logpath))
            {
                string[] files = System.IO.Directory.GetFiles(logpath);

                foreach (string s in files)
                {
                    FileInfo fi = new FileInfo(s);

                    if (DateTime.Compare(fi.LastWriteTime, DateTime.Today.Subtract(TimeSpan.FromDays(10))) < 0)
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch (System.IO.IOException e)
                        {
                            WriteLog("CLEAN: " + e.ToString());
                        }
                    }
                }
            }
        }

        private void CreateLog()
        {
            logFile = Application.StartupPath.ToString() + @"\LOG";

            // if logfile directory does not exists, create it
            bool blnDirectoryExists = Directory.Exists(logFile);
            if (!blnDirectoryExists)
            {
                try
                { Directory.CreateDirectory(logFile); }
                catch (Exception e)
                { MessageBox.Show("CREATELOG: " + e.ToString()); }
            }

            logFile += @"\" + logFileName + "_";
            logFile += DateTime.Now.ToString("yyyyMMMdd_hh.mm.ss");
            logFile += "." + logFileExt;

            bool blnFileExists = File.Exists(logFile);
            if (!blnFileExists)
            {
                try
                { logTextWriter = new StreamWriter(logFile); }
                catch (Exception e)
                { MessageBox.Show("CREATELOG: " + e.ToString()); }
            }
        }

    }
}
