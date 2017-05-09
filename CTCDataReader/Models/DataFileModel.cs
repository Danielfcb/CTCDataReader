using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTCDataReader.Models
{
    public class DataFileRecordModel
    {
        public enum Statuses
        {
            Active,
            Inactive
        }

        public string DepartmentName { get; set; }
        public string Crew_Code { get; set; }
        public string employee_name { get; set; }
        public Statuses Status { get; set; }
        public string Employee_num { get; set; }
        public string Role { get; set; }
        public string SeniorityDate { get; set; }
        public string Supervisor_name { get; set; }
        public string Supervisor_num { get; set; }
    }
}
