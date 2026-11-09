using System;

namespace w7junpro
{
    public class Employee
    {
        private int _employeeID;
        private string _loginName = "";
        private string _password = "";

        public int EmployeeID
        {
            get { return _employeeID; }
            set { _employeeID = value; }
        }

        public string LoginName
        {
            get { return _loginName; }
            set { _loginName = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        // Definisikan Constructor
        public Employee() { }

        public Employee(string loginName, string password)
        {
            LoginName = loginName;
            Password = password;
        }

        // Method Login dengan parameter
        public bool Login(string user, string pass)
        {
            if (user == "Jones" && pass == "haha")
            {
                EmployeeID = 2;
                return true;
            }
            return false;
        }

        // Method Login tanpa parameter (LAB 7.2)
        public bool Login()
        {
            if (LoginName == "Jono" && Password == "wkkw")
            {
                EmployeeID = 1;
                return true;
            }
            else if (LoginName == "Jones" && Password == "haha")
            {
                EmployeeID = 2;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}