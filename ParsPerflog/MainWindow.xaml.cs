using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace ParsPerflog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    


    public partial class MainWindow : Window
    {
        public delegate void AddStatusTextCallback(string message);
        private Dictionary<string,List<GaugePoint>> gaugePoints = new Dictionary<string,List<GaugePoint>>();
        private Thread uiThread;
        private string outputFilePath = @"C:\temp\trgfuckup\";
        private string savedObject = "data.bin";
        private string perflog = "perflog.log";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(outputFilePath+savedObject))
            {
                uiThread = new Thread(LoadObjectEngine);
                uiThread.Start();
            }
            else
            {
                uiThread = new Thread(ParseEngine);
                uiThread.Start();
            }
        }

        private void AddText(string text)
        {
            statusText.Dispatcher.Invoke(
                new AddStatusTextCallback(this.AddStatusText),
                new object[] { text }
            );
        }

        private void LoadObjectEngine()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            AddText("Reading object...");
            gaugePoints = Util.Deserialize<Dictionary<string, List<GaugePoint>>>(File.Open(outputFilePath + savedObject, FileMode.Open));
            watch.Stop();
            AddText("Loaded object in " + watch.Elapsed);
            PopulateCombo();
        }

        private void PopulateCombo()
        {
            foreach(string methodName in gaugePoints.Keys)
            {
                methodCombo.Items.Add(methodName);
            }
           
        }

        private void Export()
        {
            if(methodCombo.HasItems && methodCombo.SelectedItem!=null)
            {
                string key = (string)methodCombo.SelectedItem;
                List<GaugePoint> points = gaugePoints[key];
                string[] split = key.Split('.');
                string name = split[split.Length - 1];
                foreach (GaugePoint g in points)
                {
                    File.AppendAllText(outputFilePath+name+".csv", g.time.ToString("HH:mm:ss.fff") + "," + g.Duration + Environment.NewLine);
                }
            }
        }

        private void ParseEngine()
        {   
            AddText("Starting...\n");
            var lines = File.ReadAllLines(outputFilePath+perflog);

            AddText("Done.\n");
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
                    AddText("Adding gauge point: " + counter + " of " + lines.Count()+" ("+ string.Format("{0:N2}", (double)(((double)counter/(double)lines.Count())*100))+"%)");

                    //File.AppendAllText(outputFile, gaugePoint.time.ToString("HH:mm:ss.fff") + "," + gaugePoint.Duration+Environment.NewLine);
                }

            }

            Util.Serialize(gaugePoints, File.Open(savedObject, FileMode.Create));
            AddText("Saved.");
            //Dictionary<string, List<GaugePoint>> deserializeObject = Util.Deserialize<Dictionary<string, List<GaugePoint>>>(File.Open(savedObject, FileMode.Open));
        }

        private void AddStatusText(string text)
        {
            statusText.Document.Blocks.Clear();
            statusText.AppendText(text);
        }

        private void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            uiThread = new Thread(Export);
            uiThread.Start();
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
            AddText("Info: max: " + max + "ms, min: " + min + "ms, avg: " + string.Format("{0:N2}",avg) + "ms, median: " + median);
        }

        
    }
}
