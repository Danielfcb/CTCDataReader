using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace CTCDataReader.Utilties
{
    public class LogWriter
    {
        private static string _logFileFullName;
        private static string _logFileFolder;
        private static void InitializeLogWriter()
        {
            if (ConfigurationManager.AppSettings["AbsoluteLogFilePath"] == "" && ConfigurationManager.AppSettings["RelativeLogFilePath"] == "")
            {
                throw new Exception("There is no absolute or relateive data file paths defined in Config file!");
            }
            else if (ConfigurationManager.AppSettings["AbsoluteLogFilePath"] == "")
            {
                _logFileFolder = AppDomain.CurrentDomain.BaseDirectory + "/../../" + ConfigurationManager.AppSettings["RelativeLogFilePath"];
            }
            else
            {
                _logFileFolder = ConfigurationManager.AppSettings["AbsoluteDataFilePath"];
            }

            _logFileFullName = _logFileFolder + "/Log_" + DateTime.Today.ToString("yyyyMMdd") + ".txt";

            if (!Directory.Exists(_logFileFolder))
            {
                Directory.CreateDirectory(_logFileFolder);
            }
            if (!File.Exists(_logFileFullName))
            {
                var file = File.Create(_logFileFullName);
                file.Close();
            }
        }

        public static void WriteLog(string log)
        {
            if (string.IsNullOrEmpty(_logFileFolder) || string.IsNullOrEmpty(_logFileFullName))
            {
                InitializeLogWriter();
            }

            using (StreamWriter sw = File.AppendText(_logFileFullName))
            {
                sw.WriteLine(DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + "    " + log);
            }

        }

        public static void WriteLog(Exception ex)
        {
            if (string.IsNullOrEmpty(_logFileFolder) || string.IsNullOrEmpty(_logFileFullName))
            {
                InitializeLogWriter();
            }

            WriteLog("Exception Stack Trace: \n\n" + ex.ToString() + "\n\n");
        }
    }
}
