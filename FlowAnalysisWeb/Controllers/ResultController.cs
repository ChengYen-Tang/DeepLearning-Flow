using System.Linq;
using System.Web.Mvc;
//using PagedList;
using System.Collections.Generic;
using System;
using FlowAnalysisWeb.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace FlowAnalysisWeb.Controllers
{
    public class ResultController : Controller
    {
        FlowDatas db = new FlowDatas();

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Statistics()
        {
            StatisticsViewModels SVM = new StatisticsViewModels
            {
                Lock = db.LockTables.Count(),
                White = db.WhiteLists.Count()
            };
            DateTime StartTime = DateTime.Now.AddMinutes(-1);
            StartTime = StartTime.AddSeconds(-StartTime.Second).AddMilliseconds(-StartTime.Millisecond);
            DateTime EndTime = DateTime.Now;
            EndTime = EndTime.AddSeconds(-EndTime.Second).AddMilliseconds(-EndTime.Millisecond);
            SVM.Users = db.AnalysisResults.Where(c => c.AnalysisTime >= StartTime && c.AnalysisTime < EndTime).Count();

            return View(SVM);
        }

        public async Task<ActionResult> Chart()
        {
            DateTime SearchDate = DateTime.Now.AddDays(-1);
            SearchDate = SearchDate.AddMilliseconds(-SearchDate.Millisecond).AddSeconds(-SearchDate.Second).AddMinutes(-SearchDate.Minute);
            List<AnalysisResults> SQLAnalysisResults = await db.AnalysisResults.Where(c => c.AnalysisTime >= SearchDate).ToListAsync();
            string Time = "";
            string Value = "";

            for (int i=0;i < 24;i++)
            {
                Time += '"' + SearchDate.AddHours(i).ToString("hh:mm") + '"' + ",";
                Value += SQLAnalysisResults.Where(c => c.AnalysisTime >= SearchDate.AddHours(i) && c.AnalysisTime < SearchDate.AddHours(i + 1)).Count()/60 + ",";
            }

            ViewBag.Time = Time;
            ViewBag.Value = Value;
            return View();
        }

        public async Task<ActionResult> InstantResults()
        {
            List<AnalysisResults> SQLAnalysisResults;
            List<ResultViewModels> AnalysisResults;

            DateTime StartTime = DateTime.Now.AddMinutes(-1);
            StartTime = StartTime.AddSeconds(-StartTime.Second).AddMilliseconds(-StartTime.Millisecond);
            DateTime EndTime = DateTime.Now;
            EndTime = EndTime.AddSeconds(-EndTime.Second).AddMilliseconds(-EndTime.Millisecond);
            SQLAnalysisResults = await db.AnalysisResults.Where(c => c.AnalysisTime >= StartTime && c.AnalysisTime < EndTime).ToListAsync();
            AnalysisResults = SQLAnalysisResults.Select(c => new ResultViewModels
            {
                Id = c.Id,
                AnalysisTime = c.AnalysisTime,
                IP = c.IP,
                Result = Conversion(c.Result)
            }).ToList();

            return View(AnalysisResults);
        }

        public async Task<ActionResult> HistoricalRecord(DateTime? Date,string IP)
        {
            List<AnalysisResults> SQLAnalysisResults;
            List<ResultViewModels> AnalysisResults;



            if (Date == null)
            {
                if (IP != "" && IP != null)
                    SQLAnalysisResults = await db.AnalysisResults.Where(c => c.IP.Contains(IP)).OrderByDescending(c => c.AnalysisTime).ToListAsync();
                else
                    SQLAnalysisResults = await db.AnalysisResults.OrderByDescending(c => c.AnalysisTime).ToListAsync();
                Date = DateTime.Today;
            }
            else
            {
                DateTime EndTime = Date.Value.AddDays(1);
                if (IP != "" && IP != null)
                    SQLAnalysisResults = await db.AnalysisResults.Where(c => c.AnalysisTime >= Date && c.AnalysisTime < EndTime && c.IP.Contains(IP)).OrderByDescending(c => c.AnalysisTime).ToListAsync();
                else
                    SQLAnalysisResults = await db.AnalysisResults.Where(c => c.AnalysisTime >= Date && c.AnalysisTime < EndTime).OrderByDescending(c => c.AnalysisTime).ToListAsync();
            }
            ViewBag.datetimepicker = Date.Value.Year + "-" + Date.Value.Month + "-" + Date.Value.Day;
            ViewBag.IPAddress = IP;
            AnalysisResults = SQLAnalysisResults.Select(c => new ResultViewModels
            {
                Id = c.Id,
                AnalysisTime = c.AnalysisTime,
                IP = c.IP,
                Result = Conversion(c.Result)
            }).ToList();
            return View(AnalysisResults);
        }

        public ActionResult Time()
        {
            return View();
        }

        public static string Conversion(int Input)
        {
            if (Input is 0)
                return "Normal behavior";
            else if (Input is 1)
                return "WannaCry Virus";
            else if (Input is 2)
                return "DDOS Attack Web";
            else
                return "BT Download";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}



