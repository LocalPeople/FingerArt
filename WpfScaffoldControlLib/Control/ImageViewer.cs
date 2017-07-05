using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace XcWpfControlLib.Control
{
    public class ImageViewer : System.Windows.Controls.Control
    {
        ScaleTransform scale;
        TranslateTransform translate;
        Grid grid;
        Image image;
        bool isMouseDown = false;
        Point lastPosition = new Point();

        static ImageViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(typeof(ImageViewer)));
        }

        public double CenterToUp
        {
            get { return grid.ActualHeight / 2 + translate.Y; }
        }

        public double CenterToDown
        {
            get { return grid.ActualHeight / 2 - translate.Y; }
        }

        public double CenterToLeft
        {
            get { return grid.ActualWidth / 2 + translate.X; }
        }

        public double CenterToRight
        {
            get { return grid.ActualWidth / 2 - translate.X; }
        }

        public double ImageHeight
        {
            get { return Math.Max(image.ActualHeight * scale.ScaleY, grid.ActualHeight); }
        }

        public double ImageWidth
        {
            get { return Math.Max(image.ActualWidth * scale.ScaleX, grid.ActualWidth); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            image = Template.FindName("PART_Image", this) as Image;
            if (image != null)
            {
                TransformGroup group = new TransformGroup();
                scale = new ScaleTransform(1.0, 1.0);
                translate = new TranslateTransform(0.0, 0.0);
                group.Children.Add(scale);
                group.Children.Add(translate);
                image.RenderTransform = group;
                image.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            grid = Template.FindName("PART_Grid", this) as Grid;
            if (grid != null)
            {
                grid.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                grid.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                grid.MouseMove += Image_MouseMove;
                grid.MouseWheel += Image_MouseWheel;
            }
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isMouseDown)
            {
                Point position = e.GetPosition(this);
                if ((position.X - lastPosition.X > 0 && CenterToLeft < ImageWidth / 2)
                    || (position.X - lastPosition.X < 0 && CenterToRight < ImageWidth / 2))
                {
                    translate.X += position.X - lastPosition.X;
                }
                if ((position.Y - lastPosition.Y > 0 && CenterToUp < ImageHeight / 2)
                    || (position.Y - lastPosition.Y < 0 && CenterToDown < ImageHeight / 2))
                {
                    translate.Y += position.Y - lastPosition.Y;
                }
                lastPosition = position;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMouseDown = false;
            lastPosition = new Point();
            grid.ReleaseMouseCapture();
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMouseDown = true;
            lastPosition = e.GetPosition(this);
            grid.CaptureMouse();
        }

        private void Image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double zoom = 0.0;
            if (e.Delta > 0 && scale.ScaleX < 4)
            {
                zoom = 0.1;
            }
            else if (e.Delta < 0 && scale.ScaleX > 1)
            {
                zoom = -0.1;
            }
            scale.ScaleX += zoom;
            scale.ScaleY += zoom;
        }

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set
            {
                if (scale != null && translate != null)
                {
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 1.0;
                    translate.X = 0.0;
                    translate.Y = 0.0;
                }
                SetValue(SourceProperty, value);
            }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(ImageViewer));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(ImageViewer), new PropertyMetadata((Stretch)1));
    }
}
