using System;
using System.Collections.Generic;
using System.IO;

namespace SH4rpyBoot {
	internal static class DiskOps {
		private const string Diskpart = @"C:\Windows\System32\diskpart.exe";
		private const string Mountvol = @"C:\Windows\System32\mountvol.exe";

		public static bool Clean(int diskIndex, Action<string> log) {
			return RunScript("select disk " + diskIndex + "\r\nclean\r\nexit\r\n", log);
		}

		public static bool PartitionAndFormat(int diskIndex, string fs, string label, string letter, Action<string> log) {
			string script =
				"select disk " + diskIndex + "\r\n" +
				"clean\r\n" +
				"create partition primary\r\n" +
				"active\r\n" +
				"format fs=" + fs + " quick label=\"" + label + "\"\r\n" +
				"assign letter=" + letter + "\r\n" +
				"exit\r\n";
			return RunScript(script, log);
		}

		public static void RefreshVolumes(Action<string> log) {
			Proc.Run(Mountvol, "/r", log);
			RunScript("rescan\r\nexit\r\n", log);
		}

		public static string FindFreeLetter() {
			var used = new HashSet<string>();
			foreach (var d in DriveInfo.GetDrives()) {
				string s = d.Name.Substring(0, 1).ToUpperInvariant();
				used.Add(s);
			}
			for (char c = 'Z'; c >= 'D'; c--) {
				string s = c.ToString();
				if (!used.Contains(s)) return s;
			}
			return null;
		}

		public static string SanitizeLabel(string label) {
			if (label == null) label = "";
			var sb = new System.Text.StringBuilder();
			foreach (char ch in label.ToUpperInvariant()) {
				if (char.IsLetterOrDigit(ch)) sb.Append(ch);
			}
			if (sb.Length > 11) sb.Length = 11;
			if (sb.Length == 0) sb.Append("USB");
			return sb.ToString();
		}

		private static bool RunScript(string script, Action<string> log) {
			string tmp = Path.Combine(Path.GetTempPath(), "sb_dp_" + Guid.NewGuid().ToString("N") + ".txt");
			File.WriteAllText(tmp, script);
			try {
				int code = Proc.Run(Diskpart, "/s \"" + tmp + "\"", log);
				return code == 0;
			} finally {
				try { File.Delete(tmp); } catch { }
			}
		}
	}
}
