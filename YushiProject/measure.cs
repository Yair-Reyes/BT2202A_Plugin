using OpenTap;
using System;
/*using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;*/
using System.Threading;
// Removed jsonHelper import

namespace BT2202a
{
    [Display("Measure", Group: "instrument", Description: "Measures voltage and current for step duration.")]
    [AllowAnyChild]
    public class Measure : TestStep
    {
        #region Settings
        [Display("Instrument", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument instrument { get; set; }

        [Display("Cell group", Order: 8, Description: "Cells to measure, asign as lowest:highest or comma separated list")]
        public string cell_group { get; set; }

        [Display("Seconds", Order: 2, Description: "How many seconds measure will run, 0 is infinite")]
        public double seconds { get; set; }

        [Display("Sleep Mode", Order: 3, Description: "When enabled, measurement will pause")]
        public bool SleepMode { get; set; }
        
        [Display("Measurement Timeout (ms)", Order: 4, Description: "Timeout in milliseconds for measurement queries")]
        public int MeasurementTimeout { get; set; }
        
        [Display("Measurement Retries", Order: 5, Description: "Number of times to retry a failed measurement")]
        public int MeasurementRetries { get; set; }
        #endregion

        private int meas;

        public Measure(){
            SleepMode = false; // Default to not sleeping
            MeasurementTimeout = 5000; // Default 5 second timeout
            MeasurementRetries = 3;   // Default 3 retries
        }

        public override void PrePlanRun(){
            base.PrePlanRun();
        }

        public override void Run()
        {   // pre run
            meas = 1;
            if (seconds == 0){
                meas = -1;
            }

            try{
                // Log the start of the charging process.
                Log.Info("Starting the measure process.");

                instrument.ScpiCommand("OUTP ON");
                Log.Info("Output enabled.");

                //child steps
                RunChildSteps();
                Log.Info(meas.ToString());
                while (meas <= seconds) {
                    Log.Info(meas.ToString());
                    try
                    {
                        // Use the SleepMode property instead of reading from JSON
                        int sleep = SleepMode ? 1 : 0;
                        Log.Info($"sleep:{sleep.ToString()}");

                        if (sleep == 0) {
                            // Query the instrument for voltage and current measurements.
                            /*string statusResponse = instrument.ScpiQuery($"STATus:CELL:REPort? (@{cell_group})");
                            int statusValue = int.Parse(statusResponse);
                            Log.Info($"Status Value: {statusValue}");
                            if (statusValue == 2)
                            {
                                Log.Info($"Status 2, failed cell group {cell_group}");
                            }*/

                            // Use SafeScpiQuery instead of direct ScpiQuery to handle timeouts
                            string measuredVoltage = SafeScpiQuery($"MEAS:CELL:VOLT? (@{cell_group})");
                            string measuredCurrent = SafeScpiQuery($"MEAS:CELL:CURR? (@{cell_group})");

                            // Log the measurements.
                            Log.Info($" Voltage: {measuredVoltage} V, Current: {measuredCurrent} A");

                        } else if (sleep==1){
                            Log.Info("Sleeping");
                        }

                        Thread.Sleep(1000);
                        if (seconds != 0){
                            meas = meas + 1;
                        }

                    }
                    catch (Exception ex){
                        if (seconds != 0){
                            meas = meas + 1;
                        }
                        // Log the error and set the test verdict to fail.
                        Log.Error($"error: {ex.Message}");
                        Thread.Sleep(1000);
                    }
                }

                // Get and log the final voltage and current measurements before turning off the output
                try {
                    string finalVoltage = SafeScpiQuery($"MEAS:CELL:VOLT? (@{cell_group})");
                    string finalCurrent = SafeScpiQuery($"MEAS:CELL:CURR? (@{cell_group})");
                    
                    // Log the final measurements with a clear indication that these are the final values
                    Log.Info("======= FINAL MEASUREMENTS =======");
                    Log.Info($"Final Voltage: {finalVoltage} V");
                    Log.Info($"Final Current: {finalCurrent} A");
                    Log.Info("==================================");
                    
                    // Add these values to the test results as well
                    Results.Publish("Final Voltage (V)", finalVoltage);
                    Results.Publish("Final Current (A)", finalCurrent);
                }
                catch (Exception ex) {
                    Log.Error($"Error getting final measurements: {ex.Message}");
                }

                // Turn off the output after the charging process is complete.
                instrument.ScpiCommand("OUTP OFF");
                Log.Info("Measure process completed and output disabled.");

                // Update the test verdict to pass if everything went smoothly.
                UpgradeVerdict(Verdict.Pass);
            }

            catch (Exception ex){
                // Log the error and set the test verdict to fail.
                Log.Error($"An error occurred during the measure process: {ex.Message}");
                UpgradeVerdict(Verdict.Fail);
            }

            try{
                UpgradeVerdict(Verdict.Pass);
                // Any cleanup code that needs to run after the test plan finishes.
                Log.Info("Instrument reset after test completion.");
            }
            catch (Exception ex){
                Log.Error($"Error during PostPlanRun: {ex.Message}");
            }
        }

        /// <summary>
        /// SafeScpiQuery handles queries with timeout and retry logic
        /// </summary>
        private string SafeScpiQuery(string query, int retries = 0)
        {
            // If we've reached the maximum number of retries, try alternate commands
            if (retries >= MeasurementRetries)
            {
                if (query.StartsWith("MEAS:CELL:VOLT?"))
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
                        return "0";
                    }
                }
                else if (query.StartsWith("MEAS:CELL:CURR?"))
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
                        return "0";
                    }
                }
                
                // For other commands or if alternatives fail
                Log.Warning($"Query '{query}' failed after {MeasurementRetries} retries");
                return "0";
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
                
                // Wait a bit before retrying, with exponential backoff
                Thread.Sleep(100 * (retries + 1));
                
                // Retry the query
                if (retries < MeasurementRetries)
                {
                    Log.Info($"Retrying SCPI query (attempt {retries + 2}/{MeasurementRetries + 1})");
                    return SafeScpiQuery(query, retries + 1);
                }
                else
                {
                    throw;
                }
            }
        }

        public override void PostPlanRun(){
            base.PostPlanRun();
        }
    }
}