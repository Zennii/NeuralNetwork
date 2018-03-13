using System;

namespace ZenNeuralNet
{
    class Neuron
    {

        public float[] weights; // Maybe delete connections with a weight close to 0.000

        //private float value;
        public float Value;//{ get { return value; } set { this.value = value; } }

       // private float bias;

        public float[] slopes; // * -0.9 every time the end result is wrong after a change

        public static readonly float DEFAULT_SLOPE = 1f; // This probably has an optimized value that's perfect for quickly finding a good end value.

        public Neuron(int numWeights = 1)
        {
            if (numWeights > 0)
            {
                weights = new float[numWeights];
                slopes = new float[numWeights];
            }
        }

        public Neuron(float[] weights, float[] slopes)
        {
            if (weights != null)
            {
                this.weights = (float[])weights.Clone();
                this.slopes = (float[])slopes.Clone();
            }
        }

        public void RandomInit(Random rand, float scale)
        {
            // bias = (float)rand.NextDouble();
            for (int i = weights.Length-1; i >= 0; i--)
            {
                weights[i] = ((float)(rand.NextDouble() * 2) - 1) * scale; // -1 to 1
                slopes[i] = DEFAULT_SLOPE; //((float)(rand.NextDouble() * 2) - 1) * scale;//START_SLOPE;
            }
        }

        public void Fire(NeuronList connections)
        {
            //Default neruon: (float)Math.Tanh(value + bias);                                                      // Casting might be slow? But I don't feel like implementing tanh myself. Maybe later
            float Activ = Activator(Value);//Value < 0 ? 0 : Value;//fastTanh(Value);//

            for (int i = connections.array.Length-1; i >= 0; i--)
            {
                connections.array[i].Value += Activ * weights[i];
            }

            //value = 0; // We're done here. BUT we can't erase the values until we back prop
        }

        public void ClearValue()
        {
            Value = 0;
        }

        public static float Activator(float x)
        {
            return x * (27 + x * x) / (27 + 9 * x * x);
        }

        // Maybe unneeded?
        public static float fastTanh(float x)
        {
            //return (x * x * x) + (2 * x * x * x * x * x);
            //return 2.0f / (1.0f + (float)Math.Exp(-2.0 * x)) - 1.0f;
            if (x < -3)
                return -1;
            else if (x > 3)
                return 1;
            else
            return x * (27 + x * x) / (27 + 9 * x * x);
        }

        public Neuron Copy()
        {
            return new Neuron(weights, slopes);
        }

        public static Neuron[] Copy(Neuron[] src)
        {
            Neuron[] ret = new Neuron[src.Length];
            for(int i = src.Length-1; i >= 0; i--)
            {
                ret[i] = src[i].Copy();
            }
            return ret;
        }

    }
}
