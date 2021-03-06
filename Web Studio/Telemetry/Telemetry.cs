﻿using System;
using System.Globalization;
using Microsoft.ApplicationInsights;

namespace Web_Studio.Telemetry
{
    /// <summary>
    /// Manage Windows Telemetry in Azure
    /// </summary>
    public static class Telemetry
    {
        /// <summary>
        /// client to send the telemetry data
        /// </summary>
        public static readonly TelemetryClient TelemetryClient = new TelemetryClient();

        /// <summary>
        /// Telemetry config
        /// </summary>
        public static void Initialize()
        {
            TelemetryClient.InstrumentationKey = "58f8f90f-bb85-4646-9d62-5009739028e5";
            TelemetryClient.Context.User.Id = Environment.UserName;
            TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            TelemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            TelemetryClient.Context.Device.Language = CultureInfo.CurrentCulture.Name;
        }
    }
}