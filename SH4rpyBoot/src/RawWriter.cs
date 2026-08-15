using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace SH4rpyBoot {
	internal static class RawWriter {
		private const int CHUNK = 4 * 1024 * 1024;

		public static bool IsHybridIso(string path) {
			try {
				using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					var hdr = new byte[512];
					fs.Read(hdr, 0, hdr.Length);
					bool sig = hdr[510] == 0x55 && hdr[511] == 0xAA;
					bool boot = hdr[0] == 0xEB || hdr[0] == 0xE9;
					return sig && boot;
				}
			} catch { return false; }
		}

		public static void WriteIso(string isoPath, string devicePath, long diskBytes, Action<int, string> report) {
			var fi = new FileInfo(isoPath);
			if (!fi.Exists) throw new Exception("Образ не найден: " + isoPath);
			if (fi.Length > diskBytes) {
				throw new Exception("Размер образа (" + UsbDevice.FormatSize((ulong)fi.Length) +
					") больше размера диска (" + UsbDevice.FormatSize((ulong)diskBytes) + ").");
			}
			long total = fi.Length;

			IntPtr h = Native.CreateFile(devicePath,
				Native.GENERIC_READ | Native.GENERIC_WRITE,
				Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
				IntPtr.Zero, Native.OPEN_EXISTING,
				Native.FILE_FLAG_NO_BUFFERING | Native.FILE_FLAG_WRITE_THROUGH,
				IntPtr.Zero);
			if (h == Native.INVALID_HANDLE_VALUE) {
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Не удалось открыть диск " + devicePath);
			}
			try {
				IntPtr bufBase = Marshal.AllocHGlobal(CHUNK + 4096);
				try {
					long addr = bufBase.ToInt64();
					IntPtr buf = new IntPtr((addr + 4095) & ~4095L);
					var rb = new byte[CHUNK];
					using (var fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read, CHUNK)) {
						long written = 0;
						int lastPct = -1;
						while (written < total) {
							int want = (int)Math.Min(CHUNK, total - written);
							int got = 0;
							while (got < want) {
								int n = fs.Read(rb, got, want - got);
								if (n <= 0) throw new Exception("Преждевременный конец файла образа.");
								got += n;
							}
							int chunkSize = ((got + 4095) / 4096) * 4096;
							for (int i = got; i < chunkSize; i++) rb[i] = 0;
							Marshal.Copy(rb, 0, buf, chunkSize);
							uint wroteBytes;
							if (!Native.WriteFile(h, buf, (uint)chunkSize, out wroteBytes, IntPtr.Zero)) {
								throw new Win32Exception(Marshal.GetLastWin32Error(), "Ошибка записи на диск " + devicePath);
							}
							written += got;
							int pct = (int)(written * 100 / total);
							if (pct != lastPct) {
								lastPct = pct;
								if (report != null) report(pct, "Записано " + (written / 1048576) + " / " + (total / 1048576) + " МБ");
							}
						}
					}
					Native.FlushFileBuffers(h);
				} finally {
					Marshal.FreeHGlobal(bufBase);
				}
			} finally {
				Native.CloseHandle(h);
			}
		}
	}
}
