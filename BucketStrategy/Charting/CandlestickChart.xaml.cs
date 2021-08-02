using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Text;

using BucketStrategy.DataTypes;
using System.Numerics;

namespace BucketStrategy.Charting
{
    public sealed partial class CandlestickChart : UserControl
    {
        private List<DataPoint> m_data;

        private float m_marginLeft;
        private float m_marginRight;
        private float m_marginTop;
        private float m_marginBottom;

        private Color m_majorAxisColor;
        private Color m_minorAxisColor;
        private Color m_backgroundColor;

        private float m_majorAxisWidth;
        private float m_minorAxisWidth;

        private float m_xAxisTickLength;
        private float m_yAxisTickLength;

        private float m_chartMaxY;
        private float m_chartMinY;

        private float m_unitWidth;

        CanvasTextFormat m_xLabelFormat;
        CanvasTextFormat m_yLabelFormat;

        public CandlestickChart()
        {
            this.InitializeComponent();

            m_marginLeft = 60.0f;
            m_marginRight = 25.0f;
            m_marginTop = 25.0f;
            m_marginBottom = 60.0f;

            m_majorAxisColor = Colors.Black;
            m_minorAxisColor = Colors.Gray;
            m_backgroundColor = Colors.Azure;

            m_majorAxisWidth = 3.0f;
            m_minorAxisWidth = 2.0f;

            m_xAxisTickLength = 5.0f;
            m_yAxisTickLength = 7.5f;

            m_unitWidth = 0.0f;

            m_data = new List<DataPoint>();
            AddData();

            m_xLabelFormat = new CanvasTextFormat();
            m_xLabelFormat.FontFamily = "Segoe UI";
            m_xLabelFormat.FontSize = 12.0f;
            m_xLabelFormat.FontStretch = Windows.UI.Text.FontStretch.Normal;
            m_xLabelFormat.FontStyle = Windows.UI.Text.FontStyle.Normal;
            m_xLabelFormat.HorizontalAlignment = CanvasHorizontalAlignment.Left;
            m_xLabelFormat.VerticalAlignment = CanvasVerticalAlignment.Center;

            m_yLabelFormat = new CanvasTextFormat();
            m_yLabelFormat.FontFamily = "Segoe UI";
            m_yLabelFormat.FontSize = 12.0f;
            m_yLabelFormat.FontStretch = Windows.UI.Text.FontStretch.Normal;
            m_yLabelFormat.FontStyle = Windows.UI.Text.FontStyle.Normal;
            m_yLabelFormat.HorizontalAlignment = CanvasHorizontalAlignment.Right;
            m_yLabelFormat.VerticalAlignment = CanvasVerticalAlignment.Center;
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            DrawBackground(sender, args);
            DrawAxes(sender, args);
            DrawXTicksAndLabels(sender, args);
            DrawYTicksLabelsAndData(sender, args);
            DrawCandleSticks(sender, args);
        }

        private void DrawBackground(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(
                m_marginLeft,
                m_marginTop,
                (float)this.ActualWidth - (m_marginRight + m_marginLeft),
                (float)this.ActualHeight - (m_marginBottom + m_marginTop),
                m_backgroundColor
            );
        }

        private void DrawAxes(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // y-axis
            args.DrawingSession.DrawLine(
                m_marginLeft,
                m_marginTop,
                m_marginLeft,
                (float)this.ActualHeight - m_marginBottom,
                m_majorAxisColor,
                m_majorAxisWidth
            );

            // x-axis
            args.DrawingSession.DrawLine(
                m_marginLeft,
                (float)this.ActualHeight - m_marginBottom,
                (float)this.ActualWidth - m_marginRight,
                (float)this.ActualHeight - m_marginBottom,
                m_majorAxisColor,
                m_majorAxisWidth
            );
        }

        private void DrawXTicksAndLabels(CanvasControl sender, CanvasDrawEventArgs args)
        {
            float chartWidth = (float)this.ActualWidth - (m_marginRight + m_marginLeft);

            // Define the spacing between candlesticks to be 1 unit
            // Define the width of each candlestick to be 3 units
            // Total units is (4 * # of candlesticks) + 1 to account for extra spacing at the end
            m_unitWidth = chartWidth / ((4 * m_data.Count) + 1);
            float tickSpacing = 4 * m_unitWidth;

            // Draw each tick
            float x = m_marginLeft + m_unitWidth + (1.5f * m_unitWidth);
            float y1 = (float)this.ActualHeight - m_marginBottom;
            float y2 = y1 + m_xAxisTickLength;
            Matrix3x2 initTransform = args.DrawingSession.Transform;

            foreach (DataPoint dp in m_data)
            {
                args.DrawingSession.DrawLine(x, y1, x, y2, m_majorAxisColor);

                args.DrawingSession.Transform = initTransform * Matrix3x2.CreateRotation((float)Math.PI / 3, new Vector2(x, y2 + 3.0f));
                args.DrawingSession.DrawText(dp.DateTime.ToString("h:mm tt"), x, y2 + 3.0f, m_majorAxisColor, m_xLabelFormat);


                args.DrawingSession.Transform = initTransform;
                x += tickSpacing;
            }
        }

        private void DrawYTicksLabelsAndData(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Compute min/max
            m_chartMaxY = m_data.Select(s => s.High).Max();
            m_chartMinY = m_data.Select(s => s.Low).Min();

            // Adjust up/down by 5% of the difference between min/max
            float chartYDiff = m_chartMaxY - m_chartMinY;
            m_chartMaxY += (0.05f * chartYDiff);
            m_chartMinY -= (0.05f * chartYDiff);

            // Find the order of magnitude of the difference between min/max and round down to 1...
            //      ex. 0.01, 0.1, 1, 10, 100, etc
            int power = (int)Math.Floor(Math.Log10(chartYDiff));
            double order = Math.Pow(10.0f, power) / 10; // Drop the order of magnitude by 1 (Divide by 10)

            // Determine appropriate tick difference - start with 2 * order
            List<float> diffsToTry = new List<float>() { 2.0f, 3.0f, 4.0f, 5.0f, 10.0f, 15.0f, 20.0f };
            float tickDiff = 0.0f;
            float tickCount = 10.0f; // At most 10 ticks

            foreach (float diff in diffsToTry)
            {
                tickDiff = (float)order * diff;
                if (chartYDiff / tickDiff < tickCount)
                    break;
            }


            int factor = 1;
            while (tickDiff * factor < 1.0f)
                factor *= 10;

            // range 2.35 - 3.35
            // tickDiff 0.2
            // factor 10
            // range -> 23.5 - 33.5
            // tickDiff -> 2
            int startValue = (int)(m_chartMinY * factor) + 1;
            int tickDiffInt = (int)(tickDiff * factor);

            if (startValue % tickDiffInt != 0)
                startValue += (tickDiffInt - (startValue % tickDiffInt));

            // Draw ticks starting at the bottom
            float x0 = m_marginLeft;
            float x1 = x0 - m_yAxisTickLength;

            float yVal = (float)startValue / factor;

            float xRight = (float)this.ActualWidth - m_marginRight;

            for (float y = 0; yVal < m_chartMaxY; yVal += ((float)tickDiffInt / factor))
            {
                y = ChartYValueToPixels(yVal);

                args.DrawingSession.DrawLine(x0, y, x1, y, m_majorAxisColor);

                args.DrawingSession.DrawText(string.Format("{0:f1}", yVal), x1 - 3.0f, y, m_majorAxisColor, m_yLabelFormat);

                // Draw minor lines
                args.DrawingSession.DrawLine(x0, y, xRight, y, m_minorAxisColor);
            }
        }

        private void DrawCandleSticks(CanvasControl sender, CanvasDrawEventArgs args)
        {
            float x = m_marginLeft + m_unitWidth;
            float candlestickWidth = 3 * m_unitWidth;
            float u4 = 4 * m_unitWidth;
            float u1half = 1.5f * m_unitWidth;
            float y1, y2;
            Color color;

            foreach (DataPoint dp in m_data)
            {
                // Draw a single line from high to low
                x += u1half;

                y1 = ChartYValueToPixels(dp.High);
                y2 = ChartYValueToPixels(dp.Low);

                args.DrawingSession.DrawLine(x, y1, x, y2, Colors.Black, 1.0f);

                x -= u1half;

                // Draw rectangle
                y1 = ChartYValueToPixels(dp.Open);
                y2 = ChartYValueToPixels(dp.Close);
                color = (dp.Open < dp.Close) ? Colors.Green : Colors.Red;

                Rect rect = new Rect(
                    x,
                    Math.Min(y1, y2),
                    candlestickWidth,
                    Math.Abs(y2 - y1)
                );

                args.DrawingSession.FillRectangle(rect, color);
                args.DrawingSession.DrawRectangle(rect, Colors.Black, 1.0f);

                x += u4;
            }
        }

        private float ChartYValueToPixels(float y)
        {
            // margin top + chart height - percent up the y-axis
            float chartHeight = (float)this.ActualHeight - (m_marginTop + m_marginBottom);
            return m_marginTop + chartHeight - (((y - m_chartMinY) / (m_chartMaxY - m_chartMinY)) * chartHeight);
        }

        private void CanvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void CanvasControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

        private void CanvasControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }



        private void AddData()
        {
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 30, 00), 289f, 289.065f, 287.61f, 287.665f, 5420));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 31, 00), 287.63f, 287.63f, 287.045f, 287.35f, 1748));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 32, 00), 287.255f, 287.255f, 286.93f, 287.15f, 4727));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 33, 00), 287.11f, 287.26f, 286.93f, 287.06f, 1252));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 34, 00), 287.055f, 287.055f, 286.75f, 286.85f, 1827));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 35, 00), 286.915f, 287.23f, 286.7f, 287.23f, 16437));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 36, 00), 287.22f, 288.17f, 287.03f, 288.09f, 2918));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 37, 00), 288.21f, 288.4f, 287.87f, 288.26f, 3050));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 38, 00), 288.3f, 288.36f, 287.87f, 287.9f, 2186));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 39, 00), 287.92f, 287.92f, 287.555f, 287.555f, 1328));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 40, 00), 287.57f, 287.87f, 287.525f, 287.835f, 1392));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 41, 00), 287.93f, 287.95f, 287.695f, 287.77f, 509));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 42, 00), 287.81f, 288.03f, 287.77f, 287.79f, 1759));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 43, 00), 287.835f, 288.05f, 287.835f, 287.85f, 1173));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 44, 00), 287.985f, 288.15f, 287.92f, 288.145f, 1205));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 45, 00), 288.12f, 288.15f, 288.03f, 288.15f, 1153));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 46, 00), 287.995f, 288.21f, 287.915f, 288.21f, 1664));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 47, 00), 288.215f, 288.215f, 287.975f, 287.975f, 835));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 48, 00), 287.965f, 288.065f, 287.965f, 288.035f, 524));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 49, 00), 287.845f, 287.99f, 287.84f, 287.99f, 1111));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 50, 00), 288.005f, 288.005f, 287.79f, 287.89f, 1521));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 51, 00), 287.825f, 288f, 287.74f, 287.74f, 623));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 52, 00), 287.705f, 287.84f, 287.615f, 287.65f, 2166));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 53, 00), 287.69f, 287.84f, 287.69f, 287.81f, 838));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 54, 00), 287.785f, 287.81f, 287.72f, 287.76f, 653));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 55, 00), 287.69f, 287.79f, 287.66f, 287.79f, 656));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 56, 00), 287.685f, 287.76f, 287.685f, 287.76f, 315));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 57, 00), 287.77f, 288.02f, 287.77f, 287.97f, 359));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 58, 00), 287.96f, 288.03f, 287.955f, 288.015f, 939));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 09, 59, 00), 287.99f, 288.03f, 287.91f, 288.03f, 1891));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 00, 00), 288.07f, 288.15f, 287.97f, 288f, 688));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 01, 00), 287.95f, 288.05f, 287.86f, 287.88f, 967));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 02, 00), 287.85f, 288.06f, 287.81f, 287.92f, 1139));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 03, 00), 287.97f, 288.06f, 287.9f, 288f, 2444));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 04, 00), 287.98f, 288.08f, 287.98f, 288.08f, 915));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 05, 00), 288f, 288.04f, 287.845f, 287.845f, 1047));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 06, 00), 287.835f, 287.95f, 287.745f, 287.95f, 1008));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 07, 00), 287.95f, 288.02f, 287.95f, 287.965f, 1390));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 08, 00), 288f, 288.01f, 287.89f, 287.98f, 1754));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 09, 00), 287.915f, 287.96f, 287.855f, 287.96f, 1517));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 10, 00), 287.965f, 287.965f, 287.79f, 287.82f, 1388));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 11, 00), 287.745f, 287.745f, 287.55f, 287.555f, 2500));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 12, 00), 287.58f, 287.59f, 287.32f, 287.37f, 2610));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 13, 00), 287.31f, 287.54f, 287.31f, 287.51f, 2346));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 14, 00), 287.52f, 287.525f, 287.32f, 287.325f, 4308));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 15, 00), 287.27f, 287.365f, 287.27f, 287.36f, 1562));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 16, 00), 287.415f, 287.43f, 287.28f, 287.37f, 1182));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 17, 00), 287.38f, 287.505f, 287.38f, 287.43f, 836));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 18, 00), 287.45f, 287.45f, 287.21f, 287.225f, 3176));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 19, 00), 287.25f, 287.32f, 287.23f, 287.295f, 1462));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 20, 00), 287.16f, 287.16f, 286.98f, 287f, 3225));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 21, 00), 287f, 287.02f, 286.94f, 287f, 2226));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 22, 00), 286.96f, 287.105f, 286.93f, 287.06f, 1644));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 23, 00), 287.12f, 287.16f, 287.09f, 287.09f, 3135));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 24, 00), 287.09f, 287.135f, 286.99f, 287.035f, 3156));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 25, 00), 287.04f, 287.255f, 287.04f, 287.21f, 2068));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 26, 00), 287.14f, 287.15f, 286.955f, 287f, 682));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 27, 00), 287f, 287.02f, 286.96f, 286.96f, 1018));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 28, 00), 287f, 287.075f, 286.96f, 287f, 616));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 29, 00), 286.99f, 287.005f, 286.695f, 286.77f, 3190));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 30, 00), 286.74f, 286.93f, 286.69f, 286.93f, 2542));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 31, 00), 286.95f, 287.05f, 286.93f, 287.05f, 2255));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 32, 00), 287.18f, 287.245f, 287.16f, 287.245f, 2109));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 33, 00), 287.31f, 287.55f, 287.31f, 287.32f, 3819));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 34, 00), 287.47f, 287.52f, 287.39f, 287.39f, 1453));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 35, 00), 287.38f, 287.47f, 287.38f, 287.47f, 2427));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 36, 00), 287.5f, 287.53f, 287.455f, 287.505f, 2624));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 37, 00), 287.63f, 287.7f, 287.63f, 287.66f, 2462));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 38, 00), 287.7f, 287.81f, 287.63f, 287.81f, 5530));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 39, 00), 287.8f, 287.81f, 287.745f, 287.8f, 1424));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 40, 00), 287.87f, 287.9f, 287.59f, 287.59f, 453));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 41, 00), 287.565f, 287.58f, 287.5f, 287.5f, 1045));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 42, 00), 287.5f, 287.52f, 287.475f, 287.495f, 707));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 43, 00), 287.43f, 287.45f, 287.27f, 287.29f, 1359));
    /*        m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 44, 00), 287.32f, 287.34f, 287.27f, 287.34f, 912));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 45, 00), 287.29f, 287.29f, 287.07f, 287.19f, 1056));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 46, 00), 287.19f, 287.19f, 287.015f, 287.12f, 1615));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 47, 00), 287.05f, 287.12f, 287f, 287.08f, 878));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 48, 00), 287.01f, 287.22f, 287.01f, 287.22f, 1342));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 49, 00), 287.19f, 287.19f, 286.96f, 286.96f, 711));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 50, 00), 286.99f, 287.205f, 286.96f, 287.19f, 1632));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 51, 00), 287.23f, 287.33f, 287.165f, 287.18f, 1697));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 52, 00), 287.155f, 287.22f, 287.115f, 287.22f, 783));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 53, 00), 287.24f, 287.35f, 287.22f, 287.35f, 618));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 54, 00), 287.36f, 287.36f, 287.16f, 287.16f, 1229));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 55, 00), 287.13f, 287.23f, 287.13f, 287.23f, 1403));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 56, 00), 287.26f, 287.27f, 287.13f, 287.16f, 1567));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 57, 00), 287.2f, 287.3f, 287.18f, 287.26f, 3648));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 58, 00), 287.235f, 287.235f, 287.09f, 287.1f, 744));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 10, 59, 00), 287.14f, 287.15f, 286.9f, 286.955f, 1698));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 00, 00), 286.94f, 287.06f, 286.9f, 286.905f, 1021));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 01, 00), 286.815f, 287.105f, 286.815f, 287.075f, 2724));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 02, 00), 287.07f, 287.07f, 286.87f, 286.995f, 2087));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 03, 00), 286.955f, 286.995f, 286.88f, 286.97f, 1003));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 04, 00), 286.98f, 287f, 286.98f, 287f, 755));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 05, 00), 286.99f, 286.995f, 286.91f, 286.975f, 1395));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 06, 00), 287.06f, 287.275f, 287.06f, 287.18f, 1334));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 07, 00), 287.2f, 287.23f, 286.945f, 286.99f, 1078));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 08, 00), 286.91f, 287f, 286.83f, 286.935f, 1984));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 09, 00), 286.94f, 287.1f, 286.94f, 287.1f, 1330));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 10, 00), 287.11f, 287.11f, 286.92f, 286.97f, 1927));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 11, 00), 287.03f, 287.05f, 286.99f, 286.99f, 1950));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 12, 00), 286.96f, 287.075f, 286.93f, 287.075f, 1523));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 13, 00), 287.105f, 287.19f, 287.04f, 287.18f, 1326));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 14, 00), 287.15f, 287.2f, 287.085f, 287.19f, 1112));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 15, 00), 287.22f, 287.27f, 287.17f, 287.22f, 1605));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 16, 00), 287.23f, 287.23f, 287.09f, 287.09f, 1219));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 17, 00), 287.13f, 287.17f, 287.105f, 287.125f, 686));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 18, 00), 287.14f, 287.22f, 287.09f, 287.215f, 934));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 19, 00), 287.19f, 287.19f, 287.095f, 287.11f, 1918));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 20, 00), 287.12f, 287.13f, 287.07f, 287.07f, 854));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 21, 00), 287.085f, 287.18f, 287.07f, 287.18f, 1120));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 22, 00), 287.15f, 287.325f, 287.15f, 287.325f, 1012));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 23, 00), 287.315f, 287.465f, 287.315f, 287.46f, 6512));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 24, 00), 287.45f, 287.54f, 287.45f, 287.51f, 832));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 25, 00), 287.44f, 287.64f, 287.42f, 287.62f, 1387));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 26, 00), 287.59f, 287.65f, 287.59f, 287.59f, 3148));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 27, 00), 287.49f, 287.69f, 287.47f, 287.66f, 1977));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 28, 00), 287.6f, 287.695f, 287.6f, 287.69f, 1554));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 29, 00), 287.65f, 287.735f, 287.6f, 287.71f, 1039));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 30, 00), 287.63f, 287.785f, 287.63f, 287.785f, 839));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 31, 00), 287.78f, 287.87f, 287.75f, 287.87f, 1875));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 32, 00), 287.87f, 287.95f, 287.84f, 287.93f, 1459));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 33, 00), 287.91f, 287.93f, 287.91f, 287.915f, 621));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 34, 00), 287.915f, 288.01f, 287.915f, 287.975f, 880));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 35, 00), 288.02f, 288.05f, 287.99f, 288.05f, 546));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 36, 00), 288.03f, 288.18f, 288.03f, 288.17f, 699));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 37, 00), 288.16f, 288.16f, 288.06f, 288.06f, 420));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 38, 00), 288.08f, 288.08f, 287.97f, 287.97f, 747));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 39, 00), 287.98f, 288.03f, 287.91f, 288.03f, 1546));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 40, 00), 288.065f, 288.075f, 288.01f, 288.01f, 3168));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 41, 00), 287.985f, 287.985f, 287.9f, 287.95f, 774));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 42, 00), 287.97f, 288.03f, 287.97f, 287.97f, 1097));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 43, 00), 288.01f, 288.01f, 287.99f, 287.99f, 130));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 44, 00), 288.06f, 288.06f, 287.97f, 287.97f, 207));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 45, 00), 288.025f, 288.025f, 287.94f, 287.94f, 427));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 46, 00), 287.92f, 287.92f, 287.86f, 287.92f, 1531));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 47, 00), 287.92f, 287.965f, 287.92f, 287.93f, 1120));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 48, 00), 287.94f, 287.94f, 287.855f, 287.875f, 303));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 49, 00), 287.91f, 287.93f, 287.85f, 287.93f, 397));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 50, 00), 287.89f, 287.965f, 287.885f, 287.93f, 903));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 51, 00), 287.96f, 287.96f, 287.93f, 287.93f, 215));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 52, 00), 288.02f, 288.09f, 288.015f, 288.07f, 2853));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 53, 00), 288.19f, 288.24f, 288.17f, 288.17f, 1812));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 54, 00), 288.145f, 288.175f, 288.11f, 288.145f, 1924));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 55, 00), 288.21f, 288.26f, 288.21f, 288.26f, 1617));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 56, 00), 288.31f, 288.41f, 288.31f, 288.37f, 2009));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 57, 00), 288.36f, 288.41f, 288.36f, 288.39f, 1634));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 58, 00), 288.4f, 288.46f, 288.37f, 288.44f, 2107));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 11, 59, 00), 288.45f, 288.53f, 288.45f, 288.53f, 2615));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 00, 00), 288.55f, 288.65f, 288.55f, 288.61f, 616));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 01, 00), 288.63f, 288.7f, 288.595f, 288.595f, 1002));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 02, 00), 288.66f, 288.71f, 288.64f, 288.67f, 1723));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 03, 00), 288.66f, 288.67f, 288.63f, 288.67f, 1955));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 04, 00), 288.69f, 288.725f, 288.685f, 288.72f, 1226));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 05, 00), 288.75f, 288.8f, 288.75f, 288.76f, 2210));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 06, 00), 288.84f, 288.91f, 288.83f, 288.91f, 569));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 07, 00), 288.935f, 289.15f, 288.935f, 289.15f, 1754));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 08, 00), 289.18f, 289.225f, 289.11f, 289.225f, 1743));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 09, 00), 289.21f, 289.23f, 289.16f, 289.195f, 15341));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 10, 00), 289.19f, 289.19f, 289.065f, 289.065f, 432));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 11, 00), 289.07f, 289.09f, 289.03f, 289.09f, 679));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 12, 00), 289.11f, 289.11f, 288.94f, 289.005f, 3208));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 13, 00), 288.95f, 288.96f, 288.92f, 288.96f, 1603));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 14, 00), 289.005f, 289.22f, 289.005f, 289.22f, 4148));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 15, 00), 289.235f, 289.31f, 289.235f, 289.26f, 2567));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 16, 00), 289.29f, 289.29f, 289.12f, 289.12f, 1232));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 17, 00), 289.145f, 289.155f, 289.11f, 289.11f, 1334));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 18, 00), 289.13f, 289.235f, 289.11f, 289.235f, 709));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 19, 00), 289.245f, 289.32f, 289.22f, 289.22f, 813));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 20, 00), 289.23f, 289.295f, 289.215f, 289.22f, 3006));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 21, 00), 289.205f, 289.225f, 289.15f, 289.205f, 2289));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 22, 00), 289.2f, 289.225f, 289.2f, 289.2f, 2102));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 23, 00), 289.135f, 289.14f, 289.12f, 289.14f, 736));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 24, 00), 289.14f, 289.14f, 288.86f, 288.88f, 655));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 25, 00), 288.92f, 288.95f, 288.895f, 288.9f, 487));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 26, 00), 288.83f, 288.875f, 288.83f, 288.865f, 1011));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 27, 00), 288.875f, 288.875f, 288.83f, 288.83f, 819));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 28, 00), 288.835f, 288.875f, 288.835f, 288.865f, 3000));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 29, 00), 288.835f, 288.85f, 288.79f, 288.79f, 524));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 30, 00), 288.8f, 288.8f, 288.685f, 288.685f, 414));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 31, 00), 288.71f, 288.71f, 288.595f, 288.61f, 1920));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 32, 00), 288.63f, 288.63f, 288.535f, 288.535f, 728));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 33, 00), 288.52f, 288.575f, 288.52f, 288.575f, 302));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 34, 00), 288.58f, 288.645f, 288.55f, 288.55f, 798));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 35, 00), 288.545f, 288.6f, 288.545f, 288.57f, 701));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 36, 00), 288.5f, 288.53f, 288.43f, 288.43f, 1494));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 37, 00), 288.43f, 288.43f, 288.335f, 288.34f, 1285));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 38, 00), 288.35f, 288.35f, 288.25f, 288.25f, 729));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 39, 00), 288.25f, 288.28f, 288.2f, 288.235f, 1052));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 40, 00), 288.285f, 288.3f, 288.235f, 288.235f, 109));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 41, 00), 288.19f, 288.215f, 288.15f, 288.15f, 524));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 42, 00), 288.15f, 288.25f, 288.15f, 288.25f, 441));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 43, 00), 288.185f, 288.205f, 288.13f, 288.205f, 702));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 44, 00), 288.2f, 288.24f, 288.13f, 288.165f, 340));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 45, 00), 288.175f, 288.36f, 288.175f, 288.36f, 620));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 46, 00), 288.42f, 288.43f, 288.245f, 288.32f, 616));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 47, 00), 288.27f, 288.27f, 288.19f, 288.195f, 505));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 48, 00), 288.21f, 288.22f, 288.16f, 288.18f, 908));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 49, 00), 288.21f, 288.275f, 288.19f, 288.275f, 1017));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 50, 00), 288.33f, 288.37f, 288.285f, 288.335f, 1031));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 51, 00), 288.32f, 288.33f, 288.265f, 288.275f, 806));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 52, 00), 288.27f, 288.41f, 288.27f, 288.36f, 702));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 53, 00), 288.34f, 288.39f, 288.34f, 288.39f, 1718));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 54, 00), 288.415f, 288.48f, 288.405f, 288.48f, 862));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 55, 00), 288.475f, 288.475f, 288.41f, 288.41f, 502));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 56, 00), 288.34f, 288.38f, 288.325f, 288.325f, 502));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 57, 00), 288.37f, 288.43f, 288.36f, 288.425f, 978));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 58, 00), 288.38f, 288.435f, 288.38f, 288.435f, 636));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 12, 59, 00), 288.425f, 288.43f, 288.41f, 288.43f, 303));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 00, 00), 288.415f, 288.47f, 288.415f, 288.43f, 888));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 01, 00), 288.475f, 288.495f, 288.465f, 288.465f, 302));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 02, 00), 288.485f, 288.5f, 288.47f, 288.5f, 420));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 03, 00), 288.495f, 288.495f, 288.445f, 288.445f, 703));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 04, 00), 288.45f, 288.48f, 288.45f, 288.47f, 214));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 05, 00), 288.53f, 288.57f, 288.53f, 288.53f, 448));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 06, 00), 288.545f, 288.545f, 288.545f, 288.545f, 102));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 07, 00), 288.525f, 288.545f, 288.52f, 288.52f, 502));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 08, 00), 288.54f, 288.61f, 288.54f, 288.61f, 302));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 09, 00), 288.64f, 288.65f, 288.64f, 288.645f, 402));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 10, 00), 288.685f, 288.685f, 288.64f, 288.67f, 103));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 11, 00), 288.64f, 288.65f, 288.635f, 288.635f, 500));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 12, 00), 288.675f, 288.75f, 288.675f, 288.75f, 5));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 13, 00), 288.73f, 288.745f, 288.72f, 288.745f, 501));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 14, 00), 288.745f, 288.87f, 288.745f, 288.855f, 460));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 15, 00), 288.78f, 288.78f, 288.75f, 288.75f, 200));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 16, 00), 288.82f, 288.825f, 288.81f, 288.81f, 216));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 17, 00), 288.82f, 288.87f, 288.82f, 288.83f, 883));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 18, 00), 288.86f, 288.91f, 288.84f, 288.905f, 2345));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 19, 00), 288.9f, 288.97f, 288.9f, 288.955f, 2827));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 20, 00), 288.905f, 288.99f, 288.905f, 288.99f, 1303));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 21, 00), 288.985f, 289.025f, 288.98f, 289.01f, 1848));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 22, 00), 288.99f, 289.06f, 288.99f, 289.03f, 604));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 23, 00), 289.035f, 289.035f, 288.94f, 288.94f, 580));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 24, 00), 288.85f, 288.87f, 288.805f, 288.815f, 3750));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 25, 00), 288.82f, 288.965f, 288.805f, 288.965f, 7955));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 26, 00), 288.945f, 288.945f, 288.86f, 288.885f, 2951));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 27, 00), 288.885f, 288.89f, 288.84f, 288.84f, 1754));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 28, 00), 288.86f, 288.91f, 288.86f, 288.9f, 1516));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 29, 00), 288.88f, 288.9f, 288.805f, 288.81f, 1994));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 30, 00), 288.845f, 288.845f, 288.815f, 288.84f, 245));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 31, 00), 288.79f, 288.8f, 288.765f, 288.765f, 301));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 32, 00), 288.73f, 288.73f, 288.675f, 288.685f, 1511));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 33, 00), 288.7f, 288.73f, 288.66f, 288.73f, 1337));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 34, 00), 288.73f, 288.74f, 288.71f, 288.72f, 902));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 35, 00), 288.675f, 288.68f, 288.65f, 288.67f, 902));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 36, 00), 288.715f, 288.745f, 288.67f, 288.67f, 2588));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 37, 00), 288.69f, 288.71f, 288.68f, 288.68f, 301));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 38, 00), 288.67f, 288.68f, 288.63f, 288.63f, 560));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 39, 00), 288.65f, 288.71f, 288.635f, 288.71f, 681));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 40, 00), 288.69f, 288.74f, 288.69f, 288.71f, 1700));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 41, 00), 288.67f, 288.67f, 288.665f, 288.67f, 414));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 42, 00), 288.67f, 288.705f, 288.67f, 288.705f, 503));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 43, 00), 288.71f, 288.78f, 288.71f, 288.775f, 6837));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 44, 00), 288.78f, 288.78f, 288.68f, 288.77f, 9705));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 45, 00), 288.76f, 288.81f, 288.75f, 288.76f, 1542));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 46, 00), 288.765f, 288.77f, 288.68f, 288.74f, 999));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 47, 00), 288.75f, 288.76f, 288.67f, 288.715f, 742));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 48, 00), 288.69f, 288.69f, 288.62f, 288.65f, 1142));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 49, 00), 288.685f, 288.73f, 288.685f, 288.72f, 735));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 50, 00), 288.72f, 288.72f, 288.685f, 288.72f, 552));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 51, 00), 288.72f, 288.72f, 288.69f, 288.72f, 650));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 52, 00), 288.705f, 288.705f, 288.64f, 288.64f, 1434));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 53, 00), 288.63f, 288.66f, 288.615f, 288.63f, 1183));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 54, 00), 288.63f, 288.71f, 288.615f, 288.71f, 1205));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 55, 00), 288.745f, 288.78f, 288.745f, 288.755f, 518));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 56, 00), 288.745f, 288.75f, 288.72f, 288.735f, 507));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 57, 00), 288.775f, 288.775f, 288.745f, 288.755f, 863));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 58, 00), 288.775f, 288.775f, 288.725f, 288.755f, 4602));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 13, 59, 00), 288.745f, 288.855f, 288.745f, 288.85f, 2211));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 00, 00), 288.83f, 288.93f, 288.805f, 288.905f, 3007));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 01, 00), 288.935f, 288.96f, 288.91f, 288.92f, 1620));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 02, 00), 288.89f, 288.985f, 288.89f, 288.98f, 2316));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 03, 00), 288.96f, 289.11f, 288.96f, 289.11f, 1058));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 04, 00), 289.1f, 289.1f, 288.94f, 288.94f, 3053));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 05, 00), 288.915f, 288.99f, 288.9f, 288.965f, 1620));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 06, 00), 288.97f, 289.005f, 288.96f, 288.995f, 1786));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 07, 00), 289f, 289.02f, 288.97f, 288.98f, 1541));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 08, 00), 288.95f, 288.965f, 288.915f, 288.94f, 1010));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 09, 00), 288.94f, 288.95f, 288.89f, 288.92f, 1092));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 10, 00), 288.92f, 288.94f, 288.83f, 288.85f, 923));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 11, 00), 288.86f, 288.925f, 288.86f, 288.91f, 1064));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 12, 00), 288.9f, 288.92f, 288.895f, 288.92f, 534));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 13, 00), 288.925f, 288.925f, 288.88f, 288.88f, 422));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 14, 00), 288.88f, 288.88f, 288.765f, 288.79f, 706));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 15, 00), 288.76f, 288.765f, 288.64f, 288.69f, 718));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 16, 00), 288.69f, 288.75f, 288.69f, 288.73f, 766));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 17, 00), 288.76f, 288.88f, 288.75f, 288.88f, 5875));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 18, 00), 288.84f, 288.98f, 288.84f, 288.94f, 4710));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 19, 00), 288.94f, 288.99f, 288.94f, 288.97f, 7924));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 20, 00), 288.985f, 289f, 288.93f, 288.97f, 860));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 21, 00), 288.97f, 288.995f, 288.84f, 288.98f, 1267));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 22, 00), 288.925f, 288.97f, 288.91f, 288.95f, 636));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 23, 00), 288.97f, 288.97f, 288.92f, 288.92f, 110));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 24, 00), 288.93f, 288.95f, 288.905f, 288.905f, 407));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 25, 00), 288.885f, 288.92f, 288.885f, 288.915f, 600));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 26, 00), 288.92f, 288.945f, 288.92f, 288.94f, 438));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 27, 00), 288.92f, 289f, 288.91f, 289f, 2437));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 28, 00), 289.005f, 289.155f, 288.98f, 289.12f, 1796));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 29, 00), 289.125f, 289.14f, 289.12f, 289.13f, 2085));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 30, 00), 289.135f, 289.14f, 289.095f, 289.125f, 1066));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 31, 00), 289.09f, 289.11f, 289.005f, 289.075f, 1221));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 32, 00), 289.08f, 289.08f, 289.03f, 289.03f, 845));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 33, 00), 289.06f, 289.135f, 289.06f, 289.135f, 7069));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 34, 00), 289.125f, 289.155f, 289.11f, 289.115f, 7212));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 35, 00), 289.105f, 289.19f, 289.105f, 289.125f, 4283));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 36, 00), 289.11f, 289.19f, 289.09f, 289.19f, 3416));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 37, 00), 289.175f, 289.24f, 289.175f, 289.23f, 1915));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 38, 00), 289.215f, 289.275f, 289.205f, 289.26f, 1222));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 39, 00), 289.255f, 289.285f, 289.24f, 289.255f, 1464));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 40, 00), 289.24f, 289.325f, 289.23f, 289.325f, 1360));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 41, 00), 289.31f, 289.335f, 289.29f, 289.335f, 2284));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 42, 00), 289.335f, 289.345f, 289.3f, 289.32f, 1170));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 43, 00), 289.325f, 289.39f, 289.32f, 289.37f, 4774));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 44, 00), 289.365f, 289.365f, 289.335f, 289.355f, 7663));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 45, 00), 289.365f, 289.45f, 289.355f, 289.45f, 2697));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 46, 00), 289.455f, 289.49f, 289.455f, 289.48f, 1434));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 47, 00), 289.48f, 289.525f, 289.475f, 289.525f, 999));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 48, 00), 289.53f, 289.565f, 289.475f, 289.475f, 981));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 49, 00), 289.51f, 289.52f, 289.5f, 289.52f, 1026));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 50, 00), 289.55f, 289.58f, 289.445f, 289.49f, 6197));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 51, 00), 289.51f, 289.57f, 289.48f, 289.57f, 917));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 52, 00), 289.61f, 289.62f, 289.575f, 289.595f, 1223));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 53, 00), 289.565f, 289.575f, 289.565f, 289.57f, 140));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 54, 00), 289.55f, 289.55f, 289.525f, 289.545f, 871));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 55, 00), 289.545f, 289.545f, 289.5f, 289.53f, 575));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 56, 00), 289.55f, 289.56f, 289.48f, 289.48f, 935));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 57, 00), 289.49f, 289.49f, 289.41f, 289.43f, 626));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 58, 00), 289.49f, 289.56f, 289.48f, 289.53f, 1746));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 14, 59, 00), 289.58f, 289.58f, 289.49f, 289.51f, 2616));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 00, 00), 289.5f, 289.55f, 289.485f, 289.55f, 1544));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 01, 00), 289.54f, 289.56f, 289.405f, 289.405f, 1985));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 02, 00), 289.39f, 289.41f, 289.36f, 289.38f, 2061));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 03, 00), 289.395f, 289.52f, 289.385f, 289.49f, 1238));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 04, 00), 289.46f, 289.49f, 289.46f, 289.475f, 182));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 05, 00), 289.43f, 289.46f, 289.39f, 289.445f, 1421));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 06, 00), 289.42f, 289.42f, 289.34f, 289.38f, 1103));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 07, 00), 289.34f, 289.34f, 289.3f, 289.3f, 1437));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 08, 00), 289.28f, 289.28f, 289.25f, 289.255f, 1614));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 09, 00), 289.26f, 289.305f, 289.24f, 289.26f, 2019));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 10, 00), 289.17f, 289.17f, 289.06f, 289.1f, 2543));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 11, 00), 289.11f, 289.16f, 289.11f, 289.16f, 1671));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 12, 00), 289.18f, 289.335f, 289.18f, 289.3f, 1518));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 13, 00), 289.33f, 289.335f, 289.26f, 289.26f, 976));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 14, 00), 289.28f, 289.28f, 289.235f, 289.235f, 1313));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 15, 00), 289.215f, 289.215f, 289.165f, 289.195f, 514));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 16, 00), 289.135f, 289.135f, 289.05f, 289.125f, 612));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 17, 00), 289.15f, 289.25f, 289.105f, 289.105f, 1043));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 18, 00), 289.1f, 289.1f, 289.085f, 289.085f, 354));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 19, 00), 289.1f, 289.185f, 289.09f, 289.18f, 1383));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 20, 00), 289.175f, 289.235f, 289.155f, 289.235f, 1220));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 21, 00), 289.235f, 289.275f, 289.235f, 289.24f, 1925));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 22, 00), 289.245f, 289.255f, 289.215f, 289.245f, 977));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 23, 00), 289.245f, 289.27f, 289.235f, 289.255f, 653));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 24, 00), 289.255f, 289.34f, 289.24f, 289.34f, 2391));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 25, 00), 289.34f, 289.39f, 289.33f, 289.385f, 2188));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 26, 00), 289.38f, 289.47f, 289.375f, 289.465f, 5876));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 27, 00), 289.485f, 289.61f, 289.485f, 289.61f, 2542));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 28, 00), 289.615f, 289.62f, 289.595f, 289.61f, 1367));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 29, 00), 289.59f, 289.61f, 289.57f, 289.605f, 2147));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 30, 00), 289.61f, 289.65f, 289.555f, 289.65f, 1401));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 31, 00), 289.63f, 289.69f, 289.575f, 289.575f, 3635));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 32, 00), 289.615f, 289.62f, 289.545f, 289.56f, 462));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 33, 00), 289.555f, 289.6f, 289.525f, 289.565f, 1344));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 34, 00), 289.565f, 289.565f, 289.46f, 289.475f, 1402));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 35, 00), 289.45f, 289.46f, 289.415f, 289.415f, 1205));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 36, 00), 289.365f, 289.365f, 289.305f, 289.33f, 925));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 37, 00), 289.335f, 289.335f, 289.26f, 289.26f, 932));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 38, 00), 289.23f, 289.28f, 289.17f, 289.17f, 1278));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 39, 00), 289.165f, 289.195f, 289.13f, 289.15f, 1826));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 40, 00), 289.145f, 289.2f, 289.08f, 289.08f, 1287));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 41, 00), 289f, 289f, 288.96f, 288.98f, 5681));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 42, 00), 288.98f, 288.985f, 288.89f, 288.895f, 3267));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 43, 00), 288.89f, 288.945f, 288.775f, 288.775f, 2647));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 44, 00), 288.74f, 288.76f, 288.61f, 288.67f, 2285));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 45, 00), 288.675f, 288.7f, 288.6f, 288.63f, 2461));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 46, 00), 288.635f, 288.84f, 288.635f, 288.82f, 3914));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 47, 00), 288.85f, 288.88f, 288.68f, 288.695f, 3369));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 48, 00), 288.71f, 288.72f, 288.595f, 288.61f, 674));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 49, 00), 288.69f, 288.71f, 288.66f, 288.68f, 6092));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 50, 00), 288.71f, 288.8f, 288.67f, 288.7f, 8255));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 51, 00), 288.71f, 288.73f, 288.655f, 288.665f, 4665));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 52, 00), 288.68f, 288.68f, 288.58f, 288.625f, 6207));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 53, 00), 288.625f, 288.665f, 288.6f, 288.635f, 3983));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 54, 00), 288.61f, 288.9f, 288.61f, 288.825f, 8088));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 55, 00), 288.82f, 288.89f, 288.8f, 288.885f, 4333));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 56, 00), 288.885f, 289.07f, 288.885f, 289.065f, 5767));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 57, 00), 289.065f, 289.07f, 288.91f, 288.92f, 5966));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 58, 00), 288.915f, 289.07f, 288.915f, 289.06f, 6799));
            m_data.Add(new DataPoint("MSFT", new DateTime(2021, 07, 26, 15, 59, 00), 289.1f, 289.2f, 289.055f, 289.12f, 6850));
        */
        }


    }
}
