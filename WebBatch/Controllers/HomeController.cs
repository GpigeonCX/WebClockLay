using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebBatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;

namespace WebBatch.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        ClockContext Db = new ClockContext();
        private static readonly string posturl1 = ConfigurationManager.ConnectionStrings["PostUrl1"].ConnectionString;
        private static readonly string posturl2 = ConfigurationManager.ConnectionStrings["PostUrl2"].ConnectionString;
        public ActionResult GetEmployee()

        {
            try
            {
                var ClassName = Request["ClassName"].ToString();
                var CardId = Request["CardId"].ToString();
                var EmployeeName = Request["EmployeeName"].ToString();
                var data1 = from r in Db.ClockBatch
                            select new
                            {
                                r.guid,
                                r.ClassName,
                                r.CardId,
                                //LastClockTime = DateTime.Now.Subtract(r.LastClockTime ?? DateTime.Now).Minutes > 30 ? null : r.LastClockTime,
                                LastClockTime = r.LastClockTime.ToString(),
                                r.ClockState,
                                r.EmployeeName,
                                r.FailedReason,
                                r.Times,
                                StartClockTime = r.StartClockTime.ToString(),
                            };
                if (!CardId.Equals(""))
                    data1 = data1.Where(r => r.CardId.Equals(CardId.Trim()));
                if (!EmployeeName.Equals(""))
                    data1 = data1.Where(r => r.EmployeeName.Contains(EmployeeName.Trim()));
                if (!ClassName.Equals(""))
                    data1 = data1.Where(r => r.ClassName.Contains(ClassName));
                int total = data1.Count();//总条数
                                          //构造成Json的格式传递
                var result = new { code = 0, msg = "123", count = total, data = data1.ToList() };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(null);
            }


        }
        [HttpPost]
        public ActionResult Add()
        {
            try
            {
                var StartTime = Convert.ToDateTime(Request["AddStartTime"]);
                var ClassName = Request["AddClassName"].ToString().Trim();
                //string[] CardId = Request["CardId"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string[] AddInfo = Request["AddInfo"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string[] info;
                if (AddInfo.Length > 0)
                {
                    foreach (var item in AddInfo)
                    {
                        info = item.Split('-');
                        ClockBatch model = new ClockBatch
                        {
                            guid = Guid.NewGuid(),
                            EmployeeName = info[1],
                            CardId = info[0].Trim(),
                            ClassName = ClassName,
                            StartClockTime = StartTime,
                            LastClockTime = Convert.ToDateTime("2000-1-1 00:00"),
                            ClockState = false,
                            FailedReason = "新增的数据，还没有打卡记录！"+DateTime.Now.ToString(),
                            flag = true
                        };
                        Db.ClockBatch.Add(model);
                        Db.Entry<ClockBatch>(model).State = EntityState.Added;
                    }
                }
                if (Db.SaveChanges() > 0)
                {
                    return Json("OK");
                }
                else
                {
                    return Json("添加失败");
                }
            }
            catch (Exception ex)
            {
                return Json($"添加失败,请检查数据格式,使用英文符号！异常信息：{ex}");
            }

        }
        [HttpPost]
        public ActionResult Edit()
        {
            try
            {
                var guid = Guid.Parse(Request["guid"].ToString());
                var EditCardId = Request["EditCardId"].ToString().Trim();
                var EditEmployeeName = Request["EditEmployeeName"].ToString().Trim();
                var EditClassName = Request["EditClassName"].ToString().Trim();
                var EditStartTime = Convert.ToDateTime(Request["EditStartTime"]);
                var editData = (from r in Db.ClockBatch
                                where r.guid.Equals(guid)
                                select r).FirstOrDefault();

                editData.CardId = EditCardId;
                editData.EmployeeName = EditEmployeeName;
                editData.ClassName = EditClassName;
                editData.StartClockTime = EditStartTime;
                Db.ClockBatch.Add(editData);
                Db.Entry<ClockBatch>(editData).State = EntityState.Modified;
                if (Db.SaveChanges() > 0)
                {
                    return Json("OK");
                }
                else
                {
                    return Json("修改失败");
                }
            }
            catch (Exception ex)
            {
                return Json($"修改失败,请检查数据格式！异常信息：{ex}");
            }

        }
        public ActionResult Delete(string Id)
        {
            try
            {
                string[] ids = Id.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (ids == null) return View("Index");
                foreach (var id in ids)
                {
                    var model1 = new ClockBatch() { guid = Guid.Parse(id) };
                    Db.ClockBatch.Attach(model1);
                    Db.Entry<ClockBatch>(model1).State = EntityState.Deleted;
                }
                Db.SaveChanges();
                return Json("OK");
            }
            catch (Exception ex)
            {
                return Json(string.Format("删除失败，请检查链路是否通畅！异常信息：{0}", ex));
            }
        }
        public static Dictionary<string, string> _dicCookie = new Dictionary<string, string>();
        public static Dictionary<string, string> _dicClassID = new Dictionary<string, string>();
        public ActionResult ClockGo(string Id)
        {
            try
            {
                _dicCookie.Clear();
                _dicClassID.Clear();
                string[] ids = Id.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (ids == null) return View("Index");
                var allData = (from r in Db.ClockBatch
                               where ids.Contains(r.guid.ToString())
                               select r).ToList();
                var ClassName = "";
                JObject studentsJson;
                string strResult = "";
                IEnumerable<JToken> studentsList;
                foreach (var model in allData)
                {

                    ClassName = System.Text.RegularExpressions.Regex.Replace(model.ClassName, @"[^0-9]+", "");
                    var response1 = HttpPost(posturl1, "idcard=" + model.CardId);
                    strResult = Unicode2String(response1);//正常字符串结果
                    if (strResult.Contains("第一步成功"))
                    {
                        try
                        {
                            studentsJson = JObject.Parse(response1);
                        }
                        catch (Exception)
                        {
                            model.ClockState = false;
                            model.FailedReason += strResult;
                            Db.ClockBatch.Attach(model);
                            Db.Entry<ClockBatch>(model).State = EntityState.Modified;
                            Db.SaveChanges();
                            continue;
                        }
                        studentsList = studentsJson["data"]["classes"].AsEnumerable();
                        foreach (var item in studentsList)
                        {
                            //找到班级名称对应的班级ID 放入班级ID字典  从右边取10个字符，然后正则取纯数字。OS：我是被逼的
                            //if (ClassName.Equals(System.Text.RegularExpressions.Regex.Replace(item["tname"].ToString().Remove(0, item["tname"].ToString().Length - 10), @"[^0-9]+", "")))
                            if(ClassName.Equals(Regex.Replace(item["tname"].ToString().Replace("2019",""), @"[^0-9]+", "")))
                            {
                                if (!_dicClassID.ContainsKey(model.ClassName))
                                {
                                    _dicClassID.Add(model.ClassName, item["id"].ToString());
                                    break;
                                }
                            };
                        }
                        if (!_dicClassID.ContainsKey(model.ClassName))
                            return Json($"打卡失败，该工号{model.CardId}没有找到对应的班级，请检查！");
                        var response2 = HttpPost(posturl2, "class_id=" + _dicClassID[model.ClassName]
                         , new Dictionary<string, string>()
                         {
                            { "cookie", _dicCookie[string.Format("idcard={0}",model.CardId)] }
                         });
                        if (JObject.Parse(response2).ToString().Contains("成功"))
                        {
                            model.LastClockTime = DateTime.Now;
                            model.ClockState = true;
                            model.FailedReason = "";
                            model.Times++;
                            Db.ClockBatch.Attach(model);
                            Db.Entry<ClockBatch>(model).State = EntityState.Modified;
                            Db.SaveChanges();
                            Thread.Sleep(new Random().Next(3000, 8000));
                        }
                        else if ((JObject.Parse(response2).ToString().Contains("已签")))
                        {
                            model.LastClockTime = DateTime.Now;   //返回是已签到也更新上次打卡时间4.2
                            model.ClockState = false;
                            model.FailedReason += JObject.Parse(response2)["msg"].ToString() + DateTime.Now.ToString();
                            Db.ClockBatch.Attach(model);
                            Db.Entry<ClockBatch>(model).State = EntityState.Modified;
                            Db.SaveChanges();
                        }
                        else
                        {
                            model.ClockState = false;
                            model.FailedReason += JObject.Parse(response2)["msg"].ToString() + DateTime.Now.ToString();
                            Db.ClockBatch.Attach(model);
                            Db.Entry<ClockBatch>(model).State = EntityState.Modified;
                            Db.SaveChanges();
                            if (allData.IndexOf(model) == 0)
                                break;
                        }
                    }
                    else
                    {
                        model.ClockState = false;
                        model.FailedReason += strResult;
                        Db.ClockBatch.Attach(model);
                        Db.Entry<ClockBatch>(model).State = EntityState.Modified;
                        Db.SaveChanges();
                        if (allData.IndexOf(model) == 0)
                            break;
                    }

                }
                return Json("OK");
            }
            catch (Exception ex)
            {
                return Json(string.Format("打卡失败，请检查链路是否通畅！异常信息：{0}", ex));
            }
        }
        /// <summary>
        /// Unicode转字符串
        /// </summary>
        /// <param name="source">经过Unicode编码的字符串</param>
        /// <returns>正常字符串</returns>
        public static string Unicode2String(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
                         source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }
        /// <summary>
        /// 获取班级下拉框
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSelect()
        {
            var data = Db.ClockBatch.GroupBy(a => a.ClassName).Select(a => a.Key).ToList();
            return Json(data);
        }
        public static string HttpPost(string url, string paramData, Dictionary<string, string> headerDic = null)
        {
            string result = string.Empty;
            try
            {
                HttpWebRequest wbRequest = (HttpWebRequest)WebRequest.Create(url);
                wbRequest.Method = "POST";
                wbRequest.ContentType = "application/x-www-form-urlencoded";
                wbRequest.ContentLength = Encoding.UTF8.GetByteCount(paramData);
                if (headerDic != null && headerDic.Count > 0)
                {
                    foreach (var item in headerDic)
                    {
                        wbRequest.Headers.Add(item.Key, item.Value);
                    }
                }
                using (Stream requestStream = wbRequest.GetRequestStream())
                {
                    using (StreamWriter swrite = new StreamWriter(requestStream))
                    {
                        swrite.Write(paramData);
                    }
                }
                HttpWebResponse wbResponse = (HttpWebResponse)wbRequest.GetResponse();
                using (Stream responseStream = wbResponse.GetResponseStream())
                {
                    using (StreamReader sread = new StreamReader(responseStream))
                    {
                        result = sread.ReadToEnd();
                    }
                    if (_dicCookie.ContainsKey(paramData))
                    {
                        _dicCookie.Remove(paramData);
                        _dicCookie.Add(paramData, wbResponse.GetResponseHeader("Set-Cookie"));
                    }
                    _dicCookie.Add(paramData, wbResponse.GetResponseHeader("Set-Cookie"));
                }
            }
            catch (Exception ex)
            { }

            return result;
        }
        public static string MidStrEx(string sourse, string startstr, string endstr)
        {
            string result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                    return result;
                string tmpstr = sourse.Substring(startindex + startstr.Length);
                endindex = tmpstr.IndexOf(endstr);
                if (endindex == -1)
                    return result;
                result = tmpstr.Remove(endindex);
            }
            catch (Exception ex)
            {
                //Log.WriteLog("MidStrEx Err:" + ex.Message);
            }
            return result;
        }
    }
}