using System;

namespace SLAR_WinForms
{
    public abstract class BaseSolver
    {
        protected double ComputeResidual(double[,] A, double[] b, double[] x, int n)
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

        protected void SwapRows(double[,] a, double[] b, int r1, int r2, int n)
        {
            for (int j = 0; j < n; j++) { (a[r1, j], a[r2, j]) = (a[r2, j], a[r1, j]); }
            (b[r1], b[r2]) = (b[r2], b[r1]);
        }

        protected double[,] CloneMatrix(double[,] a, int n)
        {
            var c = new double[n, n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) c[i, j] = a[i, j];
            return c;
        }

        protected double[] CloneVector(double[] v, int n)
        {
            var c = new double[n]; Array.Copy(v, c, n); return c;
        }

        protected double[]? BackSubstitution(double[,] a, double[] b, int n)
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
    }
}