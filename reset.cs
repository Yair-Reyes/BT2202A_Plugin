using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BT2202a
{
    [Display("Reset", Group: "BT22", Description: "resets")]
    [AllowAnyChild]
    public class Reset : TestStep
    {
        #region Settings

        [Display("VISA Address", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument BT22 { get; set; }
        // Properties for voltage, current, and time
        #endregion

        public Reset()
        {
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {   // pre run
            BT22.ScpiCommand("*RST?");
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}