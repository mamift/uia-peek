using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace UiaPeek.PathFinder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[DependencyPropertyGenerator.DependencyProperty("ShowProcessesListBox", typeof(bool), DefaultValue = false)]
[DependencyPropertyGenerator.DependencyProperty("Processes", typeof(ObservableCollection<string>), DefaultValueExpression = "new()")]
[DependencyPropertyGenerator.DependencyProperty("ProcessesNamesProvider", typeof(ProcessesNameProvider))]
public partial class MainWindow : Window
{
    private static IntPtr _hookID = IntPtr.Zero;
    private static LowLevelMouseProc _proc = HookCallback;


    protected override void OnClosing(CancelEventArgs e)
    {
        this.LocatorTabView.OnClosing(e);
        base.OnClosing(e);
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    // Set hook when the application starts
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hookID = SetHook(_proc);
    }

    // Unhook when the application closes
    private void MainWindow_Closed(object sender, EventArgs e)
    {
        UnhookWindowsHookEx(_hookID);
    }

    // Hook callback method
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam) {
            MessageBox.Show("Mouse click detected!");
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // Set up the hook
    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule) {
            return SetWindowsHookEx(WH_MOUSE_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;

    private enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string lpModuleName);
}