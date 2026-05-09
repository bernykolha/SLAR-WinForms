using System.Collections.Generic;

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
}