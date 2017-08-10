﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderRenderController.newLogger
{
    public class FileLogger : LoggerBase, ILogger
    {
        //private string _lastLog;
        //private string _repeatLine;
        //private int _numLastLogs;
        private List<string> _dueMsgs = new List<string>();

        public FileLogger()
        {
            Name = nameof(FileLogger);
        }

        private void Log(string message, LogType logType)
        {
            // Ignore 'INFO' and 'WARN' logs if Verbose == false
            if ((!_verbose) && (logType != LogType.ERROR))
                return;

            string type = logType.ToString();
            var logLine = $"{type} [{_time}]: {message}";
            var status = new FileInfo(AppStrings.LOG_FILE_NAME);

            if (!IsFileLocked(status))
            {
                using (StreamWriter sw = new StreamWriter(AppStrings.LOG_FILE_NAME, true))
                {
                    if (_dueMsgs.Count > 0)
                    {
                        //write late logs
                        sw.WriteLineAsync($"{type} [{_time}]: {_dueMsgs[0]}");
                        _dueMsgs.Clear();
                    }
                    sw.WriteLine(logLine); 
                }
            }
            else
            {
                // if log file is in use, add to this list for later
                _dueMsgs.Add(logLine);
            }


        }

        public void Info(string message)
        {
            Log(message, LogType.INFO);
        }

        public void Error(string message)
        {
            Log(message, LogType.ERROR);
        }

        public void Warn(string message)
        {
            Log(message, LogType.WARNING);
        }
    }

    public class ConsoleLogger : LoggerBase, ILogger
    {
        public ConsoleLogger()
        {
            Name = nameof(ConsoleLogger);
        }

        private void Log(string message, LogType logType)
        {
            if ((!_verbose) && (logType == LogType.INFO))
                return;

            string type = logType.ToString();
            var logLine = $"{type}: {message} -- [{_time}]";
            Console.WriteLine(logLine);
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(message, LogType.ERROR);
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Log(message, LogType.INFO);
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log(message, LogType.WARNING);
        }
    }

    public class LoggerBase
    {
        protected readonly DateTime _time = DateTime.Now;
        protected bool _verbose;
        protected AppSettings appSettings = new AppSettings();
        public string Name { get; set; }

        public LoggerBase()
        {
            appSettings.RemoteLoadJsonSettings();
            this._verbose = appSettings.verboseLog;
        }

        /// <summary>
        /// This function is used to check specified file being used or not
        /// </summary>
        /// <param name="file">FileInfo of required file</param>
        /// <returns>If that specified file is being processed 
        /// or not found is return true</returns>
        public static Boolean IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                //Don't change FileAccess to ReadWrite, 
                //because if a file is in readOnly, it fails.

                stream = file.Open
                (
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None
                );
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            //file is not locked
            return false;
        }
    }
}

//if (logMode == 0)
//{
//    var logTitle = $"\n{LogConstants.LOG_TITLE} - {AppDomain.CurrentDomain.FriendlyName}\n";
//    sw.WriteLine(logTitle);
//    logMode++;
//}