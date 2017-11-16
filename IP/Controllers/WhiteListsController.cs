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

namespace IP.Controllers
{
    public class WhiteListsController : Controller
    {
        private FlowDatas db = new FlowDatas();

        // GET: WhiteLists
        public async Task<ActionResult> Index()
        {
            return View(await db.WhiteLists.ToListAsync());
        }

        // GET: WhiteLists/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: WhiteLists/Create
        // 若要免於過量張貼攻擊，請啟用想要繫結的特定屬性，如需
        // 詳細資訊，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,IP,Reason")] WhiteLists whiteLists)
        {
            if (ModelState.IsValid)
            {
                whiteLists.Id = Guid.NewGuid();
                db.WhiteLists.Add(whiteLists);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(whiteLists);
        }

        // GET: WhiteLists/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WhiteLists whiteLists = await db.WhiteLists.FindAsync(id);
            if (whiteLists == null)
            {
                return HttpNotFound();
            }
            return View(whiteLists);
        }

        // POST: WhiteLists/Edit/5
        // 若要免於過量張貼攻擊，請啟用想要繫結的特定屬性，如需
        // 詳細資訊，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,IP,Reason")] WhiteLists whiteLists)
        {
            if (ModelState.IsValid)
            {
                db.Entry(whiteLists).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(whiteLists);
        }

        // GET: WhiteLists/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WhiteLists whiteLists = await db.WhiteLists.FindAsync(id);
            if (whiteLists == null)
            {
                return HttpNotFound();
            }
            return View(whiteLists);
        }

        // POST: WhiteLists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            WhiteLists whiteLists = await db.WhiteLists.FindAsync(id);
            db.WhiteLists.Remove(whiteLists);
            await db.SaveChangesAsync();
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
