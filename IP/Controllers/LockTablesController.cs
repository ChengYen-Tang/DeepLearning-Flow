using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IP.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace IP.Controllers
{
    public class LockTablesController : Controller
    {
        private FlowDatas db = new FlowDatas();

        // GET: LockTables
        public async Task<ActionResult> Index()
        {
            return View(await db.LockTables.ToListAsync());
        }

        // GET: LockTables/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LockTables lockTables = await db.LockTables.FindAsync(id);
            if (lockTables == null)
            {
                return HttpNotFound();
            }
            return View(lockTables);
        }

        // POST: LockTables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            LockTables lockTables = await db.LockTables.FindAsync(id);
            string Message = JsonConvert.SerializeObject(
                 new
                 {
                     Action = "UnLock",
                     IP = lockTables.IP
                 });

            string Url = "http://172.16.61.26/api/Lock";
            HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
            request.Method = WebRequestMethods.Http.Post;
            request.KeepAlive = true;
            request.ContentType = "application/x-www-form-urlencoded";
            string param = "=" + Message;//注意有個「=」
            byte[] bs = Encoding.Default.GetBytes(param);
            request.ContentLength = bs.Length;

            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Flush();
            }
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //Server回傳的資料。
                        Stream data = response.GetResponseStream();
                        StreamReader sr = new StreamReader(data);
                        string retMsg = sr.ReadToEnd();
                        sr.Close();
                        data.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return RedirectToAction("Index");
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
