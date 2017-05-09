using CTCDataReader.DataAccessLayer;
using CTCDataReader.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static CTCDataReader.Models.DataFileRecordModel;

namespace CTCDataReader.Utilties
{
    public class DataValidator : iDataValidator<DataFileRecordModel>
    {
        private DAL _dal;
        private string _employeeNumberRegex;
        private string _employeeNameRegex;

        public List<DataFileRecordModel> ValidRecords { get; private set; }
        public List<string> InvalidRecords { get; private set; }
        public List<DataFileRecordModel> DuplicatedRecords { get; private set; }
        public string HeaderLine { get; private set; }

        public DataValidator()
        {
            _dal = new DAL();
            ValidRecords = new List<DataFileRecordModel>();
            DuplicatedRecords = new List<DataFileRecordModel>();
            InvalidRecords = new List<string>();
            _employeeNumberRegex = ConfigurationManager.AppSettings["EmployeeNumberRegex"];
            _employeeNameRegex = ConfigurationManager.AppSettings["EmployeeNameRegex"];
        }

        public void ValidateFileContent(string fileContent)
        {
            StringReader strReader = new StringReader(fileContent);
            HeaderLine = strReader.ReadLine();

            bool read = true;

            while (read)
            {
                string record = strReader.ReadLine();
                if (record != null)
                {
                    ValidateRecord(record);

                }
                else
                {
                    read = false;
                }
            }

        }

        public bool ValidateRecord(string record)
        {
            bool result = true;

            string[] recordFields = record.Split(',');

            result = isDepartmentNameValid(recordFields[0]);
            if (result)
                result = isCrewCodeValid(recordFields[1]);
            if (result)
                result = isEmployeeNameValid(recordFields[2]);
            if (result)
                result = isStatusValid(recordFields[3]);
            if (result)
                result = isEmployeeNumberValid(recordFields[4]);
            if (result)
                result = isRoleTypeValid(recordFields[5]);
            if (result)
                result = isSeniorityDateValid(recordFields[6]);
            if (result)
                result = isSupervisorNameValid(recordFields[7], recordFields[5]);
            if (result)
                result = isSupervisorNumberValid(recordFields[4], recordFields[8], recordFields[5]);

            if (result)
            {
                DataFileRecordModel dataFileModel = CreateDataFileRecordModel(record);

                if (isDuplicatedRecord(recordFields[4]))
                {
                    DuplicatedRecords.Add(dataFileModel);
                }
                else
                {
                    ValidRecords.Add(dataFileModel);
                }
            }
            else
            {
                InvalidRecords.Add(record);
            }

            return result;
        }

        public DataFileRecordModel CreateDataFileRecordModel(string record)
        {
            string[] recordFields = record.Split(',');

            DataFileRecordModel dataFileRecordModel = new DataFileRecordModel()
            {
                DepartmentName = recordFields[0],
                Crew_Code = recordFields[1],
                employee_name = recordFields[2],
                Status = (Statuses)Enum.Parse(typeof(Statuses), recordFields[3]),
                Employee_num = recordFields[4],
                Role = recordFields[5],
                SeniorityDate = recordFields[6],
                Supervisor_name = recordFields[7],
                Supervisor_num = recordFields[8]
            };

            return dataFileRecordModel;
        }

        private bool isDepartmentNameValid(string departmentName)
        {
            bool result = true;

            if (!string.IsNullOrEmpty(departmentName) && _dal.GetAllDistinctDepartments().Where(d => d.department_name == departmentName).Count() == 0)
            {
                result = false;
            }

            return result;
        }

        private bool isCrewCodeValid(string crewCode)
        {
            bool result = true;

            if (!string.IsNullOrEmpty(crewCode) && _dal.GetAllDisctinctCrews().Where(c => c.crew_code == crewCode).Count() == 0)
            {
                result = false;
            }
            return result;
        }

        private bool isEmployeeNameValid(string employeeName)
        {
            bool result = true;

            if (string.IsNullOrEmpty(employeeName) || !Regex.IsMatch(employeeName, _employeeNameRegex))
            {
                result = false;
            }

            return result;
        }

        private bool isStatusValid(string status)
        {
            bool result = true;

            try
            {
                Statuses theStats = (Statuses)Enum.Parse(typeof(Statuses), status);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        private bool isEmployeeNumberValid(string employeeNumber)
        {
            bool result = true;

            if (string.IsNullOrEmpty(employeeNumber) || !Regex.IsMatch(employeeNumber, _employeeNumberRegex))
            {
                result = false;
            }

            return result;
        }

        private bool isRoleTypeValid(string role)
        {
            bool result = true;

            if (!string.IsNullOrEmpty(role) && _dal.GetAllDistinctRollTypes().Where(r => r.roletype_name == role).Count() == 0)
            {
                result = false;
            }

            return result;
        }

        private bool isSeniorityDateValid(string seniorityDate)
        {
            bool result = true;

            if (string.IsNullOrEmpty(seniorityDate))
            {
                result = false;
            }
            DateTime temp;

            if (result && !DateTime.TryParse(seniorityDate, out temp))
            {
                result = false;
            }

            return result;
        }

        private bool isSupervisorNameValid(string supervisorName, string roleType)
        {
            bool result = true;

            // Other than managers, rest of the employees must have a supervisor assigned
            if ((string.IsNullOrEmpty(supervisorName) && roleType != "Manager") ||
                (!string.IsNullOrEmpty(supervisorName) && !Regex.IsMatch(supervisorName, _employeeNameRegex)))
            {
                result = false;
            }

            return result;
        }

        private bool isSupervisorNumberValid(string employeeNumber, string supervisorNumber, string roleType)
        {
            bool result = true;

            // Validation criteria:
            // 1. Other than managers, rest of the employees must have a supervisor assigned
            // 2. Supervisor number should be different from employee number
            // 3. Supervisor number should have an employee_id assigned to it in database, or it should be in the list of validated records
            //    ready to be inserted into the database
            if ((string.IsNullOrEmpty(supervisorNumber) && roleType != "Manager") ||
                (!string.IsNullOrEmpty(supervisorNumber) && !Regex.IsMatch(supervisorNumber, _employeeNumberRegex)) ||
                (employeeNumber == supervisorNumber))
            {
                result = false;
            }

            if (result)
            {
                int? employeeIdOfSupervisorNumber = _dal.GetEmployeeIdForEmployeeNumber(supervisorNumber);
                // The supervisor number should already be in database or it should be in data file ready to be inserted!
                if (employeeIdOfSupervisorNumber == null)
                {
                    if (roleType == "Manager")
                    {
                        if (!string.IsNullOrEmpty(supervisorNumber))
                        {
                            if (employeeIdOfSupervisorNumber == null
                                && GetValidatedManagerRecords().Where(r => r.Employee_num == supervisorNumber).Count() == 0
                                && GetValidatedSupervisorRecords().Where(r => r.Employee_num == supervisorNumber).Count() == 0)
                            {
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        if (GetValidatedManagerRecords().Where(r => r.Employee_num == supervisorNumber).Count() == 0 &&
                            GetValidatedSupervisorRecords().Where(r => r.Employee_num == supervisorNumber).Count() == 0)
                        {
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        private bool isDuplicatedRecord(string employeeNumner)
        {
            return _dal.DoesEmployeeNumberExist(employeeNumner);
        }


        #region GettingFinalRecords
        public List<DataFileRecordModel> GetValidatedManagerRecords()
        {
            return (ValidRecords.Where(vr => vr.Role == "Manager").ToList());

        }

        public List<DataFileRecordModel> GetDuplicatedManagerRecords()
        {
            return DuplicatedRecords.Where(vr => vr.Role == "Manager").ToList();
        }

        public List<DataFileRecordModel> GetValidatedSupervisorRecords()
        {
            return ValidRecords.Where(vr => vr.Role == "Supervisor").ToList();
        }

        public List<DataFileRecordModel> GetDuplicatedSupervisorRecords()
        {
            return DuplicatedRecords.Where(vr => vr.Role == "Supervisor").ToList();
        }

        public List<DataFileRecordModel> GetValidatedWorkerRecords()
        {
            return ValidRecords.Where(vr => vr.Role == "Worker").ToList();
        }

        public List<DataFileRecordModel> GetDuplicatedWorkerRecords()
        {
            return DuplicatedRecords.Where(vr => vr.Role == "Worker").ToList();
        }
        #endregion




        #region GettingEmployees
        public List<Employee> GetValidatedManagerEmployees()
        {
            return CreateEmployeesFromRecords(GetValidatedManagerRecords());

        }

        public List<Employee> GetDuplicatedManagerEmployees()
        {
            return CreateEmployeesFromRecords(GetDuplicatedManagerRecords());
        }

        public List<Employee> GetValidatedSupervisorEmployees()
        {
            return CreateEmployeesFromRecords(GetValidatedSupervisorRecords());
        }

        public List<Employee> GetDuplicatedSupervisorEmployees()
        {
            return CreateEmployeesFromRecords(GetDuplicatedSupervisorRecords());
        }

        public List<Employee> GetValidatedWorkerEmployees()
        {
            return CreateEmployeesFromRecords(GetValidatedWorkerRecords());
        }

        public List<Employee> GetDuplicatedWorkerEmployees()
        {
            return CreateEmployeesFromRecords(GetDuplicatedWorkerRecords());
        }

        #endregion



        public List<Employee> CreateEmployeesFromRecords(List<DataFileRecordModel> records)
        {
            List<Employee> employees = new List<Employee>();
            List<Department> allDistinctDepartments = _dal.GetAllDistinctDepartments();
            List<RoleType> allDistinctRoleTypes = _dal.GetAllDistinctRollTypes();
            List<Crew> allDistinctCrews = _dal.GetAllDisctinctCrews();

            foreach (var record in records)
            {
                try
                {
                    Employee employee = new Employee()
                    {
                        name = record.employee_name,
                        department_id = allDistinctDepartments.Where(d => d.department_name == record.DepartmentName).SingleOrDefault().department_id,
                        employee_num = record.Employee_num,
                        status = (record.Status == Statuses.Active) ? (true) : (false),
                        seniority_date = Convert.ToDateTime(record.SeniorityDate),
                        roletype_id = allDistinctRoleTypes.Where(r => r.roletype_name == record.Role).SingleOrDefault().roletype_id,
                        crew_id = allDistinctCrews.Where(c => c.crew_code == record.Crew_Code).SingleOrDefault().crew_id,
                        supervisor_id = _dal.GetEmployeeIdForEmployeeNumber(record.Supervisor_num)
                    };

                    employees.Add(employee);
                }
                catch
                {
                    // TODO: This needs to get fixed!
                    InvalidRecords.Add(record.ToString());
                }
            }

            return employees;
        }

    }
}
