namespace SLAR_WinForms
{
    public interface ISlarSolver
    {
        SolveResult Solve(double[,] A, double[] b);
    }
}