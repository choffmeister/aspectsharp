using System.Diagnostics;
using System.IO;

namespace AspectSharp.Tests
{
    public static class ProcessHelper
    {
        public static ProcessResult Execute(string fileName, string arguments = "", string workingDirectory = "")
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = workingDirectory;

            StringWriter stdOut = new StringWriter();
            StringWriter stdError = new StringWriter();
            StringWriter output = new StringWriter();

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();

                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    stdOut.Write(e.Data);
                    output.Write(e.Data);
                };
                p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    stdError.Write(e.Data);
                    output.Write(e.Data);
                };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                return new ProcessResult()
                {
                    ExitCode = p.ExitCode,
                    StdOut = stdOut.ToString(),
                    StdError = stdError.ToString(),
                    Output = output.ToString()
                };
            }
        }
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }

        public bool Success
        {
            get { return this.ExitCode == 0; }
        }

        public string StdOut { get; set; }

        public string StdError { get; set; }

        public string Output { get; set; }
    }
}
