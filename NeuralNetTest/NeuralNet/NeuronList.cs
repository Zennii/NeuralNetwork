using System;
using System.IO;

namespace NeuralNet
{
    class NeuronList : LightList<Neuron>
    {
        public NeuronList connections; // The forward layer of neurons to fire to
        private static readonly float SLOPE_MOD = -0.9f; // negative flips the slope, 0.9 makes our slope lossy and come to a conclusion. 10% loss every change.
        //private static readonly float ACC_THRESHOLD = 1f;

        public NeuronList(NeuronList nextLayer, int size = 1) : base(size)
        {
            connections = nextLayer;
        }
        
        public NeuronList(NeuronList nextLayer, Neuron[] array, int size = 1) : base(size)
        {
            connections = nextLayer;
            this.array = array;
        }

        public void FillRandom(Random rand, float scale = 1)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Neuron(connections.array.Length);
                array[i].RandomInit(rand, scale);
            }

        }

        public void Fill(Random rand, float scale = 1)
        {
            int conns = 0;
            if(connections != null)
            {
                conns = connections.array.Length;
            }
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Neuron(conns);
                if(conns > 0)
                array[i].RandomInit(rand, scale);
            }
        }

        public void Fill(float[] vals)
        {

            for (int i = 0; i < array.Length; i++)
            {
                float val = 0;
                if (i < vals.Length)
                    val = vals[i];
                array[i].Value = val;
            }
        }

        public void Fill(float[][] neuronData)
        {
            for(int i = 0; i < neuronData.Length; i++)
            {
                int len = neuronData[i].Length/2;
                float[] weights = new float[len];
                float[] slopes = new float[len];
                for(int w = 0; w < neuronData[i].Length; w+=2)
                {
                    weights[w / 2] = neuronData[i][w];
                    slopes[w / 2] = neuronData[i][w+1];
                }
                array[i] = new Neuron(weights, slopes);
            }
        }

        public float[] GetValues(bool tanh = true)
        {
            float[] ret = new float[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                float val = array[i].Value;
                if (tanh) val = Neuron.Activator(val);
                ret[i] = val;
            }
            return ret;
        }
        public float[][] GetNeuronData() // Each array is a neuron: weight, slope, weight, slope, etc
        {
            float[][] ret = new float[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                if(array[i].weights != null)
                {
                    ret[i] = new float[array[i].weights.Length * 2];
                    for (int w = 0; w < array[i].weights.Length * 2; w += 2)
                    {
                        ret[i][w] = array[i].weights[w/2];
                        ret[i][w + 1] = array[i].slopes[w/2];
                    }
                }
                else
                {
                    ret[i] = new float[0];
                }
            }
            return ret;
        }

        public void SlopeWeight(int neurInd, int weightInd)
        {
            array[neurInd].weights[weightInd] += array[neurInd].slopes[weightInd];
        }

        public void UndoSlopeWeight(int neurInd, int weightInd)
        {
            // This assumes our weight change was not for the better
            array[neurInd].weights[weightInd] -= array[neurInd].slopes[weightInd]; // Undo
            array[neurInd].slopes[weightInd] *= SLOPE_MOD; // Flip and reduce
        }

        // Closer to zero is better.
        public float CompareTo(float[] desired)
        {
            float smarts = 0;
            for (int i = 0; i < array.Length; i++)
            {
                smarts += Math.Abs(Neuron.Activator(array[i].Value) - desired[i]);
            }
            return smarts;
        }

        public void ClearValues()
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Value = 0;
            }
        }

        public void FireAll()
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Fire(connections);
            }
        }

        public void CleanFire()
        {
            connections.ClearValues();
            FireAll();
        }

        /// <summary>
        /// Back propogate through our neural network and learn from our mistakes. That'll teach ya.
        /// </summary>
        /// <param name="desired">Our desired output for the previous input.</param>
        public static float BackProp(NeuronList[] neuralNet, float[] desired)
        {
            float lastAcc = neuralNet[neuralNet.Length - 1].CompareTo(desired); // Closer to 0 is better
            float newAcc = lastAcc;
            for (int i = neuralNet.Length - 2; i >= 0; i--)
            {
                for (int j = 0; j < neuralNet[i].array.Length; j++)
                {
                    for (int k = 0; k < neuralNet[i+1].array.Length; k++)
                    {
                        neuralNet[i].SlopeWeight(j, k);
                        CleanFire(neuralNet, i); // Fires from i onward
                        newAcc = neuralNet[neuralNet.Length - 1].CompareTo(desired);
                        if (newAcc >= lastAcc)
                            neuralNet[i].UndoSlopeWeight(j, k); // Last tweak failed
                        else
                            lastAcc = newAcc;
                    } // k
                } // j
            } // i
            return lastAcc;
        }

        public static NeuronList[] Mutate(NeuronList[] neuralNet, Random r)
        {
            NeuronList[] cpy = Copy(neuralNet);
            for (int i = r.Next(cpy.Length); i < cpy.Length - 1; i++)
            {
                for(int j = r.Next(cpy[i].array.Length); j < cpy[i].array.Length - 1; j++)
                {
                    int a = r.Next(6);
                    if (a == 1)
                    {
                        cpy[i].array[j].slopes[r.Next(cpy[i].array[j].slopes.Length)] = Neuron.DEFAULT_SLOPE; //((float)r.NextDouble() * 2f) - 1f;
                        cpy[i].array[j].weights[r.Next(cpy[i].array[j].weights.Length)] = ((float)r.NextDouble()*2f)-1f;
                    }
                }
            }
            return cpy;
        }

        public static void CleanFire(NeuronList[] neuralNet, int i = 0)
        {
            // Clear next row and fire all rows except output
            for (int x = i; x < neuralNet.Length - 1; x++)
            {
                neuralNet[x].CleanFire();
            }
        }

        public static void Fire(NeuronList[] neuralNet, int i = 0)
        {
            // Clear next row and fire all rows except output
            for (int x = i; x < neuralNet.Length - 1; x++)
            {
                neuralNet[x].FireAll();
            }
        }

        public static float LoadNet(ref NeuronList[] bestNet, string path)
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
                    bestNet = new NeuronList[layers];
                    for (int i = layers - 1; i >= 0; i--)
                    {
                        int neurons = reader.ReadInt32();
                        if (i == layers - 1) // Output layer
                        {
                            bestNet[i] = new NeuronList(null, neurons);
                        }
                        else
                        {
                            bestNet[i] = new NeuronList(bestNet[i + 1], neurons);
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
                            bestNet[i].array[n] = new Neuron(weights, slopes);
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

        public static void SaveNet(NeuronList[] bestNet, float bestScore, string path)
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
                writer.Write(bestNet.Length); // Amount of layers [1]
                for (int i = bestNet.Length - 1; i >= 0; i--)
                {// For each layer

                    float[][] layer = bestNet[i].GetNeuronData();
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

        public NeuronList Copy(NeuronList connection)
        {
            return new NeuronList(connection, Neuron.Copy(array), array.Length);
        }

        public static NeuronList[] Copy(NeuronList[] src)
        {
            NeuronList connections = null;
            NeuronList[] ret = new NeuronList[src.Length];
            for(int i = src.Length - 1; i >= 0; i--)
            {
                ret[i] = connections = src[i].Copy(connections);
            }
            return ret;
        }
        public override string ToString()
        {
            string _o = "";
            for (int i = 0; i < array.Length; i++)
                _o += Neuron.Activator(array[i].Value) + ",";
            return _o;
        }

    }

    class LightList<T>
    {
        public int count;
        public T[] array;

        public LightList(int size)
        {
            count = 0;
            array = new T[size];
        }

        public void Add(T n)
        {
            if (count >= array.Length)
            {
                Array.Resize(ref array, array.Length * 2);
            }
            array[count++] = n;
        }

        /// <summary>
        /// Deletes an element with an index.
        /// </summary>
        /// <param name="ind">The index to delete</param>
        /// <returns>If it deleted anything</returns>
        public bool Del(int ind)
        {
            bool did = false;
            if (ind < count)
            {
                array[ind] = default(T);
                did = true;
                count--;
            }
            else
            {
                return false; // Can't remove, not enough elements
            }

            for (int i = ind + 1; i < array.Length; i++)
            {
                array[i - 1] = array[i]; // Move all future items backward
            }

            if (did && count <= array.Length / 2)
            {
                Array.Resize(ref array, array.Length / 2);
            }

            return did;
        }

        public void Fill(T val)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = val;
        }


    }
}