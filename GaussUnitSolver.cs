using System;

namespace SLAR_WinForms
{
    public class GaussUnitSolver : BaseSolver, ISlarSolver
    {
        public SolveResult Solve(double[,] A, double[] b)
        {
            int n = b.Length;
            var result = new SolveResult { MethodName = "Гаус (одинична діагональ)" };
            double[,] a = CloneMatrix(A, n);
            double[] r = CloneVector(b, n);

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
                    result.Steps.Add($"Перестановка рядків {i + 1} ↔️ {maxRow + 1}");
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
    }
}