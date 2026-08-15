using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Controls;
using MetroFramework.Forms;

namespace SH4rpyBoot {
	internal class MainForm : MetroForm {
		private MetroComboBox _cmbDisk;
		private MetroButton _btnRefresh;
		private MetroTextBox _txtIso;
		private MetroButton _btnBrowse;
		private MetroRadioButton _rbDD;
		private MetroRadioButton _rbWin;
		private MetroRadioButton _rbFormat;
		private MetroComboBox _cmbFs;
		private MetroTextBox _txtLabel;
		private MetroProgressBar _prog;
		private MetroButton _btnStart;
		private MetroTextBox _txtLog;
		private BackgroundWorker _worker;
		private bool _busy;

		public MainForm() {
			Text = "SH4rpyBoot — загрузочные флешки";
			ClientSize = new Size(800, 620);
			MinimumSize = new Size(740, 520);
			StartPosition = FormStartPosition.CenterScreen;
			AutoScaleMode = AutoScaleMode.Dpi;

			var sm = new MetroStyleManager();
			sm.Theme = MetroThemeStyle.Dark;
			sm.Style = MetroColorStyle.Blue;
			sm.Owner = this;

			BuildUi();
			_worker = new BackgroundWorker();
			_worker.WorkerReportsProgress = true;
			_worker.DoWork += Worker_DoWork;
			_worker.ProgressChanged += Worker_ProgressChanged;
			_worker.RunWorkerCompleted += Worker_Completed;
			RefreshDisks();
		}

		private void BuildUi() {
			var tlp = new TableLayoutPanel();
			tlp.Dock = DockStyle.Fill;
			tlp.ColumnCount = 3;
			tlp.RowCount = 8;
			tlp.Padding = new Padding(14, 6, 14, 14);
			tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
			tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
			for (int i = 0; i < 7; i++) tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			int r = 0;

			tlp.Controls.Add(MakeLabel("Накопитель:"), 0, r);
			_cmbDisk = new MetroComboBox();
			_cmbDisk.DropDownStyle = ComboBoxStyle.DropDownList;
			_cmbDisk.Dock = DockStyle.Fill;
			_cmbDisk.Margin = new Padding(0, 6, 8, 4);
			tlp.Controls.Add(_cmbDisk, 1, r);
			_btnRefresh = new MetroButton();
			_btnRefresh.Text = "Обновить";
			_btnRefresh.Dock = DockStyle.Fill;
			_btnRefresh.Margin = new Padding(0, 6, 0, 4);
			_btnRefresh.Click += delegate { RefreshDisks(); };
			tlp.Controls.Add(_btnRefresh, 2, r);
			r++;

			tlp.Controls.Add(MakeLabel("ISO-образ:"), 0, r);
			_txtIso = new MetroTextBox();
			_txtIso.Dock = DockStyle.Fill;
			_txtIso.Margin = new Padding(0, 6, 8, 4);
			_txtIso.PromptText = "Перетащите ISO или выберите через «Обзор»";
			_txtIso.AllowDrop = true;
			_txtIso.DragEnter += delegate(object s, DragEventArgs e) {
				if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
			};
			_txtIso.DragDrop += delegate(object s, DragEventArgs e) {
				string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
				if (files != null && files.Length > 0) _txtIso.Text = files[0];
			};
			tlp.Controls.Add(_txtIso, 1, r);
			_btnBrowse = new MetroButton();
			_btnBrowse.Text = "Обзор...";
			_btnBrowse.Dock = DockStyle.Fill;
			_btnBrowse.Margin = new Padding(0, 6, 0, 4);
			_btnBrowse.Click += delegate { BrowseIso(); };
			tlp.Controls.Add(_btnBrowse, 2, r);
			r++;

			_rbDD = new MetroRadioButton();
			_rbDD.Text = "Запись образа (DD) — посекторная копия ISO (загрузочная для гибридных образов)";
			_rbDD.AutoSize = true;
			_rbDD.Checked = true;
			_rbDD.Margin = new Padding(4, 8, 0, 2);
			tlp.Controls.Add(_rbDD, 0, r);
			tlp.SetColumnSpan(_rbDD, 3);
			r++;

			_rbWin = new MetroRadioButton();
			_rbWin.Text = "Создать загрузочную флешку Windows (UEFI, FAT32)";
			_rbWin.AutoSize = true;
			_rbWin.Margin = new Padding(4, 4, 0, 2);
			tlp.Controls.Add(_rbWin, 0, r);
			tlp.SetColumnSpan(_rbWin, 3);
			r++;

			_rbFormat = new MetroRadioButton();
			_rbFormat.Text = "Только форматирование";
			_rbFormat.AutoSize = true;
			_rbFormat.Margin = new Padding(4, 4, 0, 2);
			tlp.Controls.Add(_rbFormat, 0, r);
			tlp.SetColumnSpan(_rbFormat, 3);
			r++;

			tlp.Controls.Add(MakeLabel("ФС / метка:"), 0, r);
			_cmbFs = new MetroComboBox();
			_cmbFs.DropDownStyle = ComboBoxStyle.DropDownList;
			_cmbFs.Items.Add("FAT32");
			_cmbFs.Items.Add("NTFS");
			_cmbFs.Items.Add("exFAT");
			_cmbFs.SelectedIndex = 0;
			_cmbFs.Dock = DockStyle.Fill;
			_cmbFs.Margin = new Padding(0, 6, 8, 4);
			tlp.Controls.Add(_cmbFs, 1, r);
			_txtLabel = new MetroTextBox();
			_txtLabel.Text = "USB";
			_txtLabel.Dock = DockStyle.Fill;
			_txtLabel.Margin = new Padding(0, 6, 0, 4);
			tlp.Controls.Add(_txtLabel, 2, r);
			r++;

			_prog = new MetroProgressBar();
			_prog.Dock = DockStyle.Fill;
			_prog.Minimum = 0;
			_prog.Maximum = 100;
			_prog.Margin = new Padding(0, 6, 8, 4);
			tlp.Controls.Add(_prog, 0, r);
			tlp.SetColumnSpan(_prog, 2);
			_btnStart = new MetroButton();
			_btnStart.Text = "СТАРТ";
			_btnStart.Highlight = true;
			_btnStart.Dock = DockStyle.Fill;
			_btnStart.Height = 40;
			_btnStart.Margin = new Padding(0, 6, 0, 4);
			_btnStart.Click += delegate { Start(); };
			tlp.Controls.Add(_btnStart, 2, r);
			r++;

			_txtLog = new MetroTextBox();
			_txtLog.Multiline = true;
			_txtLog.ReadOnly = true;
			_txtLog.ScrollBars = ScrollBars.Vertical;
			_txtLog.Dock = DockStyle.Fill;
			_txtLog.Margin = new Padding(0, 8, 0, 0);
			_txtLog.Font = new Font("Consolas", 9.5F);
			tlp.Controls.Add(_txtLog, 0, r);
			tlp.SetColumnSpan(_txtLog, 3);

			Controls.Add(tlp);

			_rbDD.CheckedChanged += delegate { UpdateFsState(); };
			_rbWin.CheckedChanged += delegate { UpdateFsState(); };
			_rbFormat.CheckedChanged += delegate { UpdateFsState(); };
			UpdateFsState();
		}

		private static MetroLabel MakeLabel(string text) {
			var l = new MetroLabel();
			l.Text = text;
			l.Dock = DockStyle.Fill;
			l.TextAlign = ContentAlignment.MiddleLeft;
			l.Margin = new Padding(0, 8, 4, 4);
			return l;
		}

		private void UpdateFsState() {
			if (_rbDD.Checked) {
				_cmbFs.Enabled = false;
				_txtLabel.Enabled = false;
			} else if (_rbWin.Checked) {
				_cmbFs.SelectedIndex = 0;
				_cmbFs.Enabled = false;
				_txtLabel.Enabled = true;
			} else {
				_cmbFs.Enabled = true;
				_txtLabel.Enabled = true;
			}
		}

		private void RefreshDisks() {
			_cmbDisk.Items.Clear();
			try {
				List<UsbDevice> list = UsbDetector.GetUsbDevices();
				foreach (UsbDevice d in list) _cmbDisk.Items.Add(new DiskItem(d));
				if (_cmbDisk.Items.Count > 0) {
					_cmbDisk.SelectedIndex = 0;
				} else {
					Log("Съёмные USB-накопители не найдены.");
				}
			} catch (Exception ex) {
				Log("Ошибка определения дисков: " + ex.Message);
			}
		}

		private void BrowseIso() {
			using (var ofd = new OpenFileDialog()) {
				ofd.Filter = "Образы ISO|*.iso|Все файлы|*.*";
				ofd.Title = "Выберите ISO-образ";
				if (ofd.ShowDialog(this) == DialogResult.OK) _txtIso.Text = ofd.FileName;
			}
		}

		private UsbDevice SelectedDevice() {
			var item = _cmbDisk.SelectedItem as DiskItem;
			return item == null ? null : item.Device;
		}

		private void Start() {
			if (_busy) return;
			UsbDevice dev = SelectedDevice();
			if (dev == null) {
				MessageBox.Show(this, "Выберите накопитель.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			string mode;
			if (_rbWin.Checked) mode = "win";
			else if (_rbFormat.Checked) mode = "format";
			else mode = "dd";

			string iso = _txtIso.Text.Trim();
			if (mode != "format" && !File.Exists(iso)) {
				MessageBox.Show(this, "Выберите существующий ISO-образ.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			string warn = "ВНИМАНИЕ! Все данные на выбранном накопителе будут УНИЧТОЖЕНЫ:\n\n" +
				dev.Display + "\n\nПродолжить?";
			if (MessageBox.Show(this, warn, "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

			_busy = true;
			SetUiEnabled(false);
			_prog.Value = 0;
			Log("=== Начало работы ===");
			_worker.RunWorkerAsync(new object[] { mode, iso, dev });
		}

		private void SetUiEnabled(bool enabled) {
			_cmbDisk.Enabled = enabled;
			_btnRefresh.Enabled = enabled;
			_btnBrowse.Enabled = enabled;
			_txtIso.Enabled = enabled;
			_rbDD.Enabled = enabled;
			_rbWin.Enabled = enabled;
			_rbFormat.Enabled = enabled;
			_btnStart.Enabled = enabled;
			if (enabled) UpdateFsState();
		}

		private void Worker_DoWork(object sender, DoWorkEventArgs e) {
			var args = (object[])e.Argument;
			string mode = (string)args[0];
			string iso = (string)args[1];
			var dev = (UsbDevice)args[2];
			Action<int, string> report = delegate(int pct, string line) {
				_worker.ReportProgress(pct < 0 ? 0 : pct, line);
			};
			try {
				switch (mode) {
					case "dd":
						DoDD(dev, iso, report);
						break;
					case "win":
						DoWin(dev, iso, report);
						break;
					default:
						DoFormat(dev, report);
						break;
				}
				report(100, "=== Готово ===");
			} catch (Exception ex) {
				e.Result = ex;
			}
		}

		private static void DoDD(UsbDevice dev, string iso, Action<int, string> report) {
			report(-1, "Гибридный образ (загрузочный): " + (RawWriter.IsHybridIso(iso) ? "да" : "НЕТ — возможно, не загрузится!"));
			report(2, "Сброс диска (diskpart clean)...");
			if (!DiskOps.Clean(dev.DiskIndex, delegate(string l) { report(-1, l); })) {
				throw new Exception("Не удалось очистить диск (см. лог). Возможно, диск занят системой.");
			}
			report(10, "Запись образа посекторно...");
			RawWriter.WriteIso(iso, dev.DevicePath, (long)dev.SizeBytes, report);
			report(98, "Обновление списка томов...");
			DiskOps.RefreshVolumes(delegate(string l) { report(-1, l); });
		}

		private void DoFormat(UsbDevice dev, Action<int, string> report) {
			string fs = FsName();
			string label = DiskOps.SanitizeLabel(_txtLabel.Text);
			string letter = DiskOps.FindFreeLetter();
			if (letter == null) throw new Exception("Нет свободной буквы диска.");
			report(5, "Разметка и форматирование: " + fs + ", метка \"" + label + "\", буква " + letter + ":");
			if (!DiskOps.PartitionAndFormat(dev.DiskIndex, fs, label, letter, delegate(string l) { report(-1, l); })) {
				throw new Exception("Сбой форматирования (см. лог).");
			}
			report(95, "Готово: " + letter + ":");
		}

		private void DoWin(UsbDevice dev, string iso, Action<int, string> report) {
			string label = DiskOps.SanitizeLabel(_txtLabel.Text);
			WindowsMaker.Make(iso, dev.DiskIndex, label, report);
		}

		private string FsName() {
			string s = _cmbFs.SelectedItem as string;
			if (s == null) s = "FAT32";
			return s.ToLowerInvariant();
		}

		private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			string text = e.UserState as string;
			if (text != null && text.Length > 0) Log(text);
			if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= 100) {
				_prog.Value = Math.Max(_prog.Value, e.ProgressPercentage);
			}
		}

		private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e) {
			_busy = false;
			SetUiEnabled(true);
			if (e.Result is Exception) {
				var ex = (Exception)e.Result;
				Log("ОШИБКА: " + ex.Message);
				MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			} else {
				MessageBox.Show(this, "Операция завершена.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e) {
			if (_busy) {
				e.Cancel = true;
				MessageBox.Show(this, "Дождитесь завершения операции.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			base.OnFormClosing(e);
		}

		private void Log(string line) {
			if (_txtLog == null) return;
			if (_txtLog.Text.Length > 100000) _txtLog.Clear();
			_txtLog.AppendText(line + Environment.NewLine);
		}

		private class DiskItem {
			public readonly UsbDevice Device;
			public DiskItem(UsbDevice d) { Device = d; }
			public override string ToString() { return Device.Display; }
		}
	}
}
