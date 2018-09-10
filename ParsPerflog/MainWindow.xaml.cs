using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace ParsPerflog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    


    public partial class MainWindow : Window
    {
        private Dictionary<string,List<GaugePoint>> gaugePoints = new Dictionary<string,List<GaugePoint>>();
        private Thread uiThread;
        private string outputFilePath = @"C:\temp\";
        private string savedObject = "data.bin";
        private string perflog = "perflog_small.log";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            LockUI();
            if (File.Exists(outputFilePath + savedObject))
            {
                await LoadObjectAsync();
                foreach (string methodName in gaugePoints.Keys)
                {
                    methodCombo.Items.Add(methodName);
                }
            }
            else
            {
                await ParseAsync();
            }
            UnlockUI();
        }

        private async Task LoadObjectAsync()
        {
            await Task.Run(() =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                AddStatusText("Reading object...\n");
                gaugePoints = Util.Deserialize<Dictionary<string, List<GaugePoint>>>(File.Open(outputFilePath + savedObject, FileMode.Open));
                watch.Stop();
                AddStatusText("Loaded object in " + watch.Elapsed+Environment.NewLine);
                
            });
        }

        private async Task ExportAsync(string key)
        {
            await Task.Run(() =>
            {             
                List<GaugePoint> points = gaugePoints[key];
                string[] split = key.Split('.');
                string name = split[split.Length - 1];
                int counter = 0;
                foreach (GaugePoint g in points)
                {
                    counter++;
                    File.AppendAllText(outputFilePath + name + ".csv", g.time.ToString("HH:mm:ss.fff") + "," + g.Duration + Environment.NewLine);
                    AddStatusText("Adding gauge point: " + counter + " of " + points.Count() + " (" + string.Format("{0:N2}", (double)(((double)counter / (double)points.Count()) * 100)) + "%)", true);
                }
                AddStatusText("Done exporting.", true);
            });
        }

        private void AddStatusText(string text)
        {
            AddStatusText(text, false);
        }

        private void AddStatusText(string text,bool clear)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate ()
            {
                if (clear) this.statusText.Document.Blocks.Clear();
                this.statusText.AppendText(text);
            }));
        }

        private async Task ParseAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    AddStatusText("Starting parse operation...\n");
                    
                    var lines = File.ReadAllLines(outputFilePath + perflog);

                    AddStatusText("Done.\n");
                    int counter = 0;

                    foreach (var line in lines)
                    {
                        if (line.Contains("Camel"))
                        {
                            string[] split = line.Split(null);
                            int seconds = Convert.ToInt32(split[split.Length - 2]);
                            string method = split[split.Length - 3];

                            string date = split[0];
                            string time = split[1].Split(',')[0];
                            int millis = Convert.ToInt32(split[1].Split(',')[1]);
                            DateTime formattedDate = DateTime.ParseExact(date + " " + time, "yyyy-MM-dd HH:mm:ss", null);
                            formattedDate = formattedDate.AddMilliseconds(millis);

                            counter++;
                            GaugePoint gaugePoint = new GaugePoint
                            {
                                Duration = seconds,
                                Method = method,
                                time = formattedDate
                            };

                            if (gaugePoints.ContainsKey(method))
                            {
                                gaugePoints[method].Add(gaugePoint);
                            }
                            else
                            {
                                gaugePoints.Add(method, new List<GaugePoint>());
                                gaugePoints[method].Add(gaugePoint);
                            }
                      
                            AddStatusText("Adding gauge point: " + counter + " of " + lines.Count() + " (" + string.Format("{0:N2}", (double)(((double)counter / (double)lines.Count()) * 100)) + "%)",true);
                        }
                        
                    }

                    Util.Serialize(gaugePoints, File.Open(outputFilePath+savedObject, FileMode.Create));
                    AddStatusText("\nSaved.");
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            });

        }

        private async void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            LockUI();
            if (methodCombo.HasItems && methodCombo.SelectedItem != null)
            {
                string key = (string)methodCombo.SelectedItem;
                await ExportAsync(key);
            }
            UnlockUI();
        }

        private void methodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string methodKey = (string)methodCombo.SelectedItem;
            List<GaugePoint> currentGaugePoints = gaugePoints[methodKey];
            int max = currentGaugePoints.Max(gp => gp.Duration);
            int min = currentGaugePoints.Min(gp => gp.Duration);
            double avg = currentGaugePoints.Average(gp => gp.Duration);
            var list = currentGaugePoints.Select(item => item.Duration).ToList();
            int median = Util.Median(list);
            AddStatusText("Info: max: " + max + "ms, min: " + min + "ms, avg: " + string.Format("{0:N2}",avg) + "ms, median: " + median,true);
        }

        private void LockUI()
        {
            progressBar.IsIndeterminate = true;
            startBtn.IsEnabled = false;
            exportBtn.IsEnabled = false;
            methodCombo.IsEnabled = false;                 
        }

        private void UnlockUI()
        {
            progressBar.IsIndeterminate = false;
            startBtn.IsEnabled = true;
            exportBtn.IsEnabled = true;
            methodCombo.IsEnabled = true;
        }
    }
}
