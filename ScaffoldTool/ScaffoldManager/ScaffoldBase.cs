using System;

namespace ScaffoldTool.ScaffoldManager
{
    //
    // 摘要：
    //     脚手架类基础
    abstract class ScaffoldBase
    {
        // 进度条事件
        public event EventHandler<ProgressStartEventArgs> ProgressStart;
        public event EventHandler<ProgressReportEventArgs> ProgressReport;

        /// <summary>
        /// 进度条开始通知
        /// </summary>
        protected void OnProgressStart(ProgressStartEventArgs args)
        {
            if (ProgressStart != null)
            {
                ProgressStart(this, args);
            }
        }

        /// <summary>
        /// 进度条更新通知
        /// </summary>
        protected void OnProgressReport(ProgressReportEventArgs args)
        {
            if (ProgressReport != null)
            {
                ProgressReport(this, args);
            }
        }
    }

    public class ProgressReportEventArgs : EventArgs
    {
    }

    public class ProgressStartEventArgs : EventArgs
    {
        public string CurrentProgress { get; set; }
        public int ProgressMaximum { get; set; }
    }
}
