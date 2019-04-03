using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebBatch.Models
{
    public class ClockBatch
    {
        /// <summary>
        /// 唯一ID
        /// </summary>
        [Key]
        public Guid guid { get; set; }
        /// <summary>
        /// 工号
        /// </summary>
        public string CardId { get; set; }
        /// <summary>
        /// 班级
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string EmployeeName { get; set; }
        /// <summary>
        /// 打卡状态
        /// </summary>
        public Boolean ClockState { get; set; }
        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailedReason { get; set; }
        /// <summary>
        /// 上次打卡时间              默认值数据库最小时间
        /// </summary>
        public Nullable<System.DateTime> LastClockTime { get; set; }
        /// <summary>
        /// 开始打卡时间              默认值不打
        /// </summary>
        public Nullable<System.DateTime> StartClockTime { get; set; }
        /// <summary>
        /// 打卡次数              默认值数据库最小时间
        /// </summary>
        public int Times { get; set; }
        /// <summary>
        /// 是否有效        默认true
        /// </summary>
        public bool flag { get; set; }
    }
}