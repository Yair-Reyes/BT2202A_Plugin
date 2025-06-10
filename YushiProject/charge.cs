using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using jsonHelper;
using shared;    

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

        [Display("Sequence Number", Order: 5, Description: "The # of the sequence")]
        public double Sequence { get; set; }

        [Display("Step Number", Order: 6, Description: "The # of the step inside a sequence")]
        public double Step { get; set; }

        // Reference to the instrument Instrument
        [Display("Cell size", Order: 7, Description: "Number of channels per cell")]
        public double Channels { get; set; }

        [Display("Cell group", Order: 8, Description: "Number of cells per cell group, asign as lowest:highest or comma separated list")]
        public string cell_group { get; set; }
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
            var data = new { sleep = 1, test_step = "Charge" };
            jsonHelper.jsonAider.write_json(data);

            base.PrePlanRun();
        }

        public override void Run()
        {   // pre run

            try
            {
                var dataStart = new { sleep = 1, test_step = "Charge" }; //Initialize flags
                jsonHelper.jsonAider.write_json(dataStart);

                instrument.ScpiCommand("*IDN?");
                instrument.ScpiCommand("SYST:PROB:LIM 1,0");
                instrument.ScpiCommand($"CELL:DEF:QUICk {Channels}");

                Log.Info($"Charge sequence step {Sequence},{Step} defined: Voltage = {Voltage} V, Current = {Current} A, Time = {Time} s");
                Log.Info("Initializing Charge");
                Log.Info("Charge Process Started");


            }
            catch (Exception ex){
                Log.Error($"Error during PrePlanRun: {ex.Message}");
            }

            try
            {
                instrument.ScpiCommand($"SEQ:STEP:DEF {Sequence},{Step}, CHARGE, {Time}, {Current}, {Voltage}");
                char[] delimiterChars = { ',', ':' };
                cell_group = cell_group.Replace(" ", "");
                cell_list = cell_group.Split(delimiterChars);


                // Update parameters for JSON flags and report
                var data_update = new { sleep = 1, test_step = "Charge"};
                jsonHelper.jsonAider.write_json(data_update);

                // Log the start of the charging process.
                Log.Info("Starting the charging process.");
                instrument.ScpiCommand("OUTP ON");
                Log.Info("Output enabled.");

                //child steps
                RunChildSteps();

                // Enable and Initialize Cells
                instrument.ScpiCommand($"CELL:ENABLE (@{cell_group}),{Sequence}");
                instrument.ScpiCommand($"CELL:INIT (@{cell_group})");

                //Update flags, disable sleep mode to start step
                var data_after = new
                {
                    sleep = 0,
                    cell_list = cell_group,
                    test_step = "Charge",
                    status = ""
                };
                jsonHelper.jsonAider.write_json(data_after);
                
            } 

            catch (Exception ex)
            {
                // Log the error and set the test verdict to fail.
                Log.Error($"An error occurred during the charging process: {ex.Message}");
                UpgradeVerdict(Verdict.Fail);
                //StepStatus.Set(Verdict.ToString()); //Update status

                TapThread.Sleep(1000);  //Delay to clean flags
                var data_error = new
                {
                    sleep = 0,
                    cell_list = "",
                    test_step = "",
                    status = "Fail"
                };
                jsonHelper.jsonAider.write_json(data_error);
                
            }

            // post run
            try
            {
                int sec;
                for (sec = 0; sec < Time; sec++) //Manual abort routine sequence
                {
                    try
                    {
                        TapThread.Sleep(1000);
                    }
                    catch (OperationCanceledException) //Abort button pushed from Test Automation
                    {
                        Log.Warning("Abort detected from user. Aborting and turning off sequence...");
                        instrument.ScpiCommand("ABORt");
                        instrument.ScpiCommand("OUTP OFF");
                        instrument.ScpiCommand($"CELL:DISABLE (@{cell_group})");
                        
                        UpgradeVerdict(Verdict.Aborted);
                        //StepStatus.Set(Verdict.ToString()); //Update status
                        
                        Thread.Sleep(1000); //Delay to clean flags
                        var data_abort = new
                        {
                            sleep = 0,
                            cell_list = "",
                            test_step = "",
                            status = "Aborted"
                        };
                        jsonHelper.jsonAider.write_json(data_abort);
                        
                        return;
                    }
                }

                UpgradeVerdict(Verdict.Pass);
                
                if (sec >= Time) //Clean flags after step done 
                {
                    //StepStatus.Set(Verdict.ToString()); //Update status

                    Thread.Sleep(1000); //Delay to clean flags
                    var data_final = new
                    {
                        sleep = 0,
                        cell_list = "",
                        test_step = "",
                        status = "Pass"
                    };
                    jsonHelper.jsonAider.write_json(data_final);
                }
                                
                // Any cleanup code that needs to run after the test plan finishes.
                Log.Info("Instrument reset after test completion.");
            }
            catch (Exception ex){
                Log.Error($"Error during PostPlanRun: {ex.Message}");
            }

        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();

            /*var data_clean = new
            {
                sleep = 0,
                cell_list = "",
                test_step = "",
                //status = ""
            };
            jsonHelper.jsonAider.write_json(data_clean);*/
        }
    }
}