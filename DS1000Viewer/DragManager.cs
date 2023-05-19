using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace DS1000Viewer
{
    class DragManager
    {
        UIElement canvas;
        public PointerPoint StartPoint { get; set; }
        public PointerPoint EndPoint { get; set; }
        public bool IsDragging { get; set; }
        public int Channel { get; set; }

        public double Dx { get { return EndPoint.Position.X - StartPoint.Position.X; } }
        public double Dy { get { return EndPoint.Position.Y - StartPoint.Position.Y; } }

        public DragManager(UIElement canvas)
        {
            this.canvas = canvas;
            this.canvas = null;
        }

        public void Start(PointerRoutedEventArgs e)
        {
            (e.OriginalSource as UIElement).CapturePointer(e.Pointer);

            StartPoint = e.GetCurrentPoint(canvas);
            EndPoint = StartPoint;
            IsDragging = true;
        }

        public void Update(PointerRoutedEventArgs e, Action action)
        {
            if (!IsDragging)
                return;
            EndPoint = e.GetCurrentPoint(canvas);
            IsDragging = true;

            action();
        }

        public void Stop(Windows.UI.Xaml.Input.PointerRoutedEventArgs e, Action action)
        {
            if (!IsDragging)
                return;
            (e.OriginalSource as UIElement).ReleasePointerCapture(e.Pointer);

            EndPoint = e.GetCurrentPoint(canvas);
            IsDragging = false;

            action();
        }
    }
}
