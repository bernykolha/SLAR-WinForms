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

        // Поле для зберігання результатів останнього розрахунку
        private List<SolveResult> _lastResults = new();

        public MainForm()
        {
            InitializeForm();
            BuildLayout();
            RebuildMatrix();
            LoadPreset(0);
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

        private void SafeSetSplitter()
        {
            if (mainSplit == null || !mainSplit.IsHandleCreated) return;
            mainSplit.SplitterDistance = 420; 
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

            this.Shown += (s, e) => BeginInvoke(new Action(SafeSetSplitter));

            var leftScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Padding = new Padding(PAD) };
            mainSplit.Panel1.Controls.Add(leftScroll);

            var leftStack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top };
            leftScroll.Controls.Add(leftStack);

            leftStack.Controls.Add(MakeLabel("СЛАР  SOLVER", 16, WinFontStyle.Bold, ACCENT));

            leftStack.Controls.Add(MakeSectionLabel("01 · КІЛЬКІСТЬ НЕВІДОМИХ"));
            
            var lblLimits = MakeLabel("(min: 2, max: 50)", 7f, WinFontStyle.Regular, MUTED);
            lblLimits.Margin = new Padding(0, 0, 0, 5);
            leftStack.Controls.Add(lblLimits);
            
            var sizeRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            numSize = new NumericUpDown { Minimum = 2, Maximum = 50, Value = 3, BackColor = SURFACE2, ForeColor = ACCENT, Font = new WinFont("Consolas", 12f, WinFontStyle.Bold), Width = 80 };
            
            var btnApply = new Button { Text = "ОНОВИТИ", Size = new Size(110, 32), BackColor = ACCENT, FlatStyle = FlatStyle.Flat, ForeColor = WinColor.Black, Font = new WinFont("Consolas", 9f, WinFontStyle.Bold) };
            btnApply.Click += (s, e) => { N = (int)numSize.Value; RebuildMatrix(); };
            
            sizeRow.Controls.Add(numSize);
            sizeRow.Controls.Add(btnApply);
            leftStack.Controls.Add(sizeRow);

            leftStack.Controls.Add(MakeSectionLabel("02 · МАТРИЦЯ A · ВЕКТОР b"));
            var lblDataLimits = MakeLabel("(діапазон: від -1e15 до 1e15)", 7f, WinFontStyle.Italic, MUTED);
            leftStack.Controls.Add(lblDataLimits);

            matrixPanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            leftStack.Controls.Add(matrixPanel);

            leftStack.Controls.Add(MakeSectionLabel("03 · ДІЇ"));
            var actionRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            
            var btnRand = MakeSmallButton("🎲 Випадкова");
            btnRand.Click += (s, e) => RandomFill();
            actionRow.Controls.Add(btnRand);

            var btnClear = MakeSmallButton("✕ Очистити");
            btnClear.Click += (s, e) => ClearAll();
            actionRow.Controls.Add(btnClear);

            var btnSave = MakeSmallButton("💾 Зберегти звіт");
            btnSave.ForeColor = ACCENT;
            btnSave.Click += (s, e) => ExportToTxt();
            actionRow.Controls.Add(btnSave);

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
            
            var rtbSystem = new RichTextBox { 
                Dock = DockStyle.Fill, 
                BackColor = SURFACE, 
                ForeColor = TEXTCOLOR, 
                Font = new WinFont("Segoe UI", 11f), 
                BorderStyle = BorderStyle.None, 
                ReadOnly = true, 
                Padding = new Padding(PAD) 
            };
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
                var eq = MakeLabel("│", 13, WinFontStyle.Regular, MUTED);
                eq.Location = new Point(startX + N * (CELL_W + GAP) + 2, i * (CELL_H + GAP) + 4);
                matrixPanel.Controls.Add(eq);
                tbB[i] = MakeCell(startX + N * (CELL_W + GAP) + 20, i * (CELL_H + GAP), WinColor.FromArgb(0, 50, 90), WinColor.White, "0");
                matrixPanel.Controls.Add(tbB[i]);
            }
            for (int j = 0; j < N; j++) {
                var lbl = MakeLabel("x" + ToSubscript(j + 1), 8, WinFontStyle.Regular, MUTED);
                lbl.Location = new Point(startX + j * (CELL_W + GAP) + 25, N * (CELL_H + GAP) + 5);
                matrixPanel.Controls.Add(lbl);
            }
            matrixPanel.Height = (N + 1) * (CELL_H + GAP) + 50;
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
                    MessageBox.Show($"Число {textA} у комірці A[{i+1},{j+1}] занадто велике!\nБудь ласка, введіть числа в діапазоні від -1e15 до 1e15.", 
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
                MessageBox.Show($"Число {textB} у векторі b[{i+1}] занадто велике!\nБудь ласка, введіть числа в діапазоні від -1e15 до 1e15.", 
                                "Помилка діапазону", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            b[i] = valB;
        }
    } catch (FormatException) { 
        MessageBox.Show("Будь ласка, введіть коректні числа (використовуйте цифри та кому)!", "Помилка формату"); 
        return; 
    }

    _lastResults.Clear(); 

    if (cbMethods[0].Checked) _lastResults.Add(Solver.GaussUnitDiagonal(A, b));
    if (cbMethods[1].Checked) _lastResults.Add(Solver.GaussFullPivot(A, b));
    if (cbMethods[2].Checked) _lastResults.Add(Solver.CholeskyMethod(A, b));

    RenderSolutions(_lastResults);
    RenderChart(_lastResults); 
    RenderSystem(A, b);

    tcResults.SelectedIndex = 0; 
}
        private void ExportToTxt()
        {
            if (_lastResults.Count == 0)
            {
                MessageBox.Show("Спочатку натисніть кнопку 'РОЗВ'ЯЗАТИ'!", "Увага");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Text files (*.txt)|*.txt";
                sfd.FileName = $"SLAR_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("=== ЗВІТ РОЗВ'ЯЗАННЯ СЛАР ===");
                    sb.AppendLine($"Дата: {DateTime.Now}");
                    sb.AppendLine($"Розмірність: {N}");
                    sb.AppendLine(new string('-', 30));

                    foreach (var r in _lastResults)
                    {
                        sb.AppendLine($"МЕТОД: {r.MethodName}");
                        if (r.Success && r.X != null)
                        {
                            sb.AppendLine("Вектор розв'язку x:");
                            for (int i = 0; i < N; i++)
                                sb.AppendLine($"  x{i + 1} = {r.X[i]:F8}");
                            
                            sb.AppendLine($"Нев'язка: {r.Residual:E3}");
                            sb.AppendLine($"Час виконання: {r.ExecutionTimeMs:F4} мс");
                            
                            sb.AppendLine("Лог кроків:");
                            foreach (var step in r.Steps) sb.AppendLine("  " + step);
                        }
                        else
                        {
                            sb.AppendLine($"Статус: Помилка - {r.Error}");
                        }
                        sb.AppendLine(new string('-', 30));
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Звіт успішно збережено!", "Успіх");
                }
            }
        }

        private void RenderSolutions(List<SolveResult> results)
        {
            resultsPanel.Controls.Clear();
            var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top, BackColor = SURFACE };
            resultsPanel.Controls.Add(stack);

            int mi = 0;
            foreach (var r in results) {
                WinColor col = METHOD_COLORS[mi % 3];
                var card = new Panel { BackColor = SURFACE2, AutoSize = false, Size = new Size(resultsPanel.Width - 40, 220 + N * 20), Margin = new Padding(0, 0, 0, 15), Padding = new Padding(15) };
                card.Controls.Add(MakeLabel("■ " + r.MethodName, 10, WinFontStyle.Bold, col));

                if (r.Success && r.X != null) {
                    int yOff = 35;
                    for (int i = 0; i < N; i++) {
                        var l = MakeLabel("x" + ToSubscript(i + 1) + $" = {r.X[i]:F8}", 10, WinFontStyle.Regular, TEXTCOLOR);
                        l.Location = new Point(15, yOff); card.Controls.Add(l); yOff += 20;
                    }

                    yOff += 8;
                    var lblRes = MakeLabel($"Нев'язка ‖Ax-b‖ = {r.Residual:E3}", 9, WinFontStyle.Bold, ACCENT);
                    if (r.Residual > 1e-5) lblRes.ForeColor = ACCENT3; 
                    lblRes.Location = new Point(15, yOff);
                    card.Controls.Add(lblRes);
                    yOff += 25;

                    if (r.Steps.Count > 0) {
                        var box = new RichTextBox { Location = new Point(15, yOff), Size = new Size(card.Width - 30, r.Steps.Count * 16 + 10), BackColor = BG, ForeColor = MUTED, BorderStyle = BorderStyle.None, Font = new WinFont("Consolas", 7.5f), ReadOnly = true, Text = string.Join("\n", r.Steps) };
                        card.Controls.Add(box); card.Height = box.Bottom + 10;
                    }
                } else if (!r.Success) {
                    var lErr = MakeLabel("⚠ " + r.Error, 9, WinFontStyle.Italic, ACCENT3);
                    lErr.Location = new Point(15, 40); card.Controls.Add(lErr);
                    card.Height = 80;
                }
                
                card.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(col), 0, 0, 5, card.Height);
                stack.Controls.Add(card); mi++;
            }
        }

        private void RenderChart(List<SolveResult> results)
        {
            if (formsPlot == null) return;
            var plt = formsPlot.Plot; plt.Clear(); StylePlot(plt);
            int mi = 0;
            foreach (var r in results) {
                if (!r.Success || r.X == null) continue;
                WinColor col = METHOD_COLORS[mi % 3];
                var sp = plt.Add.Scatter(Enumerable.Range(0, N).Select(i => (double)i).ToArray(), r.X.Take(N).ToArray());
                sp.LegendText = r.MethodName; sp.Color = new PlotColor(col.R, col.G, col.B);
                sp.LineWidth = 2; sp.MarkerSize = 10; mi++;
            }
            plt.XLabel("Невідомі (x)");
            plt.YLabel("Значення");
            plt.Axes.Bottom.SetTicks(Enumerable.Range(0, N).Select(i => (double)i).ToArray(), Enumerable.Range(1, N).Select(i => $"x{ToSubscript(i)}").ToArray());
            plt.Legend.IsVisible = true; plt.Axes.AutoScale(); formsPlot.Refresh();
        }

        private void StylePlot(Plot plt)
        {
            plt.FigureBackground.Color = new PlotColor(SURFACE.R, SURFACE.G, SURFACE.B);
            plt.DataBackground.Color = new PlotColor(BG.R, BG.G, BG.B);
            plt.Axes.Color(new PlotColor(MUTED.R, MUTED.G, MUTED.B));
            plt.Grid.MajorLineColor = new PlotColor(45, 52, 68);
        }

        private void RenderSystem(double[,] A, double[] b)
        {
            var rtb = tcResults.TabPages[2].Controls.OfType<RichTextBox>().First();
            rtb.Clear(); rtb.BackColor = SURFACE;
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    double v = A[i, j];
                    string sign = (j > 0) ? (v >= 0 ? " + " : " - ") : (v < 0 ? "-" : "");
                    rtb.SelectionColor = MUTED; rtb.AppendText(sign);
                    rtb.SelectionColor = TEXTCOLOR; rtb.AppendText(Math.Abs(v).ToString("G"));
                    rtb.SelectionColor = ACCENT; rtb.AppendText("x" + ToSubscript(j + 1));
                }
                rtb.SelectionColor = MUTED; rtb.AppendText(" = ");
                rtb.SelectionColor = ACCENT4; rtb.AppendText($"{b[i]:G}\n\n");
            }
        }

        private void LoadPreset(int idx) { numSize.Value = 3; N = 3; RebuildMatrix(); }
        private void RandomFill() { var rnd = new Random(); for (int i = 0; i < N; i++) { for (int j = 0; j < N; j++) tbA[i, j].Text = Math.Round(rnd.NextDouble() * 20 - 10, 1).ToString(); tbB[i].Text = Math.Round(rnd.NextDouble() * 20 - 10, 1).ToString(); } }
        private void ClearAll() { for (int i = 0; i < N; i++) { for (int j = 0; j < N; j++) tbA[i, j].Text = "0"; tbB[i].Text = "0"; } }

        private void DrawTab(object? s, DrawItemEventArgs e) {
            var g = e.Graphics; var tp = tcResults.TabPages[e.Index];
            g.FillRectangle(new SolidBrush(e.Index == tcResults.SelectedIndex ? SURFACE : BG), e.Bounds);
            g.DrawString(tp.Text, Font, new SolidBrush(TEXTCOLOR), e.Bounds.X + 10, e.Bounds.Y + 5);
        }

        private WinLabel MakeLabel(string t, float s, WinFontStyle st, WinColor c) => new WinLabel { Text = t, Font = new WinFont("Consolas", s, st), ForeColor = c, AutoSize = true };
        private WinLabel MakeSectionLabel(string t) { var l = MakeLabel(t, 7.5f, WinFontStyle.Regular, ACCENT); l.Margin = new Padding(0, 10, 0, 5); return l; }
        private Button MakeSmallButton(string t) => new Button { Text = t, AutoSize = true, BackColor = SURFACE2, FlatStyle = FlatStyle.Flat, ForeColor = MUTED, Font = new WinFont("Consolas", 8f), Cursor = Cursors.Hand };
    }
}