﻿using System;
using System.Data;
using System.Linq;
using System.Web;

namespace DocumentManagerUtil
{
    /// <summary>
    /// Contains my site's global variables.
    /// </summary>
    public static class Global
    {
        /// <summary>
        /// Global variable storing important stuff.
        /// </summary>
        static string _importantData;

        /// <summary>
        /// Get or set the static important data.
        /// </summary>
        public static string ImportantData
        {
            get { return _importantData; }
            set { _importantData = value; }
        }
    }
}