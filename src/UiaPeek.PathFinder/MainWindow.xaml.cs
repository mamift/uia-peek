using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace UiaPeek.PathFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [DependencyPropertyGenerator.DependencyProperty("ShowProcessesListBox", typeof(bool), DefaultValue = false)]
    [DependencyPropertyGenerator.DependencyProperty("Processes", typeof(ObservableCollection<string>), DefaultValueExpression = "new()")]
    [DependencyPropertyGenerator.DependencyProperty("ProcessesNamesProvider", typeof(ProcessesNameProvider))]
    public partial class MainWindow : Window
    {
        protected override void OnClosing(CancelEventArgs e)
        {
            this.LocatorTabView.OnClosing(e);
            base.OnClosing(e);
        }
    }
}
