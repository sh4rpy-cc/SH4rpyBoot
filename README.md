# SH4rpyBoot

Минимальный аналог Rufus для Windows: запись загрузочных флешек из ISO-образов.
Тёмный Metro-интерфейс (MetroFramework). Ядро использует только встроенные средства
Windows: `diskpart`, `bcdboot`, `bootsect`, `robocopy`, `PowerShell`.

## Возможности

- Определение USB-накопителей (размер, метка, файловая система, буква).
- **Запись образа (DD)** — посекторная копия ISO. Работает для гибридных образов
  (Linux, FreeBSD, утилиты) — загрузка и в BIOS, и в UEFI.
- **Загрузочная флешка Windows** — создание загрузочной UEFI-флешки (FAT32) из Windows ISO.
  Ограничение: `install.wim/esd` больше 4 ГБ в FAT32 не помещается (стандартные Windows 10/11 ISO — нет).
- **Только форматирование** — FAT32 / NTFS / exFAT, с меткой.

## Сборка

Ничего устанавливать не нужно — компилятор уже есть в Windows:

```
build.cmd
```

Результат: `SH4rpyBoot.exe` (требует права администратора, UAC).
Рядом автоматически копируются `MetroFramework.dll` и `MetroFramework.Fonts.dll`.

## Использование

1. Вставьте флешку, запустите `SH4rpyBoot.exe`.
2. Выберите накопитель, ISO-образ и режим.
3. Нажмите «СТАРТ». Все данные на выбранном накопителе будут уничтожены.

## Зависимости

| Библиотека | Версия | Источник |
| --- | --- | --- |
| MetroFramework (RunTime + Fonts) | 1.2.0.3 | https://www.nuget.org/packages/MetroFramework |

DLL лежат в `lib\`. Если нужно сменить тему: в `src/MainForm.cs` поменяйте
`sm.Theme = MetroThemeStyle.Dark` на `MetroThemeStyle.Light`, а акцент —
`sm.Style = MetroColorStyle.Blue` на любой из перечисленных.

## Структура

```
app.manifest      — требование прав администратора, DPI
build.cmd         — сборка системным csc.exe (.NET Framework 4.x)
lib/              — MetroFramework.dll, MetroFramework.Fonts.dll
src/
  Program.cs      — точка входа
  MainForm.cs     — интерфейс (Metro)
  Native.cs       — P/Invoke (запись на диск, IOCTL)
  UsbDetector.cs  — список USB-дисков и их томов
  DiskOps.cs      — разметка/форматирование через diskpart
  RawWriter.cs    — посекторная запись ISO
  WindowsMaker.cs — сборка загрузочной Windows-флешки
```
