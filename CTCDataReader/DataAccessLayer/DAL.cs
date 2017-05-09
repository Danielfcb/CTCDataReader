using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTCDataReader.DataAccessLayer
{
    public class DAL
    {
        private MasterRosterEntities _db;

        public DAL()
        {
            _db = new MasterRosterEntities();
        }
        public List<Crew> GetAllDisctinctCrews()
        {
            return _db.Crews.Distinct().ToList();
        }

        public List<Department> GetAllDistinctDepartments()
        {
            return _db.Departments.Distinct().ToList();
        }

        public bool DoesEmployeeNumberExist(string employeeNumber)
        {
            return (_db.Employees.Where(e => e.employee_num == employeeNumber).Count() > 0);
        }

        public int? GetDepartmentIDByName(string departmentName)
        {
            return _db.Departments.Where(d => d.department_name == departmentName).SingleOrDefault().department_id;
        }

        public bool InsertIntoEmployeesTable(List<Employee> employees, bool disableForeignKey = false)
        {
            bool result = true;
            try
            {
                if (employees.Count() > 0)
                {
                    // For the manger we need to disable FK_Employees_Supervisors foreign key constraint 
                    // This constraint checks to make sure supervisor_id exists in database which in case of Managers
                    // there is no supervisor_id assigned
                    if (disableForeignKey)
                    {
                        DisableForeignKey();
                    }

                    foreach (var employee in employees)
                    {
                        _db.Employees.Add(employee);
                    }

                    _db.SaveChanges();

                    if (disableForeignKey)
                    {
                        EnableForeignKey();
                    }
                }
            }
            catch
            {
                result = false;
                // log the details
            }

            return result;
        }

        private void DisableForeignKey()
        {
            _db.Database.ExecuteSqlCommand("IF (OBJECT_ID('FK_Employees_Supervisors', 'F') IS NOT NULL) BEGIN ALTER TABLE Employees DROP CONSTRAINT FK_Employees_Supervisors END");
        }

        private void EnableForeignKey()
        {
            _db.Database.ExecuteSqlCommand("IF (OBJECT_ID('FK_Employees_Supervisors', 'F') IS NULL) BEGIN ALTER TABLE Employees With Nocheck Add CONSTRAINT [FK_Employees_Supervisors] foreign key(supervisor_id) references[Employees](employee_id) END");
        }

        public List<string> GetAllEmployeeNumbers()
        {
            return _db.Employees.Select(e => e.employee_num).ToList();
        }

        public void SetStatusToInactive(List<string> employeeNumbersToSetToInactive)
        {
            foreach (string employeeNumber in employeeNumbersToSetToInactive)
            {
                _db.Database.ExecuteSqlCommand("Update Employees set Status = 0 where employee_num='" + employeeNumber + "'");
            }

        }

        public void RemoveEmployeeFromDatabase(string employeeNumber)
        {
            _db.Database.ExecuteSqlCommand("delete from Employees where employee_num='" + employeeNumber + "'");
        }

        public int? GetEmployeeIdForEmployeeNumber(string employeeNumber)
        {
            int? employeeId = null;

            if (!string.IsNullOrEmpty(employeeNumber))
            {
                var employee = _db.Employees.Where(e => e.employee_num == employeeNumber).SingleOrDefault();
                if (employee != null)
                {
                    employeeId = employee.employee_id;
                }
            }
            else
            {
                employeeId = null;
            }

            return employeeId;
        }
        public List<RoleType> GetAllDistinctRollTypes()
        {
            return _db.RoleTypes.Distinct().ToList();
        }

        public int GetRoleTypeIdByName(string roleTypeName)
        {
            return _db.RoleTypes.Where(r => r.roletype_name == roleTypeName).SingleOrDefault().roletype_id;
        }

        public bool UpdateEmployeeRecords(List<Employee> employees, bool disableForeignKey = false)
        {
            bool result = true;

            try
            {
                if (employees.Count() > 0)
                {

                    if (disableForeignKey)
                    {
                        DisableForeignKey();
                    }

                    // list of employee numbers to be updated
                    List<string> updatedEmployeeNumbers = employees.Select(e => e.employee_num).ToList();

                    _db.Employees.Where(e => updatedEmployeeNumbers.Contains(e.employee_num)).ToList().ForEach(e =>
                    {
                        var newEmployeeInfo = employees.Where(newEmployee => newEmployee.employee_num == e.employee_num).SingleOrDefault();
                        e.department_id = newEmployeeInfo.department_id;
                        e.crew_id = newEmployeeInfo.crew_id;
                        e.name = newEmployeeInfo.name;
                        e.roletype_id = newEmployeeInfo.roletype_id;
                        e.seniority_date = newEmployeeInfo.seniority_date;
                        e.status = newEmployeeInfo.status;
                        e.supervisor_id = newEmployeeInfo.supervisor_id;
                    }
                    );

                    _db.SaveChanges();

                    if (disableForeignKey)
                    {
                        EnableForeignKey();
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                //log the details
            }

            return result;
        }
    }
}
