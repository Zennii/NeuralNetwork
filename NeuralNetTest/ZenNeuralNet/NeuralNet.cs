using ZenNeuralNet;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ZenNeuralNet
{
    class NeuralNet : LightList<NeuronList>
    {
        public static readonly int TASKS = 12; // How many threads to use.
        private static Task[] taskArray = new Task[TASKS];
        public static readonly float ACC_THRESHOLD = 0.26f;
        public float bestScore = float.MinValue;
        //public int Layers = 0;
        //public int Neurons = 0;
        //private Random rand;
        public int learnLen = 0;

        public NeuralNet(int layers) : base(layers)
        {
            //Layers = layers;
        }

        public NeuralNet(int layers, int inputs, int layerNeurons, int outputs, Random rand) : base(layers)
        {
            //Layers = layers;
            //Neurons = layerNeurons;
            array[array.Length - 1] = new NeuronList(null, outputs); // Outputs
            array[array.Length - 1].Fill(rand);

            for (int i = array.Length - 2; i > 0; i--)
            {
                array[i] = new NeuronList(array[i + 1], layerNeurons);
                array[i].FillRandom(rand); // Fill all our layers with random values
            }
            array[0] = new NeuronList(array[1], inputs); // Inputs
            array[0].FillRandom(rand);
        }

        public NeuralNet(int layers, int inputs, int[] layerNeurons, int outputs, Random rand) : base(layers)
        {
            array[array.Length - 1] = new NeuronList(null, outputs); // Outputs
            array[array.Length - 1].Fill(rand);

            for (int i = array.Length - 2; i > 0; i--)
            {
                array[i] = new NeuronList(array[i + 1], layerNeurons[i]);
                array[i].FillRandom(rand); // Fill all our layers with random values
            }
            array[0] = new NeuronList(array[1], inputs); // Inputs
            array[0].FillRandom(rand);
        }
        

        public void RerollNeurons(Random rand)
        {
            //rand.Next(Math.Max(3, bestNet.array.Length - 10), bestNet.array.Length + 10), 6, rand.Next(Math.Max(1, bestNet.Neurons - 10), bestNet.Neurons + 10), 

            for (int i = array.Length - 2; i > 0; i--)
            {
                array[i] = new NeuronList(array[i + 1], rand.Next(Math.Max(1, array[i].array.Length - 10), array[i].array.Length + 10));
                array[i].FillRandom(rand); // Fill all our layers with random values
            }
            // TODO: Add/remove layers
        }

        /// <summary>
        /// Back propogate through our neural network and learn from our mistakes. That'll teach ya.
        /// </summary>
        /// <param name="desired">Our desired output for the previous input.</param>
        public float BackProp(float[] desired)
        {
            float lastAcc = array[array.Length - 1].CompareTo(desired); // Closer to 0 is better
            float initAcc = lastAcc;
            float newAcc = lastAcc;
            for (int i = array.Length - 2, startk = 0; i >= 0; i--) // for each layer
            {
                startk = array[i].connections.array.Length - 1;
                for (int j = array[i].array.Length-1; j >= 0; j--) // for each neuron
                {
                    for (int k = startk; k >= 0; k--) // for each connected neuron
                    {
                        if (array[i].array[j].slopes[k] > 0.0006f || array[i].array[j].slopes[k] < -0.0006f) // if we can modify the neuron
                        {
                            array[i].array[j].weights[k] += array[i].array[j].slopes[k];//array[i].SlopeWeight(j, k);


                            for (int x = i, len = array.Length - 1; x < len; x++) // for each layer starting with i
                            {
                                for (int y = array[x].connections.array.Length - 1; y >= 0; y--) // clear each connection value
                                {
                                    array[x].connections.array[y].Value = 0;//.ClearValues();
                                }


                                for (int y = array[x].array.Length - 1; y >= 0; y--) // Fire the neurons to each connection
                                {
                                    array[x].array[y].Fire(array[x].connections);
                                }
                                //array[x].FireAll();
                                //array[x].CleanFire();
                            }
                            //CleanFire(i); // Fires from i onward
                            newAcc = array[array.Length - 1].CompareTo(desired);
                            if (newAcc > lastAcc)
                            {
                                array[i].array[j].weights[k] -= array[i].array[j].slopes[k]; // Undo
                                array[i].array[j].slopes[k] *= NeuronList.SLOPE_MOD; // Flip and reduce
                            }
                            //array[i].UndoSlopeWeight(j, k); // Last tweak failed
                            else
                            {
                                lastAcc = newAcc;
                            }
                        }
                        else if(k == startk) // Can't modify and we're at startk, push it down
                        {
                            startk--;
                        }
                    } // k
                } // j
            } // i
            return lastAcc;
        }

        public NeuralNet MutateCopy(Random r)
        {
            return Copy().Mutate(r);
        }

        public NeuralNet Mutate(Random r)
        {
            int muts = r.Next(11); // Per-network?
            for (int i = r.Next(array.Length), len0 = array.Length - 1; i < len0; i++)
            {
                for (int j = r.Next(array[i].array.Length), len1 = array[i].array.Length - 1; j < len1; j++)
                {
                    int a = r.Next(muts);
                    if (a < 5) // 0 1 2, since random doesn't seem to like repeating values often, this should help generate more "chain mutations"
                    {
                        array[i].array[j].slopes[r.Next(array[i].array[j].slopes.Length)] = Neuron.DEFAULT_SLOPE; //((float)r.NextDouble() * 2f) - 1f;
                        array[i].array[j].weights[r.Next(array[i].array[j].weights.Length)] = ((float)r.NextDouble() * 2f) - 1f;
                    }
                }
            }

            return this;
        }

        public string GetID()
        {
            return array[1].array.Length + "x" + (array.Length-2);
        }
        
        public void CleanFire(int i = 0)
        {
            // Clear next row and fire all rows except output
            for (int x = i, len = array.Length - 1; x < len; x++)
            {

                //array[x].CleanFire();
            }
        }

        public void Fire(int i = 0)
        {
            // Fire all rows except output
            for (int x = i, len = array.Length - 1; x < len; x++)
            {
                array[x].FireAll();
            }
        }

        // Might not have to load statically, but it's easy for now.
        public static bool LoadNet(ref NeuralNet bestNet, string path)
        {
            if (File.Exists(path))
            {
                /* 0 - float - Best score
                 * 1 - int - Amount of layers in net
                 * 2 - int - Amount of neurons in layer
                 * 3 - int - amount of floats in set for neuron
                 * 4/5 - float - Weight/slope data
                 * f i i i fffffff i ffffffff etc
                 * 0 1 2 3 45454545 3 45454545 3 454545 2 3 45454545 etc
                 */
                float best = 0;
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    best = reader.ReadSingle();
                    int layers = reader.ReadInt32();
                    bestNet = new NeuralNet(layers);
                    bestNet.bestScore = best;
                    for (int i = layers - 1; i >= 0; i--)
                    {
                        int neurons = reader.ReadInt32();
                        //bestNet.Neurons = neurons; // Temp
                        if (i == layers - 1) // Output layer
                        {
                            bestNet.array[i] = new NeuronList(null, neurons);
                        }
                        else
                        {
                            bestNet.array[i] = new NeuronList(bestNet.array[i + 1], neurons);
                        }

                        float[] weights;
                        float[] slopes;
                        for (int n = 0; n < neurons; n++)
                        {
                            int size = reader.ReadInt32();
                            weights = new float[size / 2];
                            slopes = new float[size / 2];
                            for (int d = 0; d < size; d += 2)
                            {
                                weights[d / 2] = reader.ReadSingle();
                                slopes[d / 2] = reader.ReadSingle();
                            }
                            bestNet.array[i].array[n] = new Neuron(weights, slopes);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void SaveNet(string path)
        {
            /* 0 - float - Best score
             * 1 - int - Amount of layers in net
             * 2 - int - Amount of neurons in layer
             * 3 - int - amount of floats in set for neuron
             * 4/5 - float - Weight/slope data
             * f i i i fffffff i ffffffff etc
             * 0 1 2 3 45454545 3 45454545 3 454545 2 3 45454545 etc
             */

            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write(bestScore); // Best score to beat in training [0]
                writer.Write(array.Length); // Amount of layers [1]
                for (int i = array.Length - 1; i >= 0; i--)
                {// For each layer

                    float[][] layer = array[i].GetNeuronData();
                    writer.Write(layer.Length); // Amount of neurons in layer [2]

                    for (int n = 0; n < layer.Length; n++)
                    {// For each neuron

                        writer.Write(layer[n].Length); // Amount of weights/slopes in neuron [3]

                        for (int w = 0; w < layer[n].Length; w++)
                        {
                            writer.Write(layer[n][w]); // Write the weight and slope data [4/5]
                        }
                    }
                }
            }
        }

        public float Learn(float[] input, float[] desired)
        {
            array[0].Fill(input);

            for (int x = 0, len = array.Length - 1; x < len; x++)
            {
                for (int y = array[x].connections.array.Length - 1; y >= 0; y--)
                {
                    array[x].connections.array[y].Value = 0;//.ClearValues();
                }


                for (int y = array[x].array.Length - 1; y >= 0; y--)
                {
                    array[x].array[y].Fire(array[x].connections);
                }
                //array[x].FireAll();
                //array[x].CleanFire();
            }
            //CleanFire();
            if (array[array.Length - 1].CompareTo(desired) >= 0.1f) // Learn only when above threshold
                BackProp(desired);
            float prop = array[array.Length - 1].CompareTo(desired);
            //NeuronList.Mutate(neuralNet, rand); // Do evolution system
            return prop;
        }

        public string Test(float[] input, float[] desired)
        {
            array[0].Fill(input);


            for (int x = 0, len = array.Length - 1; x < len; x++)
            {
                for (int y = array[x].connections.array.Length - 1; y >= 0; y--)
                {
                    array[x].connections.array[y].Value = 0;//.ClearValues();
                }
                
                for (int y = array[x].array.Length - 1; y >= 0; y--)
                {
                    array[x].array[y].Fire(array[x].connections);
                }
            }
            //CleanFire();

            float prop = array[array.Length - 1].CompareTo(desired);

            return (prop < ACC_THRESHOLD) + "\t\t" + prop.ToString("f7") + "\t" + array[array.Length - 1].ToString();
        }

        public float[] Test(float[] input)
        {
            array[0].Fill(input);


            for (int x = 0, len = array.Length - 1; x < len; x++)
            {
                for (int y = array[x].connections.array.Length - 1; y >= 0; y--)
                {
                    array[x].connections.array[y].Value = 0;//.ClearValues();
                }


                for (int y = array[x].array.Length - 1; y >= 0; y--)
                {
                    array[x].array[y].Fire(array[x].connections);
                }
                //array[x].FireAll();
                //array[x].CleanFire();
            }
            //CleanFire();

            return array[array.Length - 1].GetValues();
        }
        public NeuralNet Copy()
        {
            NeuronList connections = null;
            NeuralNet ret = new NeuralNet(array.Length);
            //ret.bestScore = bestScore;
            ret.count = count;
            //ret.Neurons = Neurons;
            for (int i = array.Length - 1; i >= 0; i--)
            {
                ret.array[i] = connections = array[i].Copy(connections);
            }
            return ret;
        }
        public void CopyFrom(NeuralNet net)
        {
            array = (NeuronList[])net.array.Clone();
            count = net.count;
        }






        private static readonly object _lock = new object();
        public void LearnSession(float[][] lessonplan, int bestof)
        {
            bool Kill = false;
            //int practice = 100; // Doesn't matter what it is, usually doesn't go above 20 anyway.
            Console.WriteLine();

            NeuralNet[] neuralNets = new NeuralNet[TASKS];
            float[] curScore = new float[TASKS];
            Console.WriteLine("[Ctrl+D] to stop learning.\n\nTIME\t\tNUM\tBEST/{0}\t\tCAP\t%ACC", TASKS);


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
                int currentLeft = Console.CursorLeft;
                int currentTop = Console.CursorTop;
                //int tryNew = new Random().Next(24);
                NeuralNet randNet = null;
                if (h > 0 && h % 10 == 0)
                {
                    Random rand = new Random(DateTime.Now.Millisecond);
                    randNet = new NeuralNet(rand.Next(Math.Max(3, array.Length - 2), array.Length + 3), 6, rand.Next(Math.Max(1, array[1].array.Length - 2), array[1].array.Length + 3), 1, rand);
                }

                for (int ts = 0; ts < taskArray.Length; ts++)
                {
                    int t = ts;
                    taskArray[ts] = Task.Factory.StartNew(() =>
                    {
                        Random rand = new Random(DateTime.Now.Millisecond + ((int)Task.CurrentId * 478));

                        float lcurScore = 0, llcurScore = -1, lllcurScore = -2, llllcurScore = -3;
                        if (randNet != null)
                        {
                            neuralNets[t] = randNet.MutateCopy(rand);//new NeuralNet(rand.Next(Math.Max(3, bestNet.array.Length - 2), bestNet.array.Length + 3), 6, rand.Next(Math.Max(1, bestNet.array[1].array.Length - 2), bestNet.array[1].array.Length + 3), 1, rand);
                        }
                        else
                        {
                            neuralNets[t] = MutateCopy(rand);
                        }
                        lock (_lock)
                        {
                            Console.SetCursorPosition(currentLeft + 94 + TASKS, currentTop);
                            Console.Write(neuralNets[t].GetID().PadRight(16));
                            Console.SetCursorPosition(currentLeft, currentTop);
                        }
                        for (int i = 0; i < 33; i++)
                        {
                            if (t == 0)
                            {
                                lock (_lock)
                                {
                                    Console.SetCursorPosition(currentLeft + 60, currentTop);
                                    Console.Write("                              " + i.ToString().PadRight(2));
                                    Console.SetCursorPosition(currentLeft, currentTop);
                                }
                            }
                            float[] res = new float[lessonplan.Length / 2];
                            for (int j = 0; j < lessonplan.Length; j += 2)
                            {
                                res[j / 2] = neuralNets[t].Learn(lessonplan[j], lessonplan[j + 1]);
                                if (t == 0)
                                {
                                    lock (_lock)
                                    {
                                        int conloc = (int)((float)j / lessonplan.Length * 30);
                                        Console.SetCursorPosition(currentLeft + 60 + conloc, currentTop);
                                        Console.Write(conloc % 2 == 0 ? '-' : '=');
                                        Console.SetCursorPosition(currentLeft, currentTop);
                                    }
                                }
                            }

                            if (Kill)
                            {
                                curScore = new float[TASKS];
                                break;
                            }

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

                            if ((curScore[t] == llcurScore && curScore[t] == lllcurScore && curScore[t] == llllcurScore && curScore[t] == lcurScore) || i == 32)
                            {    // Probably a better way to do this. Checks if we're not getting anywhere
                                break;
                            }
                            if (curScore[t] < bestScore / 2.035)//(res.Length / 7.0f))
                                                                // "res.length/?" lowered will allow more offshoots to potentially grow. Slower but maybe better results? Probably not.
                            {
                                break;
                            }

                            llllcurScore = llcurScore;
                            lllcurScore = lcurScore;
                            llcurScore = lcurScore;
                            lcurScore = curScore[t];
                        }
                        lock (_lock)
                        {
                            Console.SetCursorPosition(currentLeft + 93 + t, currentTop);
                            Console.Write(curScore[t] > bestScore ? 'o' : '.');
                            Console.SetCursorPosition(currentLeft, currentTop);
                        }

                    });
                }
                lock (_lock)
                {
                    Console.SetCursorPosition(currentLeft, currentTop);
                    Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\t" + h.ToString("d4") + "\t" + "WORKING" + "\t\t" + learnLen * 2 + "\t" + "WORKING".PadRight(78));
                    Console.SetCursorPosition(currentLeft, currentTop);
                }

                Task.WaitAll(taskArray);

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
                    CopyFrom(neuralNets[curBest]);
                    Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\t" + h.ToString("d4") + "\t" + bestScore.ToString("f5").PadRight(8) + "\t" + learnLen * 2 + "\t" + ((bestScore / (learnLen * 2)) * 100).ToString("f2") + "\t");
                }
                if (bestScore >= (learnLen * 2.0f) - 0.1f || Kill)
                    break;
            }

            Kill = true;

            Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]\tDone Test\t\t\t\t\t\t\t\n");
        }

        public void LearnSessionNoOutput(float[][] lessonplan, int bestof)
        {
            bool Kill = false;
            //int practice = 100; // Doesn't matter what it is, usually doesn't go above 20 anyway.

            NeuralNet[] neuralNets = new NeuralNet[TASKS];
            float[] curScore = new float[TASKS];
            Console.WriteLine("[Ctrl+D] to stop learning.");


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
                int currentLeft = Console.CursorLeft;
                int currentTop = Console.CursorTop;
                //int tryNew = new Random().Next(24);
                NeuralNet randNet = null;
                if (h > 0 && h % 10 == 0)
                {
                    Random rand = new Random(DateTime.Now.Millisecond);
                    randNet = new NeuralNet(rand.Next(Math.Max(3, array.Length - 2), array.Length + 3), 6, rand.Next(Math.Max(1, array[1].array.Length - 2), array[1].array.Length + 3), 1, rand);
                }

                for (int ts = 0; ts < taskArray.Length; ts++)
                {
                    int t = ts;
                    taskArray[ts] = Task.Factory.StartNew(() =>
                    {
                        Random rand = new Random(DateTime.Now.Millisecond + ((int)Task.CurrentId * 478));

                        float lcurScore = 0, llcurScore = -1, lllcurScore = -2, llllcurScore = -3;
                        if (randNet != null)
                        {
                            neuralNets[t] = randNet.MutateCopy(rand);//new NeuralNet(rand.Next(Math.Max(3, bestNet.array.Length - 2), bestNet.array.Length + 3), 6, rand.Next(Math.Max(1, bestNet.array[1].array.Length - 2), bestNet.array[1].array.Length + 3), 1, rand);
                        }
                        else
                        {
                            neuralNets[t] = MutateCopy(rand);
                        }
                        for (int i = 0; i < 33; i++)
                        {
                            float[] res = new float[lessonplan.Length / 2];
                            for (int j = 0; j < lessonplan.Length; j += 2)
                            {
                                res[j / 2] = neuralNets[t].Learn(lessonplan[j], lessonplan[j + 1]);
                            }

                            if (Kill)
                            {
                                curScore = new float[TASKS];
                                break;
                            }

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

                            if ((curScore[t] == llcurScore && curScore[t] == lllcurScore && curScore[t] == llllcurScore && curScore[t] == lcurScore) || i == 32)
                            {    // Probably a better way to do this. Checks if we're not getting anywhere
                                break;
                            }
                            if (curScore[t] < bestScore / 2.035)//(res.Length / 7.0f))
                                                                // "res.length/?" lowered will allow more offshoots to potentially grow. Slower but maybe better results? Probably not.
                            {
                                break;
                            }

                            llllcurScore = llcurScore;
                            lllcurScore = lcurScore;
                            llcurScore = lcurScore;
                            lcurScore = curScore[t];
                        }
                    });
                }

                Task.WaitAll(taskArray);

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
                    CopyFrom(neuralNets[curBest]);
                }
                if (bestScore >= (learnLen * 2.0f) - 0.1f || Kill)
                    break;
            }

            Kill = true;

            Console.WriteLine("Done Test");
        }
    }
}
