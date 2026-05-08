namespace SLAR_WinForms
{
    public class SolveResult
    {
        public string MethodName { get; set; } = "";
        public double[]? X { get; set; }
        public double Residual { get; set; }
        public string? Error { get; set; }
        public double ExecutionTimeMs { get; set; }
        public List<string> Steps { get; set; } = new();
        public bool Success => X != null && Error == null;
    }

    public static class Solver
    {
        public static SolveResult GaussUnitDiagonal(double[,] A, double[] b)
        {
            int n = b.Length;
            var result = new SolveResult { MethodName = "Гаус (одинична діагональ)" };
            double[,] a = Clone(A, n);
            double[] r = Clone(b, n);

            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                double maxVal = Math.Abs(a[i, i]);
                for (int k = i + 1; k < n; k++)
                    if (Math.Abs(a[k, i]) > maxVal) { maxVal = Math.Abs(a[k, i]); maxRow = k; }

                if (maxVal < 1e-12) { result.Error = "Система вироджена (pivot ≈ 0)"; return result; }

                if (maxRow != i)
                {
                    SwapRows(a, r, i, maxRow, n);
                    result.Steps.Add($"Перестановка рядків {i + 1} ↔ {maxRow + 1}");
                }

                double pivot = a[i, i];
                result.Steps.Add($"Нормування рядка {i + 1}: ÷ {pivot:F6}");
                for (int j = i; j < n; j++) a[i, j] /= pivot;
                r[i] /= pivot;

                for (int k = i + 1; k < n; k++)
                {
                    double factor = a[k, i];
                    if (Math.Abs(factor) < 1e-14) continue;
                    result.Steps.Add($"  Рядок {k + 1} -= {factor:F4} · рядок {i + 1}");
                    for (int j = i; j < n; j++) a[k, j] -= factor * a[i, j];
                    r[k] -= factor * r[i];
                }
            }

            result.X = BackSubstitution(a, r, n);
            if (result.X == null) result.Error = "Зворотній хід: вироджена";
            else result.Residual = ComputeResidual(A, b, result.X, n);
            return result;
        }

        public static SolveResult GaussFullPivot(double[,] A, double[] b)
        {
            int n = b.Length;
            var result = new SolveResult { MethodName = "Метод обертання (повний вибір)" };
            double[,] a = Clone(A, n);
            double[] r = Clone(b, n);
            int[] colOrder = new int[n];
            for (int i = 0; i < n; i++) colOrder[i] = i;

            for (int i = 0; i < n; i++)
            {
                int maxRow = i, maxCol = i;
                double maxVal = Math.Abs(a[i, i]);
                for (int row = i; row < n; row++)
                    for (int col = i; col < n; col++)
                        if (Math.Abs(a[row, col]) > maxVal) { maxVal = Math.Abs(a[row, col]); maxRow = row; maxCol = col; }
                
                

                if (double.IsInfinity(maxVal) || double.IsNaN(maxVal) || maxVal < 1e-20) 
                {
                    result.Error = "Матриця вироджена або числа занадто великі";
                    return result; 
                }

                if (maxRow != i) { SwapRows(a, r, i, maxRow, n); result.Steps.Add($"Перестановка рядків {i + 1} ↔ {maxRow + 1}"); }
                if (maxCol != i) { SwapCols(a, colOrder, i, maxCol, n); result.Steps.Add($"Перестановка стовпців {i + 1} ↔ {maxCol + 1}"); }

                double pivot = a[i, i];
                for (int k = i + 1; k < n; k++)
                {
                    double factor = a[k, i] / pivot;
                    if (Math.Abs(factor) < 1e-14) continue;
                    result.Steps.Add($"  Рядок {k + 1} -= {factor:F4} · рядок {i + 1}");
                    for (int j = i; j < n; j++) a[k, j] -= factor * a[i, j];
                    r[k] -= factor * r[i];
                }
            }

            double[]? xPerm = BackSubstitution(a, r, n);
            if (xPerm == null) { result.Error = "Зворотній хід: вироджена"; return result; }

            double[] x = new double[n];
            for (int i = 0; i < n; i++) x[colOrder[i]] = xPerm[i];
            result.X = x;
            result.Residual = ComputeResidual(A, b, x, n);
            return result;
        }

        public static SolveResult CholeskyMethod(double[,] A, double[] b)
        {
            int n = b.Length;
            var result = new SolveResult { MethodName = "Гаус-Холецький (квадратний корінь)" };

            double[,] AtA = new double[n, n];
            double[] Atb = new double[n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < n; k++) AtA[i, j] += A[k, i] * A[k, j];
            for (int i = 0; i < n; i++)
                for (int k = 0; k < n; k++) Atb[i] += A[k, i] * b[k];

            result.Steps.Add("Обчислено AᵀA та Aᵀb (нормальні рівняння)");

            double[,] L = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = AtA[i, j];
                    for (int k = 0; k < j; k++) sum -= L[i, k] * L[j, k];
                    if (i == j)
                    {
                        if (sum <= 1e-14) { result.Error = "Матриця AᵀA не є позитивно визначеною"; return result; }
                        L[i, j] = Math.Sqrt(sum);
                        result.Steps.Add($"L[{i + 1},{i + 1}] = √{sum:F6} = {L[i, j]:F6}");
                    }
                    else L[i, j] = sum / L[j, j];
                }
            }

            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = Atb[i];
                for (int k = 0; k < i; k++) sum -= L[i, k] * y[k];
                y[i] = sum / L[i, i];
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = y[i];
                for (int k = i + 1; k < n; k++) sum -= L[k, i] * x[k];
                x[i] = sum / L[i, i];
            }

            result.Steps.Add("Прямий та зворотній хід завершено");
            result.X = x;
            result.Residual = ComputeResidual(A, b, x, n);
            return result;
        }

        private static double[]? BackSubstitution(double[,] a, double[] b, int n)
        {
            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = b[i];
                for (int j = i + 1; j < n; j++) sum -= a[i, j] * x[j];
                if (Math.Abs(a[i, i]) < 1e-12) return null;
                x[i] = sum / a[i, i];
            }
            return x;
        }

        public static double ComputeResidual(double[,] A, double[] b, double[] x, int n)
        {
            double res = 0;
            for (int i = 0; i < n; i++)
            {
                double ax = 0;
                for (int j = 0; j < n; j++) ax += A[i, j] * x[j];
                res += (ax - b[i]) * (ax - b[i]);
            }
            return Math.Sqrt(res);
        }

        private static void SwapRows(double[,] a, double[] b, int r1, int r2, int n)
        {
            for (int j = 0; j < n; j++) { (a[r1, j], a[r2, j]) = (a[r2, j], a[r1, j]); }
            (b[r1], b[r2]) = (b[r2], b[r1]);
        }

        private static void SwapCols(double[,] a, int[] colOrder, int c1, int c2, int n)
        {
            for (int i = 0; i < n; i++) { (a[i, c1], a[i, c2]) = (a[i, c2], a[i, c1]); }
            (colOrder[c1], colOrder[c2]) = (colOrder[c2], colOrder[c1]);
        }

        private static double[,] Clone(double[,] a, int n)
        {
            var c = new double[n, n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) c[i, j] = a[i, j];
            return c;
        }

        private static double[] Clone(double[] v, int n)
        {
            var c = new double[n]; Array.Copy(v, c, n); return c;
        }
    }
}
