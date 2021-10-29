using System;
using System.Runtime.InteropServices;

namespace AudioSwitcher.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LV_ITEM
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
