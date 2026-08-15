using System;
using System.Diagnostics;
using System.Text;

namespace SH4rpyBoot {
	internal static class Proc {
		public static int Run(string exe, string args, Action<string> log) {
			using (var p = new Process()) {
				p.StartInfo.FileName = exe;
				p.StartInfo.Arguments = args;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				p.StartInfo.CreateNoWindow = true;
				try { p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding((int)Native.GetOEMCP()); } catch { }
				try { p.StartInfo.StandardErrorEncoding = Encoding.GetEncoding((int)Native.GetOEMCP()); } catch { }
				p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
					if (e.Data != null && log != null) log(e.Data);
				};
				p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
					if (e.Data != null && log != null) log(e.Data);
				};
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				if (!p.WaitForExit(30 * 60 * 1000)) {
					try { p.Kill(); } catch { }
					return -1;
				}
				p.WaitForExit();
				return p.ExitCode;
			}
		}
	}
}
