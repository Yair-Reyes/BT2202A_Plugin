using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BT2202a
{
    [Display("Clear Variables", Group: "instrument", Description: "Clears all cells and sequences.")]
    [AllowAnyChild]
    public class ClearVariables : TestStep
    {
        #region Settings

        [Display("Instrument", Order: 1, Description: "The instrument to use for clearing variables.")]
        public ScpiInstrument instrument { get; set; }

        #endregion

        public ClearVariables()
        {
            // Constructor with default values if needed
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {
            try
            {
                Log.Info("Starting variable clearing process");
                
                // Verify instrument connection
                instrument.ScpiCommand("*IDN?");
                
                // Execute the clear commands
                Log.Info("Aborting all cell operations");
                instrument.ScpiCommand("CELL:ABOR 0");
                
                Log.Info("Clearing all cells");
                instrument.ScpiCommand("CELL:CLE 0");
                
                Log.Info("Clearing all sequences");
                instrument.ScpiCommand("SEQ:CLE 0");
                
                Log.Info("All variables successfully cleared");
                
                // Run any child steps if needed
                RunChildSteps();
                
                // Set the verdict to pass
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                // Log the error and set the test verdict to fail
                Log.Error($"An error occurred during the clearing process: {ex.Message}");
                UpgradeVerdict(Verdict.Error);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}