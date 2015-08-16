using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Admin
{
    public class WindowHelper
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindowA")]
        public static extern IntPtr FindWindowA(String lp1, String lp2);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        public static extern bool AllocConsole(); //启动窗口

        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "FreeConsole")]
        public static extern bool FreeConsole();      //释放窗口，即关闭

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);//找出运行的窗口

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        public extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert); //取出窗口运行的菜单

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        public extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags); //灰掉按钮

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        public static extern bool SetConsoleTitle(string strMessage);
    }
}
