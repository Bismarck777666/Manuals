        private void getAllChildAgent(List<int> ids, List<int> allChildAgents)
        {
            List<int> iList = new List<int>();
            List<agent> agentList = new List<agent>();

            string agentQuery = string.Format("SELECT * FROM Agents WHERE parentid In ({0})", string.Join(",", ids));
            List<agent> childAgents = db.Database.SqlQuery<agent>(agentQuery).ToList();

            foreach (agent agent in childAgents)
            {
                iList.Add(agent.id);
            }

            if (iList.Count < 1)
                return;
            else
            {
                allChildAgents.AddRange(iList);
                getAllChildAgent(iList, allChildAgents);
            }
        }

        private string getAgentReport(string parentIdString, DateTime sDate, DateTime eDate, string gameType)
        {
            try
            {
                eDate = eDate.AddSeconds(1);
                int parentId = 0;
                if (!int.TryParse(parentIdString, out parentId))
                    return doJson(0, null, "No Transaction Data!");

                List<int> agentIds = new List<int>();
                getAllChildAgent(new List<int>() { parentId }, agentIds);
                agentIds.Add(parentId);

                List<SumReportViewModel> totalVars = new List<SumReportViewModel>();
                decimal total = 0;

                var dataTable = new DataTable();
                dataTable.TableName = "dbo.AgentIDs";
                dataTable.Columns.Add("id", typeof(int));
                foreach (var item in agentIds)
                {
                    dataTable.Rows.Add(item);
                }

                SqlParameter parameter1 = new SqlParameter("agentids", SqlDbType.Structured);
                parameter1.TypeName = "dbo.AgentIDs";
                parameter1.Value = dataTable;
                SqlParameter parameter2 = new SqlParameter("startDate", SqlDbType.DateTime);
                parameter2.Value = sDate;
                SqlParameter parameter3 = new SqlParameter("endDate", SqlDbType.DateTime);
                parameter3.Value = eDate;
                SqlParameter parameter4 = new SqlParameter("gameType", SqlDbType.Int);
                parameter4.Value = Convert.ToInt32(gameType);

                string sqlquery = "EXEC [dbo].[ProcReportByDate] @agentids, @startDate, @endDate, @gameType";
                List<SumReportViewModel> vars = db.Database.SqlQuery<SumReportViewModel>(sqlquery, parameter1, parameter2, parameter3, parameter4).ToList();

                Dictionary<DateTime, SumReportViewModel> dicVars = new Dictionary<DateTime, SumReportViewModel>();

                foreach (SumReportViewModel var in vars)
                {
                    if (var.WinField.HasValue)
                    {
                        total += (var.BetField.Value - var.WinField.Value);
                    }
                    var.DateTimeField = var.DateTimeField.AddSeconds(-28800);
                    var.DateField = var.DateTimeField.ToString("yyyy-MM-dd HH:mm:ss");
                    dicVars.Add(var.DateTimeField, var);
                }

                for (DateTime time = sDate; time < eDate; time = time.AddDays(1))
                {
                    if (!dicVars.ContainsKey(time))
                    {
                        //SumReportViewModel var = new SumReportViewModel();
                        //var.DateField = time.ToString("yyyy-MM-dd HH:mm:ss");
                        //totalVars.Add(var);
                    }
                    else
                    {
                        totalVars.Add(dicVars[time]);
                    }
                }
                return doJson(total, totalVars, string.Empty);
            }
            catch (Exception)
            {
                return doJson(0, null, "No Transaction Data!");
            }
        }

	private string getTotalAgentReport(string parentIdString, DateTime sDate, DateTime eDate, string gameType)
        {
            eDate = eDate.AddSeconds(1);
            int parentId = 0;
            if (!int.TryParse(parentIdString, out parentId))
                return doJson(0, null, "No Transaction Data!");

            List<SumReportViewModel> totalVars = new List<SumReportViewModel>();
            decimal total = 0;

            string sqlquery = "SELECT * FROM agents WHERE parentid=@p0";
            List<agent> childAgents = db.Database.SqlQuery<agent>(sqlquery, parentId).ToList();
            foreach (agent agent in childAgents)
            {
                List<int> agentIds = new List<int>();
                getAllChildAgent(new List<int>() { agent.id }, agentIds);
                agentIds.Add(agent.id);

                SumReportViewModel var = new SumReportViewModel();
                var.UserField = agent.username;
                try
                {
                    var dataTable = new DataTable();
                    dataTable.TableName = "dbo.AgentIDs";
                    dataTable.Columns.Add("id", typeof(int));
                    foreach (var item in agentIds)
                    {
                        dataTable.Rows.Add(item);
                    }
                    SqlParameter parameter1 = new SqlParameter("agentids", SqlDbType.Structured);
                    parameter1.TypeName = "dbo.AgentIDs";
                    parameter1.Value = dataTable;
                    SqlParameter parameter2 = new SqlParameter("startDate", SqlDbType.DateTime);
                    parameter2.Value = sDate;
                    SqlParameter parameter3 = new SqlParameter("endDate", SqlDbType.DateTime);
                    parameter3.Value = eDate;
                    SqlParameter parameter4 = new SqlParameter("gameType", SqlDbType.Int);
                    parameter4.Value = Convert.ToInt32(gameType);

                    sqlquery = "EXEC [dbo].[ProcReport] @agentids, @startDate, @endDate, @gameType";
                    List<SumReportViewModel> vars = db.Database.SqlQuery<SumReportViewModel>(sqlquery, parameter1, parameter2, parameter3, parameter4).ToList();
                    if (vars[0].WinField.HasValue)
                    {
                        total += (vars[0].BetField.Value - vars[0].WinField.Value);
                        var.WinField = vars[0].WinField;
                        var.BetField = vars[0].BetField;
                        var.TurnOverField = vars[0].TurnOverField;

                        var.name = agent.name;
                        var.tel = agent.tel;
                        var.description = agent.description;
                        var.sadescription = agent.sadescription;

                        totalVars.Add(var);
                    }
                }
                catch (Exception ex)
                {
                    System.IO.File.WriteAllText(Server.MapPath("/log.txt"), ex.ToString());
                }
            }

            return doJson(total, totalVars, string.Empty);
        }

        private string doJson(decimal total, IEnumerable<SumReportViewModel> vars, string error)
        {
            ReportWrapper wrapper = new ReportWrapper();
            wrapper.total = total;
            wrapper.reports = vars.ToList().Count < 1 ? null : vars.ToList();
            wrapper.error = error;
            return JsonConvert.SerializeObject(wrapper);
        }


	    public class SumReportViewModel
    {
        public string UserField { get; set; }
        public string DateField { get; set; }
        public DateTime DateTimeField { get; set; }
        public decimal? WinField { get; set; }
        public decimal? BetField { get; set; }
        public decimal? TurnOverField { get; set; }
        public string name { get; set; }
        public string tel { get; set; }
        public string description { get; set; }
        public string sadescription { get; set; }
        public SumReportViewModel()
        {
            UserField = string.Empty;
            DateField = string.Empty;
            WinField = 0;
            BetField = 0;
            TurnOverField = 0;
            name = string.Empty;
            tel = string.Empty;
            description = string.Empty;
            sadescription = string.Empty;
        }
    }