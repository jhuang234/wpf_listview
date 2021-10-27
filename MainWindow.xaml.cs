using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_autoScrollEnabled = true;
        public bool IsAutoScrollEnabled
        {
            get
            {
                return m_autoScrollEnabled;
            }

            set
            {
                m_autoScrollEnabled = value;
                //AddLogEntry(0.0f, "INTERNAL", "Auto-scroll is now " + m_autoScrollEnabled);
            }
        }

        //  http://coding-scars.com/log-window-3/
        //LogEntries variable (which is an ObservableCollection) has a CollectionChanged event handler we can subscribe to.
        //So, first of all let’s create a method in MainWindow.xaml.cs to handle the scroll
        //Basically, we ask the LogEntryList ListView for its children,
        //get the scroll control for the list and ask it to go to the bottom.
        //We can also check the NotifyCollectionChangedEventArgs to know
        //whether it was triggered because an item was added or deleted, if we wanted to.
        private void OnLogEntriesChangedScrollToBottom(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsAutoScrollEnabled)
            {
                return;
            }

            if (VisualTreeHelper.GetChildrenCount(LogEntryList) > 0)
            {
                Decorator border = VisualTreeHelper.GetChild(LogEntryList, 0) as Decorator;
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }


    private void WindowKeyDown(object sender, KeyEventArgs e)
     {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                
                CopySelectedValuesToClipboard();
            }
     }

       

    private void CopySelectedValuesToClipboard()
    {
        var builder = new StringBuilder();
        foreach (LogEntry item in LogEntryList.SelectedItems)
            builder.AppendLine(item.Message.ToString());

            Clipboard.SetText(builder.ToString());
            
     }



    private void ListView_Drop(object sender, System.Windows.DragEventArgs e)
        {

            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                long ticks = DateTime.Now.Ticks;
                int counter = 0;

                foreach (var file in files)
                {
                    string previousLine = "";

                    foreach (string line in System.IO.File.ReadLines(file))
                    {

                        decodedLine decodedLine = WpfApp1.ParseFile.ParseLine(previousLine, line);

                        if (decodedLine.valid) {

                            LogEntries.Add(new LogEntry
                            {
                                Timestamp = counter,
                                System = line,//decodedLine.duration,
                                Message = decodedLine.decodedData
                            }); ;
                            previousLine = line;
                            counter++;
                        }
                    }                                        
                }
            }
        }

        public ObservableCollection<LogEntry> LogEntries;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;


            LogEntries = new ObservableCollection<LogEntry>();

            // tie View with ViewModel
            LogEntryList.ItemsSource = LogEntries;

            //tie it with our checkbox, add this
            LogEntries.CollectionChanged += OnLogEntriesChangedScrollToBottom;


            // add test data
            /*
            new Thread(() =>
            {
                long ticks = DateTime.Now.Ticks;
                //Random random = new Random();

                for (int i = 0; i < 100; ++i)
                {
                    App.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        LogEntries.Add(new LogEntry
                        {
                            Timestamp = (float)(DateTime.Now.Ticks - ticks) / TimeSpan.TicksPerSecond,
                            System = "TEST",
                            Message = "Sample message!"
                        });
                    });

                    //Thread.Sleep(random.Next() % 100 + 50);
                }
            }).Start();
            */

        }

        private void LogEntryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    }

    public class LogEntry
    {
        public float Timestamp { get; set; }
        public string System { get; set; }
        public string Message { get; set; }
    }

    


}
