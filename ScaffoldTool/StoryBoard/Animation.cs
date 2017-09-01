using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScaffoldTool.StoryBoard
{
    public abstract class Animation
    {
        protected int millisecondOne = 8;// 循环一次的毫秒数

        /// <summary>
        /// 所需时间
        /// </summary>
        private int Time { get; set; }

        /// <summary>
        /// 需要变化的次数
        /// </summary>
        public int Number { get; protected set; }

        public Control Control { get; protected set; }

        public string Property { get; protected set; }

        public abstract object Change();

        public Animation(Control control, string propertyName, int spanTime)
        {
            Control = control;
            Property = propertyName;
            Time = spanTime;
            int num = spanTime / millisecondOne;
            if (spanTime % millisecondOne != 0)
                num++;
            Number = num;
        }

        public Animation(int spanTime)
        {
            Control = null;
            Property = string.Empty;
            Time = spanTime;
            int num = spanTime / millisecondOne;
            if (spanTime % millisecondOne != 0)
                num++;
            Number = num;
        }
    }

    public class IntAnimation : Animation
    {
        private double value;
        private double changeValue;

        public IntAnimation(Control control, string propertyName, int spanTime, int from, int to)
            : base(control, propertyName, spanTime)
        {
            value = from;
            changeValue = (double)(to - from) / Number;
        }

        public IntAnimation(int spanTime, int from, int to)
            : base(spanTime)
        {
            value = from;
            changeValue = (double)(to - from) / Number;
        }

        public override object Change()
        {
            value += changeValue;
            return (int)value;
        }
    }

    public class SizeAnimation : Animation
    {
        private IntAnimation widthAnimation, heightAnimation;

        public SizeAnimation(Control control, string propertyName, int spanTime, Size from, Size to)
            : base(control, propertyName, spanTime)
        {
            widthAnimation = new IntAnimation(spanTime, from.Width, to.Width);
            heightAnimation = new IntAnimation(spanTime, from.Height, to.Height);
        }

        public override object Change()
        {
            return new Size((int)widthAnimation.Change(), (int)heightAnimation.Change());
        }
    }
}