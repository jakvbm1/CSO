
namespace CSO
{
    class Program
    {
        public static double Rosenbrock_function(params double[] x) //minimum (1, 1, ..., 1) 0 df: caly przedzial R
        {
            int dimension = x.Length;
            double sum = 0;
            if (dimension != 1)
            {
                for (int i = 0; i < dimension - 1; i++)
                {
                    sum += 100 * Math.Pow((x[i + 1] - (x[i] * x[i])), 2) + Math.Pow((1 - x[i]), 2);
                }
            }
            return sum;
        }

        private static void Main(string[] args)
        {
            double[] upper_limit = new double[5];
            upper_limit[0] = 5;
            upper_limit[1] = 5;
            upper_limit[2] = 5;
            upper_limit[3] = 5;
            upper_limit[4] = 5;

            double[] lower_limit = new double[5];
            lower_limit[0] = -5;
            lower_limit[1] = -5;
            lower_limit[2] = -5;
            lower_limit[3] = -5;
            lower_limit[4] = -5;

            CSO test = new CSO(100,5,200, Rosenbrock_function, upper_limit, lower_limit);
            test.Solve();
        }
    }
}