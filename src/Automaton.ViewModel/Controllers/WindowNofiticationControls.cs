﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Automaton.ViewModel.Controllers.Interfaces;

namespace Automaton.ViewModel.Controllers
{
    public class WindowNotificationController : IWindowNotificationController
    {
        private static string ProcessName = "Automaton";

        [DllImportAttribute("user32.dll")]
        private static extern int FindWindow(String ClassName, String WindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        public void MoveToFront()
        {
            var allProcs = Process.GetProcessesByName(ProcessName);
            if (allProcs.Length > 0)
            {
                Process proc = allProcs[0];
                var hWnd = FindWindow(null, proc.MainWindowTitle);

                SetForegroundWindow(new IntPtr(hWnd));
                FlashWindow(new IntPtr(hWnd), true);
            }
        }
    }
}