using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace tobid.util {
    
    public class KeyBoardUtil{

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInput, ref INPUT pInput, int cbSize);

        private enum InputType
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2,
        }
        [Flags()]
        private enum KEYEVENTF
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            UNICODE = 0x0004,
            SCANCODE = 0x0008,
        }  

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

        private static IDictionary<string, WindowsInput.VirtualKeyCode> keycode = new Dictionary<string, WindowsInput.VirtualKeyCode>();
        static KeyBoardUtil()
        {
            keycode.Add("LCONTROL", WindowsInput.VirtualKeyCode.LCONTROL);
            keycode.Add("RCONTROL", WindowsInput.VirtualKeyCode.RCONTROL);
        }

        static public void simulateKeyUP(String key)
        {
            WindowsInput.InputSimulator.SimulateKeyUp(keycode[key]);
        }

        static public void sendMessage(String message, int interval = 0)
        {
            if (interval == 0)
                WindowsInput.InputSimulator.SimulateTextEntry(message);
            else
            {
                for (int i = 0; i < message.Length; i++)
                {
                    byte sendChar = ScreenUtil.keycode[message[i].ToString()];
                    INPUT[] keyUpDown = new INPUT[2];
                    keyUpDown[0] = new INPUT();
                    keyUpDown[0].type = (int)InputType.INPUT_KEYBOARD;
                    keyUpDown[0].ki.dwFlags = 0;
                    keyUpDown[0].ki.wVk = sendChar;
                    keyUpDown[1] = new INPUT();
                    keyUpDown[1].type = (int)InputType.INPUT_KEYBOARD;
                    keyUpDown[1].ki.wVk = sendChar;
                    keyUpDown[1].ki.dwFlags = (int)KEYEVENTF.KEYUP;
                    SendInput(2, ref keyUpDown[0], Marshal.SizeOf(keyUpDown[0]));
                    System.Threading.Thread.Sleep(interval);
                }
            }
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

        static public void moveMouse(int x, int y)
        {
            INPUT Input = new INPUT();
            Input.type = 0;
            Input.mi.dx = x;
            Input.mi.dy = y;
            Input.mi.mouseData = 0;
            Input.mi.dwFlags = (int)(MouseEventFlags.Absolute | MouseEventFlags.Move);   //MOUSEEVENTF_ABSOLUTE 代表决对位置  MOUSEEVENTF_MOVE代表移动事件
            Input.mi.time = 0;
            Input.mi.dwExtraInfo = (IntPtr)0;
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }

        static public void mouseClick(int x, int y) {

            INPUT Input = new INPUT();
            Input.type = 0;
            Input.mi.dx = x;
            Input.mi.dy = y;
            Input.mi.mouseData = 0;
            Input.mi.dwFlags = (int)(MouseEventFlags.Absolute | MouseEventFlags.Move | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp);   //MOUSEEVENTF_ABSOLUTE 代表决对位置  MOUSEEVENTF_MOVE代表移动事件
            Input.mi.time = 0;
            Input.mi.dwExtraInfo = (IntPtr)0;
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }
    }
}
