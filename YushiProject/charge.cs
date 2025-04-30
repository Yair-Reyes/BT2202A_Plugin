using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BT2202a
{
    [Display("Charge", Group: "instrument", Description: "Charges a device with specified voltage and current for a set duration.")]
    [AllowAnyChild]
    public class Charge : TestStep
    {
        #region Settings

        [Display("Instrument", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument instrument { get; set; }
        // Properties for voltage, current, and time
        [Display("Voltage (V)", Order: 2, Description: "The voltage level to set during charging.")]
        public double Voltage { get; set; }

        [Display("Current (A)", Order: 3, Description: "The current level to set during charging.")]
        public double Current { get; set; }

        [Display("Time (s)", Order: 4, Description: "The duration of the charge in seconds.")]
        public double Time { get; set; }

        // Reference to the instrument Instrument
        [Display("Cell size", Order: 5, Description: "Number of channels per cell")]
        public double Channels { get; set; }

        [Display("Cell group", Order: 6, Description: "Number of cells per cell group, asign as lowest:highest or comma separated list")]
        public string cell_group { get; set; }
        
        [Display("Enable Measurements", Order: 7, Description: "Enable voltage and current measurements during charging")]
        public bool EnableMeasurements { get; set; } = true;
        #endregion

        public string[] cell_list;
        public Charge()
        {
            // Set default values for the properties.
            Voltage = 0; // Default voltage, adjust as needed.
            Current = 0; // Default current, adjust as needed.
            Time = 0;   // Default duration, adjust as needed.
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {   // pre run
            try
            {

                instrument.ScpiCommand("*IDN?");
                instrument.ScpiCommand("*RST");
                instrument.ScpiCommand("SYST:PROB:LIM 1,0");

                instrument.ScpiCommand($"CELL:DEF:QUICk {Channels}");

                Log.Info($"Charge sequence step defined: Voltage = {Voltage} V, Current = {Current} A, Time = {Time} s");

                Log.Info("Initializing Charge");
                Log.Info("Charge Process Started");
            }
            catch (Exception ex)
            {
                Log.Error($"Error during PrePlanRun: {ex.Message}");
            }

            // run

            try
            {
                instrument.ScpiCommand($"SEQ:STEP:DEF 1,1, CHARGE, {Time}, {Current}, {Voltage}");
                char[] delimiterChars = { ',', ':' };
                cell_group = cell_group.Replace(" ", "");
                cell_list = cell_group.Split(delimiterChars);

                // Log the start of the charging process.
                Log.Info("Starting the charging process.");

                instrument.ScpiCommand("OUTP ON");
                Log.Info("Output enabled.");

                //child steps
                RunChildSteps();

                // Enable and Initialize Cells
                instrument.ScpiCommand($"CELL:ENABLE (@{cell_group}),1");
                instrument.ScpiCommand($"CELL:INIT (@{cell_group})");

                DateTime startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalSeconds < Time)
                {
                    try {
                        // Check elapsed time
                        double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                        Log.Info($"Time: {elapsedSeconds:F2}s of {Time}s completed");
                        
                        // Measure voltage and current if measurements are enabled
                        if (EnableMeasurements)
                        {
                            try
                            {
                                // Measure voltage for all channels in the group
                                string voltageResponse = instrument.ScpiQuery($"MEAS:VOLT? (@{cell_group})");
                                string[] voltageValues = voltageResponse.Trim().Split(',');
                                
                                // Measure current for all channels in the group
                                string currentResponse = instrument.ScpiQuery($"MEAS:CURR? (@{cell_group})");
                                string[] currentValues = currentResponse.Trim().Split(',');
                                
                                // Log measurements for each channel
                                for (int i = 0; i < cell_list.Length && i < voltageValues.Length && i < currentValues.Length; i++)
                                {
                                    if (double.TryParse(voltageValues[i], out double voltageValue) && 
                                        double.TryParse(currentValues[i], out double currentValue))
                                    {
                                        Log.Info($"Channel {cell_list[i]}: Voltage = {voltageValue:F3}V, Current = {currentValue:F3}A");
                                    }
                                }
                            }
                            catch (Exception measEx)
                            {
                                Log.Warning($"Measurement error: {measEx.Message}");
                            }
                        }
                        
                        // Wait for 1 second before next measurement
                        Thread.Sleep(1000);
                    }
                    catch (Exception loopEx) {
                        Log.Error($"Error during measurement loop: {loopEx.Message}");
                        UpgradeVerdict(Verdict.Fail);
                        instrument.ScpiCommand("OUTP OFF"); // Turn off output
                        return;
                    }
                }

                // Turn off the output after the charging process is complete.
                instrument.ScpiCommand("OUTP OFF");
                Log.Info("Charging process completed and output disabled.");

                // Update the test verdict to pass if everything went smoothly.
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                // Log the error and set the test verdict to fail.
                Log.Error($"An error occurred during the charging process: {ex.Message}");
                UpgradeVerdict(Verdict.Fail);
            }

            // post run
            try
            {
                UpgradeVerdict(Verdict.Pass);
                // Any cleanup code that needs to run after the test plan finishes.
                instrument.ScpiCommand("*RST"); // Reset the instrument again after the test.
                Log.Info("Instrument reset after test completion.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error during PostPlanRun: {ex.Message}");
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}