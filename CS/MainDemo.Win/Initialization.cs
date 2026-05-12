using System;
using System.IO;
using System.Diagnostics;
using DevExpress.Persistent.Base;

namespace MainDemo.Win {
    public class Initialization {
        public static void KillServerProcess() {
            Process existProc = Process.GetProcessesByName("MainDemo.MiddleTier").FirstOrDefault();
            if(existProc != null) {
                existProc.Kill();
                existProc.WaitForExit();
            }
        }
        public static Process RunSecurityServer(IEnumerable<string> args) {
            KillServerProcess();
            var proc = new Process();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            if(args.Any()) {
                proc.StartInfo.Arguments = string.Concat(proc.StartInfo.Arguments, " ", string.Join(" ", args.Select(a => "\"" + a + "\"")));
            }
            string workingDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"..\MainDemo.MiddleTier");
            if(!Directory.Exists(workingDirectory)) {
                Tracing.Tracer.LogText("The default WebApi folder does not exist. Fallback to the developer WebApi folder.");
                string buildConfiguration = Path.GetFileName(Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ApplicationBase));
                workingDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @$"..\..\..\MainDemo.MiddleTier\Bin\{buildConfiguration}\net9.0-windows");
                if(!Directory.Exists(workingDirectory)) {
                    workingDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, $@"..\..\..\MainDemo.MiddleTier\Bin\{buildConfiguration}");
                }
            }
            string fileName = Path.Combine(workingDirectory, "MainDemo.MiddleTier.exe");
            if(!File.Exists(fileName)) {
                throw new FileNotFoundException("Could not start a server process. The MainDemo.MiddleTier.exe file is missing.\r\nPlease ensure that you have built the MainDemo.MiddleTier project.");
            }
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.Start();
            return proc;
        }
    }
}
