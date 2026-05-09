using System;

namespace SLAR_WinForms
{
    public class CholeskySolver : BaseSolver, ISlarSolver
    {
        public SolveResult Solve(double[,] A, double[] b)
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

            result.X = x;
            result.Residual = ComputeResidual(A, b, x, n);
            return result;
        }
    }
}