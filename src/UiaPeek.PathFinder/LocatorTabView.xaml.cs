#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UiaPeek.Domain;
using UiaPeek.Domain.Models;
using UiaPeek.PathFinder.Extensions;
using UIAutomationClient;

using Timer = System.Timers.Timer;

namespace UiaPeek.PathFinder;

[DependencyPropertyGenerator.DependencyProperty("ShowProcessesListBox", typeof(bool), DefaultValue = false)]
[DependencyPropertyGenerator.DependencyProperty("Processes", typeof(System.Collections.ObjectModel.ObservableCollection<string>), DefaultValueExpression = "new()")]
[DependencyPropertyGenerator.DependencyProperty("ProcessesNamesProvider", typeof(ProcessesNameProvider))]
public partial class LocatorTabView : UserControl, IDisposable
{
    private readonly UiaPeekRepository _domain = new();

    private bool _isRunning;
    private double _refreshSpeed = 1000;

    public LocatorTabView()
    {
        InitializeComponent();
        Writer = new LogWriter();
        ProcessesNamesProvider = new ProcessesNameProvider(this.Processes);
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetPhysicalCursorPos(out TagPoint lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetPhysicalCursorPos(int x, int y);

    public LogWriter Writer { get; }

    public void Dispose()
    {
        Writer.Dispose();
    }

    private List<string> GetCurrentProcesses()
    {
        return Process.GetProcesses().Select(p => p.ProcessName).ToList();
    }

    public void OnClosing(CancelEventArgs e)
    {
        Writer.SafeDispose();
    }

    protected override void OnInitialized(EventArgs e)
    {
        this.Processes.CollectionChanged += ProcessesOnCollectionChanged;
        this.Processes.AddRange(GetCurrentProcesses());

        base.OnInitialized(e);
    }

    private void ProcessesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Processes.Any() && ShowProcessesListBox == false) {
            ShowProcessesListBox = true;
        }
    }

    private Window? GetOwnerWindow()
    {
        return Dispatcher.Invoke(() => Window.GetWindow(this));
    }

    private string GetProcessFilterTextSafely()
    {
        var owner = GetOwnerWindow();
        return owner is null
            ? FilterTextBox.Text
            : (string)owner.Dispatcher.Invoke(() => FilterTextBox.Text);
    }

    private void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        StartStop((Button)sender);
    }

    private void BtnStartStop_AccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
    {
        StartStop((Button)sender);
    }

    private void StartStop(Button startStopButton)
    {
        _isRunning = !_isRunning;
        SetLabel(startStopButton);

        Task.Run(() =>
        {
            while (_isRunning)
            {
                var text = GetProcessFilterTextSafely();

                GetPhysicalCursorPos(out TagPoint point);

                UiaChainModel chain = _domain.Peek(point.X, point.Y);

                var possibleProcessName = chain.TopWindow.GetPossibleProcessName();

                if (text.Equals(possibleProcessName, StringComparison.CurrentCultureIgnoreCase))
                {
                    Writer.SerializeAndWrite(chain, possibleProcessName);
                }

                string xpath = chain.Locator;

                Dispatcher.BeginInvoke(() =>
                {
                    TxbPath.Text = xpath;
                    TxbAxisX.Text = point.X.ToString();
                    TxbAxisY.Text = point.Y.ToString();
                });

                Thread.Sleep(TimeSpan.FromMilliseconds(_refreshSpeed));
            }
        });
    }

    private void SetLabel(Button button)
    {
        button.Content = _isRunning ? "⬛ _Stop" : "▶ _Start";
    }

    private void SldRefreshSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _refreshSpeed = ((Slider)sender).Value;
    }

    private void BtnSetPosition_Click(object sender, RoutedEventArgs e)
    {
        SetPosition();
    }

    private void BtnSetPosition_AccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
    {
        SetPosition();
    }

    private void SetPosition()
    {
        Dispatcher.BeginInvoke(() =>
        {
            _ = int.TryParse(TxbAxisX.Text, out int x);
            _ = int.TryParse(TxbAxisY.Text, out int y);

            SetPhysicalCursorPos(x, y);
        });
    }

    private void BtnTestPath_Click(object sender, RoutedEventArgs e)
    {
        TestPath(sender, e);
    }

    private void BtnTestPath_AccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
    {
        TestPath(sender, e);
    }

    private async void TestPath(object sender, RoutedEventArgs e)
    {
        BtnTestPath.IsEnabled = false;

        LblStatus.Content = "Testing...";
        LblStatus.Foreground = Brushes.Black;
        LblStatus.Visibility = Visibility.Visible;

        string xpath = TxbPath.Text.Trim();

        Stopwatch sw = Stopwatch.StartNew();

        Timer ticker = new(100) { AutoReset = true };
        ticker.Start();

        try
        {
            bool found = await Task.Run(() =>
            {
                CUIAutomation8 automation = new();
                IUIAutomationElement element = automation.GetElement(xpath);
                return element != null;
            });

            sw.Stop();
            ticker.Stop();

            LblStatus.Foreground = found ? Brushes.Green : Brushes.Red;
            LblStatus.Content = found ? "Found" : "Not Found";
            LblTime.Content = $"{sw.Elapsed.TotalSeconds:0.000}s";
        }
        catch (Exception)
        {
            ticker.Stop();

            LblStatus.Foreground = Brushes.Red;
            LblStatus.Content = "Error";
        }
        finally
        {
            BtnTestPath.IsEnabled = true;
        }
    }

    private void OpenLogBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var newp = new ProcessStartInfo(Writer.FilePath)
        {
            UseShellExecute = true
        };

        Dispatcher.Invoke(() =>
        {
            Process.Start(newp);
        });
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct TagPoint
    {
        public int X;
        public int Y;
    }
}
