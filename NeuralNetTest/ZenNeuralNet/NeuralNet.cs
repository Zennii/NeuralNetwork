using ZenNeuralNet;
using System;
using System.IO;

namespace ZenNeuralNet
{
    class NeuralNet : LightList<NeuronList>
    {
        public const float ACC_THRESHOLD = 0.26f;

        public NeuralNet(int layers) : base(layers)
        {}

        public NeuralNet(int layers, int inputs, int layerNeurons, int outputs, Random rand) : base(layers)
        {
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
        

        /// <summary>
        /// Back propogate through our neural network and learn from our mistakes. That'll teach ya.
        /// </summary>
        /// <param name="desired">Our desired output for the previous input.</param>
        public float BackProp(float[] desired)
        {
            float lastAcc = array[array.Length - 1].CompareTo(desired); // Closer to 0 is better
            float newAcc = lastAcc;
            for (int i = array.Length - 2; i >= 0; i--)
            {
                for (int j = array[i].array.Length-1; j >= 0; j--)
                {
                    for (int k = array[i + 1].array.Length -1; k >= 0; k--)
                    {
                        array[i].SlopeWeight(j, k);
                        CleanFire(i); // Fires from i onward
                        newAcc = array[array.Length - 1].CompareTo(desired);
                        if (newAcc >= lastAcc)
                            array[i].UndoSlopeWeight(j, k); // Last tweak failed
                        else
                            lastAcc = newAcc;
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
            for (int i = r.Next(array.Length); i < array.Length - 1; i++)
            {
                for (int j = r.Next(array[i].array.Length), len1 = array[i].array.Length - 1; j < len1; j++)
                {
                    int a = r.Next(6);
                    if (a == 1)
                    {
                        array[i].array[j].slopes[r.Next(array[i].array[j].slopes.Length)] = Neuron.DEFAULT_SLOPE; //((float)r.NextDouble() * 2f) - 1f;
                        array[i].array[j].weights[r.Next(array[i].array[j].weights.Length)] = ((float)r.NextDouble() * 2f) - 1f;
                    }
                }
            }

            return this;
        }
        
        public void CleanFire(int i = 0)
        {
            // Clear next row and fire all rows except output
            for (int x = i, len = array.Length - 1; x < len; x++)
            {
                array[x].CleanFire();
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
        public static float LoadNet(ref NeuralNet bestNet, string path)
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
                    for (int i = layers - 1; i >= 0; i--)
                    {
                        int neurons = reader.ReadInt32();
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
                return best;
            }
            else
            {
                return -1;
            }
        }

        public void SaveNet(float bestScore, string path)
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

            CleanFire();
            if (array[array.Length - 1].CompareTo(desired) >= 0.1f) // Learn only when above threshold
                BackProp(desired);
            float prop = array[array.Length - 1].CompareTo(desired);
            //NeuronList.Mutate(neuralNet, rand); // Do evolution system
            return prop;
        }

        public string Test(float[] input, float[] desired)
        {
            array[0].Fill(input);

            CleanFire();

            float prop = array[array.Length - 1].CompareTo(desired);

            return (prop < ACC_THRESHOLD) + "\t\t" + prop.ToString("f7") + "\t" + array[array.Length - 1].ToString();
        }

        public float[] Test(float[] input)
        {
            array[0].Fill(input);

            CleanFire();

            return array[array.Length - 1].GetValues();
        }
        public NeuralNet Copy()
        {
            NeuronList connections = null;
            NeuralNet ret = new NeuralNet(array.Length);
            for (int i = array.Length - 1; i >= 0; i--)
            {
                ret.array[i] = connections = array[i].Copy(connections);
            }
            return ret;
        }
    }
}
