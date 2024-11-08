using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAn_LapTrinhWeb.Controllers
{
    public class ErrorController : Controller
    {
        //
        // GET: /Error/

        public ActionResult NotFound()
        {
            Response.StatusCode = 404; // Thiết lập mã trạng thái là 404
            return View();
        }
    }
}
