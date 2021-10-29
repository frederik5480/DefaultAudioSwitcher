using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        private const string HEADSET = "Focusrite Usb Audio";
        private const string SPEAKERS = "Realtek High Definition Audio";
        private const string AUDIO_TITLE = "Sound";
        private const string BUTTON = "Button";
        private const string APPLY = "&Apply";

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
            Debug.WriteLine(parentWindow);
            while (parentWindow == 0)
            {
                System.Threading.Thread.Sleep(500);
                parentWindow = FindWindowByCaption(null, AUDIO_TITLE);
                Debug.WriteLine(parentWindow);
            }
            if (parentWindow != 0)
            {
                var control = FindWindowEx(new IntPtr(parentWindow), IntPtr.Zero, null, "Playback");
                var defaultsButton = FindWindowEx(new IntPtr(control), IntPtr.Zero, "Button", "&Set Default");
                var speakerList = FindWindowEx(new IntPtr(control), IntPtr.Zero, "SysListView32", null);
                Debug.WriteLine(defaultsButton);
                bool res = IsWindowEnabled(new IntPtr(defaultsButton));
                if (res)
                    Debug.WriteLine("ENABLED");
                else
                    Debug.WriteLine("DISABLED");
                System.Threading.Thread.Sleep(500);
                var count = SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
                System.Threading.Thread.Sleep(500);
                LV_ITEM[] items = new LV_ITEM[count];
                string[] texts = new string[count];
                for (int i = 0; i < count; i++)
                {
                    const int BUFFER_SIZE = 512;
                    const int LVIF_TEXT = 0x0001;
                    items[i].mask = LVIF_TEXT;
                    items[i].cchTextMax = BUFFER_SIZE;
                    items[i].iItem = 0;
                    items[i].pszText = Marshal.AllocHGlobal(BUFFER_SIZE);
                    IntPtr ptrLvi = Marshal.AllocHGlobal(Marshal.SizeOf(items[i]));
                    var something = SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEM, IntPtr.Zero, ptrLvi);
                    texts[i] = Marshal.PtrToStringAuto(items[i].pszText);
                }
            }
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern int FindWindowByCaption(string ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }

    public enum LVM
    {
        FIRST = 0x1000,
        SETUNICODEFORMAT = 0x2005,        // CCM_SETUNICODEFORMAT,
        GETUNICODEFORMAT = 0x2006,        // CCM_GETUNICODEFORMAT,
        GETBKCOLOR = (FIRST + 0),
        SETBKCOLOR = (FIRST + 1),
        GETIMAGELIST = (FIRST + 2),
        SETIMAGELIST = (FIRST + 3),
        GETITEMCOUNT = (FIRST + 4),
        GETITEMA = (FIRST + 5),
        GETITEMW = (FIRST + 75),
        GETITEM = GETITEMW,
        SETITEMSTATE = (FIRST + 43),
        GETITEMSTATE = (FIRST + 44),
        GETITEMTEXTA = (FIRST + 45),
        GETITEMTEXTW = (FIRST + 115),
        SETITEMTEXTA = (FIRST + 46),
        SETITEMTEXTW = (FIRST + 116),
        SETITEMCOUNT = (FIRST + 47),
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct LV_ITEM
    {
        public UInt32 mask;
        public Int32 iItem;
        public Int32 iSubItem;
        public UInt32 state;
        public UInt32 stateMask;
        public IntPtr pszText;
        public Int32 cchTextMax;
        public Int32 iImage;
        public IntPtr lParam;
    }
}
