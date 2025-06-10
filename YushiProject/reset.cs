using OpenTap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BT2202a
{
    [Display("Reset", Group: "instrument", Description: "resets")]
    [AllowAnyChild]
    public class Reset : TestStep
    {
        #region Settings

        [Display("Instrument", Order: 1, Description: "The instrument instrument to use for charging.")]
        public ScpiInstrument instrument { get; set; }
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
            instrument.ScpiCommand("*RST?");

            //Clean flags
            var data = new 
            {
                sleep = 0,
                cell_list = "",
                test_step = "",
                verdict = "",
                command = ""
            };
            jsonHelper.jsonAider.write_json(data);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}