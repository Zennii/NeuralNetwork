using System;

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

        public void FillValues(float[] vals)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Value = vals[i];
            }

        }

        public float[] GetValues()
        {
            float[] ret = new float[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].Value;
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
            float smarts = 0.0f;
            for (int i = 0; i < array.Length; i++)
            {
                smarts += Math.Abs(Neuron.fastTanh(array[i].Value) - desired[i]);
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
                _o += Neuron.fastTanh(array[i].Value);
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