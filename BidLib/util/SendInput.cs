using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace tobid.util {
    
    public class KeyBoardUtil{

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInput, ref INPUT pInput, int cbSize);


        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT {
            [FieldOffset(0)]
            internal int type;//0:mouse event;1:keyboard event;2:hardware event
            [FieldOffset(4)]
            internal MOUSEINPUT mi;
            [FieldOffset(4)]
            internal KEYBDINPUT ki;
            [FieldOffset(4)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT {
            internal int uMsg;
            internal short wParamL;
            internal short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT {
            internal ushort wVk;
            internal ushort wScan;
            internal uint dwFlags;
            internal uint time;
            internal IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal int dwFlags;
            internal int time;
            internal IntPtr dwExtraInfo;
        }

        static public void sendKeyDown(String keyCode) {

            INPUT Input=new INPUT();
            Input.type = 1; //keyboard_input
            Input.ki.wVk = ScreenUtil.keycode[keyCode];
            Input.ki.dwFlags = 0;
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }

        static public void sendKeyUp(String keyCode) {

            INPUT Input = new INPUT();
            Input.type = 1;
            Input.ki.wVk = ScreenUtil.keycode[keyCode];
            Input.ki.dwFlags = 2;//key_up
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }

        static public void clickKey(String keyCode, int interval)
        {
            sendKeyDown(keyCode);
            System.Threading.Thread.Sleep(interval);
            sendKeyUp(keyCode);
        }
    }
}
