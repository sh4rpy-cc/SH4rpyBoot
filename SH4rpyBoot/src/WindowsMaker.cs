using System;
using System.Collections.Generic;
using System.IO;

namespace SH4rpyBoot {
	internal static class WindowsMaker {
		private const long FAT32_MAX_FILE = 0xFFFFFFFFL;
		private const string Robocopy = @"C:\Windows\System32\robocopy.exe";
		private const string Bcdboot = @"C:\Windows\System32\bcdboot.exe";
		private const string Bootsect = @"C:\Windows\System32\bootsect.exe";
		private const string Powershell = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

		public static void Make(string isoPath, int diskIndex, string label, Action<int, string> report) {
			string target = DiskOps.FindFreeLetter();
			if (target == null) throw new Exception("Нет свободной буквы диска для целевого раздела.");

			report(2, "Монтирование ISO-образа...");
			string src = MountIso(isoPath, report);
			if (src == null) throw new Exception("Не удалось смонтировать образ (см. лог). Возможно, он уже смонтирован.");
			try {
				CheckWim(src, report);

				report(8, "Разметка и форматирование (FAT32)...");
				if (!DiskOps.PartitionAndFormat(diskIndex, "fat32", label, target,
					delegate(string l) { report(-1, l); })) {
					throw new Exception("Сбой разметки/форматирования (см. лог).");
				}

				report(30, "Копирование файлов...");
				int code = Proc.Run(Robocopy,
					"\"" + src + ":\\\" \"" + target + ":\\\" /E /COPY:DAT /R:1 /W:1 /MT:16 /NFL /NDL /NJH /NJS /NP",
					delegate(string l) { report(-1, l); });
				if (code >= 8) throw new Exception("Ошибка копирования файлов (код robocopy: " + code + ").");

				report(75, "Создание загрузочных записей (bcdboot)...");
				Proc.Run(Bcdboot, target + ":\\Windows /s " + target + ": /f ALL",
					delegate(string l) { report(-1, l); });

				report(85, "Запись загрузочного сектора (bootsect)...");
				Proc.Run(Bootsect, "/nt60 " + target + ": /mbr /force",
					delegate(string l) { report(-1, l); });

				report(97, "Флешка готова.");
			} finally {
				DismountIso(isoPath, report);
				DiskOps.RefreshVolumes(delegate(string l) { report(-1, l); });
			}
		}

		private static void CheckWim(string src, Action<int, string> report) {
			string dir = src + @":\sources";
			if (!Directory.Exists(dir)) {
				report(-1, "ВНИМАНИЕ: папка sources не найдена — похоже, это не Windows ISO.");
				return;
			}
			foreach (string f in Directory.GetFiles(dir, "install.*")) {
				long len = new FileInfo(f).Length;
				if (len > FAT32_MAX_FILE) {
					throw new Exception("Файл " + Path.GetFileName(f) + " (" + UsbDevice.FormatSize((ulong)len) +
						") больше 4 ГБ и не помещается в FAT32. Такой образ пока не поддерживается.");
				}
			}
		}

		private static string MountIso(string iso, Action<int, string> report) {
			string safe = iso.Replace("'", "''");
			string cmd = "$i = Mount-DiskImage -ImagePath '" + safe + "' -PassThru; " +
				"$v = Get-Volume -DiskImage $i; " +
				"if ($v) { $v.DriveLetter.ToString() }";
			string letter = null;
			int code = Proc.Run(Powershell,
				"-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"",
				delegate(string l) {
					report(-1, l);
					if (letter == null && l != null) {
						string t = l.Trim();
						if (t.Length == 1 && t[0] >= 'A' && t[0] <= 'Z') letter = t;
					}
				});
			if (code != 0 || letter == null) return null;
			return letter;
		}

		private static void DismountIso(string iso, Action<int, string> report) {
			try {
				string safe = iso.Replace("'", "''");
				Proc.Run(Powershell,
					"-NoProfile -ExecutionPolicy Bypass -Command \"Dismount-DiskImage -ImagePath '" + safe + "'\"",
					delegate(string l) { report(-1, l); });
			} catch { }
		}
	}
}
