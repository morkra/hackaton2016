using System;
using System.Globalization;

namespace NotificationLamishtakenWorkerRole
{
    public class ProjectProperties
    {
        private static IFormatProvider m_culture = new CultureInfo("he-IL", true);

        #region Properties

        public RegistrationStatus RegistrationStatus { get; set; }
        public string Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public string ProjectName { get; set; }

        #endregion

        #region Constructors

        public ProjectProperties(string[] projectProperties)
        {
            RegistrationStatus = GetRegistrationStatus(projectProperties[0]);
            Id = projectProperties[1];
            StartDate = DateTime.Parse(projectProperties[3], m_culture);
            EndDate = DateTime.Parse(projectProperties[4], m_culture);
            Location = projectProperties[5];
            ProjectName = projectProperties[6];
        }

        public ProjectProperties(RegistrationStatus status, string id, DateTime startDate, DateTime endDate, string location, string projectName)
        {
            RegistrationStatus = status;
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            Location = location;
            ProjectName = projectName;
        }

        #endregion

        #region Helper Methods

        private static RegistrationStatus GetRegistrationStatus(string registrationStatus)
        {
            if (registrationStatus.Equals("להרשמה", StringComparison.InvariantCultureIgnoreCase))
            {
                return RegistrationStatus.Open;
            }
            if (registrationStatus.Equals("סגור", StringComparison.InvariantCultureIgnoreCase))
            {
                return RegistrationStatus.Close;
            }

            return RegistrationStatus.NotRelevant;
        }

        #endregion
    }
    public enum RegistrationStatus
    {
        Open = 0,
        Close = 1,
        NotRelevant = 99
    }
}
