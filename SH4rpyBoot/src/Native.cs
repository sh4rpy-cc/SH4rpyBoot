using System;
using System.Runtime.InteropServices;

namespace SH4rpyBoot {
	internal static class Native {
		public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		public const int GENERIC_READ = unchecked((int)0x80000000);
		public const int GENERIC_WRITE = 0x40000000;
		public const int FILE_SHARE_READ = 0x00000001;
		public const int FILE_SHARE_WRITE = 0x00000002;
		public const int OPEN_EXISTING = 3;
		public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;

		public const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr CreateFile(
			string lpFileName, int dwDesiredAccess, int dwShareMode,
			IntPtr lpSecurityAttributes, int dwCreationDisposition,
			uint dwFlagsAndAttributes, IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteFile(
			IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite,
			out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice, uint dwIoControlCode,
			IntPtr lpInBuffer, uint nInBufferSize,
			IntPtr lpOutBuffer, uint nOutBufferSize,
			out uint lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FlushFileBuffers(IntPtr hFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		public static extern uint GetOEMCP();
	}
}
