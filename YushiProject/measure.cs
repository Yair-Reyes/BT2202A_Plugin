using OpenTap;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
//using System.Collections.Generic;
using System.Threading;
using jsonHelper;
using shared;

namespace BT2202a
{
    [Display("Measure", Group: "instrument", Description: "Measures voltage and current for step duration.")]
    [AllowAnyChild]
    public class Measure : TestStep
    {
        #region Settings
        [Display("Instrument", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument instrument { get; set; }
       
        /*[Display("Seconds", Order: 2, Description: "How many seconds measure will run, 0 is infinite")]
        public double seconds { get; set; }*/

        #endregion

        //private int meas;

        public Measure()
        {
            //seconds = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {

            try
            {
                Log.Info($"Starting the measurement process.");

                instrument.ScpiCommand("OUTP ON");
                Log.Info("Output enabled.");

                RunChildSteps();

                Stack<string> meas = new Stack<string>(); //Measurement stack
                meas.Push("Voltage (V),Current (A)"); //CSV headers

                bool isMeasuring = false; //Acknowledge to know if a measurement is taken in place
                //string status = StepStatus.Get(); //Obtain verdict of steps

                while (true)
                {
                    try
                    {
                        string cell_group = jsonHelper.jsonAider.ReadJson(flags => flags.cell_list); //Read values of flags
                        int sleep = jsonHelper.jsonAider.ReadJson(flags => flags.sleep); //
                        string step = jsonHelper.jsonAider.ReadJson(flags => flags.test_step);
                        string status = jsonHelper.jsonAider.ReadJson(flags => flags.status);

                        if (!string.IsNullOrWhiteSpace(cell_group))
                        {
                            

                            if (sleep == 0)
                            {
                                isMeasuring = true; //Instrument executing measurements

                                string measuredVoltage = instrument.ScpiQuery($"MEAS:CELL:VOLT? (@{cell_group})");
                                string measuredCurrent = instrument.ScpiQuery($"MEAS:CELL:CURR? (@{cell_group})");

                                Log.Info($"Test step: {step} [Cells: {cell_group}] Voltage: {measuredVoltage} V, Current: {measuredCurrent} A");
                                meas.Push($"{measuredVoltage.Substring(0, 6)},{measuredCurrent.Substring(0, 6)}"); //Append values on the stack

                                //Save a RealTime report for plotting
                                string path = $@"C:\Program Files\OpenTAP\Steps Reports\BT2202_RealTimeReportPlot.csv";
                                Directory.CreateDirectory(Path.GetDirectoryName(path));

                                using (StreamWriter writer = new StreamWriter(path))
                                {
                                    foreach (var linea in meas.Reverse())
                                        writer.WriteLine(linea);
                                }
                            }
                            else
                            {
                                Log.Info("Sleep mode active. Waiting...");
                            }
                        }
                        else
                        {
                            if (isMeasuring && meas.Count > 0) // Save when done measurements
                            {
                                Log.Info($"Step Done with verdict: {status}");

                                meas.Push($"Final verdict:,{status}"); //Append final verdict

                                //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                string path = $@"C:\Program Files\OpenTAP\Steps Reports\BT2202_Report.csv";
                                Directory.CreateDirectory(Path.GetDirectoryName(path));

                                using (StreamWriter writer = new StreamWriter(path))
                                {
                                    foreach (var linea in meas.Reverse())
                                        writer.WriteLine(linea);
                                }

                                Log.Info("Step finished. Complete report file saved in " + path);

                                // Reset values in order to save more files of more measurments
                                meas.Clear();
                                isMeasuring = false;
                            }
                            else
                            {
                                Log.Info("Waiting for active cells...");
                            }
                        }

                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in measurement cycle " + ex.Message);
                        Thread.Sleep(1000);
                    }
                }
            }

            catch (Exception ex)
            {
                // Log the error and set the test verdict to fail.
                Log.Error($"An error occurred during the measure process: {ex.Message}");
                UpgradeVerdict(Verdict.Fail);
            }

            try
            {
                UpgradeVerdict(Verdict.Pass);
                // Any cleanup code that needs to run after the test plan finishes.
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