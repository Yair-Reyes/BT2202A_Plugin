// ReportingHelper.cs (actualizado con soporte para archivo compartido incremental)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// SharedVerdict.cs
namespace shared
{
    public static class StepStatus 
    { 
        private static string _verdict;

        public static void Set(string verdict)
        {
            _verdict = verdict;
        }

        public static string Get()
        {
            return _verdict;
        }

    }
}


