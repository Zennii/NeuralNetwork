using System;

namespace NeuralNet
{
    class NeuronList
    {
        public int count;
        public Neuron[] array;

        public NeuronList(int size = 1)
        {
            count = 0;
            array = new Neuron[size];
        }

        public void add(Neuron n)
        {
            if(count >= array.Length)
            {
                Array.Resize(ref array, array.Length * 2);
            }
            array[count] = n;
        }
    }
    internal class WeightList
    {
        public int count;
        public float[] array;

        public WeightList(int size = 1)
        {
            count = 0;
            array = new float[size];
        }

        public void add(float n)
        {
            if (count >= array.Length)
            {
                Array.Resize(ref array, array.Length * 2);
            }
            array[count] = n;
        }
    }
}