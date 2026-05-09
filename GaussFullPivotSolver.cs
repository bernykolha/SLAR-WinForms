using System;

namespace SLAR_WinForms
{
    public class GaussFullPivotSolver : BaseSolver, ISlarSolver
    {
        public SolveResult Solve(double[,] A, double[] b)
        {
            int n = b.Length;
            var result = new SolveResult { MethodName = "Метод обертання (повний вибір)" };
            double[,] a = CloneMatrix(A, n);
            double[] r = CloneVector(b, n);
            int[] colOrder = new int[n];
            for (int i = 0; i < n; i++) colOrder[i] = i;

            for (int i = 0; i < n; i++)
            {
                int maxRow = i, maxCol = i;
                double maxVal = Math.Abs(a[i, i]);
                for (int row = i; row < n; row++)
                    for (int col = i; col < n; col++)
                        if (Math.Abs(a[row, col]) > maxVal) { maxVal = Math.Abs(a[row, col]); maxRow = row; maxCol = col; }

                if (maxVal < 1e-20) { result.Error = "Матриця вироджена"; return result; }

                if (maxRow != i) { SwapRows(a, r, i, maxRow, n); result.Steps.Add($"Перестановка рядків {i + 1} ↔️ {maxRow + 1}"); }
                if (maxCol != i) { SwapCols(a, colOrder, i, maxCol, n); result.Steps.Add($"Перестановка стовпців {i + 1} ↔️ {maxCol + 1}"); }

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

        private void SwapCols(double[,] a, int[] colOrder, int c1, int c2, int n)
        {
            for (int i = 0; i < n; i++) { (a[i, c1], a[i, c2]) = (a[i, c2], a[i, c1]); }
            (colOrder[c1], colOrder[c2]) = (colOrder[c2], colOrder[c1]);
        }
    }
}