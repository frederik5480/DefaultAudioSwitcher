using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioSwitcher
{
    internal class Controller : IDisposable
    {
        private GlobalKeyboardHook _globalKeyboardHook;
        private bool ctrl;
        private bool ctrlAlt;
        private bool all;
        private bool success;
        private const string HEADSET = "Speakers";
        private const string HEADSET_SOUNDCARD = "Focusrite Usb Audio";
        private const string SPEAKERS = "Speakers";
        private const string SPEAKERS_SOUNDCARD = "Realtek High Definition Audio";
        private const string AUDIO_TITLE = "Sound";
        private const string DEFAULT_DEVICE = "Default Device";
        private const string BUTTON = "Button";
        private const string SET_DEFAULT = "&Set Default";
        private const string OK = "OK";
        private const string PLAYBACK = "Playback";
        private const string LIST_VIEW = "SysListView32";
        private const int LVIS_SELECTED = 2;
        private const int BUFFER_SIZE = 512;
        private const int LVIF_TEXT = 0x0001;
        private const int LVIF_STATE = 0x0008;
        private const int BM_CLICK = 0x00F5;
        private const uint DELETE = 0x00010000;
        private const uint READ_CONTROL = 0x00020000;
        private const uint WRITE_DAC = 0x00040000;
        private const uint WRITE_OWNER = 0x00080000;
        private const uint SYNCHRONIZE = 0x00100000;
        private const uint END = 0xFFF;
        private const uint PROCESS_ALL_ACCESS = (DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE | END);

        public void SetupKeyboardHooks()
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            var code = e.KeyboardData.VirtualCode;
            if (code == 162)
                ctrl = true;
            else if (ctrl && code == 164)
                ctrlAlt = true;
            else if (ctrlAlt && code == 77 && !all)
            {
                all = true;
                ctrl = false;
                ctrlAlt = false;
                SwitchAudio();
            }
            else
            {
                ctrl = false;
                ctrlAlt = false;
                all = false;
            }
        }

        public void Dispose()
        {
            _globalKeyboardHook?.Dispose();
        }

        private void SwitchAudio()
        {
            Process.Start("control", "mmsys.cpl,,0");
            var parentWindow = FindWindowByCaption(null, AUDIO_TITLE);
            while (parentWindow == 0)
            {
                System.Threading.Thread.Sleep(500);
                parentWindow = FindWindowByCaption(null, AUDIO_TITLE);
            }
            var okButton = FindWindowEx(new IntPtr(parentWindow), IntPtr.Zero, BUTTON, OK);
            var control = FindWindowEx(new IntPtr(parentWindow), IntPtr.Zero, null, PLAYBACK);
            var defaultButton = FindWindowEx(new IntPtr(control), IntPtr.Zero, BUTTON, SET_DEFAULT);
            var speakerList = FindWindowEx(new IntPtr(control), IntPtr.Zero, LIST_VIEW, null);

            var count = SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            LV_ITEM[,] items = new LV_ITEM[count, 3];
            string[][] texts = new string[count][];

            IntPtr lpRemoteBuffer = IntPtr.Zero;
            IntPtr lpLocalBuffer = IntPtr.Zero;

            lpLocalBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);

            uint processId;
            var threadId = GetWindowThreadProcessId(new IntPtr(speakerList), out processId);
            if ((threadId == 0) || (processId == 0))
                throw new ArgumentException("hWnd");

            var process = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (process == IntPtr.Zero)
                throw new ApplicationException("Failed to access process");

            lpRemoteBuffer = VirtualAllocEx(process, IntPtr.Zero, BUFFER_SIZE, (uint)AllocationType.Commit, (uint)MemoryProtection.ReadWrite);
            if (lpRemoteBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in remote process");

            for (int i = 0; i < count; i++)
            {
                texts[i] = new string[3];
                for (int j = 0; j < 3; j++)
                {
                    items[i, j].mask = LVIF_TEXT;
                    items[i, j].cchTextMax = 50;
                    items[i, j].iItem = i;
                    items[i, j].iSubItem = j;
                    items[i, j].pszText = (IntPtr)(lpRemoteBuffer.ToInt32() + Marshal.SizeOf(typeof(LV_ITEM)));
                    success = WriteProcessMemory(process, lpRemoteBuffer, ref items[i, j], new IntPtr(Marshal.SizeOf(typeof(LV_ITEM))), IntPtr.Zero);
                    if (!success)
                        throw new SystemException("Failed to write to process memory");

                    SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEM, IntPtr.Zero, lpRemoteBuffer);

                    success = ReadProcessMemory(process, lpRemoteBuffer, lpLocalBuffer, BUFFER_SIZE, IntPtr.Zero);
                    if (!success)
                        throw new SystemException("Failed to read from process memory");
                    texts[i][j] = Marshal.PtrToStringAuto((IntPtr)(lpLocalBuffer + Marshal.SizeOf(typeof(LV_ITEM))));
                }
            }

            var r = from text in texts
                    where text[2] == DEFAULT_DEVICE
                    select text;
            if (r == null)
                return;
            var pos = GetNewDevicePos(texts, r.First()[1] == HEADSET_SOUNDCARD);
            var item = new LV_ITEM
            {
                mask = LVIF_STATE,
                stateMask = LVIS_SELECTED,
                state = LVIS_SELECTED
            };

            lpRemoteBuffer = VirtualAllocEx(process, IntPtr.Zero, BUFFER_SIZE, (uint)AllocationType.Commit, (uint)MemoryProtection.ReadWrite);
            success = WriteProcessMemory(process, lpRemoteBuffer, ref item, new IntPtr(Marshal.SizeOf(typeof(LV_ITEM))), IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to write to process memory");
            SendMessage(new IntPtr(speakerList), (uint)LVM.SETITEMSTATE, new IntPtr(pos), lpRemoteBuffer);

            SendMessage(new IntPtr(defaultButton), BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            SendMessage(new IntPtr(okButton), BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        private int GetNewDevicePos(string[][] texts, bool speakers)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (speakers)
                {
                    if (texts[i][0] == SPEAKERS && texts[i][1] == SPEAKERS_SOUNDCARD)
                        return i;
                }
                else
                {
                    if (texts[i][0] == HEADSET && texts[i][1] == HEADSET_SOUNDCARD)
                        return i;
                }
            }
            return 0;
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref LV_ITEM lpBuffer, IntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, Int32 nSize, IntPtr lpNumberOfBytesRead);


        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern int FindWindowByCaption(string ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageLVItem(IntPtr hWnd, uint msg, IntPtr wParam, ref LV_ITEM lvi);
    }

    public enum LVM
    {
        FIRST = 0x1000,
        GETITEMCOUNT = (FIRST + 4),
        GETITEM = (FIRST + 75),
        SETITEMSTATE = (FIRST + 43),
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LV_ITEM
    {
        public int mask;
        public int iItem;
        public int iSubItem;
        public int state;
        public int stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        internal int lParam;
        internal int iIndent;
    }
}
