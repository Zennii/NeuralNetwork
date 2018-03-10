using NeuralNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuralNetTest
{
    class Program
    {
        public const int TASKS = 10;
        public const int NUM_NEURONS = 12;
        public const float ACC_THRESHOLD = 0.26f;
        private NeuronList[] bestNet = null;
        private float bestScore = 0;
        private NeuronList[][] neuralNets;
        private bool Kill;
        private int learnLen = 0;

        static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            string bestline;
            int bestof;
            while (true)
            {
                Console.WriteLine("Best of: ");
                if (!int.TryParse(bestline = Console.ReadLine(), out bestof))
                {
                    string[] spl = bestline.Split(' ');
                    Commands(spl); // TODO: Allow exit here
                }
                else
                {
                    break;
                }
            }
            //Console.WriteLine("Practice times: ");
            //if (!int.TryParse(Console.ReadLine(), out int practice))
            //{
            //   return;
            //}
            
            LearnSession(bestof);
        }

        private void Commands(string[] args)
        {
            string path = "";
            switch (args[0].ToLower())
            {
                case "new":
                    bestNet = null;
                    bestScore = 0;
                    Console.WriteLine("Best network cleared.");
                    break;
                case "test":
                    float[] vals = new float[args.Length - 1];
                    bool failed = false;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (failed = !float.TryParse(args[i + 1], out vals[i]))
                        {
                            break;
                        }
                    }
                    if (failed)
                    {
                        Console.WriteLine("Parsing inputs failed.");
                        break;
                    }
                    float[] result = Test(vals);
                    Console.Write("Result: ");
                    for (int i = 0; i < result.Length; i++)
                        Console.WriteLine(result[i]);
                    break;
                case "info":
                    if (bestNet == null)
                    {
                        Console.WriteLine("Neural network is empty.");
                        break;
                    }
                    Console.Write("\nInputs: {0}\nOutputs: {1}\nLayers: ", bestNet[0].array.Length, bestNet[bestNet.Length - 1].array.Length);
                    for (int i = 1; i < bestNet.Length - 2; i++)
                        Console.Write(bestNet[i].array.Length + " ");
                    Console.WriteLine("\nCurrent best score: {0}\nCurrent %ACC: {1}", bestScore, learnLen != 0 ? (bestScore / (learnLen * 2) * 100) + "%" : "Unknown");
                    break;
                case "save":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please specify a file path. Example: C:\\dir\\MyNeuralNet");
                        break;
                    }
                    if (bestNet == null)
                    {
                        Console.WriteLine("Cannot save an empty network.");
                        break;
                    }
                    args[0] = "";
                    path = string.Join(" ", args).Trim();
                    if (!path.EndsWith(".znn"))
                        path += ".znn";

                    NeuronList.SaveNet(bestNet, bestScore, path);

                    Console.WriteLine("Saved to file {0}\n", path);
                    break;
                case "load":
                    args[0] = ""; // TODO: Load without bestScore
                    bool noBest = false;
                    if (args[1] == "--noscore" || args[1] == "-ns")
                    {
                        noBest = true;
                        args[1] = "";
                    }
                    path = string.Join(" ", args).Trim();
                    if (!path.EndsWith(".znn"))
                        path += ".znn";
                    if ((bestScore = NeuronList.LoadNet(ref bestNet, path)) >= 0)
                    {
                        if (noBest)
                            bestScore = 0;
                        Console.WriteLine(path + " loaded.\n");
                    }
                    else
                    {
                        Console.WriteLine("File does not exist! {0}\n", path);
                    }
                    break;

                case "learn":
                    int bestof;
                    Console.WriteLine("Best of " + args[1]);
                    if (int.TryParse(args[1], out bestof))
                    {
                        LearnSession(bestof);
                    }
                    break;
                case "help":
                    Console.WriteLine("\nhelp\n- Shows this help dialog.\n\n" +
                        "new\n- Clears the current best neural network so a new one can be created.\n\n" +
                        "save [file]\n- Saves the 'best' neural network to [file].\n\n" +
                        "load [file]\n- Loads the 'best' neural network from [file].\n\n" +
                        "test [inputs]\n- Tests the neural network against the inputs and returns its outputs. Missing inputs are 0. Excess inputs are truncated.\n\n" +
                        "learn [x]\n- Sends the current neural network through [x] training sessions, or creates one.\n\n" +
                        "info\n- Gives information about the current neural network.\n\n" +
                        "exit\n- Quits the program.\n");
                    break;
                case "exit":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private void CreateNeuralNet(int a, Random rand)
        {
            neuralNets[a][neuralNets[a].Length - 1] = new NeuronList(null, 1); // Outputs
            neuralNets[a][neuralNets[a].Length - 1].Fill(rand);

            for (int i = neuralNets[a].Length - 2; i >= 1; i--)
            {
                neuralNets[a][i] = new NeuronList(neuralNets[a][i + 1], NUM_NEURONS);
                neuralNets[a][i].FillRandom(rand); // Fill all our layers with random values
            }
            neuralNets[a][0] = new NeuronList(neuralNets[a][1], 6); // Inputs
            neuralNets[a][0].FillRandom(rand);
        }

        private float Learn(NeuronList[] neuralNet, float[] input, float[] desired)
        {
            neuralNet[0].Fill(input);

            NeuronList.CleanFire(neuralNet);
            if(neuralNet[neuralNet.Length - 1].CompareTo(desired) >= 0.1f) // Learn only when above threshold
                NeuronList.BackProp(neuralNet, desired);
            float prop = neuralNet[neuralNet.Length - 1].CompareTo(desired);
            //NeuronList.Mutate(neuralNet, rand); // Do evolution system
            return prop;
        }

        private string Test(float[] input, float[] desired)
        {
            bestNet[0].Fill(input);

            NeuronList.CleanFire(bestNet);

            float prop = bestNet[bestNet.Length - 1].CompareTo(desired);

            return (prop < ACC_THRESHOLD) + "\t\t" + prop.ToString("f7") + "\t"+ bestNet[bestNet.Length - 1].ToString();
        }

        private float[] Test(float[] input)
        {
            bestNet[0].Fill(input);

            NeuronList.CleanFire(bestNet);

            return bestNet[bestNet.Length - 1].GetValues();
        }

        private void LearnSession(int bestof)
        {
            Kill = false;
            int practice = 33; // Doesn't matter what it is, usually doesn't go above 20 anyway.
            Console.WriteLine();

            neuralNets = new NeuronList[TASKS][];
            float[] curScore = new float[TASKS];
            Console.WriteLine("Ctrl+D to stop learning.\n\nTIME\t\tNUM\tBEST/{0}\t\tCAP\t%ACC", TASKS);


            Task Listen = Task.Factory.StartNew(() =>
            {
                ConsoleKeyInfo key;
                while (!Kill)
                {
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.D && key.Modifiers == ConsoleModifiers.Control)
                        {
                            Kill = true;
                        }
                    }
                }
            });
            for (int h = 0; h < bestof; h++)
            {
                int curBest = -1;

                Task[] taskArray = new Task[TASKS];
                for (int ts = 0; ts < taskArray.Length; ts++)
                {
                    int t = ts;
                    taskArray[ts] = Task.Factory.StartNew(() =>
                    {
                        Random rand = new Random(DateTime.Now.Millisecond + ((int)Task.CurrentId * 478));

                        float[] res = new float[0];
                        float lcurScore = 0, llcurScore = 0, lllcurScore = 0;
                        if (bestNet != null)
                            neuralNets[t] = NeuronList.Mutate(bestNet, rand);
                        else
                        {
                            neuralNets[t] = new NeuronList[11];
                            CreateNeuralNet(t, rand);
                        }
                        for (int i = 0; i < practice; i++)
                        {

                            res = new float[] {
                         Learn(neuralNets[t], new float[] { 0, 1, 2, 3, 4, 5 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 3, 4, 5, 6, 7, 8 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 0, 2, 4, 6, 8, 9 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 0, 1, 2, 3, 5, 5 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 4, 1, 2, 3, 4, 2 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 1, 9, 8, 6, 5, 7 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 7, 8, 9, 2, 3, 4 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 0, 4, 4, 0, 0, 2 }, new float[] { 0 })

                        ,Learn(neuralNets[t], new float[] { 2, 2, 4, 6, 9, 9 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 1, 2, 3, 4, 5, 6 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 2, 3, 3, 6, 9, 9 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 6, 5, 4, 2, 7, 8 }, new float[] { 1 })

                        ,Learn(neuralNets[t], new float[] { 5, 5, 5, 4, 3, 3 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 9, 0, 8, 1, 6, 4 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 5, 5, 5, 6, 6, 6 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 6, 5, 7, 8, 9, 0 }, new float[] { 1 }) // TODO: Remove this mess
                        ,Learn(neuralNets[t], new float[] { 3, 2, 3, 4, 3, 6 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 0, 1, 9, 2, 8, 3 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 6, 3, 3, 4, 2, 6 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 4, 1, 8, 6, 7, 9 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 3, 2, 3, 2, 2, 1 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 7, 7, 8, 6, 7, 9 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 1, 8, 5, 4, 9, 3 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 2, 3, 8, 6, 4, 9 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 7, 7, 7, 7, 7, 7 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 2, 2, 2, 2, 2, 2 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 5, 5, 4, 4, 5, 5 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 9, 9, 9, 4, 6, 6 }, new float[] { 0 })
                        ,Learn(neuralNets[t], new float[] { 6, 5, 7, 8, 4, 3 }, new float[] { 1 })
                        ,Learn(neuralNets[t], new float[] { 1, 3, 5, 7, 0, 2 }, new float[] { 1 })
                            };

                            if (Kill) break;

                            learnLen = res.Length;
                            curScore[t] = 0;
                            for (int j = 0; j < res.Length; j++)
                            {
                                float accu = res[j];
                                if (accu < ACC_THRESHOLD - 0.1f)
                                {
                                    curScore[t] += 1f;
                                }
                                curScore[t] += 1f - accu;
                            }

                            if (curScore[t] == lcurScore && curScore[t] == llcurScore && curScore[t] == lllcurScore || i == practice - 1)
                            {    // Probably a better way to do this. Checks if we're not getting anywhere
                                    break;
                            }
                            else if (curScore[t] < bestScore/1.3f)//(res.Length / 7.0f))
                                                                 // "res.length/?" lowered will allow more offshoots to potentially grow. Slower but maybe better results? Probably not.
                            {
                                break;
                            }

                            lllcurScore = lcurScore;
                            llcurScore = lcurScore;
                            lcurScore = curScore[t];
                        }

                    });
                }

                int currentLeft = Console.CursorLeft;
                int currentTop = Console.CursorTop;
                Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\t" + h.ToString("d4") + "\t" + "WORKING" + "\t\t" + learnLen * 2 + "\t" + "WORKING" + "\t");
                Console.SetCursorPosition(currentLeft, currentTop);

                Task.WaitAll(taskArray);
                if (Kill) break;

                for (int i = 0; i < curScore.Length; i++)
                {
                    if (curScore[i] > bestScore)
                    {
                        bestScore = curScore[i];
                        curBest = i;
                    }
                }
                if (curBest >= 0)
                {
                    bestNet = NeuronList.Copy(neuralNets[curBest]);
                    Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\t" + h.ToString("d4") + "\t" + bestScore.ToString("f5") + "\t" + learnLen * 2 + "\t" + ((bestScore / (learnLen * 2)) * 100).ToString("f2") + "\t");
                }
                if (bestScore >= (learnLen * 2.0f) - 0.1f)
                    break;
            }

            Kill = true;
            
            Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\tDone Test\t\t\t\t\t\t\t\n");

            if (bestNet != null)
            {
                Console.WriteLine("-\t-\t- FINAL -\t-\t-\n\nSUCCESS\t\tACC [MIN " + ACC_THRESHOLD.ToString("f2") + "]");
                Console.WriteLine(Test(new float[] { 4, 3, 2, 3, 4, 5 }, new float[] { 0 }));
                Console.WriteLine(Test(new float[] { 4, 4, 2, 2, 3, 3 }, new float[] { 0 }));
                Console.WriteLine(Test(new float[] { 9, 9, 9, 9, 9, 9 }, new float[] { 0 }));
                Console.WriteLine(Test(new float[] { 7, 8, 2, 3, 4, 5 }, new float[] { 1 }));
                Console.WriteLine(Test(new float[] { 0, 8, 2, 3, 4, 1 }, new float[] { 1 }));
                Console.WriteLine(Test(new float[] { 5, 3, 2, 0, 9, 1 }, new float[] { 1 }));

                Console.WriteLine(Test(new float[] { 1, 2, 9, 8, 4, 5 }, new float[] { 1 }));
                Console.WriteLine(Test(new float[] { 1, 0, 2, 3, 4, 6 }, new float[] { 1 }));
                Console.WriteLine(Test(new float[] { 8, 7, 7, 8, 8, 7 }, new float[] { 0 }));
            }

            Console.WriteLine("\nDone! Type 'help' for commands.\n");
            string cmd;
            while ((cmd = Console.ReadLine().ToLower()) != "")
            {
                string[] args = cmd.Split(' ');
                Commands(args);
            }
        }

    }
}
