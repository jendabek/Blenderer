﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace BRClib.Commands
{
    /// <summary>
    /// Base class that represents a external process / command
    /// </summary>
    public abstract class ExternalCommand
    {
        // 0=Process name, 1=Exit Code, 2=Std Error, 3=Std Output
        protected const string REPORT_FMT = "{0} exited w/ code {1}\n\n" +
                                            "Std Error:\n{2}\n\n" +
                                            "Std Output:\n{3}";

        string _stdErr, _stdOut, _procName;
        int _procExitCode;

        string _rfn;

        public string ProgramPath { get; set; }

        protected Logger Log { get; private set; }

        protected virtual string ReportFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_rfn))
                {
                    var tmp = Path.GetRandomFileName();
                    _rfn = _procName + '_' + Path.ChangeExtension(tmp, "txt");
                }

                return _rfn;
            }
        }

        protected string StdOutput => _stdOut;
        protected string StdError => _stdErr;
        protected int ExitCode => _procExitCode;


        public ExternalCommand(string programPath)
        {
            ProgramPath = programPath;
            Log = LogManager.GetLogger(GetType().FullName);
            _procName = Path.GetFileNameWithoutExtension(ProgramPath);
        }


        public virtual Process GetProcess()
        {
            var proc = new Process();
            var info = new ProcessStartInfo()
            {
                FileName = ProgramPath,
                CreateNoWindow = true,

                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,

                Arguments = GetArgs()
            };
            proc.StartInfo = info;
            proc.Exited += (ps, pe) =>
            {
                Log.Info("{0} exit code: {1}", _procName, (ps as Process).ExitCode);
            };

            Log.Info("Running {0}", _procName);
            Log.Debug("cmd:> {0} {1}", _procName, proc.StartInfo.Arguments);

            return proc;
        }

        protected abstract string GetArgs();

        public virtual Task<bool> Run(CancellationToken token = default)
        {
            var process = GetProcess();
            var tcs = new TaskCompletionSource<bool>();

            var pTask = process.StartAsync(true, true, token);

            pTask.ContinueWith(t =>
            {
                tcs.SetResult(false);
            },
            TaskContinuationOptions.OnlyOnCanceled);

            pTask.ContinueWith(t =>
            {
                _procExitCode = t.Result.ExitCode;
                _stdOut = t.Result.StdOutput;
                _stdErr = t.Result.StdError;

                tcs.SetResult(_procExitCode == 0);
            },
            TaskContinuationOptions.ExecuteSynchronously |
            TaskContinuationOptions.OnlyOnRanToCompletion);

            return tcs.Task;
        }

        public Task<int> StartAsync(CancellationToken token = default)
        {
            var process = GetProcess();
            var tcs = new TaskCompletionSource<int>();

            var pTask = process.StartAsync(token);

            pTask.ContinueWith(t =>
            {
                _procExitCode = t.Result;
                tcs.SetResult(_procExitCode);
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public Task<ProcessResult> StartAsync(bool getStdOut,
                                              bool getStdErr,
                                              CancellationToken token = default)
        {
            var process = GetProcess();
            var tcs = new TaskCompletionSource<ProcessResult>();

            var pTask = process.StartAsync(getStdOut, getStdErr, token);

            pTask.ContinueWith(t =>
            {
                _procExitCode = t.Result.ExitCode;
                _stdOut = t.Result.StdOutput;
                _stdErr = t.Result.StdError;

                tcs.SetResult(t.Result);
            }, 
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public virtual void SaveReport(string folderPath)
        {
            var filePath = Path.Combine(folderPath, ReportFileName);

            string fmt = REPORT_FMT;
            using (var sw = File.AppendText(filePath))
            {
                sw.WriteLine(string.Format(fmt, _procName, _procExitCode, 
                                            _stdErr, _stdOut));
                sw.Write("\n\n");
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {_procName} {GetArgs()}";
        }
    }
}