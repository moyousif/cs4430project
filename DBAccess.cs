using System;
using System.Collections.Generic;
using System.Text;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;


namespace Launcher {

    // utility class 
    public class SwPackage {
        public string Name { get; set; } = "";
        public string Fullpath { get; set; } = "";
        public int ProcessId { get; set; } = 0;
        public string ProcessName { get; set; } = "";
        public string Status { get; set; } = "";

        public SwPackage() {
            Name = "";
            Fullpath = "";
            ProcessId = 0;
            ProcessName = "";
            Status = "";
        } // end ctor 

        public SwPackage(SwPackage rhs) {
            Name = rhs.Name;
            Fullpath = rhs.Fullpath;
            ProcessId = rhs.ProcessId;
            ProcessName = rhs.ProcessName;
            Status = rhs.Status;
        } // end copy ctor 

    } // end class

    // utility class 
    public class Usage {
        public string User { get; set; } = "";
        public string SwName { get; set; } = "";
        public string start { get; set; } = "";
        public string end { get; set; } = "";

        public Usage() {
            User = "";
            SwName = "";
            start = "";
            end = "";
        } //  ctor 

        public Usage(Usage rhs) {
            User = rhs.User;
            SwName = rhs.SwName;
            start = rhs.start;
            end = rhs.end;
        } // copy ctor 

    } // end class


    class DBAccess {
        private List<SwPackage> _assignedSw;
        private List<SwPackage> _availableSw;
        private Dictionary<int, Usage> _swRunning;


        // log in string property 
        private const string LoginStr = "server=localhost;port=33060;user=root;password=root;";
        private const string TheDatabase = "swlicensing";

        public string User { get; set; }
        public string Password { private get; set; }
        private Boolean _validLogIn = false;

        public string UserFirst { get; set; }
        public string UserLast { get; set; }

        public DBAccess() {
            _assignedSw = new List<SwPackage>();
            _availableSw = new List<SwPackage>();
            _swRunning = new Dictionary<int, Usage>();
        }

        // get non-reference copy 
        public List<SwPackage> GetAssigned() {
            List<SwPackage> ret = new List<SwPackage>();
            foreach(SwPackage swp in _assignedSw)
                ret.Add(new SwPackage(swp));
            return ret;
        } // end GetAvailable

        // get non-reference copy 
        public List<SwPackage> GetAvailable() {
            List<SwPackage> ret = new List<SwPackage>();
            foreach (SwPackage swp in _availableSw)
                ret.Add(new SwPackage(swp));
            return ret;
        } // end GetAvailable


        private int OpenDb(ref Session sess) {

            sess = MySQLX.GetSession(LoginStr);
            if (sess == null) {
                return -1;
            }

            SqlResult stmt = sess.SQL("USE " + TheDatabase).Execute();
            if (sess == null) {
                return -1;
            }

            return 0;
        } // end OpenDb


        public int Login() {
            int ret = -1;

            if (_validLogIn == true) return 0;

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            string qry = "select fname, lname from users where userid = ";
            qry += "'" + User + "' and " + "pwd = '" + Password + "' ";

            DateTime loginTime = DateTime.Now;
            string login_time = loginTime.ToString("yyyy-MM-dd HH:mm:ss");

            sess.StartTransaction();

            try {
                SqlResult result = sess.SQL(qry).Execute();

                Row row = result.FetchOne();
                UserFirst = row[0].ToString();
                UserLast = row[1].ToString();

                qry =  "update users ";
                qry += "set login_time = '" + login_time + "' ";
                qry += "where userid = '" + User + "' and " + "pwd = '" + Password + "' ";

                // this needs the usage_cleanup trigger
                result = sess.SQL(qry).Execute();

                _validLogIn = true;
                ret = 0;
            }
            catch (Exception ex) {
                ret = -1;
                // System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch

            sess.Commit();

            sess.Close();
            return ret;
        }

        public int ReadAvailableFromDb() {
            int ret = -1;

            if (_validLogIn == false) return -1;

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            sess.StartTransaction();

            try {

                string qry = "select sw_name " +
                             "from software " +
                             "where sw_name not in " +
                                 "(select sw_name from license where depart_name = any " +
                                 "(select depart_name from works where userid = '" + User + "')) ";

                // run query 
                SqlResult result = sess.SQL(qry).Execute();

                _availableSw.Clear();

                // process each row 
                Row row = result.FetchOne();
                while (row != null) {
                    SwPackage swp = new SwPackage();
                    swp.Name = row[0].ToString();
                    _availableSw.Add(swp);
                    row = result.FetchOne();
                } // end while 
                ret = 0;

            }
            catch (Exception) {
                //  System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch

            sess.Commit();

            sess.Close();
            return ret;
        }

        public int ReadAssignedFromDb() {
            int ret = -1;

            if (_validLogIn == false) return -1;

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            sess.StartTransaction();

            try {

                string qry = "select sw_name, fullpath " +
                             "from software " +
                             "where sw_name = any (select sw_name " +
                             "from license " +
                             "where depart_name = " +
                             "any (select depart_name from works where userid = '" +
                             User + "')) ";

                // run query 
                SqlResult result = sess.SQL(qry).Execute();

                _assignedSw.Clear();

                // process each row 
                Row row = result.FetchOne();
                while (row != null) {
                    SwPackage swp = new SwPackage();
                    swp.Name = row[0].ToString();
                    swp.Fullpath = row[1].ToString();
                    _assignedSw.Add(swp);
                    row = result.FetchOne();
                } // end while 
                ret = 0;
            }
            catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch

            sess.Commit();

            sess.Close();
            return ret;
        }


        public int RequestAddRemoveSoftware(List<string> sw_name, bool add = true) {
            int ret = -1;

            if (_validLogIn == false) return -1;

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            StringBuilder sb = new StringBuilder("insert into request(userid,sw_name,request_type) values");
            foreach (var sw in sw_name) {
                sb.Append("('" + User + "','" + sw + "','"
                    + (add == true ? "add" : "remove") + "'), ");
            } // end for 

            // remove the last ", "
            sb.Remove(sb.Length-2, 2);

            sess.StartTransaction();

            try {

                // run query 
                SqlResult result = sess.SQL(sb.ToString()).Execute();
                ret = 0;
            }
            catch (Exception) {
                // System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch

            sess.Commit();

            sess.Close();
            return ret;
        }


        public int SetStartUsage(int processID, string sw_name) {
            int ret = -1;

            if (_validLogIn == false) return -1;

            // get the start datetime
            DateTime startingDateTime = DateTime.Now;
            string startingDateTimeStr = startingDateTime.ToString("yyyy-MM-dd HH:mm:ss");

            Usage usage = new Usage();
            usage.User = User;
            usage.SwName = sw_name;
            usage.start = startingDateTimeStr;
            usage.end = "";

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            StringBuilder sb = new StringBuilder("insert into sw_usage(userid,sw_name,start_time) ");
            sb.Append("values('" + User + "','" + sw_name + "','" + startingDateTimeStr + "') ");

            sess.StartTransaction();

            try {
                // run insert
                SqlResult result = sess.SQL(sb.ToString()).Execute();
                ret = 0;
            }
            catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch


            // add partial to dictionary save for end, use as key
            _swRunning.Add(processID, usage);

            sess.Commit();

            sess.Close();
            return ret;
        }

        public int SetEndUsage(int processID) {
            int ret = -1;

            if (_validLogIn == false) return -1;

            // get the matching start data, remove if not found 
            Usage usage = new Usage();
            try {
                usage = _swRunning[processID];
            }
            catch (Exception) {
                // _swRunning.Remove(processID);
                return 0;
            } // end try/catch

            // get the start datetime
            DateTime endingDateTime = DateTime.Now;
            string endingDateTimeStr = endingDateTime.ToString("yyyy-MM-dd HH:mm:ss");

            Session sess = null;
            int openRet = OpenDb(ref sess);
            if (openRet == -1) return -1;

            StringBuilder sb = new StringBuilder("update sw_usage ");
            sb.Append("set end_time = '" + endingDateTimeStr + "' ");
            sb.Append("where sw_name = '" + usage.SwName + "' and ");
            sb.Append("userid = '" + usage.User + "' and ");
            sb.Append("start_time = '" + usage.start + "' ");

            sess.StartTransaction();

            try {
                // run query 
                SqlResult result = sess.SQL(sb.ToString()).Execute();
                ret = 0;
            }
            catch (Exception) {
                // System.Windows.Forms.MessageBox.Show(ex.Message);
            } // end try/catch

            // in all cases remove the item from the dictionary
            _swRunning.Remove(processID);

            sess.Commit();

            sess.Close();
            return ret;
        }

    }
}
