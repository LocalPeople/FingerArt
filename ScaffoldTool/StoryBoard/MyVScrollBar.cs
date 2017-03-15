using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScaffoldTool.StoryBoard
{
    public class MyVScrollBar : VScrollBar
    {
        public MyVScrollBar()
        {
            this.ParentChanged += MyVScrollBar_ParentChanged;
            this.Scroll += MyVScrollBar_Scroll;
        }

        private int scrollLocationOffsetY;// 滑动条一次平均滑动距离
        private int scrollBottomRemainder;// 滑动条最后一次滑动补余距离

        private void MyVScrollBar_ParentChanged(object sender, EventArgs e)
        {
            int maxScrollCount = this.Maximum - 9;
            Control topCtrl = this.Parent.Controls[0];
            Control bottomCtrl = this.Parent.Controls[0];
            for (int i = 1; i < this.Parent.Controls.Count; i++)
            {
                if (this.Parent.Controls[i].Location.Y + this.Parent.Controls[i].Size.Height
                    < topCtrl.Location.Y + topCtrl.Size.Height)
                    topCtrl = this.Parent.Controls[i];
                else if (this.Parent.Controls[i].Location.Y + this.Parent.Controls[i].Size.Height
                    > bottomCtrl.Location.Y + bottomCtrl.Size.Height)
                    bottomCtrl = this.Parent.Controls[i];
            }
            int groupBoxBottomY = this.Parent.Size.Height;// GroupBox底边Location的Y分量
            int bottomCtrlBottomY = bottomCtrl.Location.Y + bottomCtrl.Size.Height;// GroupBox最底子控件的底面Location的Y分量
            scrollLocationOffsetY = (bottomCtrlBottomY - groupBoxBottomY) / maxScrollCount;
            scrollBottomRemainder = (bottomCtrlBottomY - groupBoxBottomY) - (maxScrollCount * scrollLocationOffsetY);
        }

        private bool needAddRemainder = false;// 标记滑动条是否需要末端距离补余
        private bool needRemoveRemainder = false;// 标记滑动条是否需要还原末端距离补余

        private void MyVScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            this.Parent.SuspendLayout();
            if (e.NewValue == this.Maximum - 9)
                needAddRemainder = true;
            if (e.OldValue == this.Maximum - 9)
                needRemoveRemainder = true;
            foreach (Control ctrl in this.Parent.Controls)
            {
                if (ctrl is VScrollBar) continue;
                Point location = ctrl.Location;
                /* 滑动条从末端向上滑时还原末端距离补余 */
                if (needAddRemainder)
                    location.Y -= scrollBottomRemainder;
                location.Y -= scrollLocationOffsetY * (e.NewValue - e.OldValue);
                /* 滑动条从上滑到末端时末端距离补余 */
                if (needRemoveRemainder)
                    location.Y += scrollBottomRemainder;
                ctrl.Location = location;
            }
            needAddRemainder = false;
            needRemoveRemainder = false;
            this.Parent.ResumeLayout();
        }

    }
}
