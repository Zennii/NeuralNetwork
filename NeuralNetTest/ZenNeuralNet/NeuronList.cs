using System;
using System.IO;

namespace ZenNeuralNet
{
    class NeuronList : LightList<Neuron>
    {
        public NeuronList connections; // The forward layer of neurons to fire to
        public static readonly float SLOPE_MOD = -0.9f; // negative flips the slope, 0.9 makes our slope lossy and come to a conclusion. 10% loss every change.
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
            for (int i = array.Length-1; i >= 0; i--)
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
            for (int i = array.Length-1; i >= 0; i--)
            {
                array[i] = new Neuron(conns);
                if(conns > 0)
                array[i].RandomInit(rand, scale);
            }
        }

        public void Fill(float[] vals)
        {

            for (int i = array.Length-1; i >= 0; i--)
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
            for (int i = array.Length-1; i >= 0; i--)
            {
                float val = array[i].Value;
                if (tanh) val = (float)(Neuron.SIGMOID_HEIGHT / (1.0 + Math.Exp(-val)) - Neuron.SIGMOID_OFFSET);//Neuron.Activator(val);
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
            for (int i = array.Length-1; i >= 0; i--)
            {
                smarts += Math.Abs((float)(Neuron.SIGMOID_HEIGHT / (1.0 + Math.Exp(-array[i].Value)) - Neuron.SIGMOID_OFFSET)/*Neuron.Activator(array[i].Value)*/ - desired[i]);
            }
            return smarts;
        }

        public void ClearValues()
        {
            for (int i = array.Length-1; i >= 0; i--)
            {
                array[i].Value = 0;
            }
        }

        public void FireAll()
        {
            for (int i = array.Length-1; i >= 0; i--)
            {
                array[i].Fire(connections);
            }
        }

        public void CleanFire()
        {
            connections.ClearValues();
            FireAll();
        }

 

        public NeuronList Copy(NeuronList connection)
        {
            return new NeuronList(connection, Neuron.Copy(array), array.Length);
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
            for (int i = array.Length-1; i >= 0; i--)
                array[i] = val;
        }


    }
}