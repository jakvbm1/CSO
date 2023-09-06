using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSO
{
    internal class CSO : IOptimizationAlgorithm
    {
        //constant variables applied by the user

        double MR; //mixture ratio, a constant in range of [0; 1] which decides which type of transformation will be used more oftenly
        double[] max_velocity; //another set of constants representing maximum velocity for each dimension,
                               //i decided to set it as x * (upper_boundaries[d] - lower_boundaries[d]) where x is given by the user in the constructor
        double velocity_const;
        int SMP; //seeking memory pool
        double SRD; //seeking range of the selected dimension
        bool SPC; //self-position considering, decides whether current position is considered among positions to choose from
        double CDC; //counts of dimension to change [0; 1], decides how many dimensions should be changed;

        int n_population;
        int n_dimensions;
        int n_iterations;
        int number_of_calls;
        int current_iteration;
        public delegate double tested_function(params double[] arg);
        tested_function f;
        double[] upper_boundaries;
        double[] lower_boundaries;


        //variables describing cats
        double[] fitness_values;
        double[][] arguments;
        double[][] velocity;
        double[][] saved_cats; //for each iteration, the best cat is saved in the memory i decided to make 2d array first array size of
                               //the first dimension depends on number of iterations, while the second's on the number of dimensions
        double[] saved_fitnesses;

        public double[] XBest => saved_cats[findingEndResult()];

        public double FBest => saved_fitnesses[findingEndResult()];

        public int NumberOfEvaluationFitnessFunction => number_of_calls;

        

        public CSO(int n_population, int n_dimensions, int n_iterations, tested_function f,  double[] upper_boundaries, double[] lower_boundaries, double MR = 0.3, double max_velocity_const = 0.1, int SMP = 5, bool SPC = false, double SRD = 0.2, double CDC = 0.8)
        {
            //initialiizng variables such as number of dimensions/iterations
            this.n_population = n_population;
            this.n_dimensions = n_dimensions;
            this.n_iterations = n_iterations;
            this.number_of_calls = 0;
            this.current_iteration = 0;

            //initializing arrays
            this.max_velocity = new double[n_dimensions];
            this.upper_boundaries = new double[n_dimensions];
            this.lower_boundaries = new double[n_dimensions];
            this.fitness_values = new double[n_population];
            this.velocity_const = max_velocity_const;
            this.saved_fitnesses = new double[n_iterations];
            this.arguments = new double[n_population][];
            this.velocity = new double[n_population][];
            for (int i=0; i<n_population; i++)
            {
                this.arguments[i] = new double[n_dimensions];
                this.velocity[i] = new double[n_dimensions];
            }

            this.saved_cats = new double[n_iterations][];
            for (int i=0;i<n_iterations;i++)
            {
                this.saved_cats[i] = new double[n_dimensions];
            }

            //initializing constants

            for (int i=0; i<n_dimensions;i++)
            {
                this.upper_boundaries[i] = upper_boundaries[i];
                this.lower_boundaries[i] = lower_boundaries[i];

                this.max_velocity[i] = (upper_boundaries[i] - lower_boundaries[i]) * max_velocity_const;
            }


            this.MR = MR;
            this.SMP = SMP;
            this.SRD = SRD;
            this.SPC = SPC;
            this.CDC = CDC;
            this.f = f;

           
        }
        
        double function(double[] x)
        {
            number_of_calls++;
            return f(x);
        }

        void findingBest()
        {
            int the_best = 0;

            for (int i=0; i<n_population-1;  i++)
            {
                if (fitness_values[i] > fitness_values[i+1])
                {
                    the_best = i + 1;
                }
            }

            for(int i=0; i<n_dimensions; i++)
            {
                saved_cats[current_iteration][i] = arguments[the_best][i];
            }

            saved_fitnesses[current_iteration] = fitness_values[the_best];
            current_iteration++;
        }

        void creatingInitialPopulation()
        {
            Random random = new Random();
            for (int i=0; i<n_population;i++)
            {
                for(int j=0; j<n_dimensions;j++)
                {
                    arguments[i][j] = random.NextDouble() * (upper_boundaries[j] - lower_boundaries[j]);
                    velocity[i][j] = random.NextDouble() * max_velocity[j];
                }
            }

            for(int i=0; i<n_population; i++)
            {
                fitness_values[i] = function(arguments[i]);
            }

            findingBest();
        }

        void tracingMode(int number)
        {
            Random random = new Random();
            //updating velocity for selected cat
            //im not sure what c1 and r1 are supposed to mean so at the moment im just using the max velocity const
            for (int i=0; i<n_dimensions;i++) 
            {
                double new_velocity = velocity[number][i] + random.NextDouble()*velocity_const* (saved_cats[current_iteration][i] - arguments[number][i]);

                if (new_velocity > max_velocity[i]) { new_velocity = max_velocity[i]; }
                if (new_velocity < -max_velocity[i]) { new_velocity = -max_velocity[i]; }

                velocity[number][i] = new_velocity;
            }

            double[] next_position = new double[n_dimensions];
            //choosing new position for the transformed cat
            for (int i=0; i<n_dimensions;i++)
            {
                next_position[i] = arguments[number][i] + velocity[number][i];

                if (next_position[i] > upper_boundaries[i])
                {
                    next_position[i] = upper_boundaries[i];
                }

                if (next_position[i] < lower_boundaries[i])
                {
                    next_position[i] = lower_boundaries[i];
                }
            }

            for (int i=0; i<n_dimensions;i++)
            {
                arguments[number][i] = next_position[i];
            }

            fitness_values[number] = function(arguments[number]);


        }

        void seekingMode(int number)
        {
            int number_of_copies;

            if (SPC)
            {
                number_of_copies = SMP - 1;
            }

            else
            {
                number_of_copies = SMP;
            }

            double[][] copies = new double[SMP][];

            for (int i = 0; i < number_of_copies; i++)
            {
                copies[i] = new double[n_dimensions];

                for (int j = 0; j < n_dimensions; j++)
                {
                    copies[i][j] = arguments[number][j];
                }
            }

            if (SPC)
            {
                copies[SMP - 1] = new double[n_dimensions];

                for (int j = 0; j < n_dimensions; j++)
                {
                    copies[SMP - 1][j] = arguments[number][j];
                }
            }

            //creating smp (or smp-1 if spc is set to true) copies of cat that are going to be transformed

            double dimensions_to_change_double = n_dimensions * CDC + 0.1;
            int dimensions_to_change = (int)dimensions_to_change_double;

            //calculating how many of dimensions are to be changed, the 0.1 is added to compensate for inaccuracies while changing from double to int


            for (int copy = 0; copy < number_of_copies; copy++)
            {

                int[] dimensions_to_be_changed = new int[dimensions_to_change]; //selected dimensions
                Random random = new Random();
                if (dimensions_to_change == n_dimensions) //in that case, every dimension is changed
                {
                    for (int i = 0; i < n_dimensions; i++)
                    {
                        dimensions_to_be_changed[i] = i;
                    }
                }

                else //in that case, some dimensions are left as they are, so dimensions that will be transformed should be choosen randomly
                {
                    int wh_condition = 0;

                    while (wh_condition < dimensions_to_change)
                    {
                        int choosen_dimension = random.Next(n_dimensions);
                        if (!dimensions_to_be_changed.Contains(choosen_dimension))
                        {
                            dimensions_to_be_changed[wh_condition] = choosen_dimension;
                            wh_condition++;
                        }
                    }
                }

                for (int i = 0; i < dimensions_to_change; i++)
                {
                    int positive_or_negative = random.Next(2);
                    if (positive_or_negative == 0) { positive_or_negative = -1; }

                    copies[copy][dimensions_to_be_changed[i]] = arguments[number][dimensions_to_be_changed[i]] * (1 + positive_or_negative * random.NextDouble() * SRD);


                    //checking whether the newly chosen arguments are within the scope of searched area
                    if (copies[copy][dimensions_to_be_changed[i]] > upper_boundaries[dimensions_to_be_changed[i]])
                    {
                        copies[copy][dimensions_to_be_changed[i]] = upper_boundaries[dimensions_to_be_changed[i]];
                    }

                    if (copies[copy][dimensions_to_be_changed[i]] < lower_boundaries[dimensions_to_be_changed[i]])
                    {
                        copies[copy][dimensions_to_be_changed[i]] = lower_boundaries[dimensions_to_be_changed[i]];
                    }
                }



            }

            //after each copy of currently transformed cat is altered it is time to calculate their fitness values and choose one of them

            double[] copies_fit = new double[SMP];

            for (int i = 0; i < SMP; i++)
            {
                copies_fit[i] = function(copies[i]);
            }

            int current_best = 0;
            int current_worst = 0;

            for (int i = 0; i < SMP; i++)
            {
                if (copies_fit[current_best] > copies_fit[i])
                {
                    current_best = i;
                }

                if (copies_fit[current_worst] < copies_fit[i])
                {
                    current_worst = i;
                }
            }

           // Console.WriteLine("Najlepszy " + current_best);
           // Console.WriteLine("Najgorszy " + current_worst);

            int[] probabilities = new int[SMP];
            int probabilities_sum = 0;

            //calculated probabilities doesnt add up to any nice integer number (bc why would they right..) and are in range [0;1]
            //the way i decided to choose 'randomly' is pretty cursed but for now i dont have any better idea

            for (int i = 0; i < SMP; i++)
            {
                double probability_as_double = 10000000;
                if (current_best != current_worst)
                {
                    probability_as_double = 10000000 * ((copies_fit[current_worst] - copies_fit[i]) / (copies_fit[current_worst] - copies_fit[current_best]));
                }


                probabilities[i] = (int)probability_as_double;
                probabilities_sum += probabilities[i];
                //Console.WriteLine("suma "+probabilities_sum);
            }


            int[] probabilities_choosing = new int[SMP];
            probabilities_choosing[0] = 0;

            for (int i = 1; i < SMP; i++)
            {
                //in this array every next element is the previous one incremented by one probability value
                probabilities_choosing[i] = probabilities_choosing[i - 1] + probabilities[i - 1];
            }

            //now, we need to generate random int between 0 and probabilites sum, if the number is between a certain robabilities_choosing[x] and robabilities_choosing[x+1],
            //then x is the chosen copy
            Random random2 = new Random();
            int choosing_number = random2.Next(probabilities_sum + 1);
            int chosen_one = 0;

            for(int i=0; i < SMP; i++)
            {
                if (i == SMP-1)
                {
                    chosen_one = i;
                }

                else if (probabilities_choosing[i] < choosing_number && probabilities_choosing[i + 1] > choosing_number)
                {
                    chosen_one = i;
                    break;
                }
            }

            //Console.WriteLine("wybrany " + chosen_one);

            for(int i=0; i<n_dimensions;  i++)
            {
                arguments[number][i] = copies[chosen_one][i];
            }
            fitness_values[number] = copies_fit[chosen_one];

        }

        int findingEndResult() //comparing all of the saved cats to find the best one
        {
            int the_best=0;

            for (int i=0; i<n_iterations; i++)
            {
                if (saved_fitnesses[i] < saved_fitnesses[the_best])
                {
                    the_best = i;
                }
            }

            return the_best;
        }

        public double Solve()
        {

            Random random = new Random();

            LoadFromFileStateOfAlghoritm();

            if (current_iteration == 0)
            {
                creatingInitialPopulation();
                Console.WriteLine("utworzono populacje startowa");
            }

            //Console.WriteLine(current_iteration);

            for(int startingPoint = current_iteration; startingPoint < n_iterations; startingPoint++)
            {
                //Console.WriteLine("ITERACJA " + startingPoint);
                for (int i=0; i < n_population; i++)
                {

                    double decider = random.NextDouble();
                    
                    if (decider > MR)
                    {
                        seekingMode(i);
                    }

                    else
                    {
                        tracingMode(i);
                    }
                }
                findingBest();
                SaveToFileStateOfAlghoritm();
            }
            Console.WriteLine("KONCOWY NAJLEPSZY WYNIK TO: " + saved_fitnesses[findingEndResult()]);
           SaveResult();
            return saved_fitnesses[findingEndResult()];
        }

        public void SaveToFileStateOfAlghoritm()
        {
            //creating a file that will be used to load state from

            StreamWriter sw = File.CreateText("CSO_in_work.txt");
            sw.WriteLine(number_of_calls);
            sw.WriteLine(current_iteration);

            for (int i = 0; i < n_population; i++)
            {
                for (int j = 0; j < n_dimensions; j++)
                {
                    sw.Write(arguments[i][j] + ", ");
                }
                sw.Write(fitness_values[i]);
                sw.Write('\n');
            }

            for (int i = 0; i < n_population; i++)
            {
                for (int j = 0; j < n_dimensions; j++)
                {
                    sw.Write(velocity[i][j] + ", ");
                }
                sw.Write('\n');
            }

            for (int i = 0; i < current_iteration; i++)
            {
                for (int j = 0; j < n_dimensions; j++)
                {
                    sw.Write(saved_cats[i][j] + ", ");
                }
                sw.Write(saved_fitnesses[i] + ", ");
                sw.Write("\n");
            }

            sw.Close();

            //creating file for the user to look into, so they can check how's the algorithm doing

            StreamWriter sw2 = File.CreateText("CSO_ITERACJA_" + current_iteration + ".txt");
            sw2.WriteLine("ILOSC WYMIAROW: " + n_dimensions);
            sw2.WriteLine("ILOSC ITERACJI: " + n_iterations);
            sw2.WriteLine("ROZMIAR POPULACJI: " + n_population);

            sw2.WriteLine("WARTOSCI DLA STALYCH:");

            sw2.WriteLine("MIXTURE RATIO " + MR);
            sw2.WriteLine("SMP: " + SMP);
            sw2.WriteLine("CDC: " + CDC);
            sw2.WriteLine("SRD: " + SRD);
            sw2.WriteLine("SPC: " + SPC);
            sw2.WriteLine("STALA DLA VELOCITY "+velocity_const);

            sw2.WriteLine("ILOSC WYWOLAN FUNKCJI CELU: " + number_of_calls);
            sw2.WriteLine("UTWORZONE KOTY:");
            for (int i = 0; i < n_population; i++)
            {
                for (int j = 0; j < n_dimensions; j++)
                {
                    sw2.Write(arguments[i][j] + ", ");
                }
                sw2.Write(fitness_values[i]);
                sw2.Write('\n');
            }

            sw2.Close();



        }

        public void LoadFromFileStateOfAlghoritm()
        {
            if (File.Exists("CSO_in_work.txt"))
            {
                StreamReader sr = new StreamReader("CSO_in_work.txt");

                string line = "";
                line = sr.ReadLine();
                number_of_calls = Convert.ToInt32(line);

                line = sr.ReadLine();
                current_iteration = Convert.ToInt32(line);

                for (int i=0; i < n_population; i++)
                {
                    line = sr.ReadLine();
                    string[] read_args = line.Split(", ");
                    for(int j=0; j < n_dimensions; j++)
                    {
                        arguments[i][j] = Convert.ToDouble(read_args[j]);
                    }
                    fitness_values[i] = Convert.ToDouble(read_args[n_dimensions]);
                }

                for (int i = 0; i < n_population; i++)
                {
                    line = sr.ReadLine();
                    string[] read_vel = line.Split(", ");
                    for (int j = 0; j < n_dimensions; j++)
                    {
                        velocity[i][j] = Convert.ToDouble(read_vel[j]);
                    }
                }

                for (int i = 0; i< current_iteration; i++)
                {
                    line = sr.ReadLine();
                    string[] read_cat = line.Split(", ");
                    for (int j=0; j < n_dimensions; j++)
                    {
                        saved_cats[i][j] = Convert.ToDouble(read_cat[j]);
                    }
                    saved_fitnesses[i] = Convert.ToDouble(read_cat[n_dimensions]);
                }
                Console.WriteLine("wczytano plik!");
                sr.Close();


            }
        }

        public void SaveResult()
        {
            StreamWriter sw = File.CreateText("CSO_end_result.txt");
            for (int i = 0; i < n_dimensions; i++)
            {
                sw.Write(saved_cats[findingEndResult()][i] + ", ");
            }
            sw.Write(saved_fitnesses[findingEndResult()]);
            sw.Write('\n');
            sw.WriteLine(number_of_calls);
            sw.WriteLine(n_dimensions);
            sw.WriteLine(n_iterations);
            sw.WriteLine(n_population);

            sw.WriteLine("MIXTURE RATIO " + MR);
            sw.WriteLine("SMP: " + SMP);
            sw.WriteLine("CDC: " + CDC);
            sw.WriteLine("SRD: " + SRD);
            sw.WriteLine("SPC: " + SPC);
            sw.WriteLine("STALA DLA VELOCITY " + velocity_const);

            sw.Close();


        }
    }
}
