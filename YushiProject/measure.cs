using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BT2202a
{
    [Display("Measure", Group: "instrument", Description: "Performs measurements on a device without charging or discharging.")]
    [AllowAnyChild]
    public class Measure : TestStep
    {
        #region Settings
        [Display("Instrument", Order: 1, Description: "The instrument to use for measurements.")]
        public ScpiInstrument instrument { get; set; }

        [Display("Cell group", Order: 2, Description: "Number of cells per cell group, assign as lowest:highest or comma separated list")]
        public string cell_group { get; set; }

        [Display("Cell size", Order: 3, Description: "Number of channels per cell")]
        public double Channels { get; set; }

        [Display("Measurement Type", Order: 4, Description: "Type of measurement to perform")]
        public MeasurementTypes MeasurementType { get; set; }

        [Display("Number of Samples", Order: 5, Description: "Number of measurement samples to collect")]
        public int SampleCount { get; set; } = 1;

        [Display("Sample Interval (ms)", Order: 6, Description: "Time between measurement samples in milliseconds")]
        public int SampleInterval { get; set; } = 500;

        [Display("Measurement Timeout (ms)", Order: 7, Description: "Timeout in milliseconds for measurement queries")]
        public int MeasurementTimeout { get; set; } = 5000;

        [Display("Retry Count", Order: 8, Description: "Number of times to retry measurements if they fail")]
        public int RetryCount { get; set; } = 3;

        [Display("Log Results to CSV", Order: 9, Description: "Save measurement results to a CSV file")]
        public bool LogToFile { get; set; } = false;

        [Display("CSV File Path", Order: 10, Description: "Path to save measurement CSV file (leave empty for auto-generated filename)", Collapsed: true)]
        [EnabledIf("LogToFile", true)]
        public string CsvFilePath { get; set; } = "";
        #endregion

        // Define measurement types
        public enum MeasurementTypes
        {
            [Display("Voltage")] Voltage,
            [Display("Current")] Current,
            [Display("Both")] Both
        }

        public string[] cell_list;
        private Dictionary<string, List<double>> voltageResults = new Dictionary<string, List<double>>();
        private Dictionary<string, List<double>> currentResults = new Dictionary<string, List<double>>();
        private DateTime measurementStartTime;

        public Measure()
        {
            // Set default values
            MeasurementType = MeasurementTypes.Both;
            cell_group = "1001";
            Channels = 1;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {
            try
            {
                // Initialize the instrument
                InitializeInstrument();
                
                // Parse cell group input
                ParseCellGroup();
                
                // Clear results dictionaries
                voltageResults.Clear();
                currentResults.Clear();
                
                // Initialize results collection for each cell
                foreach (string cell in cell_list)
                {
                    voltageResults[cell] = new List<double>();
                    currentResults[cell] = new List<double>();
                }
                
                // Log the start of the measurement process
                Log.Info($"Starting measurement process for {cell_list.Length} channel(s)");
                Log.Info($"Measurement type: {MeasurementType}, Sample count: {SampleCount}, Sample interval: {SampleInterval}ms");
                
                // Run any child steps
                RunChildSteps();
                
                // Record start time
                measurementStartTime = DateTime.Now;
                
                // Perform the measurements
                for (int sample = 0; sample < SampleCount; sample++)
                {
                    PerformMeasurement(sample);
                    
                    // Wait for the specified interval unless it's the last sample
                    if (sample < SampleCount - 1)
                    {
                        Thread.Sleep(SampleInterval);
                    }
                }
                
                // Calculate and log statistics
                LogMeasurementStatistics();
                
                // Export results to CSV if enabled
                if (LogToFile)
                {
                    ExportResultsToCsv();
                }
                
                // Update test verdict to pass if everything went smoothly
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                // Log the error and set the test verdict to fail
                Log.Error($"An error occurred during the measurement process: {ex.Message}");
                UpgradeVerdict(Verdict.Fail);
            }
            finally
            {
                // Clean up
                try
                {
                    instrument.ScpiCommand("*RST");
                    Log.Info("Instrument reset after measurement completion");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error during cleanup: {ex.Message}");
                }
            }
        }

        private void InitializeInstrument()
        {
            // Initialize the instrument for measurement
            instrument.ScpiCommand("*IDN?");
            instrument.ScpiCommand("*RST");
            instrument.ScpiCommand("SYST:PROB:LIM 1,0");
            instrument.ScpiCommand($"CELL:DEF:QUICk {Channels}");
            Log.Info("Instrument initialized for measurement");
        }

        private void ParseCellGroup()
        {
            // Parse the cell group string into individual cell identifiers
            char[] delimiterChars = { ',', ':' };
            cell_group = cell_group.Replace(" ", "");
            cell_list = cell_group.Split(delimiterChars);
            Log.Info($"Cell group parsed: {string.Join(", ", cell_list)}");
        }

        private void PerformMeasurement(int sampleIndex)
        {
            try
            {
                // Enable and initialize cells before measurement
                instrument.ScpiCommand($"CELL:ENABLE (@{cell_group}),1");
                instrument.ScpiCommand($"CELL:INIT (@{cell_group})");
                
                // Allow a brief pause for the cells to initialize
                Thread.Sleep(100);
                
                // Calculate elapsed time
                TimeSpan elapsed = DateTime.Now - measurementStartTime;
                Log.Info($"Sample {sampleIndex + 1}/{SampleCount} at {elapsed.TotalSeconds:F1}s");
                
                // Perform voltage measurement if needed
                if (MeasurementType == MeasurementTypes.Voltage || MeasurementType == MeasurementTypes.Both)
                {
                    MeasureVoltage();
                }
                
                // Perform current measurement if needed
                if (MeasurementType == MeasurementTypes.Current || MeasurementType == MeasurementTypes.Both)
                {
                    MeasureCurrent();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error in sample {sampleIndex + 1}: {ex.Message}");
            }
        }

        private void MeasureVoltage()
        {
            string voltageResponse = SafeScpiQuery($"MEAS:VOLT? (@{cell_group})");
            if (!string.IsNullOrEmpty(voltageResponse))
            {
                string[] voltageValues = voltageResponse.Trim().Split(',');
                for (int i = 0; i < cell_list.Length && i < voltageValues.Length; i++)
                {
                    if (double.TryParse(voltageValues[i], out double voltage))
                    {
                        voltageResults[cell_list[i]].Add(voltage);
                        Log.Info($"Channel {cell_list[i]}: Voltage = {voltage:F3} V");
                    }
                    else
                    {
                        Log.Warning($"Failed to parse voltage value for channel {cell_list[i]}: {voltageValues[i]}");
                    }
                }
            }
        }

        private void MeasureCurrent()
        {
            string currentResponse = SafeScpiQuery($"MEAS:CURR? (@{cell_group})");
            if (!string.IsNullOrEmpty(currentResponse))
            {
                string[] currentValues = currentResponse.Trim().Split(',');
                for (int i = 0; i < cell_list.Length && i < currentValues.Length; i++)
                {
                    if (double.TryParse(currentValues[i], out double current))
                    {
                        currentResults[cell_list[i]].Add(current);
                        Log.Info($"Channel {cell_list[i]}: Current = {current:F3} A");
                    }
                    else
                    {
                        Log.Warning($"Failed to parse current value for channel {cell_list[i]}: {currentValues[i]}");
                    }
                }
            }
        }

        private string SafeScpiQuery(string query, int retries = 0)
        {
            // If we've reached the maximum number of retries, try alternate commands
            if (retries >= RetryCount)
            {
                if (query.StartsWith("MEAS:VOLT?"))
                {
                    // Try alternative voltage measurement command
                    try
                    {
                        Log.Info("Trying alternative voltage measurement command");
                        return instrument.ScpiQuery("READ:VOLT?");
                    }
                    catch
                    {
                        Log.Warning("Alternative voltage measurement also failed");
                        return "";
                    }
                }
                else if (query.StartsWith("MEAS:CURR?"))
                {
                    // Try alternative current measurement command
                    try
                    {
                        Log.Info("Trying alternative current measurement command");
                        return instrument.ScpiQuery("READ:CURR?");
                    }
                    catch
                    {
                        Log.Warning("Alternative current measurement also failed");
                        return "";
                    }
                }
                
                // For other commands or if alternatives fail
                Log.Warning($"Query '{query}' failed after {RetryCount} retries");
                return "";
            }

            try
            {
                // Set a timeout for the query
                var originalTimeout = instrument.IoTimeout;
                instrument.IoTimeout = MeasurementTimeout;
                
                try
                {
                    // First try sending a clear command to clear any buffers
                    instrument.ScpiCommand("*CLS");
                    Thread.Sleep(50); // Small delay after clearing
                    
                    // Now send the actual query
                    string response = instrument.ScpiQuery(query);
                    
                    // Reset the timeout to original value
                    instrument.IoTimeout = originalTimeout;
                    
                    return response;
                }
                catch
                {
                    // Reset timeout even if there was an error
                    instrument.IoTimeout = originalTimeout;
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Query attempt {retries + 1} failed: {ex.Message}");
                
                // Wait a bit before retrying
                Thread.Sleep(100 * (retries + 1));
                
                // Retry the query
                return SafeScpiQuery(query, retries + 1);
            }
        }

        private void LogMeasurementStatistics()
        {
            Log.Info("--- Measurement Statistics ---");
            
            // Log voltage statistics if applicable
            if (MeasurementType == MeasurementTypes.Voltage || MeasurementType == MeasurementTypes.Both)
            {
                foreach (var cell in cell_list)
                {
                    if (voltageResults.ContainsKey(cell) && voltageResults[cell].Count > 0)
                    {
                        double min = voltageResults[cell].Min();
                        double max = voltageResults[cell].Max();
                        double avg = voltageResults[cell].Average();
                        Log.Info($"Channel {cell} Voltage (V) - Min: {min:F3}, Max: {max:F3}, Avg: {avg:F3}, Samples: {voltageResults[cell].Count}");
                    }
                }
            }
            
            // Log current statistics if applicable
            if (MeasurementType == MeasurementTypes.Current || MeasurementType == MeasurementTypes.Both)
            {
                foreach (var cell in cell_list)
                {
                    if (currentResults.ContainsKey(cell) && currentResults[cell].Count > 0)
                    {
                        double min = currentResults[cell].Min();
                        double max = currentResults[cell].Max();
                        double avg = currentResults[cell].Average();
                        Log.Info($"Channel {cell} Current (A) - Min: {min:F3}, Max: {max:F3}, Avg: {avg:F3}, Samples: {currentResults[cell].Count}");
                    }
                }
            }
        }

        private void ExportResultsToCsv()
        {
            try
            {
                string filePath = CsvFilePath;
                
                // Generate auto filename if not specified
                if (string.IsNullOrEmpty(filePath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string measureType = MeasurementType.ToString();
                    filePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        $"BT2202A_Measurement_{measureType}_{timestamp}.csv");
                }
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create CSV content
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    // Write header
                    List<string> headers = new List<string> { "Sample" };
                    foreach (string cell in cell_list)
                    {
                        if (MeasurementType == MeasurementTypes.Voltage || MeasurementType == MeasurementTypes.Both)
                            headers.Add($"Voltage_{cell} (V)");
                        
                        if (MeasurementType == MeasurementTypes.Current || MeasurementType == MeasurementTypes.Both)
                            headers.Add($"Current_{cell} (A)");
                    }
                    writer.WriteLine(string.Join(",", headers));
                    
                    // Write data rows
                    int maxSamples = voltageResults.Values.Concat(currentResults.Values)
                        .Max(list => list.Count);
                        
                    for (int i = 0; i < maxSamples; i++)
                    {
                        List<string> rowValues = new List<string> { (i + 1).ToString() };
                        
                        foreach (string cell in cell_list)
                        {
                            if (MeasurementType == MeasurementTypes.Voltage || MeasurementType == MeasurementTypes.Both)
                            {
                                string voltageValue = i < voltageResults[cell].Count ? 
                                    voltageResults[cell][i].ToString("F3") : "";
                                rowValues.Add(voltageValue);
                            }
                            
                            if (MeasurementType == MeasurementTypes.Current || MeasurementType == MeasurementTypes.Both)
                            {
                                string currentValue = i < currentResults[cell].Count ? 
                                    currentResults[cell][i].ToString("F3") : "";
                                rowValues.Add(currentValue);
                            }
                        }
                        
                        writer.WriteLine(string.Join(",", rowValues));
                    }
                }
                
                Log.Info($"Measurement results exported to CSV: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error exporting results to CSV: {ex.Message}");
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}