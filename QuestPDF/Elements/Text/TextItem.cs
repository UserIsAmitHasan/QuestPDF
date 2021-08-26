﻿using System;
using QuestPDF.Drawing;
using QuestPDF.Drawing.SpacePlan;
using QuestPDF.Infrastructure;
using Size = QuestPDF.Infrastructure.Size;

namespace QuestPDF.Elements.Text
{
    internal class TextMeasurementRequest
    {
        public ICanvas Canvas { get; set; }
        public IPageContext PageContext { get; set; }
        
        public int StartIndex { get; set; }
        public float AvailableWidth { get; set; }
    }
    
    internal class TextMeasurementResult
    {
        public float Width { get; set; }
        public float Height => Math.Abs(Descent) + Math.Abs(Ascent);

        public float Ascent { get; set; }
        public float Descent { get; set; }

        public float LineHeight { get; set; }
        
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        
        public int TotalIndex { get; set; }

        public bool HasContent => StartIndex < EndIndex;
        public bool IsLast => EndIndex == TotalIndex;
    }

    internal class TextDrawingRequest
    {
        public ICanvas Canvas { get; set; }
        public IPageContext PageContext { get; set; }
        
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        
        public float TotalAscent { get; set; }
        public Size TextSize { get; set; }
    }

    internal interface ITextElement
    {
        TextMeasurementResult? Measure(TextMeasurementRequest request);
        void Draw(TextDrawingRequest request);
    }
    
    internal class TextItem : ITextElement
    {
        public string Text { get; set; }
        public TextStyle Style { get; set; } = new TextStyle();

        public TextMeasurementResult? Measure(TextMeasurementRequest request)
        {
            var paint = Style.ToPaint();
            
            // start breaking text from requested position
            var text = Text.Substring(request.StartIndex);
            var breakingIndex = (int)paint.BreakText(text, request.AvailableWidth);

            if (breakingIndex <= 0)
                return null;
            
            // break text only on spaces
            if (breakingIndex < text.Length)
            {
                breakingIndex = text.Substring(0, breakingIndex).LastIndexOf(" ");

                if (breakingIndex <= 0)
                    return null;

                breakingIndex += 1;
            }

            text = text.Substring(0, breakingIndex);
            
            // measure final text
            var width = paint.MeasureText(text);
            
            return new TextMeasurementResult
            {
                Width = width,
                
                Ascent = paint.FontMetrics.Ascent,
                Descent = paint.FontMetrics.Descent,
     
                LineHeight = Style.LineHeight,
                
                StartIndex = request.StartIndex,
                EndIndex = request.StartIndex + breakingIndex,
                TotalIndex = Text.Length
            };
        }
        
        public void Draw(TextDrawingRequest request)
        {
            var fontMetrics = Style.ToPaint().FontMetrics;

            var text = Text.Substring(request.StartIndex, request.EndIndex - request.StartIndex);
            
            request.Canvas.DrawRectangle(new Position(0, request.TotalAscent), new Size(request.TextSize.Width, request.TextSize.Height), Style.BackgroundColor);
            request.Canvas.DrawText(text, Position.Zero, Style);

            // draw underline
            if (Style.IsUnderlined && fontMetrics.UnderlinePosition.HasValue)
                DrawLine(fontMetrics.UnderlinePosition.Value, fontMetrics.UnderlineThickness.Value);
            
            // draw stroke
            if (Style.IsStroked && fontMetrics.StrikeoutPosition.HasValue)
                DrawLine(fontMetrics.StrikeoutPosition.Value, fontMetrics.StrikeoutThickness.Value);

            void DrawLine(float offset, float thickness)
            {
                request.Canvas.DrawRectangle(new Position(0, offset - thickness / 2f), new Size(request.TextSize.Width, thickness), Style.Color);
            }
        }
    }

    internal class PageNumberTextItem : ITextElement
    {
        public TextStyle Style { get; set; } = new TextStyle();
        public string SlotName { get; set; }
        
        public TextMeasurementResult? Measure(TextMeasurementRequest request)
        {
            return GetItem(request.PageContext).Measure(request);
        }

        public void Draw(TextDrawingRequest request)
        {
            GetItem(request.PageContext).Draw(request);
        }

        private TextItem GetItem(IPageContext context)
        {
            var pageNumberPlaceholder = 123;
            
            var pageNumber = context.GetRegisteredLocations().Contains(SlotName)
                ? context.GetLocationPage(SlotName)
                : pageNumberPlaceholder;
            
            return new TextItem
            {
                Style = Style,
                Text = pageNumber.ToString()
            };
        }
    }
}