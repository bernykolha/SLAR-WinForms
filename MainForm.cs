using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO; 
using System.Linq;
using System.Text; 
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.WinForms;

using WinColor = System.Drawing.Color;
using WinFont = System.Drawing.Font;
using WinFontStyle = System.Drawing.FontStyle;
using WinLabel = System.Windows.Forms.Label;
using WinOrientation = System.Windows.Forms.Orientation;
using WinHorizontalAlignment = System.Windows.Forms.HorizontalAlignment;
using PlotColor = ScottPlot.Color;

namespace SLAR_WinForms
{
    public class MainForm : Form
    {
        private const int PAD = 14;
        private const int CELL_W = 72;
        private const int CELL_H = 30;
        private const int GAP = 4;

        private static readonly WinColor BG = WinColor.FromArgb(12, 14, 20);
        private static readonly WinColor SURFACE = WinColor.FromArgb(20, 24, 32);
        private static readonly WinColor SURFACE2 = WinColor.FromArgb(28, 33, 44);
        private static readonly WinColor BORDER = WinColor.FromArgb(45, 52, 68);
        private static readonly WinColor ACCENT = WinColor.FromArgb(0, 220, 150);
        private static readonly WinColor ACCENT2 = WinColor.FromArgb(0, 150, 255);
        private static readonly WinColor ACCENT3 = WinColor.FromArgb(255, 100, 100);
        private static readonly WinColor ACCENT4 = WinColor.FromArgb(255, 200, 80);
        private static readonly WinColor TEXTCOLOR = WinColor.FromArgb(220, 230, 245);
        private static readonly WinColor MUTED = WinColor.FromArgb(90, 105, 130);

        private static readonly WinColor[] METHOD_COLORS = { ACCENT, ACCENT2, ACCENT4 };

        private int N = 3;
        private TextBox[,] tbA = new TextBox[50, 50]; 
        private TextBox[] tbB = new TextBox[50];
        private Panel matrixPanel = new();
        private Panel resultsPanel = new();
        private FormsPlot? formsPlot;
        private SplitContainer mainSplit = null!;
        private NumericUpDown numSize = new();

        private CheckBox[] cbMethods = new CheckBox[3];
        private Button btnSolve = new();
        private TabControl tcResults = new();

        private List<SolveResult> _lastResults = new();

        private List<ISlarSolver> _solvers = new List<ISlarSolver>
        {
            new GaussUnitSolver(),
            new GaussFullPivotSolver(),
            new CholeskySolver()
        };

        public MainForm()
        {
            InitializeForm();
            BuildLayout();
            RebuildMatrix();
        }

        private string ToSubscript(int number)
        {
            string normal = number.ToString();
            string res = "";
            foreach (char c in normal)
            {
                switch (c)
                {
                    case '0': res += "₀"; break;
                    case '1': res += "₁"; break;
                    case '2': res += "₂"; break;
                    case '3': res += "₃"; break;
                    case '4': res += "₄"; break;
                    case '5': res += "₅"; break;
                    case '6': res += "₆"; break;
                    case '7': res += "₇"; break;
                    case '8': res += "₈"; break;
                    case '9': res += "₉"; break;
                    default: res += c; break;
                }
            }
            return res;
        }

        private void InitializeForm()
        {
            Text = "СЛАР — Розв'язання систем лінійних рівнянь";
            Size = new Size(1150, 850);
            MinimumSize = new Size(950, 750);
            BackColor = BG;
            ForeColor = TEXTCOLOR;
            Font = new WinFont("Consolas", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
        }

        private void BuildLayout()
        {
            mainSplit = new SplitContainer { 
                Dock = DockStyle.Fill, 
                Orientation = WinOrientation.Vertical, 
                SplitterWidth = 6, 
                BackColor = BORDER,
                Panel1MinSize = 350
            };
            Controls.Add(mainSplit);

            this.Shown += (s, e) => { if (mainSplit != null) mainSplit.SplitterDistance = 420; };

            var leftScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Padding = new Padding(PAD) };
            mainSplit.Panel1.Controls.Add(leftScroll);

            var leftStack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top };
            leftScroll.Controls.Add(leftStack);

            leftStack.Controls.Add(MakeLabel("СЛАР  SOLVER", 16, WinFontStyle.Bold, ACCENT));
            leftStack.Controls.Add(MakeSectionLabel("01 · КІЛЬКІСТЬ НЕВІДОМИХ"));
            
            var sizeRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            numSize = new NumericUpDown { Minimum = 2, Maximum = 50, Value = 3, BackColor = SURFACE2, ForeColor = ACCENT, Font = new WinFont("Consolas", 12f, WinFontStyle.Bold), Width = 80 };
            
            var btnApply = new Button { Text = "ОНОВИТИ", Size = new Size(110, 32), BackColor = ACCENT, FlatStyle = FlatStyle.Flat, ForeColor = WinColor.Black, Font = new WinFont("Consolas", 9f, WinFontStyle.Bold) };
            btnApply.Click += (s, e) => { N = (int)numSize.Value; RebuildMatrix(); };
            
            sizeRow.Controls.Add(numSize);
            sizeRow.Controls.Add(btnApply);
            leftStack.Controls.Add(sizeRow);

            leftStack.Controls.Add(MakeSectionLabel("02 · МАТРИЦЯ A · ВЕКТОР b"));
            matrixPanel = new Panel { Width = 370, Height = 420, AutoScroll = true, Margin = new Padding(0, 5, 10, 10), BackColor = BG };
            matrixPanel.Paint += (s, e) => {
                using (var p = new Pen(BORDER, 1)) e.Graphics.DrawRectangle(p, 0, 0, matrixPanel.Width - 1, matrixPanel.Height - 1);
            };
            leftStack.Controls.Add(matrixPanel);

            leftStack.Controls.Add(MakeSectionLabel("03 · ДІЇ"));
            var actionRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            
            var btnRand = MakeSmallButton("🎲 Випадкова"); btnRand.Click += (s, e) => RandomFill();
            var btnClear = MakeSmallButton("✕ Очистити"); btnClear.Click += (s, e) => ClearAll();
            var btnSave = MakeSmallButton("💾 Зберегти звіт"); btnSave.Click += (s, e) => ExportToTxt();
            
            actionRow.Controls.AddRange(new Control[] { btnRand, btnClear, btnSave });
            leftStack.Controls.Add(actionRow);

            leftStack.Controls.Add(MakeSectionLabel("04 · МЕТОДИ"));
            string[] mNames = { "Гаус (одинична діагональ)", "Метод обертання (повний вибір)", "Гаус-Холецький (квадратний корінь)" };
            for (int m = 0; m < 3; m++) {
                cbMethods[m] = new CheckBox { Text = mNames[m], Checked = true, AutoSize = true, ForeColor = METHOD_COLORS[m], Margin = new Padding(0, 0, 0, 6) };
                leftStack.Controls.Add(cbMethods[m]);
            }

            btnSolve = new Button { Text = "▶  РОЗВ'ЯЗАТИ", Size = new Size(340, 44), BackColor = ACCENT, ForeColor = WinColor.Black, FlatStyle = FlatStyle.Flat, Font = new WinFont("Consolas", 11f, WinFontStyle.Bold), Margin = new Padding(0, 14, 0, 0), Cursor = Cursors.Hand };
            btnSolve.Click += (s, e) => Solve();
            leftStack.Controls.Add(btnSolve);

            tcResults = new TabControl { Dock = DockStyle.Fill, DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(140, 28), SizeMode = TabSizeMode.Fixed };
            tcResults.DrawItem += DrawTab;
            
            var tabSolve = new TabPage("Розв'язки") { BackColor = SURFACE };
            var tabChart = new TabPage("Графік") { BackColor = SURFACE };
            var tabSystem = new TabPage("Система") { BackColor = SURFACE };
            tcResults.TabPages.AddRange(new[] { tabSolve, tabChart, tabSystem });
            
            resultsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(PAD) };
            tabSolve.Controls.Add(resultsPanel);
            formsPlot = new FormsPlot { Dock = DockStyle.Fill };
            tabChart.Controls.Add(formsPlot);
            
            var rtbSystem = new RichTextBox { Dock = DockStyle.Fill, BackColor = SURFACE, ForeColor = TEXTCOLOR, Font = new WinFont("Segoe UI", 11f), BorderStyle = BorderStyle.None, ReadOnly = true, Padding = new Padding(PAD) };
            tabSystem.Controls.Add(rtbSystem);
            
            mainSplit.Panel2.Controls.Add(tcResults);
        }

        private void RebuildMatrix()
        {
            matrixPanel.Controls.Clear();
            int startX = 8;
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    tbA[i, j] = MakeCell(startX + j * (CELL_W + GAP), i * (CELL_H + GAP), SURFACE2, TEXTCOLOR, "0");
                    matrixPanel.Controls.Add(tbA[i, j]);
                }
                tbB[i] = MakeCell(startX + N * (CELL_W + GAP) + 20, i * (CELL_H + GAP), WinColor.FromArgb(0, 50, 90), WinColor.White, "0");
                matrixPanel.Controls.Add(tbB[i]);
            }
        }

        private TextBox MakeCell(int x, int y, WinColor bg, WinColor fg, string text) => 
            new TextBox { Location = new Point(x, y), Size = new Size(CELL_W, CELL_H), BackColor = bg, ForeColor = fg, BorderStyle = BorderStyle.FixedSingle, TextAlign = WinHorizontalAlignment.Center, Text = text };

        private void Solve()
{
    double[,] A = new double[N, N];
    double[] b = new double[N];
    const double LIMIT = 1e15; 

    try {
        for (int i = 0; i < N; i++) {
            for (int j = 0; j < N; j++) {
                string textA = tbA[i, j].Text.Trim().Replace('.', ',');
                if (string.IsNullOrWhiteSpace(textA)) textA = "0";
                
                double valA = double.Parse(textA);

                if (Math.Abs(valA) > LIMIT)
                {
                    MessageBox.Show($"Число {textA} у комірці A[{i+1},{j+1}] занадто велике!\nВведіть числа в діапазоні від -1e15 до 1e15.", 
                                    "Помилка діапазону", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                A[i, j] = valA;
            }

            string textB = tbB[i].Text.Trim().Replace('.', ',');
            if (string.IsNullOrWhiteSpace(textB)) textB = "0";

            double valB = double.Parse(textB);

            if (Math.Abs(valB) > LIMIT)
            {
                MessageBox.Show($"Число {textB} у векторі b[{i+1}] занадто велике!\nВведіть числа в діапазоні від -1e15 до 1e15.", 
                                "Помилка діапазону", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            b[i] = valB;
        }
    } catch (FormatException) { 
        MessageBox.Show("Будь ласка, введіть коректні числа!", "Помилка формату"); 
        return; 
    }

    _lastResults.Clear();

    for (int i = 0; i < _solvers.Count; i++)
    {
        if (cbMethods[i].Checked)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = _solvers[i].Solve(A, b);
            sw.Stop();
            res.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
            _lastResults.Add(res);
        }
    }

    RenderSolutions(_lastResults);
    RenderChart(_lastResults); 
    RenderSystem(A, b);
    tcResults.SelectedIndex = 0;
}

        private void RenderSolutions(List<SolveResult> results)
        {
            resultsPanel.Controls.Clear();
            var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top };
            resultsPanel.Controls.Add(stack);

            int mi = 0;
            foreach (var r in results) {
                WinColor col = METHOD_COLORS[mi % 3];
                var card = new Panel { BackColor = SURFACE2, AutoSize = true, MinimumSize = new Size(resultsPanel.Width - 60, 50), Margin = new Padding(0, 0, 0, 15), Padding = new Padding(15) };
                
                card.Paint += (s, e) => {
                    e.Graphics.DrawRectangle(new Pen(BORDER, 1), 0, 0, card.Width - 1, card.Height - 1);
                    e.Graphics.FillRectangle(new SolidBrush(col), 0, 0, 5, card.Height);
                };

                var lblTitle = MakeLabel("■ " + r.MethodName, 10, WinFontStyle.Bold, col);
                lblTitle.Location = new Point(15, 15); card.Controls.Add(lblTitle);

                if (r.Success && r.X != null) {
                    int y = 45;
                    for (int i = 0; i < r.X.Length; i++) {
                        var l = MakeLabel("x" + ToSubscript(i + 1) + $" = {r.X[i]:F8}", 10, WinFontStyle.Regular, TEXTCOLOR);
                        l.Location = new Point(15, y); card.Controls.Add(l); y += 22;
                    }
                    var lblRes = MakeLabel($"Нев'язка = {r.Residual:E3}", 9, WinFontStyle.Bold, ACCENT);
                    lblRes.Location = new Point(15, y + 10); card.Controls.Add(lblRes);
                    
                    if (r.Steps.Count > 0) {
                        var box = new RichTextBox { Location = new Point(15, y + 40), Size = new Size(card.Width - 40, 100), BackColor = WinColor.FromArgb(15, 18, 25), ForeColor = MUTED, BorderStyle = BorderStyle.None, Font = new WinFont("Consolas", 8f), ReadOnly = true, Text = string.Join("\n", r.Steps) };
                        card.Controls.Add(box);
                    }
                } else {
                    var lErr = MakeLabel("⚠ " + r.Error, 9, WinFontStyle.Italic, ACCENT3);
                    lErr.Location = new Point(15, 45); card.Controls.Add(lErr);
                }
                stack.Controls.Add(card); mi++;
            }
        }

        private void RenderChart(List<SolveResult> results)
        {
            if (formsPlot == null) return;
            formsPlot.Plot.Clear();
            StylePlot(formsPlot.Plot);
            int mi = 0;
            foreach (var r in results) {
                if (!r.Success || r.X == null) continue;
                var sp = formsPlot.Plot.Add.Scatter(Enumerable.Range(0, N).Select(i => (double)i).ToArray(), r.X);
                sp.LegendText = r.MethodName;
                WinColor c = METHOD_COLORS[mi % 3];
                sp.Color = new PlotColor(c.R, c.G, c.B); mi++;
            }
            formsPlot.Refresh();
        }

        private void StylePlot(Plot plt)
        {
            plt.FigureBackground.Color = new PlotColor(SURFACE.R, SURFACE.G, SURFACE.B);
            plt.DataBackground.Color = new PlotColor(BG.R, BG.G, BG.B);
    
            plt.XLabel("Невідомі (x)", size: 12);
            plt.YLabel("Значення", size: 12);
    
            plt.Grid.MajorLineColor = new PlotColor(45, 52, 68); 
            plt.Grid.IsVisible = true; 

            plt.Axes.Color(new PlotColor(MUTED.R, MUTED.G, MUTED.B));
    
            plt.Legend.IsVisible = true;
        }

        private void RenderSystem(double[,] A, double[] b)
        {
            var rtb = tcResults.TabPages[2].Controls.OfType<RichTextBox>().First();
            rtb.Clear();
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) rtb.AppendText($"{A[i, j]:G}x{ToSubscript(j+1)} ");
                rtb.AppendText($"= {b[i]:G}\n");
            }
        }

        private void RandomFill() { var rnd = new Random(); for (int i = 0; i < N; i++) { for (int j = 0; j < N; j++) tbA[i, j].Text = rnd.Next(-10, 11).ToString(); tbB[i].Text = rnd.Next(-10, 11).ToString(); } }
        private void ClearAll() { for (int i = 0; i < N; i++) { for (int j = 0; j < N; j++) tbA[i, j].Text = "0"; tbB[i].Text = "0"; } }
        private void DrawTab(object? s, DrawItemEventArgs e) { e.Graphics.FillRectangle(new SolidBrush(e.Index == tcResults.SelectedIndex ? SURFACE : BG), e.Bounds); e.Graphics.DrawString(tcResults.TabPages[e.Index].Text, Font, new SolidBrush(TEXTCOLOR), e.Bounds.X + 10, e.Bounds.Y + 5); }
        private WinLabel MakeLabel(string t, float s, WinFontStyle st, WinColor c) => new WinLabel { Text = t, Font = new WinFont("Consolas", s, st), ForeColor = c, AutoSize = true };
        private WinLabel MakeSectionLabel(string t) { var l = MakeLabel(t, 7.5f, WinFontStyle.Regular, ACCENT); l.Margin = new Padding(0, 10, 0, 5); return l; }
        private Button MakeSmallButton(string t) => new Button { Text = t, AutoSize = true, BackColor = SURFACE2, FlatStyle = FlatStyle.Flat, ForeColor = MUTED, Font = new WinFont("Consolas", 8f), Cursor = Cursors.Hand };
        private void ExportToTxt() {  }
    }
}