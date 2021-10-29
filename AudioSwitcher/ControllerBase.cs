using AudioSwitcher.Enums;
using AudioSwitcher.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AudioSwitcher
{
    public class ControllerBase
    {
        protected const string AUDIO_TITLE = "Sound";
        protected const string DEFAULT_DEVICE = "Default Device";
        protected const string BUTTON = "Button";
        protected const string SET_DEFAULT = "&Set Default";
        protected const string OK = "OK";
        protected const string PLAYBACK = "Playback";
        protected const string LIST_VIEW = "SysListView32";
        protected const int RETRY_ATTEMPTS = 4;
        protected const int LVIS_SELECTED = 2;
        protected const int BUFFER_SIZE = 512;
        protected const int MAX_TEXT = 50;
        protected const int LVIF_TEXT = 0x0001;
        protected const int LVIF_STATE = 0x0008;
        protected const int BM_CLICK = 0x00F5;
        protected const uint DELETE = 0x00010000;
        protected const uint READ_CONTROL = 0x00020000;
        protected const uint WRITE_DAC = 0x00040000;
        protected const uint WRITE_OWNER = 0x00080000;
        protected const uint SYNCHRONIZE = 0x00100000;
        protected const uint END = 0xFFF;
        protected const uint PROCESS_ALL_ACCESS = (DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE | END);

        public static string Device1Name { get; set; } = "Speakers";
        public static string Device1Information { get; set; } = "Focusrite Usb Audio";
        public static string Device2Name { get; set; } = "Speakers";
        public static string Device2Information { get; set; } = "Realtek High Definition Audio";
        public static Dictionary<VirtualCode, bool> Hotkeys { get; set; } = new Dictionary<VirtualCode, bool>() { { VirtualCode.LCONTROL, false }, { VirtualCode.LMENU, false }, { VirtualCode.KEY_M, false } };

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        protected static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        protected static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref LV_ITEM lpBuffer, IntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, Int32 nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        protected static extern int FindWindowByCaption(string ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern int FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        protected static extern int SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        protected static extern IntPtr SendMessageLVItem(IntPtr hWnd, uint msg, IntPtr wParam, ref LV_ITEM lvi);
    }
}