using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using jsonHelper;

namespace BT2202a
{
    [Display("Abort", Group: "instrument", Description: "Abort process")]
    [AllowAnyChild]
    public class Abort : TestStep
    {
        #region Settings

        [Display("Instrument", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument instrument { get; set; }
        // Properties for voltage, current, and time
        #endregion

        public Abort()
        {
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {   // pre run
            if (TapThread.Current.AbortToken.IsCancellationRequested)
            {
                Log.Warning("User aborted the test plan. Sending ABORt to instrument...");
                instrument.ScpiCommand("ABORt");
                instrument.ScpiCommand("OUTP OFF");
                UpgradeVerdict(Verdict.Aborted);
            }
        }
        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}