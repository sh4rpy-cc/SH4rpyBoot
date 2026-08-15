using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace SH4rpyBoot {
	public class UsbDevice {
		public int DiskIndex;
		public string DevicePath = "";
		public string Model = "";
		public string InterfaceType = "";
		public string MediaType = "";
		public string PnpDeviceId = "";
		public ulong SizeBytes;
		public string DriveLetter = "";
		public string VolumeLabel = "";
		public string FileSystem = "";

		public string Display {
			get {
				string size = FormatSize(SizeBytes);
				string extra = "";
				if (DriveLetter.Length > 0) {
					extra = "  [" + DriveLetter + ":";
					if (VolumeLabel.Length > 0) extra += " " + VolumeLabel;
					if (FileSystem.Length > 0) extra += " " + FileSystem;
					extra += "]";
				}
				return "Диск " + DiskIndex + "  " + size + "  " + Model + extra;
			}
		}

		public static string FormatSize(ulong bytes) {
			double b = bytes;
			string[] u = new string[] { "Б", "КБ", "МБ", "ГБ", "ТБ" };
			int i = 0;
			while (b >= 1024 && i < u.Length - 1) { b /= 1024; i++; }
			return b.ToString("0.0") + " " + u[i];
		}
	}

	public static class UsbDetector {
		public static List<UsbDevice> GetUsbDevices() {
			var result = new List<UsbDevice>();
			using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive")) {
				foreach (ManagementObject mo in searcher.Get()) {
					string iface = GetStr(mo["InterfaceType"]);
					string media = GetStr(mo["MediaType"]);
					string pnp = GetStr(mo["PNPDeviceID"]);
					bool isUsb =
						iface.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
						media.IndexOf("Removable", StringComparison.OrdinalIgnoreCase) >= 0 ||
						pnp.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0;
					if (!isUsb) continue;

					var dev = new UsbDevice();
					object sz = mo["Index"];
					dev.DiskIndex = sz == null ? 0 : Convert.ToInt32(sz);
					dev.DevicePath = @"\\.\PHYSICALDRIVE" + dev.DiskIndex;
					dev.Model = GetStr(mo["Model"]);
					dev.InterfaceType = iface;
					dev.MediaType = media;
					dev.PnpDeviceId = pnp;
					object sizeObj = mo["Size"];
					dev.SizeBytes = sizeObj == null ? 0UL : Convert.ToUInt64(sizeObj);
					result.Add(dev);
				}
			}
			MapVolumes(result);
			return result;
		}

		private static void MapVolumes(List<UsbDevice> devices) {
			var infoByDisk = new Dictionary<int, string[]>();
			try {
				foreach (var d in DriveInfo.GetDrives()) {
					if (!d.IsReady) continue;
					if (d.DriveType != DriveType.Removable && d.DriveType != DriveType.Fixed) continue;
					string letter = d.Name.Substring(0, 1).ToUpperInvariant();
					int disk = GetDiskNumber(letter);
					if (disk < 0 || infoByDisk.ContainsKey(disk)) continue;
					string label = "";
					string fs = "";
					try { label = d.VolumeLabel; } catch { }
					try { fs = d.DriveFormat; } catch { }
					infoByDisk[disk] = new string[] { letter, label, fs };
				}
			} catch { }
			foreach (UsbDevice dev in devices) {
				string[] v;
				if (infoByDisk.TryGetValue(dev.DiskIndex, out v)) {
					dev.DriveLetter = v[0];
					dev.VolumeLabel = v[1];
					dev.FileSystem = v[2];
				}
			}
		}

		private static int GetDiskNumber(string driveLetter) {
			IntPtr h = Native.CreateFile(@"\\.\" + driveLetter + ":",
				0, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
				IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
			if (h == Native.INVALID_HANDLE_VALUE) return -1;
			IntPtr buf = Marshal.AllocHGlobal(64);
			try {
				uint ret;
				bool ok = Native.DeviceIoControl(h, Native.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS,
					IntPtr.Zero, 0, buf, 64, out ret, IntPtr.Zero);
				if (!ok) return -1;
				int count = Marshal.ReadInt32(buf, 0);
				if (count < 1) return -1;
				return Marshal.ReadInt32(buf, 4);
			} finally {
				Marshal.FreeHGlobal(buf);
				Native.CloseHandle(h);
			}
		}

		private static string GetStr(object o) {
			return o == null ? "" : Convert.ToString(o);
		}
	}
}
