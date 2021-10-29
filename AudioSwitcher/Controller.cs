using AudioSwitcher.Enums;
using AudioSwitcher.KeyboardHook;
using AudioSwitcher.Structs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace AudioSwitcher
{
    internal class Controller : ControllerBase, IDisposable
    {
        private GlobalKeyboardHook _globalKeyboardHook;
        private bool ctrl;
        private bool ctrlAlt;
        private bool all;

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
            // Get the handles of all the necessary windows.
            var parentWindowHandle = GetParentWindow();
            var controlHandle = FindWindowEx(new IntPtr(parentWindowHandle), IntPtr.Zero, null, PLAYBACK);
            var okButtonHandle = FindWindowEx(new IntPtr(parentWindowHandle), IntPtr.Zero, BUTTON, OK);
            var defaultButtonHandle = FindWindowEx(new IntPtr(controlHandle), IntPtr.Zero, BUTTON, SET_DEFAULT);
            var speakerListHandle = FindWindowEx(new IntPtr(controlHandle), IntPtr.Zero, LIST_VIEW, null);

            var lpRemoteBuffer = IntPtr.Zero;
            var lpLocalBuffer = IntPtr.Zero;
            IntPtr processHandle;

            PrepareProcessAndBuffers(speakerListHandle, out lpRemoteBuffer, out lpLocalBuffer, out processHandle);
            int listViewItemSize = Marshal.SizeOf(typeof(LV_ITEM));
            string[][] itemText = GetItemTexts(speakerListHandle, lpRemoteBuffer, lpLocalBuffer, processHandle, listViewItemSize);

            // Find the current default device which we use to determine the position of the to-be default device.
            var defaultDevice = from text in itemText
                                where text[2] == DEFAULT_DEVICE
                                select text;
            if (defaultDevice == null)
                return;
            var pos = GetNewDevicePos(itemText, defaultDevice.First()[1] == HEADSET_SOUNDCARD);

            SelectDevice(speakerListHandle, processHandle, lpRemoteBuffer, listViewItemSize, pos);

            SendMessage(new IntPtr(defaultButtonHandle), BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            SendMessage(new IntPtr(okButtonHandle), BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        private static void SelectDevice(int speakerListHandle, IntPtr processHandle, IntPtr lpRemoteBuffer, int listViewItemSize, int pos)
        {
            var item = new LV_ITEM
            {
                mask = LVIF_STATE,
                stateMask = LVIS_SELECTED,
                state = LVIS_SELECTED
            };

            lpRemoteBuffer = VirtualAllocEx(processHandle, IntPtr.Zero, BUFFER_SIZE, (uint)AllocationType.Commit, (uint)MemoryProtection.ReadWrite);
            if (!WriteProcessMemory(processHandle, lpRemoteBuffer, ref item, new IntPtr(listViewItemSize), IntPtr.Zero))
                throw new SystemException("Failed to write to process memory");

            SendMessage(new IntPtr(speakerListHandle), (uint)LVM.SETITEMSTATE, new IntPtr(pos), lpRemoteBuffer);
        }

        private string[][] GetItemTexts(int speakerList, IntPtr lpRemoteBuffer, IntPtr lpLocalBuffer, IntPtr process, int listViewItemSize)
        {
            var itemCount = SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            string[][] itemText = new string[itemCount][];
            for (int i = 0; i < itemCount; i++)
            {
                itemText[i] = new string[3];
                for (int j = 0; j < 3; j++)
                {
                    itemText[i][j] = GetTextFromItem(speakerList, lpRemoteBuffer, lpLocalBuffer, process, listViewItemSize, i, j);
                }
            }
            return itemText;
        }

        private string GetTextFromItem(int speakerList, IntPtr lpRemoteBuffer, IntPtr lpLocalBuffer, IntPtr process, int listViewItemSize, int itemId, int subItemId)
        {
            var item = new LV_ITEM
            {
                mask = LVIF_TEXT,
                cchTextMax = MAX_TEXT,
                iItem = itemId,
                iSubItem = subItemId,
                pszText = (IntPtr)(lpRemoteBuffer.ToInt32() + listViewItemSize)
            };

            if (!WriteProcessMemory(process, lpRemoteBuffer, ref item, new IntPtr(listViewItemSize), IntPtr.Zero))
                throw new SystemException("Failed to write to process memory");

            SendMessage(new IntPtr(speakerList), (uint)LVM.GETITEM, IntPtr.Zero, lpRemoteBuffer);

            if (!ReadProcessMemory(process, lpRemoteBuffer, lpLocalBuffer, BUFFER_SIZE, IntPtr.Zero))
                throw new SystemException("Failed to read from process memory");

            return Marshal.PtrToStringAuto(lpLocalBuffer + listViewItemSize);
        }

        private static void PrepareProcessAndBuffers(int speakerList, out IntPtr lpRemoteBuffer, out IntPtr lpLocalBuffer, out IntPtr process)
        {
            uint processId;
            var threadId = GetWindowThreadProcessId(new IntPtr(speakerList), out processId);
            if ((threadId == 0) || (processId == 0))
                throw new ArgumentException("hWnd");

            process = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (process == IntPtr.Zero)
                throw new ApplicationException("Failed to access process");

            lpRemoteBuffer = VirtualAllocEx(process, IntPtr.Zero, BUFFER_SIZE, (uint)AllocationType.Commit, (uint)MemoryProtection.ReadWrite);
            if (lpRemoteBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in remote process");

            lpLocalBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);
        }

        private static int GetParentWindow()
        {
            var parentWindow = FindWindowByCaption(null, AUDIO_TITLE);
            for (int i = 0; i < RETRY_ATTEMPTS && parentWindow == 0; i++)
            {
                System.Threading.Thread.Sleep(500);
                parentWindow = FindWindowByCaption(null, AUDIO_TITLE);
            }

            return parentWindow;
        }

        private int GetNewDevicePos(string[][] texts, bool speakers)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (speakers && texts[i][0] == SPEAKERS && texts[i][1] == SPEAKERS_SOUNDCARD)
                    return i;
                if (!speakers && texts[i][0] == HEADSET && texts[i][1] == HEADSET_SOUNDCARD)
                    return i;
            }
            throw new SystemException("Can't find position of the new device.");
        }
    }
}
