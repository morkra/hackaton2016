using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace NotificationLamishtakenWorkerRole
{
    public static class Diagnostics
    {
        public enum Severity { Error, Information, Critical, Verbose, Warning };
        private static TelemetryClient telemetryClient = new TelemetryClient();
        static Diagnostics()
        {
            //Setup our telemtry client to be able to call 
            string appInsightsKey = ConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            telemetryClient.InstrumentationKey = appInsightsKey;
        }

        public static void TrackException(Exception thrownException, int eventID, string message)
        {
            //Data to push into AI which can be searched on
            Dictionary<string, string> prop = new Dictionary<string, string>();
            prop["message"] = message;
            prop["eventID"] = eventID.ToString();
            telemetryClient.TrackException(thrownException, prop);

            //Log to System.Diagnostics as well for redundancy
            Trace.TraceError("Exception: {0}, Message:{1}", thrownException.GetType().FullName, thrownException.Message);
        }


        public static void TrackTrace(string message, Severity sev)
        {
            try
            {
                TraceTelemetry telemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry();
                Dictionary<string, string> prop = new Dictionary<string, string>();


                telemetry.Message = message;
                telemetry.Timestamp = DateTime.UtcNow;

                switch (sev)
                {
                    case Severity.Critical:
                        telemetry.SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical;
                        break;

                    case Severity.Error:
                        telemetry.SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error;
                        break;

                    case Severity.Information:
                        telemetry.SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information;
                        break;

                    case Severity.Verbose:
                        telemetry.SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Verbose;
                        break;

                    case Severity.Warning:
                        telemetry.SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning;
                        break;
                }

                telemetryClient.TrackTrace(telemetry);

                //Log to System.Diagnostics as well for redundancy
                Trace.WriteLine(String.Format("Message:{0}, Severity:{1}", message, sev));

            }
            catch (Exception ex)
            {
                TrackException(ex, 0, "Error writing trace");
            }

        }
    }
}
