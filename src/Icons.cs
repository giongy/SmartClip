using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

// The signs, as lines: the icons of Lucide (https://lucide.dev), ISC license, some of them from Feather, MIT
// license; both notices are in LICENSE. Each is drawn on a grid of 24 with lines 2 wide, round at their ends and
// corners, as Lucide draws them, and scaled to the box it is given: the same on every Windows, with or without its
// fonts of icons. A sign is a few shapes split by ';': "circle cx cy r", "rect x y width height radius", or the
// outline of an SVG path (M, L, H, V, C, S, Q, A, Z, and their relative forms).
static class Icons
{
    const float Grid = 24, Stroke = 2;   // as Lucide draws them
    const float LeastStroke = 1.2f;      // in pixels: thinner, a small sign would fade
    static readonly Dictionary<Sign, GraphicsPath> made = new Dictionary<Sign, GraphicsPath>();   // read once each

    // Draws a sign in 'colour', as large as the box lets it and in its middle.
    public static void Draw(Graphics g, Sign sign, Rectangle box, Color colour)
    {
        GraphicsPath shape;
        if (!made.TryGetValue(sign, out shape)) made[sign] = shape = Read(Shapes(sign));
        float scale = Math.Min(box.Width, box.Height) / Grid;
        SmoothingMode mode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath placed = (GraphicsPath)shape.Clone())
        using (Matrix fit = new Matrix())
        using (Pen pen = new Pen(colour, Math.Max(Stroke * scale, LeastStroke)))
        using (Brush fill = new SolidBrush(colour))
        {
            fit.Translate(box.Left + (box.Width - Grid * scale) / 2, box.Top + (box.Height - Grid * scale) / 2);
            fit.Scale(scale, scale);
            placed.Transform(fit);
            pen.StartCap = pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            if (sign == Sign.FullFolder) g.FillPath(fill, placed);
            g.DrawPath(pen, placed);
        }
        g.SmoothingMode = mode;
    }

    // Each sign, by the name Lucide gives it.
    static string Shapes(Sign sign)
    {
        switch (sign)
        {
            case Sign.Text:   // file-text
                return "M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z;"
                    + "M14 2v5a1 1 0 0 0 1 1h5;M10 9H8;M16 13H8;M16 17H8";
            case Sign.Link:   // link
                return "M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71;M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71";
            case Sign.Star:   // star
                return "M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904"
                    + "l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0"
                    + "L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.906"
                    + "l5.165-.755a2.122 2.122 0 0 0 1.597-1.16z";
            case Sign.Plus:   // plus
                return "M5 12h14;M12 5v14";
            case Sign.Edit:   // pencil
                return "M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622"
                    + "l4.353-1.32a2 2 0 0 0 .83-.497z;m15 5 4 4";
            case Sign.Delete:   // trash
                return "M10 11v6;M14 11v6;M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6;M3 6h18;M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2";
            case Sign.Options:   // settings
                return "M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831"
                    + " 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915"
                    + " 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915;circle 12 12 3";
            case Sign.Keys:   // keyboard
                return "M10 8h.01;M12 12h.01;M14 8h.01;M16 12h.01;M18 8h.01;M6 8h.01;M7 16h10;M8 12h.01;rect 2 4 20 16 2";
            case Sign.Search:   // search
                return "m21 21-4.34-4.34;circle 11 11 8";
            case Sign.Help:   // circle-question-mark
                return "circle 12 12 10;M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3;M12 17h.01";
            case Sign.Opened:   // chevron-down
                return "m6 9 6 6 6-6";
            case Sign.Closed:   // chevron-right
                return "m9 18 6-6-6-6";
            case Sign.Bolt:   // zap
                return "M15.914 4a1.5 1.5 0 00-2.474-1.561l-9 9A1.5 1.5 0 005.5 14h4.002a.5.5 0 01.471.666L8.086 20a1.5 1.5 0 002.475 1.56"
                    + "l9-9A1.5 1.5 0 0018.5 10h-3.997a.5.5 0 01-.472-.667z";
            case Sign.History:   // rotate-ccw-clock
                return "M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8;M3 3v5h5;M12 7v5l4 2";
            case Sign.Up:   // arrow-up
                return "m5 12 7-7 7 7;M12 19V5";
            case Sign.Bold:   // bold
                return "M6 12h9a4 4 0 0 1 0 8H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h7a4 4 0 0 1 0 8";
            case Sign.Italic:   // italic
                return "M19 4H10;M14 20H5;M15 4 9 20";
            case Sign.Underline:   // underline
                return "M6 4v6a6 6 0 0 0 12 0V4;M4 20H20";
            case Sign.Erase:   // eraser
                return "M21 21H8a2 2 0 0 1-1.42-.587l-3.994-3.999a2 2 0 0 1 0-2.828l10-10a2 2 0 0 1 2.829 0l5.999 6a2 2 0 0 1 0 2.828"
                    + "L12.834 21;m5.082 11.09 8.828 8.828";
            case Sign.Formatted:   // type
                return "M12 4v16;M4 7V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v2;M9 20h6";
            case Sign.Folder:
            case Sign.FullFolder:   // folder, the full one filled
                return Folder;
            case Sign.NewFolder:   // folder-plus
                return "M12 10v6;M9 13h6;" + Folder;
            case Sign.Move:   // folder-input
                return "M2 9V5a2 2 0 0 1 2-2h3.9a2 2 0 0 1 1.69.9l.81 1.2a2 2 0 0 0 1.67.9H20a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-1;"
                    + "M2 13h10;m9 16 3-3-3-3";
            case Sign.Pause:   // pause
                return "rect 14 3 5 18 1;rect 5 3 5 18 1";
            case Sign.Resume:   // play
                return "M5 5a2 2 0 0 1 3.008-1.728l11.997 6.998a2 2 0 0 1 .003 3.458l-12 7A2 2 0 0 1 5 19z";
            case Sign.Open:   // app-window
                return "rect 2 4 20 16 2;M10 4v4;M2 8h20;M6 4v4";
            case Sign.Exit:
            case Sign.Power:   // power
                return "M12 2v10;M18.4 6.6a9 9 0 1 1-12.77.04";
            case Sign.Show:   // eye
                return "M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0;circle 12 12 3";
            case Sign.Hide:   // eye-off
                return "M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49;"
                    + "M14.084 14.158a3 3 0 0 1-4.242-4.242;"
                    + "M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143;m2 2 20 20";
            case Sign.Check:   // check
                return "M20 6 9 17l-5-5";
            case Sign.Paint:   // palette
                return "M12 22a1 1 0 0 1 0-20 10 9 0 0 1 10 9 5 5 0 0 1-5 5h-2.25a1.75 1.75 0 0 0-1.4 2.8l.3.4a1.75 1.75 0 0 1-1.4 2.8z;"
                    + "circle 13.5 6.5 .5;circle 17.5 10.5 .5;circle 6.5 12.5 .5;circle 8.5 7.5 .5";
            case Sign.Info:   // info
                return "circle 12 12 10;M12 16v-4;M12 8h.01";
            case Sign.Done:   // circle-check
                return "circle 12 12 10;m16 9-5.5 5.5L8 12";
            case Sign.Warning:   // triangle-alert
                return "m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3;M12 9v4;M12 17h.01";
            case Sign.Error:   // circle-x
                return "circle 12 12 10;m15 9-6 6;m9 9 6 6";
            case Sign.Paste:   // clipboard-paste
                return "M11 14h10;M16 4h2a2 2 0 0 1 2 2v1.344;m17 18 4-4-4-4;M8 4H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 1.793-1.113;"
                    + "rect 8 2 8 4 1";
            case Sign.Unclip:   // clipboard-x
                return "rect 8 2 8 4 1;M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2;m14.5 11.5-5 5;m9.5 11.5 5 5";
            case Sign.Code:   // code-xml
                return "m18 16 4-4-4-4;m6 8-4 4 4 4;m14.5 4-5 16";
            case Sign.Picture:   // image
                return "rect 3 3 18 18 2;circle 9 9 2;m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21";
            case Sign.Apps:   // layout-grid
                return "rect 3 3 7 7 1;rect 14 3 7 7 1;rect 14 14 7 7 1;rect 3 14 7 7 1";
            case Sign.More:   // ellipsis
                return "circle 12 12 1;circle 19 12 1;circle 5 12 1";
            case Sign.Copy:   // copy
                return "rect 8 8 14 14 2;M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2";
            case Sign.Menu:   // menu
                return "M4 5h16;M4 12h16;M4 19h16";
            case Sign.Cancel:   // x
                return "M18 6 6 18;m6 6 12 12";
            case Sign.Timer:   // timer
                return "M10 2H14;M12 14 15 11;circle 12 14 8";
            case Sign.Globe:   // globe
                return "circle 12 12 10;M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20;M2 12h20";
            case Sign.Enter:   // corner-down-left
                return "M20 4v7a4 4 0 0 1-4 4H4;m9 10-5 5 5 5";
            case Sign.Slow:   // snail
                return "M2 13a6 6 0 1 0 12 0 4 4 0 1 0-8 0 2 2 0 0 0 4 0;circle 10 13 8;M2 21h12c4.4 0 8-3.6 8-8V7a2 2 0 1 0-4 0v6;"
                    + "M18 3 19.1 5.2;M22 3 20.9 5.2";
            case Sign.Hash:   // hash
                return "M4 9H20;M4 15H20;M10 3 8 21;M16 3 14 21";
            case Sign.Pin:   // map-pin
                return "M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0;circle 12 10 3";
            case Sign.Numbers:   // list-ordered
                return "M11 5h10;M11 12h10;M11 19h10;M4 4h1v5;M4 9h2;M6.5 20H3.4c0-1 2.6-1.925 2.6-3.5a1.5 1.5 0 0 0-2.6-1.02";
            case Sign.Calendar:   // calendar
                return "M8 2v3;M16 2v3;rect 3 3 18 18 2;M3 9h18";
            case Sign.Clean:   // brush-cleaning
                return "m16 22-1-4;M19 14a1 1 0 0 0 1-1v-1a2 2 0 0 0-2-2h-3a1 1 0 0 1-1-1V4a2 2 0 0 0-4 0v5a1 1 0 0 1-1 1H6"
                    + "a2 2 0 0 0-2 2v1a1 1 0 0 0 1 1;M19 14H5l-1.973 6.767A1 1 0 0 0 4 22h16a1 1 0 0 0 .973-1.233z;m8 22 1-4";
            case Sign.Language:   // languages
                return "m5 8 6 6;m4 14 6-6 2-3;M2 5h12;M7 2h1;m22 22-5-10-5 10;M14 18h6";
            case Sign.Contrast:   // sun-moon
                return "M12 2v2;M14.837 16.385a6 6 0 1 1-7.223-7.222c.624-.147.97.66.715 1.248a4 4 0 0 0 5.26 5.259c.589-.255 1.396.09 1.248.715;"
                    + "M16 12a4 4 0 0 0-4-4;m19 5-1.256 1.256;M20 12h2";
            case Sign.Letters:   // a-large-small
                return "m15 16 2.536-7.328a1.02 1.02 1 0 1 1.928 0L22 16;M15.697 14h5.606;m2 16 4.039-9.69a.5.5 0 0 1 .923 0L11 16;"
                    + "M3.304 13h6.392";
            case Sign.Rows:   // rows-3
                return "rect 3 3 18 18 2;M21 9H3;M21 15H3";
            case Sign.Tray:   // panel-bottom
                return "rect 3 3 18 18 2;M3 15h18";
            case Sign.Archive:   // archive
                return "rect 2 3 20 5 1;M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8;M10 12h4";
            default:
                throw new ArgumentException("No icon for " + sign);
        }
    }

    const string Folder = "M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13"
        + "a2 2 0 0 0 2 2Z";

    static GraphicsPath Read(string shapes)
    {
        GraphicsPath path = new GraphicsPath();
        foreach (string shape in shapes.Split(';'))
        {
            string[] words = shape.Split(' ');
            if (words[0] == "circle")
            {
                float x = Value(words[1]), y = Value(words[2]), r = Value(words[3]);
                path.AddEllipse(x - r, y - r, 2 * r, 2 * r);
            }
            else if (words[0] == "rect")
                Rect(path, Value(words[1]), Value(words[2]), Value(words[3]), Value(words[4]), Value(words[5]));
            else
                Trace(path, shape);
        }
        return path;
    }

    static float Value(string word)
    {
        return float.Parse(word, CultureInfo.InvariantCulture);
    }

    static void Rect(GraphicsPath path, float x, float y, float width, float height, float radius)
    {
        float side = 2 * radius;
        path.StartFigure();
        path.AddArc(x, y, side, side, 180, 90);
        path.AddArc(x + width - side, y, side, side, 270, 90);
        path.AddArc(x + width - side, y + height - side, side, side, 0, 90);
        path.AddArc(x, y + height - side, side, side, 90, 90);
        path.CloseFigure();
    }

    // The outline of an SVG path. After a move, more pairs of numbers are lines; after any other command, more of its
    // numbers repeat it.
    static void Trace(GraphicsPath path, string data)
    {
        int at = 0;
        char command = 'M';
        PointF current = PointF.Empty, start = PointF.Empty, control = PointF.Empty;   // control: the second of the last curve
        bool curved = false;
        path.StartFigure();
        while (true)
        {
            Skip(data, ref at);
            if (at >= data.Length) break;
            if (char.IsLetter(data[at])) command = data[at++];
            bool relative = char.IsLower(command);
            float x = relative ? current.X : 0, y = relative ? current.Y : 0;
            char kind = char.ToUpperInvariant(command);
            switch (kind)
            {
                case 'M':
                    current = start = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    path.StartFigure();
                    command = relative ? 'l' : 'L';
                    break;
                case 'L':
                    current = Line(path, current, new PointF(x + Number(data, ref at), y + Number(data, ref at)));
                    break;
                case 'H':
                    current = Line(path, current, new PointF(x + Number(data, ref at), current.Y));
                    break;
                case 'V':
                    current = Line(path, current, new PointF(current.X, y + Number(data, ref at)));
                    break;
                case 'C':
                case 'S':
                {
                    // A smooth curve starts as the last one ended, mirrored.
                    PointF first = kind == 'C' ? new PointF(x + Number(data, ref at), y + Number(data, ref at))
                        : curved ? new PointF(2 * current.X - control.X, 2 * current.Y - control.Y) : current;
                    control = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    PointF end = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    path.AddBezier(current, first, control, end);
                    current = end;
                    break;
                }
                case 'Q':
                {
                    PointF middle = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    PointF end = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    path.AddBezier(current, new PointF(current.X + 2 * (middle.X - current.X) / 3, current.Y + 2 * (middle.Y - current.Y) / 3),
                        new PointF(end.X + 2 * (middle.X - end.X) / 3, end.Y + 2 * (middle.Y - end.Y) / 3), end);
                    current = end;
                    break;
                }
                case 'A':
                {
                    float rx = Number(data, ref at), ry = Number(data, ref at), turn = Number(data, ref at);
                    bool large = Flag(data, ref at), sweep = Flag(data, ref at);
                    PointF end = new PointF(x + Number(data, ref at), y + Number(data, ref at));
                    Arc(path, current, rx, ry, turn, large, sweep, end);
                    current = end;
                    break;
                }
                case 'Z':
                    path.CloseFigure();
                    current = start;
                    break;
                default:
                    throw new FormatException("Unknown command " + command + " in " + data);
            }
            curved = kind == 'C' || kind == 'S';
        }
    }

    static PointF Line(GraphicsPath path, PointF from, PointF to)
    {
        path.AddLine(from, to);
        return to;
    }

    static void Skip(string data, ref int at)
    {
        while (at < data.Length && (data[at] == ' ' || data[at] == ',')) at++;
    }

    // A number as SVG writes them: "-.5", "1e-3"; a second point or a sign starts the next one.
    static float Number(string data, ref int at)
    {
        Skip(data, ref at);
        int from = at;
        if (at < data.Length && (data[at] == '-' || data[at] == '+')) at++;
        bool point = false;
        while (at < data.Length && (char.IsDigit(data[at]) || data[at] == '.' && !point))
        {
            if (data[at] == '.') point = true;
            at++;
        }
        if (at < data.Length && (data[at] == 'e' || data[at] == 'E'))
        {
            at++;
            if (data[at] == '-' || data[at] == '+') at++;
            while (at < data.Length && char.IsDigit(data[at])) at++;
        }
        return float.Parse(data.Substring(from, at - from), CultureInfo.InvariantCulture);
    }

    // The flags of an arc are a single digit each, often written together: "a1.5 1.5 0 00-2.474-1.561".
    static bool Flag(string data, ref int at)
    {
        Skip(data, ref at);
        return data[at++] == '1';
    }

    // An arc of an ellipse from 'from' to 'to', as SVG gives it, in curves of a quarter turn at most (the way of the
    // SVG specification, appendix B.2.4: from its ends to its centre).
    static void Arc(GraphicsPath path, PointF from, float rx, float ry, float turn, bool large, bool sweep, PointF to)
    {
        if (from == to) return;
        if (rx == 0 || ry == 0)
        {
            path.AddLine(from, to);
            return;
        }
        double phi = turn * Math.PI / 180, cos = Math.Cos(phi), sin = Math.Sin(phi);
        double dx = (from.X - to.X) / 2.0, dy = (from.Y - to.Y) / 2.0;
        double x1 = cos * dx + sin * dy, y1 = -sin * dx + cos * dy;
        double a = Math.Abs(rx), b = Math.Abs(ry);
        double grow = x1 * x1 / (a * a) + y1 * y1 / (b * b);   // radii too small for the two ends are made larger
        if (grow > 1)
        {
            a *= Math.Sqrt(grow);
            b *= Math.Sqrt(grow);
        }
        double above = a * a * b * b - a * a * y1 * y1 - b * b * x1 * x1, below = a * a * y1 * y1 + b * b * x1 * x1;
        double share = Math.Sqrt(Math.Max(0, above / below)) * (large == sweep ? -1 : 1);
        double cx1 = share * a * y1 / b, cy1 = -share * b * x1 / a;
        double cx = cos * cx1 - sin * cy1 + (from.X + to.X) / 2.0, cy = sin * cx1 + cos * cy1 + (from.Y + to.Y) / 2.0;
        double begin = Angle(1, 0, (x1 - cx1) / a, (y1 - cy1) / b);
        double span = Angle((x1 - cx1) / a, (y1 - cy1) / b, (-x1 - cx1) / a, (-y1 - cy1) / b);
        if (!sweep && span > 0) span -= 2 * Math.PI;
        else if (sweep && span < 0) span += 2 * Math.PI;
        int parts = Math.Max(1, (int)Math.Ceiling(Math.Abs(span) / (Math.PI / 2) - 1e-6));
        double step = span / parts, reach = 4.0 / 3 * Math.Tan(step / 4);
        PointF last = from;
        for (int i = 0; i < parts; i++)
        {
            double t1 = begin + i * step, t2 = t1 + step;
            PointF end = i == parts - 1 ? to : On(cx, cy, a, b, cos, sin, Math.Cos(t2), Math.Sin(t2));
            path.AddBezier(last,
                On(cx, cy, a, b, cos, sin, Math.Cos(t1) - reach * Math.Sin(t1), Math.Sin(t1) + reach * Math.Cos(t1)),
                On(cx, cy, a, b, cos, sin, Math.Cos(t2) + reach * Math.Sin(t2), Math.Sin(t2) - reach * Math.Cos(t2)),
                end);
            last = end;
        }
    }

    // A point of the unit circle, taken onto the ellipse.
    static PointF On(double cx, double cy, double a, double b, double cos, double sin, double ux, double uy)
    {
        return new PointF((float)(cx + cos * a * ux - sin * b * uy), (float)(cy + sin * a * ux + cos * b * uy));
    }

    static double Angle(double ux, double uy, double vx, double vy)
    {
        return Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
    }
}
