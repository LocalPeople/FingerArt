using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ScaffoldTool.StoryBoard
{
    public class StoryBoard
    {
        private Control _control;
        private List<Animation> _list;
        private Action _storyBoardEndAction;

        public StoryBoard(Control control)
        {
            _control = control;
            _list = new List<Animation>();
        }

        public StoryBoard(Control control, Action endAction) : this(control)
        {
            _storyBoardEndAction = endAction;
        }
        /// <summary>
        /// 开始故事板动画
        /// </summary>
        /// <param name="dueTime">动画延时启动毫秒值</param>
        public void Start(int dueTime = 0)
        {
            _dueTime = dueTime;
            Thread thread = new Thread(new ThreadStart(StopWatchAction));
            if (thread != null)
                thread.Start();
        }
        /// <summary>
        /// 动画延时启动毫秒值
        /// </summary>
        private int _dueTime;

        private void StopWatchAction()
        {
            System.Threading.Timer timer = new System.Threading.Timer(new TimerCallback(TimerCallbackProcessor));// 初始化计时器函数回调
            timer.Change(_dueTime, 8);// 8毫秒触发一次计时器函数回调
        }

        private int timerInt = 0;// 计数器
        private int maxTimerInt = 0;// 故事板结束次数

        private void TimerCallbackProcessor(object state)
        {
            if (_control == null || _control.IsDisposed || timerInt > maxTimerInt)
            {
                System.Threading.Timer timer = (System.Threading.Timer)state;
                timer.Dispose();
                if (_storyBoardEndAction != null)
                    _control.BeginInvoke(_storyBoardEndAction);// 触发故事板结束事件
                return;
            }
            _control.BeginInvoke((Action)AnimationHandler);
        }

        private void AnimationHandler()
        {
            foreach (var animation in _list)
            {
                Type t = animation.Control.GetType();
                PropertyInfo property = t.GetProperty(animation.Property);
                if (property != null)
                    property.SetValue(animation.Control, animation.Change());
            }
            timerInt++;
        }

        #region List<Animation> 增删函数
        public void Add(Animation item)
        {
            _list.Add(item);
            if (item.Number > maxTimerInt)
                maxTimerInt = item.Number;
        }

        public void Clear()
        {
            _list.Clear();
            timerInt = 0;
            maxTimerInt = 0;
        }
        #endregion
    }
}
