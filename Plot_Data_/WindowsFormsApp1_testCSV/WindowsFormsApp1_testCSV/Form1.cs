using System;
using System.Windows.Forms;
using System.Drawing;  // add Color namespace
using ScottPlot;        // add ScottPlot namespace for Plot extensions
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics; // For Process.Start to open file explorer

namespace WindowsFormsApp1_testCSV
{
    public partial class Form1 : Form
    {
        // fields for incremental plotting
        private System.Windows.Forms.Timer plotTimer;
        private int plotIndex;
        private List<double> xsData;
        private List<double> ysData;
        private List<double> xsDisplayed;
        private List<double> ysDisplayed;
        // fields for current data plotting
        private List<double> currentData;
        private List<double> currentDisplayed;
        private string csvPath;
        private DateTime lastModified;
        private bool monitorCsvUpdates = true;

        public Form1()
        {
            InitializeComponent();
            Form1_Load(null, null);
        }

        // THIS is the correct place to add plot logic
        private void Form1_Load(object sender, EventArgs e)
        {
            // load all data
            var xsList = new List<double>();
            var ysList = new List<double>();
            var currentList = new List<double>();
            
            csvPath = @"C:\Program Files\OpenTAP\Steps Reports\BT2202_RealTimeReportPlot.csv";
            if (!File.Exists(csvPath))
            {
                MessageBox.Show($"CSV file not found: {csvPath}");
                return;
            }

            // Store last modified time
            lastModified = File.GetLastWriteTime(csvPath);

            LoadCsvData(xsList, ysList, currentList);
            
            // initialize for incremental display
            xsData = xsList;
            ysData = ysList;
            currentData = currentList;
            xsDisplayed = new List<double>();
            ysDisplayed = new List<double>();
            currentDisplayed = new List<double>();
            plotIndex = 0;
            plotTimer = new System.Windows.Forms.Timer();
            plotTimer.Interval = 500; // 0.5 second interval
            plotTimer.Tick += PlotTimer_Tick;
            plotTimer.Start();
        }        

        private void LoadCsvData(List<double> xsList, List<double> ysList, List<double> currentList)
        {
            string[] lines = File.ReadAllLines(csvPath);
            // Skip header row
            int timeCounter = 0;
            for (int i = 1; i < lines.Length; i++) // Start from 1 to skip header
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2 && 
                    double.TryParse(parts[0], out double voltage) && 
                    double.TryParse(parts[1], out double current))
                {
                    // Use voltage as Y value, current as current value, and auto-increment time as X value
                    ysList.Add(voltage);
                    currentList.Add(current);
                    xsList.Add(timeCounter);
                    timeCounter++;
                }
            }
        }        

        // handler to plot one new point per second
        private void PlotTimer_Tick(object sender, EventArgs e)
        {
            // Check if CSV file has been updated
            if (monitorCsvUpdates && File.Exists(csvPath))
            {
                DateTime currentModified = File.GetLastWriteTime(csvPath);
                if (currentModified > lastModified)
                {
                    // File has been updated, reload data
                    var newXsList = new List<double>();
                    var newYsList = new List<double>();
                    var newCurrentList = new List<double>();
                    LoadCsvData(newXsList, newYsList, newCurrentList);
                    
                    // First check for changes in existing data points
                    int existingCount = Math.Min(ysData.Count, newYsList.Count);
                    for (int i = 0; i < existingCount; i++)
                    {
                        // Check if voltage or current data has changed
                        if (Math.Abs(ysData[i] - newYsList[i]) > 0.0001 || 
                            Math.Abs(currentData[i] - newCurrentList[i]) > 0.0001)
                        {
                            // Update the existing data points
                            ysData[i] = newYsList[i];
                            currentData[i] = newCurrentList[i];
                            
                            // If we've already plotted this point, update it in displayed lists too
                            if (i < plotIndex)
                            {
                                ysDisplayed[i] = newYsList[i];
                                currentDisplayed[i] = newCurrentList[i];
                            }
                        }
                    }
                    
                    // Then check if there are new data points to add
                    int currentCount = ysData.Count;
                    int newCount = newYsList.Count;
                    if (newCount > currentCount)
                    {
                        // Get the next time value for new points
                        double nextTimeValue = currentCount > 0 ? xsData.Max() + 1 : 0;
                        
                        // Add only new points to our data
                        for (int i = currentCount; i < newCount; i++)
                        {
                            // Use voltage from newYsList and increment time
                            ysData.Add(newYsList[i]);
                            currentData.Add(newCurrentList[i]);
                            xsData.Add(nextTimeValue);
                            nextTimeValue++; // Increment time by 1 second for each new point
                        }
                    }
                    lastModified = currentModified;
                }
            }
            
            // Add next point to displayed data if available
            if (plotIndex < xsData.Count)
            {
                xsDisplayed.Add(xsData[plotIndex]);
                ysDisplayed.Add(ysData[plotIndex]);
                currentDisplayed.Add(currentData[plotIndex]);
                plotIndex++;
            }
            
            // Always update plots, even if no new points were added
            // Update voltage plot
            formsPlot1.Plot.Clear();
            formsPlot1.Plot.Add.Scatter(xsDisplayed.ToArray(), ysDisplayed.ToArray());
            formsPlot1.Plot.Title("Battery Voltage vs Time");
            formsPlot1.Plot.XLabel("Time (s)");
            formsPlot1.Plot.YLabel("Voltage (V)");
            formsPlot1.Refresh();
            
            // Update current plot
            formsPlot2.Plot.Clear();
            formsPlot2.Plot.Add.Scatter(xsDisplayed.ToArray(), currentDisplayed.ToArray());
            formsPlot2.Plot.Title("Current vs Time");
            formsPlot2.Plot.XLabel("Time (s)");
            formsPlot2.Plot.YLabel("Current (A)");
            formsPlot2.Refresh();
            
            // Check if we should stop the timer
            if (plotIndex >= xsData.Count && !monitorCsvUpdates)
            {
                plotTimer.Stop();
            }
        }

        private void formsPlot2_Load(object sender, EventArgs e)
        {
            // Initialize the second plot
            formsPlot2.Plot.Title("Current vs Time");
            formsPlot2.Plot.XLabel("Time (s)");
            formsPlot2.Plot.YLabel("Current (A)");
            formsPlot2.Refresh();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Create a SaveFileDialog to allow the user to choose where to save the CSV
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save CSV Data";
                    saveFileDialog.DefaultExt = "csv";
                    saveFileDialog.FileName = "data_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                    
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Build the CSV content with time, voltage, and current data
                        string csvContent = "Time,Voltage,Current\n";
                        for (int i = 0; i < xsDisplayed.Count; i++)
                        {
                            csvContent += $"{xsDisplayed[i]},{ysDisplayed[i]},{currentDisplayed[i]}\n";
                        }
                        
                        // Save the CSV content to the selected file
                        File.WriteAllText(saveFileDialog.FileName, csvContent);
                        
                        // Open the folder containing the saved file
                        string folderPath = Path.GetDirectoryName(saveFileDialog.FileName);
                        Process.Start("explorer.exe", folderPath);
                        
                        // Show success message
                        MessageBox.Show($"Data successfully saved to {saveFileDialog.FileName}", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
