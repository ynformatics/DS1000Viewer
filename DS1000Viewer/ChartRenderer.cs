using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.StartScreen;

namespace DS1000Viewer
{
    class ChartRenderer
    {
        public void RenderGrid(CanvasControl canvas, CanvasDrawEventArgs args)
        {
            var width = (float)canvas.ActualWidth;
            var height = (float)canvas.ActualHeight;
           
            using (var cpb = new CanvasPathBuilder(args.DrawingSession))
            {
                float vgrid = height / 8;
               
                for (int i = 0; i < 9; i++)
                {
                    // Horizontal line
                    cpb.BeginFigure(new Vector2(0, i * vgrid));
                    cpb.AddLine(new Vector2(width, i * vgrid));
                    cpb.EndFigure(CanvasFigureLoop.Open);
                }

                float tickx = width / 60;
                for (int i = 0; i < 60; i++)
                {
                    // tick
                    cpb.BeginFigure(new Vector2(i * tickx, 4.1f * vgrid));
                    cpb.AddLine(new Vector2(i * tickx, 3.9f * vgrid));
                    cpb.EndFigure(CanvasFigureLoop.Open);
                }
           
                float hgrid = width / 12;
                for (int i = 0; i < 13; i++)
                {
                    // Vertical line
                    cpb.BeginFigure(new Vector2(i *hgrid, 0));
                    cpb.AddLine(new Vector2(i * hgrid, height));
                    cpb.EndFigure(CanvasFigureLoop.Open);
                }
                float ticky = height / 40;
                for (int i = 0; i < 40; i++)
                {
                    // tick
                    cpb.BeginFigure(new Vector2(6.1f * hgrid, i * ticky));
                    cpb.AddLine(new Vector2(5.9f * hgrid, i * ticky));
                    cpb.EndFigure(CanvasFigureLoop.Open);
                }
                args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Gray, 1);
            }
        }

       
        public void RenderData(CanvasControl canvas, CanvasDrawEventArgs args, List<double>[] traces, double[] offsets, double offsetX)
        {
            for (int chan = 0; chan < 4; chan++)
            {
                using (var cpb = new CanvasPathBuilder(args.DrawingSession))
                {
                    if (traces[chan] == null || traces[chan].Count == 0)
                    {
                        continue;
                    }

                    int start = traces[chan].Count == 1200 ? 0 : 1200 - traces[chan].Count;

                    cpb.BeginFigure(new Vector2(start + (float)offsetX,
                        (float)(canvas.ActualHeight * (1 - traces[chan][0] + offsets[chan]))));

                    float xscale = (float)canvas.ActualWidth / 1200;
                    for (int i = 1; i < traces[chan].Count; i++)
                    {
                        cpb.AddLine(new Vector2((float)((i + start) * xscale + offsetX),
                            (float)(canvas.ActualHeight * (1 - traces[chan][i] + offsets[chan]))));
                    }

                    cpb.EndFigure(CanvasFigureLoop.Open);
                    args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb),
                        chan == 0 ? Colors.Yellow : chan == 1 ? Colors.Cyan : chan == 2 ? Colors.Magenta : Color.FromArgb(0xff,0x33,0x66,0x99));
                }
            }
        }
    }
}
